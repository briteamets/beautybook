using App.DTO.v1;

namespace ClientApp.Services;

public class AvailabilityApiService
{
    private readonly BackendApiClient _api;

    public AvailabilityApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<AvailabilityResponse>> GetAsync(Guid providerId, DateOnly date)
    {
        var url = $"/api/v1/availability?providerId={providerId}&date={date:yyyy-MM-dd}";
        return await _api.AnonymousGetAsync<List<AvailabilityResponse>>(url) ?? [];
    }

    public async Task<ICollection<AvailabilityResponse>> GetMineAsync(DateOnly? date = null)
    {
        var url = date.HasValue
            ? $"/api/v1/availability/mine?date={date.Value:yyyy-MM-dd}"
            : "/api/v1/availability/mine";
        return await _api.GetAsync<List<AvailabilityResponse>>(url) ?? [];
    }

    public Task<AvailabilityResponse?> CreateAsync(AvailabilityCreateRequest request)
        => _api.PostAsync<AvailabilityResponse>("/api/v1/availability", request);

    public Task DeleteAsync(Guid id)
        => _api.DeleteAsync($"/api/v1/availability/{id}");
}
