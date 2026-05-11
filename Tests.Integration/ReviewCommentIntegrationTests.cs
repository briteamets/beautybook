using System.Net;
using System.Net.Http.Headers;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using App.DTO.v1;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Tests.Integration;

public class ReviewCommentIntegrationTests : IClassFixture<BeautyBookFactory>
{
    private readonly BeautyBookFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReviewCommentIntegrationTests(BeautyBookFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid ReviewId, Guid ProviderUserId, Guid OtherUserId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var providerUserId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        // Must exist in AspNetUsers so Include(r => r.User) INNER JOIN succeeds.
        // Use clientUserId in the email to avoid unique-constraint collisions across tests
        // (all tests in this class share the same database via IClassFixture).
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
            ProfileName = "Integration Provider",
            Address = "Test Street 1",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(provider);

        var category = new ServiceCategory
        {
            Name = new LangStr { ["en"] = "Hair" },
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = category.Id,
            Name = new LangStr { ["en"] = "Haircut" },
            Price = 25,
            DurationMinutes = 30,
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
            AppointmentDateTime = DateTime.UtcNow.AddDays(-2),
            From = DateTime.UtcNow
        };
        db.Bookings.Add(booking);

        var review = new Review
        {
            BookingId = booking.Id,
            UserId = clientUserId,
            Rating = 5,
            Comment = "Excellent work!",
            From = DateTime.UtcNow
        };
        db.Reviews.Add(review);

        await db.SaveChangesAsync();
        return (review.Id, providerUserId, otherUserId);
    }

    [Fact]
    public async Task DeleteComment_Unauthenticated_Returns401()
    {
        var (reviewId, _, _) = await SeedAsync();
        var response = await _client.DeleteAsync($"/api/v1.0/reviews/{reviewId}/comment");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_ByOwnProvider_Returns200WithNullComment()
    {
        var (reviewId, providerUserId, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(providerUserId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1.0/reviews/{reviewId}/comment");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ReviewResponse>(body, JsonOptions);
        Assert.NotNull(result);
        Assert.Null(result!.Comment);
        Assert.Equal(5, result.Rating);
    }

    [Fact]
    public async Task DeleteComment_ByOtherProvider_Returns403()
    {
        var (reviewId, _, otherUserId) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(otherUserId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1.0/reviews/{reviewId}/comment");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_NonExistentReview_Returns404()
    {
        var token = TestJwtHelper.CreateToken(Guid.NewGuid());

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1.0/reviews/{Guid.NewGuid()}/comment");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
