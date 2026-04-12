using App.DTO.v1;

namespace ClientApp.Services;

public class ProvidersApiService
{
    private readonly BackendApiClient _api;

    public ProvidersApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<BeautyProviderResponse>> GetAllAsync()
        => await _api.GetAsync<List<BeautyProviderResponse>>("/api/v1/providers") ?? [];

    public Task<BeautyProviderResponse?> GetAsync(Guid id)
        => _api.GetAsync<BeautyProviderResponse>($"/api/v1/providers/{id}");

    public Task<BeautyProviderResponse?> CreateAsync(BeautyProviderCreateRequest request)
        => _api.PostAsync<BeautyProviderResponse>("/api/v1/providers", request);

    public Task<BeautyProviderResponse?> UpdateAsync(Guid id, BeautyProviderUpdateRequest request)
        => _api.PutAsync<BeautyProviderResponse>($"/api/v1/providers/{id}", request);

    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"/api/v1/providers/{id}");
}
