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
    private readonly CategoriesApiService _categoriesApiService;
    private readonly SalonsApiService _salonsApiService;

    public BookingsController(
        BookingsApiService bookingsApiService,
        ServicesApiService servicesApiService,
        ProvidersApiService providersApiService,
        AvailabilityApiService availabilityApiService,
        ServiceTypesApiService serviceTypesApiService,
        ReviewsApiService reviewsApiService,
        CategoriesApiService categoriesApiService,
        SalonsApiService salonsApiService)
    {
        _bookingsApiService = bookingsApiService;
        _servicesApiService = servicesApiService;
        _providersApiService = providersApiService;
        _availabilityApiService = availabilityApiService;
        _serviceTypesApiService = serviceTypesApiService;
        _reviewsApiService = reviewsApiService;
        _categoriesApiService = categoriesApiService;
        _salonsApiService = salonsApiService;
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
    public async Task<IActionResult> WizardStep1Post(
        Guid providerId,
        Guid? salonId = null,
        string? salonName = null,
        Guid? serviceTypeId = null)
    {
        var provider = await _providersApiService.GetAsync(providerId);
        if (provider == null) return NotFound();

        var state = new BookingWizardState
        {
            ProviderId = providerId,
            ProviderName = provider.ProfileName,
            SalonId = salonId,
            SalonName = salonName,
            ServiceTypeId = serviceTypeId
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

        var effectiveServiceTypeId = serviceTypeId ?? state.ServiceTypeId;
        var services = await _servicesApiService.GetPublicAsync(state.ProviderId, effectiveServiceTypeId);
        var serviceTypes = await _serviceTypesApiService.GetAsync();
        var lang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        return View("WizardStep2", new WizardStep2ViewModel
        {
            ProviderId = state.ProviderId!.Value,
            ProviderName = state.ProviderName!,
            ServiceTypeId = effectiveServiceTypeId,
            SalonName = state.SalonName,
            ServiceTypes = serviceTypes
                .Select(t => new ServiceTypeOptionViewModel
                {
                    Id = t.Id,
                    Name = lang == "et" ? t.NameEt : t.NameEn
                })
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
            SalonName = state.SalonName,
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
            Notes = notes,
            SalonId = state.SalonId
        });
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    // ── Service-first wizard ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ServiceWizard(Guid? salonId = null, string? salonName = null)
    {
        var categories = await _categoriesApiService.GetAllAsync();
        return View("WizardCategoryStep", new WizardCategoryStepViewModel
        {
            Categories = categories
                .Select(c => new WizardCategoryItemViewModel { Id = c.Id, Name = c.Name })
                .ToList(),
            SalonId = salonId,
            SalonName = salonName
        });
    }

    [HttpGet]
    public async Task<IActionResult> ServiceWizardTypes(Guid categoryId, Guid? salonId = null, string? salonName = null)
    {
        var categories = await _categoriesApiService.GetAllAsync();
        var category = categories.FirstOrDefault(c => c.Id == categoryId);
        if (category == null) return RedirectToAction(nameof(ServiceWizard), new { salonId, salonName });

        var serviceTypes = await _serviceTypesApiService.GetAsync(categoryId);
        var lang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        return View("WizardServiceTypeStep", new WizardServiceTypeStepViewModel
        {
            CategoryId = categoryId,
            CategoryName = category.Name,
            ServiceTypes = serviceTypes
                .Select(t => new WizardServiceTypeItemViewModel
                {
                    Id = t.Id,
                    Name = lang == "et" ? t.NameEt : t.NameEn
                })
                .ToList(),
            SalonId = salonId,
            SalonName = salonName
        });
    }

    [HttpGet]
    public async Task<IActionResult> ServiceWizardProviders(Guid categoryId, Guid serviceTypeId, Guid? salonId = null, string? salonName = null)
    {
        var serviceTypes = await _serviceTypesApiService.GetAsync(categoryId);
        var lang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        var serviceType = serviceTypes.FirstOrDefault(t => t.Id == serviceTypeId);
        if (serviceType == null) return RedirectToAction(nameof(ServiceWizardTypes), new { categoryId, salonId, salonName });

        var providers = await _providersApiService.GetAllAsync(serviceTypeId: serviceTypeId);

        if (salonId.HasValue)
        {
            var salon = await _salonsApiService.GetAsync(salonId.Value);
            if (salon != null)
            {
                var staffIds = salon.StaffMembers.Select(s => s.ProviderId).ToHashSet();
                providers = providers.Where(p => staffIds.Contains(p.Id)).ToList();
            }
        }

        return View("WizardProviderByServiceStep", new WizardProviderByServiceStepViewModel
        {
            CategoryId = categoryId,
            ServiceTypeId = serviceTypeId,
            ServiceTypeName = lang == "et" ? serviceType.NameEt : serviceType.NameEn,
            Providers = providers
                .Select(p => new ProviderListItemViewModel
                {
                    Id = p.Id,
                    ProfileName = p.ProfileName,
                    Address = p.Address,
                    AverageRating = p.AverageRating,
                    TodayAvailabilityCount = p.TodayAvailability.Count
                })
                .ToList(),
            SalonId = salonId,
            SalonName = salonName
        });
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
        HasPayment = booking.HasPayment,
        HasReview = booking.HasReview
    };
}
