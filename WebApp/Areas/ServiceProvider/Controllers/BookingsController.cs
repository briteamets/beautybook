using App.DAL.EF;
using App.Domain;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.ServiceProvider.ViewModels;
using WebApp;

namespace WebApp.Areas.ServiceProvider.Controllers;

public class BookingsController : ServiceProviderControllerBase
{
    public BookingsController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index()
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return RedirectToAction("Index", "Dashboard");
        }

        var bookings = await Context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .Where(b => b.Service!.BeautyProviderId == provider.Id)
            .OrderByDescending(b => b.AppointmentDateTime)
            .ToListAsync();

        var model = new ProviderBookingIndexViewModel
        {
            Bookings = bookings
                .Select(b => new ProviderBookingListItemViewModel
                {
                    Id = b.Id,
                    CustomerEmail = b.User!.Email!,
                    ServiceName = Translate(b.Service!.Name),
                    AppointmentDateTime = b.AppointmentDateTime,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status
                })
                .ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await BuildDetailsModelAsync(id);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(Guid id)
    {
        return await ChangeStatus(id, Booking.StatusConfirmed, "Accepted by service provider.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id)
    {
        return await ChangeStatus(id, Booking.StatusCancelled, "Rejected by service provider.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id)
    {
        return await ChangeStatus(id, Booking.StatusCompleted, "Completed by service provider.");
    }

    private async Task<IActionResult> ChangeStatus(Guid id, string newStatus, string reason)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return NotFound();

        var booking = await Context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Availability)
            .FirstOrDefaultAsync(b => b.Id == id && b.Service!.BeautyProviderId == provider.Id);

        if (booking == null) return NotFound();
        if (booking.Status == newStatus)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var oldStatus = booking.Status;
        var allowed = (oldStatus, newStatus) switch
        {
            (Booking.StatusPending, Booking.StatusConfirmed) => true,
            (Booking.StatusPending, Booking.StatusCancelled) => true,
            (Booking.StatusConfirmed, Booking.StatusCompleted) => true,
            _ => false
        };

        if (!allowed)
        {
            Error("InvalidStatusTransition");
            return RedirectToAction(nameof(Details), new { id });
        }

        booking.Status = newStatus;

        if (newStatus == Booking.StatusCancelled && booking.Availability != null)
        {
            booking.Availability.IsBooked = false;
        }

        Context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            UserId = CurrentUserId,
            ChangedAt = DateTime.UtcNow,
            Reason = reason,
            From = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
        Success("BookingStatusUpdated");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<ProviderBookingDetailsViewModel?> BuildDetailsModelAsync(Guid id)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return null;

        var booking = await Context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .Include(b => b.Salon)
            .Include(b => b.BookingHistory)
            .FirstOrDefaultAsync(b => b.Id == id && b.Service!.BeautyProviderId == provider.Id);

        if (booking == null) return null;

        return new ProviderBookingDetailsViewModel
        {
            Id = booking.Id,
            CustomerEmail = booking.User!.Email!,
            ServiceName = Translate(booking.Service!.Name),
            SalonName = Translate(booking.Salon?.Name),
            BookedAt = booking.BookedAt,
            AppointmentDateTime = booking.AppointmentDateTime,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            Notes = booking.Notes,
            TotalPrice = booking.Service.Price,
            History = booking.BookingHistory?
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new ProviderBookingHistoryItemViewModel
                {
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangedAt = h.ChangedAt,
                    Reason = h.Reason
                })
                .ToList() ?? []
        };
    }
}
