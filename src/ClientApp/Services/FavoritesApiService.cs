using App.DTO.v1;

namespace ClientApp.Services;

public class FavoritesApiService
{
    private readonly BackendApiClient _api;

    public FavoritesApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<FavoriteProviderResponse>> GetAsync()
        => await _api.GetAsync<List<FavoriteProviderResponse>>("/api/v1/favorites") ?? [];

    public Task<FavoriteProviderResponse?> AddAsync(Guid providerId)
        => _api.PostAsync<FavoriteProviderResponse>("/api/v1/favorites",
            new FavoriteProviderCreateRequest { BeautyProviderId = providerId });

    public Task RemoveAsync(Guid id)
        => _api.DeleteAsync($"/api/v1/favorites/{id}");
}
