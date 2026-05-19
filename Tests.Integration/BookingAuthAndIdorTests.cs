using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using App.DTO.v1;
using Base.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class BookingAuthAndIdorTests : IClassFixture<BeautyBookFactory>
{
    private readonly BeautyBookFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BookingAuthAndIdorTests(BeautyBookFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid BookingId, Guid ClientUserId, Guid OtherUserId, Guid AvailabilityId, Guid ServiceId, Guid ProviderUserId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var clientUserId = Guid.NewGuid();
        var providerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var clientEmail = $"client_{clientUserId:N}@test.com";
        var clientUser = new AppUser
        {
            Id = clientUserId,
            UserName = clientEmail,
            NormalizedUserName = clientEmail.ToUpperInvariant(),
            Email = clientEmail,
            NormalizedEmail = clientEmail.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(clientUser);

        var provider = new BeautyProvider
        {
            UserId = providerUserId,
            ProfileName = $"Provider_{providerUserId:N}",
            Address = "Test Street",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(provider);

        var category = new ServiceCategory
        {
            Name = new LangStr { ["en"] = "Test Category" },
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = category.Id,
            Name = new LangStr { ["en"] = "Test Service" },
            Price = 40,
            DurationMinutes = 60,
            IsActive = true,
            From = DateTime.UtcNow
        };
        db.Services.Add(service);

        var availability = new Availability
        {
            BeautyProviderId = provider.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            IsBooked = false,
            From = DateTime.UtcNow
        };
        db.Availabilities.Add(availability);

        var booking = new Booking
        {
            UserId = clientUserId,
            ServiceId = service.Id,
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
        return (booking.Id, clientUserId, otherUserId, availability.Id, service.Id, providerUserId);
    }

    [Fact]
    public async Task GetMyBookings_Unauthenticated_Returns401()
    {
        await SeedAsync();
        var response = await _client.GetAsync("/api/v1.0/bookings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyBookings_Authenticated_ReturnsOnlyOwnBookings()
    {
        var (bookingId, clientUserId, _, _, _, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(clientUserId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/bookings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<List<BookingResponse>>(body, JsonOptions)!;

        Assert.Contains(bookings, b => b.Id == bookingId);
    }

    [Fact]
    public async Task GetBookingById_OtherUser_Returns403()
    {
        var (bookingId, _, otherUserId, _, _, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(otherUserId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1.0/bookings/{bookingId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBookingById_Owner_Returns200()
    {
        var (bookingId, clientUserId, _, _, _, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(clientUserId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1.0/bookings/{bookingId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BookingResponse>(body, JsonOptions)!;
        Assert.Equal(bookingId, result.Id);
    }

    [Fact]
    public async Task CancelBooking_OtherUser_Returns403()
    {
        var (bookingId, _, otherUserId, _, _, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(otherUserId);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1.0/bookings/{bookingId}/cancel");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookingStatus_RequiresServiceProviderRole_Returns403WithClientToken()
    {
        var (bookingId, clientUserId, _, _, _, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(clientUserId);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1.0/bookings/{bookingId}/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { status = "Confirmed" }),
            Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookingStatus_WithServiceProviderRole_Returns200()
    {
        var (bookingId, _, _, _, _, providerUserId) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(providerUserId, "ServiceProvider");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1.0/bookings/{bookingId}/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { status = "Confirmed", reason = "Approved" }),
            Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BookingResponse>(body, JsonOptions)!;
        Assert.Equal("Confirmed", result.Status);
    }
}
