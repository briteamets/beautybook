using System.Net;
using System.Text.Json;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using Base.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class ProvidersFilterIntegrationTests : IClassFixture<BeautyBookFactory>
{
    private readonly BeautyBookFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProvidersFilterIntegrationTests(BeautyBookFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid IndependentProviderId, Guid SalonProviderId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var independentProvider = new BeautyProvider
        {
            UserId = Guid.NewGuid(),
            ProfileName = "Independent Provider",
            Address = "Anywhere",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(independentProvider);

        var salonProvider = new BeautyProvider
        {
            UserId = Guid.NewGuid(),
            ProfileName = "Salon Provider",
            Address = "Salon Street",
            From = DateTime.UtcNow
        };
        db.BeautyProviders.Add(salonProvider);

        var salon = new Salon
        {
            Name = new LangStr { ["en"] = "Test Salon" },
            Address = "Salon Street",
            From = DateTime.UtcNow
        };
        db.Salons.Add(salon);

        var staff = new SalonStaff
        {
            SalonId = salon.Id,
            BeautyProviderId = salonProvider.Id,
            Role = SalonStaff.RoleEmployee,
            From = DateTime.UtcNow
        };
        db.SalonStaff.Add(staff);

        await db.SaveChangesAsync();
        return (independentProvider.Id, salonProvider.Id);
    }

    [Fact]
    public async Task GetProviders_NoFilter_ReturnsBothProviders()
    {
        var (indId, salonId) = await SeedAsync();

        var response = await _client.GetAsync("/api/v1.0/providers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var providers = JsonSerializer.Deserialize<List<BeautyProviderResponse>>(body, JsonOptions)!;

        Assert.Contains(providers, p => p.Id == indId);
        Assert.Contains(providers, p => p.Id == salonId);
    }

    [Fact]
    public async Task GetProviders_IndependentFilter_ExcludesSalonProviders()
    {
        var (indId, salonId) = await SeedAsync();

        var response = await _client.GetAsync("/api/v1.0/providers?providerType=independent");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var providers = JsonSerializer.Deserialize<List<BeautyProviderResponse>>(body, JsonOptions)!;

        Assert.Contains(providers, p => p.Id == indId);
        Assert.DoesNotContain(providers, p => p.Id == salonId);
    }

    [Fact]
    public async Task GetProviders_SalonFilter_ExcludesIndependentProviders()
    {
        var (indId, salonId) = await SeedAsync();

        var response = await _client.GetAsync("/api/v1.0/providers?providerType=salon");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var providers = JsonSerializer.Deserialize<List<BeautyProviderResponse>>(body, JsonOptions)!;

        Assert.DoesNotContain(providers, p => p.Id == indId);
        Assert.Contains(providers, p => p.Id == salonId);
    }
}
