using App.DTO.v1;

namespace ClientApp.Services;

public class ServicesApiService
{
    private readonly BackendApiClient _api;

    public ServicesApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<ServiceResponse>> GetAllAsync(Guid? providerId = null)
    {
        var url = providerId.HasValue ? $"/api/v1/services?providerId={providerId}" : "/api/v1/services";
        return await _api.GetAsync<List<ServiceResponse>>(url) ?? [];
    }

    public async Task<ICollection<ServiceResponse>> GetPublicAsync(Guid? providerId = null, Guid? serviceTypeId = null)
    {
        var parts = new List<string>();
        if (providerId.HasValue) parts.Add($"providerId={providerId}");
        if (serviceTypeId.HasValue) parts.Add($"serviceTypeId={serviceTypeId}");
        var url = parts.Count > 0 ? $"/api/v1/services?{string.Join("&", parts)}" : "/api/v1/services";
        return await _api.AnonymousGetAsync<List<ServiceResponse>>(url) ?? [];
    }

    public async Task<ServiceResponse?> GetAsync(Guid id)
        => (await GetAllAsync()).FirstOrDefault(s => s.Id == id);

    public async Task<ServiceResponse?> GetPublicAsync(Guid id)
        => (await GetPublicAsync()).FirstOrDefault(s => s.Id == id);

    public async Task<ICollection<ServiceResponse>> GetMyAsync()
        => await _api.GetAsync<List<ServiceResponse>>("/api/v1/services/mine") ?? [];

    public Task<ServiceResponse?> CreateAsync(ServiceCreateRequest request)
        => _api.PostAsync<ServiceResponse>("/api/v1/services", request);

    public Task<ServiceResponse?> UpdateAsync(Guid id, ServiceUpdateRequest request)
        => _api.PutAsync<ServiceResponse>($"/api/v1/services/{id}", request);

    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"/api/v1/services/{id}");
}
