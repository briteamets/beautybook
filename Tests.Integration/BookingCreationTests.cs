using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using App.DTO.v1;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class BookingCreationTests : IClassFixture<BeautyBookFactory>
{
    private readonly BeautyBookFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BookingCreationTests(BeautyBookFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid ClientUserId, Guid ServiceId, Guid AvailabilityId, Guid ProviderId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var clientUserId = Guid.NewGuid();
        var providerUserId = Guid.NewGuid();

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
            Address = "Test Address",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(provider);

        var category = new ServiceCategory
        {
            Name = new LangStr { ["en"] = "Category" },
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = category.Id,
            Name = new LangStr { ["en"] = "Booking Test Service" },
            Price = 50,
            DurationMinutes = 60,
            IsActive = true,
            From = DateTime.UtcNow
        };
        db.Services.Add(service);

        var availability = new Availability
        {
            BeautyProviderId = provider.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            IsBooked = false,
            From = DateTime.UtcNow
        };
        db.Availabilities.Add(availability);

        await db.SaveChangesAsync();
        return (clientUserId, service.Id, availability.Id, provider.Id);
    }

    [Fact]
    public async Task CreateBooking_Unauthenticated_Returns401()
    {
        var (_, serviceId, availabilityId, _) = await SeedAsync();

        var body = new StringContent(
            JsonSerializer.Serialize(new { serviceId, availabilityId, notes = "test" }),
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1.0/bookings", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_ValidRequest_Returns201()
    {
        var (clientUserId, serviceId, availabilityId, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(clientUserId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/bookings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { serviceId, availabilityId, notes = "please be on time" }),
            Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BookingResponse>(responseBody, JsonOptions)!;
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Pending", result.Status);

        var db = _factory.CreateDbContext();
        var av = await db.Availabilities.FindAsync(availabilityId);
        Assert.NotNull(av);
        Assert.True(av!.IsBooked);
    }

    [Fact]
    public async Task CreateBooking_AlreadyBookedSlot_Returns400()
    {
        var (clientUserId, serviceId, availabilityId, _) = await SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var av = await db.Availabilities.AsTracking().FirstAsync(a => a.Id == availabilityId);
        av.IsBooked = true;
        await db.SaveChangesAsync();

        var token = TestJwtHelper.CreateToken(clientUserId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/bookings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { serviceId, availabilityId }),
            Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_ServiceNotFound_Returns400()
    {
        var (clientUserId, _, availabilityId, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(clientUserId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/bookings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { serviceId = Guid.NewGuid(), availabilityId }),
            Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
