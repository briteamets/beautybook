using App.DTO.v1;

namespace ClientApp.Services;

public class PaymentsApiService
{
    private readonly BackendApiClient _api;

    public PaymentsApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<PaymentResponse>> GetAsync()
        => await _api.GetAsync<List<PaymentResponse>>("/api/v1/payments") ?? [];

    public Task<PaymentResponse?> PayAsync(Guid bookingId, string paymentMethod)
        => _api.PostAsync<PaymentResponse>("/api/v1/payments",
            new PaymentCreateRequest { BookingIds = [bookingId], PaymentMethod = paymentMethod });
}
