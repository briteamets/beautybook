using App.DAL.EF;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsController : BaseApiController
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotifications()
    {
        var userId = GetCurrentUserId();
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.From)
            .ToListAsync();

        return Ok(notifications.Select(n => n.ToResponse()));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkAsRead(Guid id)
    {
        var notification = await _context.Notifications
            .AsTracking()
            .FirstOrDefaultAsync(n => n.Id == id);
        if (notification == null) return NotFound();
        if (!IsCurrentUser(notification.UserId)) return Forbid();

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(notification.ToResponse());
    }
}
