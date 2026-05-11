using App.DTO.v1;

namespace ClientApp.Services;

public class NotificationsApiService
{
    private readonly BackendApiClient _api;

    public NotificationsApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<NotificationResponse>> GetAsync()
        => await _api.GetAsync<List<NotificationResponse>>("/api/v1/notifications") ?? [];

    public Task<NotificationResponse?> MarkReadAsync(Guid id)
        => _api.PutAsync<NotificationResponse>($"/api/v1/notifications/{id}/read", new { });
}
