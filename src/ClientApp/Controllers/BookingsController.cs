using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class BookingsController : Controller
{
    private readonly BookingsApiService _bookingsApiService;
    private readonly ServicesApiService _servicesApiService;
    private readonly ProvidersApiService _providersApiService;

    public BookingsController(
        BookingsApiService bookingsApiService,
        ServicesApiService servicesApiService,
        ProvidersApiService providersApiService)
    {
        _bookingsApiService = bookingsApiService;
        _servicesApiService = servicesApiService;
        _providersApiService = providersApiService;
    }

    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingsApiService.GetAllAsync();
        return View(new BookingIndexViewModel
        {
            Bookings = bookings.Select(ToListItem).ToList()
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var booking = await _bookingsApiService.GetAsync(id);
        if (booking == null) return NotFound();
        return View(ToListItem(booking));
    }

    public async Task<IActionResult> Create()
    {
        var model = new BookingCreateViewModel();
        await LoadOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(model);
            return View(model);
        }

        var created = await _bookingsApiService.CreateAsync(new BookingCreateRequest
        {
            ServiceId = model.ServiceId,
            AvailabilityId = model.AvailabilityId,
            Notes = model.Notes
        });
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var booking = await _bookingsApiService.GetAsync(id);
        if (booking == null) return NotFound();
        return View(new BookingEditViewModel
        {
            Id = booking.Id,
            ServiceName = booking.ServiceName,
            ProviderName = booking.ProviderName,
            AppointmentDateTime = booking.AppointmentDateTime,
            Status = booking.Status
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BookingEditViewModel model)
    {
        if (id != model.Id) return NotFound();
        await _bookingsApiService.CancelAsync(id, model.Reason);
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var booking = await _bookingsApiService.GetAsync(id);
        if (booking == null) return NotFound();
        return View(new BookingEditViewModel
        {
            Id = booking.Id,
            ServiceName = booking.ServiceName,
            ProviderName = booking.ProviderName,
            AppointmentDateTime = booking.AppointmentDateTime,
            Status = booking.Status
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _bookingsApiService.CancelAsync(id, "Cancelled from separate MVC client.");
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOptionsAsync(BookingCreateViewModel model)
    {
        model.Services = (await _servicesApiService.GetAllAsync())
            .Select(s => new SelectOptionViewModel { Id = s.Id, Label = $"{s.Name} ({s.Price:0.00} EUR)" })
            .ToList();

        model.AvailabilitySlots = (await _providersApiService.GetAllAsync())
            .SelectMany(p => p.TodayAvailability.Select(a => new SelectOptionViewModel
            {
                Id = a.Id,
                Label = $"{p.ProfileName}: {a.Date:yyyy-MM-dd} {a.StartTime:HH:mm}-{a.EndTime:HH:mm}"
            }))
            .ToList();
    }

    private static BookingListItemViewModel ToListItem(BookingResponse booking) => new()
    {
        Id = booking.Id,
        ServiceName = booking.ServiceName,
        ProviderName = booking.ProviderName,
        SalonName = booking.SalonName,
        AppointmentDateTime = booking.AppointmentDateTime,
        Status = booking.Status,
        TotalPrice = booking.TotalPrice,
        IsPaid = booking.IsPaid
    };
}
