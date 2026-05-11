using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class AvailabilityController : Controller
{
    private readonly AvailabilityApiService _availabilityApiService;
    private readonly ProvidersApiService _providersApiService;

    public AvailabilityController(
        AvailabilityApiService availabilityApiService,
        ProvidersApiService providersApiService)
    {
        _availabilityApiService = availabilityApiService;
        _providersApiService = providersApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, string? error)
    {
        BeautyProviderResponse? provider;
        try
        {
            provider = await _providersApiService.GetMineAsync();
        }
        catch (HttpRequestException)
        {
            provider = null;
        }

        if (provider == null)
        {
            TempData["SetupRequired"] = "true";
            return RedirectToAction("EditProfile", "Providers");
        }

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        ICollection<AvailabilityResponse> slots;
        try
        {
            slots = await _availabilityApiService.GetMineAsync(selectedDate);
        }
        catch (HttpRequestException)
        {
            slots = [];
            error = "load";
        }

        return View(new AvailabilityIndexViewModel
        {
            HasProviderProfile = true,
            ProviderId = provider.Id,
            SelectedDate = selectedDate,
            Slots = slots.Select(s => new AvailabilitySlotViewModel
            {
                Id = s.Id,
                Date = s.Date,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsBooked = s.IsBooked
            }).ToList(),
            ErrorMessage = error
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AvailabilityCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index), new { date = model.Date.ToString("yyyy-MM-dd") });

        try
        {
            await _availabilityApiService.CreateAsync(new AvailabilityCreateRequest
            {
                BeautyProviderId = model.BeautyProviderId,
                Date = model.Date,
                StartTime = model.StartTime,
                EndTime = model.EndTime
            });
        }
        catch (HttpRequestException)
        {
            return RedirectToAction(nameof(Index), new
            {
                date = model.Date.ToString("yyyy-MM-dd"),
                error = "create"
            });
        }

        return RedirectToAction(nameof(Index), new { date = model.Date.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string returnDate)
    {
        try
        {
            await _availabilityApiService.DeleteAsync(id);
        }
        catch (HttpRequestException)
        {
            return RedirectToAction(nameof(Index), new { date = returnDate, error = "delete" });
        }

        return RedirectToAction(nameof(Index), new { date = returnDate });
    }
}
