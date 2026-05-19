using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using Base.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class ServiceIdorTests : IClassFixture<BeautyBookFactory>
{
    private readonly BeautyBookFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ServiceIdorTests(BeautyBookFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid OwnerProviderId, Guid OwnerUserId, Guid OtherUserId, Guid ServiceId, Guid CategoryId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var provider = new BeautyProvider
        {
            UserId = ownerUserId,
            ProfileName = $"Owner_{ownerUserId:N}",
            Address = "Owner Street",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(provider);

        var category = new ServiceCategory
        {
            Name = new LangStr { ["en"] = "IDOR Test Category" },
            IsActive = true
        };
        db.ServiceCategories.Add(category);

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = category.Id,
            Name = new LangStr { ["en"] = "IDOR Test Service" },
            Price = 35,
            DurationMinutes = 45,
            IsActive = true,
            From = DateTime.UtcNow
        };
        db.Services.Add(service);

        await db.SaveChangesAsync();
        return (provider.Id, ownerUserId, otherUserId, service.Id, category.Id);
    }

    [Fact]
    public async Task GetServices_Public_Returns200WithoutAuth()
    {
        var (_, _, _, serviceId, _) = await SeedAsync();

        var response = await _client.GetAsync("/api/v1.0/services");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var services = JsonSerializer.Deserialize<List<ServiceResponse>>(body, JsonOptions)!;

        Assert.Contains(services, s => s.Id == serviceId);
    }

    [Fact]
    public async Task PostService_Unauthenticated_Returns401()
    {
        var (ownerProviderId, _, _, _, categoryId) = await SeedAsync();

        var body = new StringContent(
            JsonSerializer.Serialize(new
            {
                beautyProviderId = ownerProviderId,
                serviceCategoryId = categoryId,
                name = new Dictionary<string, string> { ["en"] = "New Service" },
                price = 30,
                durationMinutes = 45,
                isActive = true
            }),
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1.0/services", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostService_ForOwnProvider_Returns201()
    {
        var (ownerProviderId, ownerUserId, _, _, categoryId) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(ownerUserId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/services");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                beautyProviderId = ownerProviderId,
                serviceCategoryId = categoryId,
                name = new Dictionary<string, string> { ["en"] = "Test Service" },
                price = 30,
                durationMinutes = 45,
                isActive = true
            }),
            Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ServiceResponse>(body, JsonOptions)!;
        Assert.False(string.IsNullOrEmpty(result.Name));
    }

    [Fact]
    public async Task PutService_ByOtherUser_Returns403()
    {
        var (_, _, otherUserId, serviceId, categoryId) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(otherUserId);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1.0/services/{serviceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                serviceCategoryId = categoryId,
                name = new Dictionary<string, string> { ["en"] = "Hacked" },
                price = 99,
                durationMinutes = 60,
                isActive = true
            }),
            Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteService_ByOwner_Returns204()
    {
        var (_, ownerUserId, _, serviceId, _) = await SeedAsync();
        var token = TestJwtHelper.CreateToken(ownerUserId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1.0/services/{serviceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var db = _factory.CreateDbContext();
        var service = await db.Services.FindAsync(serviceId);
        Assert.NotNull(service);
        Assert.False(service!.IsActive);
    }
}
