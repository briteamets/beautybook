using System.Data.Common;
using System.Security.Claims;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using App.Domain;
using App.DTO.v1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApp.ApiControllers;

namespace Tests.Unit;

public class ReviewCreationTests : IDisposable
{
    private readonly string _dbName = "unit_" + Guid.NewGuid().ToString("N");
    private readonly SqliteConnection _masterConnection;

    public ReviewCreationTests()
    {
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
        var controller = new ReviewsController(new ReviewService(new AppUnitOfWork(db)));
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

    private static async Task<Guid> SeedCompletedBookingAsync(AppDbContext db, Guid userId)
    {
        var booking = new Booking
        {
            UserId = userId,
            ServiceId = Guid.NewGuid(),
            Status = Booking.StatusCompleted,
            BookedAt = DateTime.UtcNow,
            AppointmentDateTime = DateTime.UtcNow.AddDays(-1),
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            From = DateTime.UtcNow
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    [Fact]
    public async Task PostReview_InvalidRating_Returns400()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var bookingId = await SeedCompletedBookingAsync(db, userId);
        var controller = CreateController(db, userId);

        var result = await controller.PostReview(new ReviewCreateRequest
        {
            BookingId = bookingId,
            Rating = 6,
            Comment = "Too high rating"
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task PostReview_BookingNotCompleted_Returns400()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = Guid.NewGuid(),
            Status = Booking.StatusPending,
            BookedAt = DateTime.UtcNow,
            AppointmentDateTime = DateTime.UtcNow.AddDays(1),
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            From = DateTime.UtcNow
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var result = await controller.PostReview(new ReviewCreateRequest
        {
            BookingId = booking.Id,
            Rating = 4,
            Comment = "Good"
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task PostReview_AlreadyHasReview_Returns400()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var bookingId = await SeedCompletedBookingAsync(db, userId);

        var existingReview = new Review
        {
            BookingId = bookingId,
            UserId = userId,
            Rating = 5,
            Comment = "Already reviewed",
            From = DateTime.UtcNow
        };
        db.Reviews.Add(existingReview);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var result = await controller.PostReview(new ReviewCreateRequest
        {
            BookingId = bookingId,
            Rating = 5,
            Comment = "Trying again"
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
    }

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
