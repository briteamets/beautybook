using App.DAL.EF;
using App.Domain;
using Base.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class ProviderManageController : ClientControllerBase
{
    public ProviderManageController(AppDbContext context) : base(context)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModel();
        if (vm == null)
        {
            SetErrorMessage(T("NoProviderProfile"));
            return RedirectToAction("Browse", "Home");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService([Bind(Prefix = "NewService")] ProviderServiceCreateViewModel newService)
    {
        var provider = await GetCurrentProvider();
        if (provider == null)
        {
            SetErrorMessage(T("NoProviderProfile"));
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildViewModel(newService: newService);
            return View("Index", vm);
        }

        var categoryExists = await Context.ServiceCategories.AnyAsync(c => c.Id == newService.ServiceCategoryId);
        if (!categoryExists)
        {
            SetErrorMessage(T("SelectedCategoryNotFound"));
            return RedirectToAction(nameof(Index));
        }

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = newService.ServiceCategoryId,
            Name = BuildLangStr(newService.NameEn, newService.NameEt),
            Description = BuildOptionalLangStr(newService.DescriptionEn, newService.DescriptionEt),
            Price = newService.Price,
            DurationMinutes = newService.DurationMinutes,
            IsActive = true,
            From = DateTime.UtcNow
        };

        Context.Services.Add(service);
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ServiceAdded"));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateService(Guid id)
    {
        var provider = await GetCurrentProvider();
        if (provider == null) return RedirectToAction(nameof(Index));

        var service = await Context.Services
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.BeautyProviderId == provider.Id && s.To == null);
        if (service == null) return NotFound();

        service.IsActive = false;
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ServiceDeactivated"));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAvailability([Bind(Prefix = "NewAvailability")] ProviderAvailabilityCreateViewModel newAvailability)
    {
        var provider = await GetCurrentProvider();
        if (provider == null)
        {
            SetErrorMessage(T("NoProviderProfile"));
            return RedirectToAction(nameof(Index));
        }

        if (newAvailability.StartTime >= newAvailability.EndTime)
        {
            ModelState.AddModelError("NewAvailability.EndTime", T("EndTimeAfterStartTime"));
        }

        if (newAvailability.Date == DateOnly.FromDateTime(DateTime.UtcNow) &&
            newAvailability.StartTime <= TimeOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError("NewAvailability.Date", T("ChooseFutureTimeSlot"));
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildViewModel(newAvailability: newAvailability);
            return View("Index", vm);
        }

        var exists = await Context.Availabilities.AnyAsync(a =>
            a.BeautyProviderId == provider.Id &&
            a.Date == newAvailability.Date &&
            a.StartTime == newAvailability.StartTime &&
            a.EndTime == newAvailability.EndTime &&
            a.To == null);
        if (exists)
        {
            SetErrorMessage(T("AvailabilityAlreadyExists"));
            return RedirectToAction(nameof(Index));
        }

        Context.Availabilities.Add(new Availability
        {
            BeautyProviderId = provider.Id,
            Date = newAvailability.Date,
            StartTime = newAvailability.StartTime,
            EndTime = newAvailability.EndTime,
            IsBooked = false,
            From = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("AvailabilityAdded"));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAvailability(Guid id)
    {
        var provider = await GetCurrentProvider();
        if (provider == null) return RedirectToAction(nameof(Index));

        var availability = await Context.Availabilities
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.BeautyProviderId == provider.Id && a.To == null);
        if (availability == null) return NotFound();
        if (availability.IsBooked)
        {
            SetErrorMessage(T("BookedSlotsCannotBeRemoved"));
            return RedirectToAction(nameof(Index));
        }

        availability.To = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("AvailabilityRemoved"));
        return RedirectToAction(nameof(Index));
    }

    private async Task<BeautyProvider?> GetCurrentProvider()
    {
        var userId = GetCurrentUserId();
        return await Context.BeautyProviders.FirstOrDefaultAsync(p => p.UserId == userId && p.To == null);
    }

    private async Task<ProviderManageViewModel?> BuildViewModel(
        ProviderServiceCreateViewModel? newService = null,
        ProviderAvailabilityCreateViewModel? newAvailability = null)
    {
        var provider = await Context.BeautyProviders
            .Include(p => p.Services!.Where(s => s.To == null))
                .ThenInclude(s => s.ServiceCategory)
            .Include(p => p.Availabilities!.Where(a => a.To == null))
            .FirstOrDefaultAsync(p => p.UserId == GetCurrentUserId() && p.To == null);
        if (provider == null) return null;

        var categories = await Context.ServiceCategories
            .OrderBy(c => c.Name)
            .ToListAsync();

        return new ProviderManageViewModel
        {
            ProviderId = provider.Id,
            ProfileName = provider.ProfileName,
            Categories = categories.Select(c => new CategoryOptionViewModel
            {
                Id = c.Id,
                Name = c.Name.Translate() ?? string.Empty
            }).ToList(),
            Services = provider.Services?
                .OrderBy(s => s.Name.Translate())
                .Select(s => new ServiceManageItemViewModel
                {
                    Id = s.Id,
                    Name = s.Name.Translate() ?? string.Empty,
                    CategoryName = s.ServiceCategory?.Name.Translate() ?? string.Empty,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    IsActive = s.IsActive
                }).ToList() ?? [],
            Availabilities = provider.Availabilities?
                .Where(a => a.Date > DateOnly.FromDateTime(DateTime.UtcNow) ||
                            (a.Date == DateOnly.FromDateTime(DateTime.UtcNow) &&
                             a.StartTime > TimeOnly.FromDateTime(DateTime.UtcNow)))
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .Select(a => new AvailabilityManageItemViewModel
                {
                    Id = a.Id,
                    Date = a.Date,
                    StartTime = a.StartTime.ToString("HH:mm"),
                    EndTime = a.EndTime.ToString("HH:mm"),
                    IsBooked = a.IsBooked
                }).ToList() ?? [],
            NewService = newService ?? new ProviderServiceCreateViewModel(),
            NewAvailability = newAvailability ?? new ProviderAvailabilityCreateViewModel()
        };
    }

    private static LangStr BuildLangStr(string en, string et)
    {
        var langStr = new LangStr();
        langStr["en"] = en.Trim();
        langStr["et"] = et.Trim();
        return langStr;
    }

    private static LangStr? BuildOptionalLangStr(string? en, string? et)
    {
        var hasEn = !string.IsNullOrWhiteSpace(en);
        var hasEt = !string.IsNullOrWhiteSpace(et);
        if (!hasEn && !hasEt) return null;

        var langStr = new LangStr();
        if (hasEn) langStr["en"] = en!.Trim();
        if (hasEt) langStr["et"] = et!.Trim();
        if (!hasEn && hasEt) langStr["en"] = et!.Trim();
        if (!hasEt && hasEn) langStr["et"] = en!.Trim();
        return langStr;
    }
}
