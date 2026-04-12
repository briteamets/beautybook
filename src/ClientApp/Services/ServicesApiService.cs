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

    public async Task<ServiceResponse?> GetAsync(Guid id)
        => (await GetAllAsync()).FirstOrDefault(s => s.Id == id);

    public Task<ServiceResponse?> CreateAsync(ServiceCreateRequest request)
        => _api.PostAsync<ServiceResponse>("/api/v1/services", request);

    public Task<ServiceResponse?> UpdateAsync(Guid id, ServiceUpdateRequest request)
        => _api.PutAsync<ServiceResponse>($"/api/v1/services/{id}", request);

    public Task DeleteAsync(Guid id) => _api.DeleteAsync($"/api/v1/services/{id}");
}
