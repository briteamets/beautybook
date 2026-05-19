using System.Data.Common;
using System.Security.Claims;
using App.BLL.Services;
using App.Contracts;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using App.Domain;
using Base.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApp.ApiControllers;

namespace Tests.Unit;

public class BookingCancellationTests : IDisposable
{
    private readonly string _dbName = "unit_" + Guid.NewGuid().ToString("N");
    private readonly SqliteConnection _masterConnection;

    public BookingCancellationTests()
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

    // BookingsController constructor (post-BLL refactor): BookingsController(IBookingService, ISlotGeneratorService)
    private static BookingsController CreateController(AppDbContext db, Guid userId)
    {
        var controller = new BookingsController(
            new BookingService(new AppUnitOfWork(db)),
            new StubSlotGeneratorService());
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

    // Seeds the minimal chain (Provider → ServiceCategory → Service) required so
    // BookingQuery()'s Include(b => b.Service) JOIN succeeds and returns the booking.
    private static async Task<Guid> SeedServiceAsync(AppDbContext db)
    {
        var provider = new BeautyProvider
        {
            UserId = Guid.NewGuid(),
            ProfileName = "Cancel Test Provider",
            Address = "Test Address",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(provider);

        var category = new ServiceCategory
        {
            Name = new LangStr { ["en"] = "Cancel Test Category" },
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = category.Id,
            Name = new LangStr { ["en"] = "Cancel Test Service" },
            Price = 30,
            DurationMinutes = 45,
            IsActive = true,
            From = DateTime.UtcNow
        };
        db.Services.Add(service);

        await db.SaveChangesAsync();
        return service.Id;
    }

    [Fact]
    public async Task CancelBooking_WithinOneHour_Returns400()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var serviceId = await SeedServiceAsync(db);

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = serviceId,
            Status = Booking.StatusPending,
            BookedAt = DateTime.UtcNow,
            AppointmentDateTime = DateTime.UtcNow.AddMinutes(30),
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            From = DateTime.UtcNow
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var result = await controller.CancelBooking(booking.Id, null);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_AlreadyCancelled_Returns400()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var serviceId = await SeedServiceAsync(db);

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = serviceId,
            Status = Booking.StatusCancelled,
            BookedAt = DateTime.UtcNow,
            AppointmentDateTime = DateTime.UtcNow.AddDays(2),
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            From = DateTime.UtcNow
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var result = await controller.CancelBooking(booking.Id, null);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_ValidBooking_Returns200AndFreesSlot()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var serviceId = await SeedServiceAsync(db);

        var availability = new Availability
        {
            BeautyProviderId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            IsBooked = true,
            From = DateTime.UtcNow
        };
        db.Availabilities.Add(availability);

        var booking = new Booking
        {
            UserId = userId,
            ServiceId = serviceId,
            AvailabilityId = availability.Id,
            Status = Booking.StatusPending,
            BookedAt = DateTime.UtcNow,
            AppointmentDateTime = DateTime.UtcNow.AddDays(2),
            BookingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            From = DateTime.UtcNow
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId);
        var result = await controller.CancelBooking(booking.Id, null);

        Assert.IsType<OkObjectResult>(result.Result);

        var freshDb = CreateDb();
        var persisted = await freshDb.Availabilities.FindAsync(availability.Id);
        Assert.NotNull(persisted);
        Assert.False(persisted!.IsBooked);
    }

    private sealed class StubSlotGeneratorService : ISlotGeneratorService
    {
        public Task<IReadOnlyCollection<TimeSlot>> GenerateSlotsAsync(
            Guid providerServiceId, DateOnly date, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IReadOnlyCollection<TimeSlot> GenerateSlots(SlotGenerationInput input) =>
            throw new NotImplementedException();
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
