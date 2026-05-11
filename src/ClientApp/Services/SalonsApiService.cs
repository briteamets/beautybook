using App.DTO.v1;

namespace ClientApp.Services;

public class SalonsApiService
{
    private readonly BackendApiClient _api;

    public SalonsApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<SalonResponse>> GetAllAsync()
        => await _api.AnonymousGetAsync<List<SalonResponse>>("/api/v1/salons") ?? [];

    public Task<SalonResponse?> GetAsync(Guid id)
        => _api.AnonymousGetAsync<SalonResponse>($"/api/v1/salons/{id}");
}
