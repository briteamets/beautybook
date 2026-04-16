using App.DTO.v1;

namespace ClientApp.Services;

public class BookingsApiService
{
    private readonly BackendApiClient _api;

    public BookingsApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<BookingResponse>> GetAllAsync()
        => await _api.GetAsync<List<BookingResponse>>("/api/v1/bookings") ?? [];

    public Task<BookingResponse?> GetAsync(Guid id)
        => _api.GetAsync<BookingResponse>($"/api/v1/bookings/{id}");

    public Task<BookingResponse?> CreateAsync(BookingCreateRequest request)
        => _api.PostAsync<BookingResponse>("/api/v1/bookings", request);

    public Task<BookingResponse?> UpdateAsync(Guid id, string? notes)
        => _api.PatchAsync<BookingResponse>($"/api/v1/bookings/{id}", new BookingUpdateRequest { Notes = notes });

    public Task<BookingResponse?> CancelAsync(Guid id, string? reason)
        => _api.PutAsync<BookingResponse>($"/api/v1/bookings/{id}/cancel", new BookingCancelRequest { Reason = reason });
}
