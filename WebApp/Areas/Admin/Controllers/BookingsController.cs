using App.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class BookingsController : AdminControllerBase
{
    public BookingsController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Index(string? status, DateOnly? dateFrom, DateOnly? dateTo, Guid? providerId, string? providerSearch, Guid? serviceTypeId, int page = 1)
    {
        page = Math.Max(1, page);
        var query = Context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .ThenInclude(p => p!.User)
            .Include(b => b.Service)
            .ThenInclude(s => s!.ServiceType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        if (dateFrom.HasValue)
        {
            var fromDateTime = dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(b => b.AppointmentDateTime >= fromDateTime);
        }

        if (dateTo.HasValue)
        {
            var toDateTime = dateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(b => b.AppointmentDateTime <= toDateTime);
        }

        if (providerId.HasValue)
        {
            query = query.Where(b => b.Service!.BeautyProviderId == providerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(providerSearch))
        {
            query = query.Where(b =>
                b.Service!.BeautyProvider!.ProfileName.ToLower().Contains(providerSearch.ToLower()) ||
                (b.Service.BeautyProvider.User != null &&
                 b.Service.BeautyProvider.User.Email != null &&
                 b.Service.BeautyProvider.User.Email.ToLower().Contains(providerSearch.ToLower())));
        }

        if (serviceTypeId.HasValue)
        {
            query = query.Where(b => b.Service!.ServiceTypeId == serviceTypeId.Value);
        }

        var totalItems = await query.CountAsync();
        var items = await query.OrderByDescending(b => b.AppointmentDateTime)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(b => new BookingIndexItem
            {
                Id = b.Id,
                ClientName = b.User!.UserName ?? b.User.Email ?? string.Empty,
                ServiceName = (b.Service!.ServiceType != null ? b.Service.ServiceType.Name.Translate() : b.Service.Name.Translate()) ?? string.Empty,
                ProviderName = b.Service.BeautyProvider!.ProfileName,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status,
                CreatedAt = b.BookedAt
            })
            .ToListAsync();

        return View(new BookingIndexViewModel
        {
            Filter = new BookingIndexFilterViewModel
            {
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo,
                ProviderId = providerId,
                ProviderSearch = providerSearch,
                ServiceTypeId = serviceTypeId
            },
            Providers = await GetProviderOptions(providerId),
            ServiceTypes = await GetServiceTypeOptions(serviceTypeId),
            Page = new PagedViewModel<BookingIndexItem>
            {
                Items = items,
                PageNumber = page,
                TotalItems = totalItems,
                TotalPages = (int) Math.Ceiling(totalItems / (double) PageSize)
            }
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var booking = await Context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Service)
            .ThenInclude(s => s!.ServiceType)
            .Include(b => b.Salon)
            .Include(b => b.CancelledByAdmin)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return NotFound();

        var history = await Context.BookingHistories
            .Where(h => h.BookingId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

        return View(new BookingDetailsViewModel
        {
            Booking = booking,
            HistoryEntries = history,
            CanCancel = booking.Status is not (Booking.StatusCancelled or Booking.StatusCompleted)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        var allowed = new[] { Booking.StatusPending, Booking.StatusConfirmed, Booking.StatusCompleted, Booking.StatusCancelled };
        if (!allowed.Contains(status))
        {
            SetErrorMessage(T("InvalidStatusTransition"));
            return RedirectToAction(nameof(Details), new { id });
        }

        var booking = await Context.Bookings.AsTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return NotFound();

        if (booking.Status != status)
        {
            var oldStatus = booking.Status;
            booking.Status = status;
            Context.BookingHistories.Add(new BookingHistory
            {
                BookingId = booking.Id,
                OldStatus = oldStatus,
                NewStatus = status,
                UserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
                ChangedAt = DateTime.UtcNow,
                Reason = T("AdminStatusUpdate"),
                From = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();
            SetSuccessMessage(T("BookingStatusUpdated"));
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Cancel(Guid id)
    {
        var booking = await Context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Service)
            .ThenInclude(s => s!.ServiceType)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return NotFound();

        if (booking.Status is Booking.StatusCancelled or Booking.StatusCompleted)
        {
            SetErrorMessage(T("BookingCannotBeCancelled"));
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new BookingCancelViewModel
        {
            Id = booking.Id,
            ClientName = booking.User?.UserName ?? booking.User?.Email ?? string.Empty,
            ProviderName = booking.Service?.BeautyProvider?.ProfileName ?? string.Empty,
            ServiceName = booking.Service?.ServiceType?.Name.Translate() ?? booking.Service?.Name.Translate() ?? string.Empty,
            AppointmentDateTime = booking.AppointmentDateTime
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, BookingCancelViewModel vm)
    {
        if (id != vm.Id) return NotFound();

        var booking = await Context.Bookings
            .Include(b => b.User)
            .Include(b => b.Service)
            .ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Service)
            .ThenInclude(s => s!.ServiceType)
            .AsTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return NotFound();

        vm.ClientName = booking.User?.UserName ?? booking.User?.Email ?? string.Empty;
        vm.ProviderName = booking.Service?.BeautyProvider?.ProfileName ?? string.Empty;
        vm.ServiceName = booking.Service?.ServiceType?.Name.Translate() ?? booking.Service?.Name.Translate() ?? string.Empty;
        vm.AppointmentDateTime = booking.AppointmentDateTime;

        if (booking.Status is Booking.StatusCancelled or Booking.StatusCompleted)
        {
            ModelState.AddModelError(string.Empty, T("BookingCannotBeCancelled"));
        }

        if (!ModelState.IsValid) return View(vm);

        var oldStatus = booking.Status;
        booking.Status = Booking.StatusCancelled;
        booking.CancelledByAdminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        booking.CancellationReason = vm.Reason.Trim();

        Context.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = oldStatus,
            NewStatus = Booking.StatusCancelled,
            UserId = booking.CancelledByAdminId.Value,
            ChangedAt = DateTime.UtcNow,
            Reason = vm.Reason.Trim(),
            From = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("BookingCancelled"));
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<SelectListItem>> GetProviderOptions(Guid? selectedId)
    {
        return await Context.BeautyProviders
            .OrderBy(p => p.ProfileName)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.ProfileName,
                Selected = selectedId == p.Id
            })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetServiceTypeOptions(Guid? selectedId)
    {
        return await Context.ServiceTypes
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name.Translate() ?? string.Empty,
                Selected = selectedId == t.Id
            })
            .ToListAsync();
    }
}
