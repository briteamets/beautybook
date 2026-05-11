using App.DTO.v1;

namespace ClientApp.Services;

public class ServiceTypesApiService
{
    private readonly BackendApiClient _api;

    public ServiceTypesApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<ServiceTypeResponse>> GetAsync(Guid? categoryId = null)
    {
        try
        {
            var url = categoryId.HasValue
                ? $"/api/v1/service-types?categoryId={categoryId}"
                : "/api/v1/service-types";
            return await _api.AnonymousGetAsync<List<ServiceTypeResponse>>(url) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}
