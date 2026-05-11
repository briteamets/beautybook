using ClientApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly NotificationsApiService _service;
    private readonly ILogger<NotificationBellViewComponent> _logger;

    public NotificationBellViewComponent(NotificationsApiService service, ILogger<NotificationBellViewComponent> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!HttpContext.Session.HasUsableToken())
            return Content(string.Empty);

        int unreadCount;
        try
        {
            var notifications = await _service.GetAsync();
            unreadCount = notifications.Count(n => !n.IsRead);
        }
        catch (UnauthorizedAccessException)
        {
            // Token was invalidated (401 from API); tokens already cleared by BackendApiClient.
            return Content(string.Empty);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("NotificationBell: API request failed — {Message}", ex.Message);
            unreadCount = 0;
        }

        return View(unreadCount);
    }
}
