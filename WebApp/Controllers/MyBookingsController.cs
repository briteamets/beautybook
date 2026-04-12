using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class MyBookingsController : ClientControllerBase
{
    public MyBookingsController(AppDbContext context) : base(context)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "Upcoming")
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;
        var bookings = await Context.Bookings
            .Where(b => b.UserId == userId && b.To == null)
            .Include(b => b.Service)
                .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .Include(b => b.Review)
            .OrderByDescending(b => b.AppointmentDateTime)
            .ToListAsync();

        var filtered = bookings.Where(b => tab switch
        {
            "Cancelled" => b.Status == Booking.StatusCancelled,
            "Past" => b.Status != Booking.StatusCancelled && (b.Status == Booking.StatusCompleted || b.AppointmentDateTime < now),
            _ => b.Status != Booking.StatusCancelled && b.AppointmentDateTime >= now
        }).ToList();

        return View(new MyBookingsIndexViewModel
        {
            Tab = tab,
            Bookings = filtered.Select(b => new MyBookingCardViewModel
            {
                BookingId = b.Id,
                ProviderId = b.Service?.BeautyProviderId ?? Guid.Empty,
                ServiceId = b.ServiceId,
                ProfilePictureUrl = b.Service?.BeautyProvider?.ProfilePictureUrl,
                ProviderName = b.Service?.BeautyProvider?.ProfileName ?? string.Empty,
                SalonName = b.Salon?.Name.Translate(),
                ServiceName = b.Service?.Name.Translate() ?? string.Empty,
                AppointmentDateTime = b.AppointmentDateTime,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Price = b.Service?.Price ?? 0,
                Status = b.Status,
                HasReview = b.Review != null
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var booking = await Context.Bookings
            .AsTracking()
            .Include(b => b.Availability)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == GetCurrentUserId() && b.To == null);
        if (booking == null) return NotFound();
        if (booking.Status == Booking.StatusCancelled)
        {
            SetErrorMessage(T("BookingAlreadyCancelled"));
            return RedirectToAction(nameof(Index), new { tab = "Cancelled" });
        }

        var oldStatus = booking.Status;
        booking.Status = Booking.StatusCancelled;
        if (booking.Availability != null) booking.Availability.IsBooked = false;

        Context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = Booking.StatusCancelled,
            UserId = booking.UserId,
            ChangedAt = DateTime.UtcNow,
            Reason = "Cancelled by customer",
            From = DateTime.UtcNow
        });

        Context.Notifications.Add(new Notification
        {
            UserId = booking.UserId,
            BookingId = booking.Id,
            Type = Notification.TypeBookingCancelled,
            Title = "Booking cancelled",
            Body = "Your booking has been cancelled.",
            IsRead = false,
            From = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("BookingCancelled"));
        return RedirectToAction(nameof(Index), new { tab = "Cancelled" });
    }
}
