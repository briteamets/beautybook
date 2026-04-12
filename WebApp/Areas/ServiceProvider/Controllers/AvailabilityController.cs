using App.DAL.EF;
using App.Domain;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.ServiceProvider.ViewModels;
using WebApp;

namespace WebApp.Areas.ServiceProvider.Controllers;

public class AvailabilityController : ServiceProviderControllerBase
{
    public AvailabilityController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index()
    {
        var model = await BuildViewModelAsync();
        if (model == null) return RedirectToAction("Index", "Dashboard");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewAvailability")] ProviderAvailabilityFormViewModel model)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return RedirectToAction("Index", "Dashboard");
        }

        if (model.StartTime >= model.EndTime)
        {
            ModelState.AddModelError("NewAvailability.EndTime", T("EndTimeAfterStartTime"));
        }

        if (model.Date < DateOnly.FromDateTime(DateTime.UtcNow) ||
            (model.Date == DateOnly.FromDateTime(DateTime.UtcNow) &&
             model.StartTime <= TimeOnly.FromDateTime(DateTime.UtcNow)))
        {
            ModelState.AddModelError("NewAvailability.Date", T("ChooseFutureTimeSlot"));
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildViewModelAsync(model);
            return View("Index", vm);
        }

        var exists = await Context.Availabilities.AnyAsync(a =>
            a.BeautyProviderId == provider.Id &&
            a.Date == model.Date &&
            a.StartTime == model.StartTime &&
            a.EndTime == model.EndTime &&
            a.To == null);

        if (exists)
        {
            Error("AvailabilityAlreadyExists");
            return RedirectToAction(nameof(Index));
        }

        Context.Availabilities.Add(new Availability
        {
            BeautyProviderId = provider.Id,
            Date = model.Date,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            IsBooked = false,
            From = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
        Success("AvailabilityAdded");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return RedirectToAction("Index", "Dashboard");

        var availability = await Context.Availabilities
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.BeautyProviderId == provider.Id && a.To == null);

        if (availability == null) return NotFound();
        if (availability.IsBooked)
        {
            Error("BookedSlotsCannotBeRemoved");
            return RedirectToAction(nameof(Index));
        }

        availability.To = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        Success("AvailabilityRemoved");
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProviderAvailabilityIndexViewModel?> BuildViewModelAsync(ProviderAvailabilityFormViewModel? form = null)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return null;
        }

        var nowDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);

        var slots = await Context.Availabilities
            .Where(a => a.BeautyProviderId == provider.Id &&
                        a.To == null &&
                        (a.Date > nowDate || (a.Date == nowDate && a.StartTime > nowTime)))
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(a => new ProviderAvailabilityListItemViewModel
            {
                Id = a.Id,
                Date = a.Date,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                IsBooked = a.IsBooked
            })
            .ToListAsync();

        return new ProviderAvailabilityIndexViewModel
        {
            Availabilities = slots,
            NewAvailability = form ?? new ProviderAvailabilityFormViewModel()
        };
    }
}
