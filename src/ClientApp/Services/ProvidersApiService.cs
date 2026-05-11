using App.DTO.v1;

namespace ClientApp.Services;

public class ProvidersApiService
{
    private readonly BackendApiClient _api;

    public ProvidersApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<BeautyProviderResponse>> GetAllAsync(
        string? providerType = null, Guid? serviceTypeId = null, string? search = null)
    {
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(providerType)) q.Add($"providerType={providerType}");
        if (serviceTypeId.HasValue) q.Add($"serviceTypeId={serviceTypeId.Value}");
        if (!string.IsNullOrWhiteSpace(search)) q.Add($"search={Uri.EscapeDataString(search)}");
        var url = q.Count == 0 ? "/api/v1/providers" : "/api/v1/providers?" + string.Join("&", q);
        return await _api.AnonymousGetAsync<List<BeautyProviderResponse>>(url) ?? [];
    }

    public Task<BeautyProviderResponse?> GetAsync(Guid id)
        => _api.GetAsync<BeautyProviderResponse>($"/api/v1/providers/{id}");

    public Task<BeautyProviderResponse?> GetMineAsync()
        => _api.GetAsync<BeautyProviderResponse>("/api/v1/providers/mine");

    public Task<BeautyProviderResponse?> SaveMineAsync(BeautyProviderCreateRequest request)
        => _api.PostAsync<BeautyProviderResponse>("/api/v1/providers", request);

    public Task<BeautyProviderResponse?> CreateAsync(BeautyProviderCreateRequest request)
        => _api.PostAsync<BeautyProviderResponse>("/api/v1/providers", request);

    public Task<BeautyProviderResponse?> UpdateAsync(Guid id, BeautyProviderUpdateRequest request)
        => _api.PutAsync<BeautyProviderResponse>($"/api/v1/providers/{id}", request);

    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"/api/v1/providers/{id}");
}
