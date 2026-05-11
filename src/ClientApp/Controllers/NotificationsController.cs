using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class NotificationsController : Controller
{
    private readonly NotificationsApiService _notificationsApiService;

    public NotificationsController(NotificationsApiService notificationsApiService)
    {
        _notificationsApiService = notificationsApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ICollection<App.DTO.v1.NotificationResponse> notifications;
        try
        {
            notifications = await _notificationsApiService.GetAsync();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (HttpRequestException)
        {
            notifications = [];
        }

        return View(new NotificationsIndexViewModel
        {
            Notifications = notifications.Select(n => new NotificationListItemViewModel
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        try
        {
            await _notificationsApiService.MarkReadAsync(id);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (HttpRequestException) { }

        return RedirectToAction(nameof(Index));
    }
}
