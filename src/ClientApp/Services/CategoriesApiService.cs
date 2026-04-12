using App.DTO.v1;

namespace ClientApp.Services;

public class CategoriesApiService
{
    private readonly BackendApiClient _api;

    public CategoriesApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<ServiceCategoryResponse>> GetAllAsync()
        => await _api.GetAsync<List<ServiceCategoryResponse>>("/api/v1/categories") ?? [];
}
