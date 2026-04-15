using App.DAL.EF;
using App.Domain;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.SalonOwner.ViewModels;

namespace WebApp.Areas.SalonOwner.Controllers;

public class BookingsController : SalonOwnerControllerBase
{
    public BookingsController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index(Guid? salonId, DateOnly? dateFrom, DateOnly? dateTo, string? status)
    {
        var ownedSalonIds = await GetOwnedSalonIdsAsync();
        if (salonId.HasValue && !ownedSalonIds.Contains(salonId.Value)) return NotFound();

        var selectedSalonIds = salonId.HasValue ? new List<Guid> { salonId.Value } : ownedSalonIds;
        var query = Context.Bookings
            .Include(b => b.Salon)
            .Include(b => b.User)
            .Include(b => b.Service!)
            .ThenInclude(s => s.BeautyProvider)
            .Where(b => b.SalonId != null && selectedSalonIds.Contains(b.SalonId.Value));

        if (dateFrom.HasValue)
        {
            query = query.Where(b => b.BookingDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(b => b.BookingDate <= dateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.StartTime)
            .ToListAsync();

        var model = new SalonOwnerBookingIndexViewModel
        {
            SalonId = salonId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Status = status,
            SalonOptions = await GetSalonOptionsAsync(salonId),
            StatusOptions =
            [
                new SelectListItem(T("Pending"), Booking.StatusPending, status == Booking.StatusPending),
                new SelectListItem(T("Confirmed"), Booking.StatusConfirmed, status == Booking.StatusConfirmed),
                new SelectListItem(T("Cancelled"), Booking.StatusCancelled, status == Booking.StatusCancelled),
                new SelectListItem(T("Completed"), Booking.StatusCompleted, status == Booking.StatusCompleted)
            ],
            Bookings = bookings.Select(b => new SalonOwnerBookingItemViewModel
            {
                Id = b.Id,
                SalonName = Translate(b.Salon?.Name),
                ClientName = b.User?.Email ?? string.Empty,
                ProviderName = b.Service?.BeautyProvider?.ProfileName ?? string.Empty,
                ServiceName = Translate(b.Service?.Name),
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status,
                CreatedAt = b.BookedAt
            }).ToList()
        };

        return View(model);
    }
}
