using System.Text.Json;
using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class BookingsController : Controller
{
    private const string WizardKey = "BookingWizard";

    private readonly BookingsApiService _bookingsApiService;
    private readonly ServicesApiService _servicesApiService;
    private readonly ProvidersApiService _providersApiService;
    private readonly AvailabilityApiService _availabilityApiService;
    private readonly ServiceTypesApiService _serviceTypesApiService;
    private readonly ReviewsApiService _reviewsApiService;

    public BookingsController(
        BookingsApiService bookingsApiService,
        ServicesApiService servicesApiService,
        ProvidersApiService providersApiService,
        AvailabilityApiService availabilityApiService,
        ServiceTypesApiService serviceTypesApiService,
        ReviewsApiService reviewsApiService)
    {
        _bookingsApiService = bookingsApiService;
        _servicesApiService = servicesApiService;
        _providersApiService = providersApiService;
        _availabilityApiService = availabilityApiService;
        _serviceTypesApiService = serviceTypesApiService;
        _reviewsApiService = reviewsApiService;
    }

    // ── Existing CRUD ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingsApiService.GetAllAsync();
        return View(new BookingIndexViewModel
        {
            Bookings = bookings.Select(ToListItem).ToList()
        });
    }

    public async Task<IActionResult> Details(Guid id, string? error)
    {
        var booking = await _bookingsApiService.GetAsync(id);
        if (booking == null) return NotFound();
        var vm = ToListItem(booking);
        vm.ErrorMessage = error;
        return View(vm);
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
            Status = booking.Status,
            Notes = booking.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BookingEditViewModel model)
    {
        if (id != model.Id) return NotFound();
        await _bookingsApiService.UpdateAsync(id, model.Notes);
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

    // ── Provider Bookings ──────────────────────────────────────────────────────

    public async Task<IActionResult> ProviderBookings()
    {
        ICollection<BookingResponse> bookings;
        try
        {
            bookings = await _bookingsApiService.GetIncomingAsync();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (HttpRequestException)
        {
            TempData["SetupRequired"] = "true";
            return RedirectToAction("EditProfile", "Providers");
        }

        return View(new ProviderBookingsViewModel
        {
            HasProviderProfile = true,
            Bookings = bookings.Select(b => new ProviderBookingItemViewModel
            {
                Id = b.Id,
                ServiceName = b.ServiceName,
                ClientName = b.ClientName ?? b.ProviderName,
                AppointmentDateTime = b.AppointmentDateTime,
                Status = b.Status,
                TotalPrice = b.TotalPrice,
                IsPaid = b.IsPaid,
                Notes = b.Notes
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProviderBookingStatus(Guid id, string status, string? reason = null)
    {
        try
        {
            await _bookingsApiService.UpdateStatusAsync(id, status, reason);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (HttpRequestException) { }

        return RedirectToAction(nameof(ProviderBookings));
    }

    // ── Review Creation ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Review(Guid id)
    {
        var booking = await _bookingsApiService.GetAsync(id);
        if (booking == null) return NotFound();
        if (booking.Status != "Completed") return RedirectToAction(nameof(Details), new { id });

        return View(new ReviewCreateViewModel
        {
            BookingId = id,
            ServiceName = booking.ServiceName,
            ProviderName = booking.ProviderName,
            AppointmentDateTime = booking.AppointmentDateTime
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _reviewsApiService.CreateAsync(new App.DTO.v1.ReviewCreateRequest
            {
                BookingId = model.BookingId,
                Rating = model.Rating,
                Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment
            });
        }
        catch (HttpRequestException)
        {
            model.ErrorMessage = "review_error";
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = model.BookingId });
    }

    // ── Booking Wizard ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Wizard()
    {
        var providers = await _providersApiService.GetAllAsync();
        return View("WizardStep1", new WizardStep1ViewModel
        {
            Providers = providers.Select(p => new ProviderListItemViewModel
            {
                Id = p.Id,
                ProfileName = p.ProfileName,
                Address = p.Address,
                AverageRating = p.AverageRating,
                TodayAvailabilityCount = p.TodayAvailability.Count
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WizardStep1Post(Guid providerId)
    {
        var provider = await _providersApiService.GetAsync(providerId);
        if (provider == null) return NotFound();

        var state = new BookingWizardState
        {
            ProviderId = providerId,
            ProviderName = provider.ProfileName
        };
        TempData[WizardKey] = JsonSerializer.Serialize(state);
        return RedirectToAction(nameof(WizardStep2));
    }

    [HttpGet]
    public async Task<IActionResult> WizardStep2(Guid? serviceTypeId = null)
    {
        var json = TempData[WizardKey] as string;
        if (json == null) return RedirectToAction(nameof(Wizard));
        var state = JsonSerializer.Deserialize<BookingWizardState>(json)!;
        TempData[WizardKey] = json;

        var services = await _servicesApiService.GetPublicAsync(state.ProviderId, serviceTypeId);
        var serviceTypes = await _serviceTypesApiService.GetAsync();

        return View("WizardStep2", new WizardStep2ViewModel
        {
            ProviderId = state.ProviderId!.Value,
            ProviderName = state.ProviderName!,
            ServiceTypeId = serviceTypeId,
            ServiceTypes = serviceTypes
                .Select(t => new ServiceTypeOptionViewModel { Id = t.Id, Name = t.Name })
                .ToList(),
            Services = services
                .Where(s => s.IsActive)
                .Select(s => new WizardServiceViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    CategoryName = s.CategoryName
                }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WizardStep2Post(Guid serviceId)
    {
        var json = TempData[WizardKey] as string;
        if (json == null) return RedirectToAction(nameof(Wizard));
        var state = JsonSerializer.Deserialize<BookingWizardState>(json)!;

        var services = await _servicesApiService.GetPublicAsync(state.ProviderId, null);
        var service = services.FirstOrDefault(s => s.Id == serviceId);
        if (service == null) return NotFound();

        state.ServiceId = serviceId;
        state.ServiceName = service.Name;
        state.ServicePrice = service.Price;
        state.ServiceDurationMinutes = service.DurationMinutes;
        TempData[WizardKey] = JsonSerializer.Serialize(state);
        return RedirectToAction(nameof(WizardStep3));
    }

    [HttpGet]
    public async Task<IActionResult> WizardStep3(DateOnly? date = null)
    {
        var json = TempData[WizardKey] as string;
        if (json == null) return RedirectToAction(nameof(Wizard));
        var state = JsonSerializer.Deserialize<BookingWizardState>(json)!;

        var selectedDate = date ?? state.Date ?? DateOnly.FromDateTime(DateTime.Today);
        state.Date = selectedDate;
        TempData[WizardKey] = JsonSerializer.Serialize(state);

        var slots = await _availabilityApiService.GetAsync(state.ProviderId!.Value, selectedDate);

        return View("WizardStep3", new WizardStep3ViewModel
        {
            ProviderId = state.ProviderId!.Value,
            ProviderName = state.ProviderName!,
            ServiceId = state.ServiceId!.Value,
            ServiceName = state.ServiceName!,
            ServicePrice = state.ServicePrice,
            Date = selectedDate,
            Slots = slots
                .Where(s => !s.IsBooked)
                .Select(s => new WizardSlotViewModel
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WizardStep4Post(Guid availabilityId, string? notes)
    {
        var json = TempData[WizardKey] as string;
        if (json == null) return RedirectToAction(nameof(Wizard));
        var state = JsonSerializer.Deserialize<BookingWizardState>(json)!;

        var created = await _bookingsApiService.CreateAsync(new BookingCreateRequest
        {
            ServiceId = state.ServiceId!.Value,
            AvailabilityId = availabilityId,
            Notes = notes
        });
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

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
        IsPaid = booking.IsPaid,
        HasReview = booking.HasReview
    };
}
