using System.Data.Common;
using System.Security.Claims;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using App.DTO.v1;
using Base.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApp.ApiControllers;

namespace Tests.Unit;

public class ReviewCommentDeleteTests : IDisposable
{
    private readonly string _dbName = "unit_" + Guid.NewGuid().ToString("N");
    private readonly SqliteConnection _masterConnection;

    public ReviewCommentDeleteTests()
    {
        // Keep one connection open so the named in-memory database persists for the test lifetime.
        _masterConnection = new SqliteConnection(ConnectionString);
        _masterConnection.Open();
        using var ctx = CreateDb();
        ctx.Database.EnsureCreated();
    }

    public void Dispose() => _masterConnection.Dispose();

    private string ConnectionString => $"DataSource={_dbName};Mode=Memory;Cache=Shared";

    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString)
            .AddInterceptors(new DisableFkInterceptor())
            .Options;
        return new AppDbContext(options);
    }

    private static ReviewsController CreateController(AppDbContext db, Guid userId)
    {
        var controller = new ReviewsController(db);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return controller;
    }

    private static async Task<(Guid ReviewId, Guid ProviderUserId, Guid OtherUserId)> SeedAsync(AppDbContext db)
    {
        var providerUserId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        // Must exist in AspNetUsers so Include(r => r.User) INNER JOIN succeeds.
        var clientUser = new AppUser
        {
            Id = clientUserId,
            UserName = "client@test.com",
            NormalizedUserName = "CLIENT@TEST.COM",
            Email = "client@test.com",
            NormalizedEmail = "CLIENT@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(clientUser);

        var provider = new BeautyProvider
        {
            UserId = providerUserId,
            ProfileName = "Test Provider",
            Address = "Test Address",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(provider);

        var category = new ServiceCategory
        {
            Name = new LangStr { ["en"] = "Nails" },
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = category.Id,
            Name = new LangStr { ["en"] = "Manicure" },
            Price = 30,
            DurationMinutes = 45,
            IsActive = true,
            From = DateTime.UtcNow
        };
        db.Services.Add(service);

        var booking = new Booking
        {
            UserId = clientUserId,
            ServiceId = service.Id,
            Status = Booking.StatusCompleted,
            BookedAt = DateTime.UtcNow,
            AppointmentDateTime = DateTime.UtcNow.AddDays(-1),
            From = DateTime.UtcNow
        };
        db.Bookings.Add(booking);

        var review = new Review
        {
            BookingId = booking.Id,
            UserId = clientUserId,
            Rating = 4,
            Comment = "Loved the result!",
            From = DateTime.UtcNow
        };
        db.Reviews.Add(review);

        await db.SaveChangesAsync();
        return (review.Id, providerUserId, otherUserId);
    }

    [Fact]
    public async Task DeleteComment_ByOwnProvider_ClearsCommentAndPreservesRating()
    {
        var db = CreateDb();
        var (reviewId, providerUserId, _) = await SeedAsync(db);
        var controller = CreateController(db, providerUserId);

        var result = await controller.DeleteComment(reviewId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ReviewResponse>(ok.Value);
        Assert.Null(response.Comment);
        Assert.Equal(4, response.Rating);

        var persisted = await db.Reviews.FindAsync(reviewId);
        Assert.NotNull(persisted);
        Assert.Null(persisted!.Comment);
        Assert.Equal(4, persisted.Rating);
    }

    [Fact]
    public async Task DeleteComment_ByUnrelatedProvider_ReturnsForbidden()
    {
        var db = CreateDb();
        var (reviewId, _, otherUserId) = await SeedAsync(db);
        var controller = CreateController(db, otherUserId);

        var result = await controller.DeleteComment(reviewId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var persisted = await db.Reviews.FindAsync(reviewId);
        Assert.Equal("Loved the result!", persisted!.Comment);
    }

    [Fact]
    public async Task DeleteComment_NonExistentReview_ReturnsNotFound()
    {
        var db = CreateDb();
        await SeedAsync(db);
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.DeleteComment(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteComment_AlreadyNullComment_StillSucceeds()
    {
        var db = CreateDb();
        var (reviewId, providerUserId, _) = await SeedAsync(db);

        var review = await db.Reviews.AsTracking().FirstAsync(r => r.Id == reviewId);
        review.Comment = null;
        await db.SaveChangesAsync();

        var controller = CreateController(db, providerUserId);
        var result = await controller.DeleteComment(reviewId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ReviewResponse>(ok.Value);
        Assert.Null(response.Comment);
        Assert.Equal(4, response.Rating);
    }

    // Disables SQLite FK enforcement per connection so seeds can use synthetic UserId GUIDs.
    private sealed class DisableFkInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
            => RunPragma(connection);

        public override Task ConnectionOpenedAsync(
            DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            RunPragma(connection);
            return Task.CompletedTask;
        }

        private static void RunPragma(DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();
        }
    }
}
