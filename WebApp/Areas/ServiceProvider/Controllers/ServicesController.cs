using App.DAL.EF;
using App.Domain;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.ServiceProvider.ViewModels;
using WebApp;

namespace WebApp.Areas.ServiceProvider.Controllers;

public class ServicesController : ServiceProviderControllerBase
{
    public ServicesController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index()
    {
        var model = await BuildIndexViewModelAsync();
        if (model == null) return RedirectToAction("Index", "Dashboard");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService([Bind(Prefix = "NewService")] ProviderServiceFormViewModel model)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return RedirectToAction("Index", "Dashboard");
        }

        if (!await Context.ServiceCategories.AnyAsync(c => c.Id == model.ServiceCategoryId))
        {
            ModelState.AddModelError("NewService.ServiceCategoryId", T("SelectedCategoryNotFound"));
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildIndexViewModelAsync(newService: model);
            return View("Index", vm);
        }

        Context.Services.Add(new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = model.ServiceCategoryId,
            Name = BuildLangStr(model.NameEn, model.NameEt),
            Description = BuildOptionalLangStr(model.DescriptionEn, model.DescriptionEt),
            Price = model.Price,
            DurationMinutes = model.DurationMinutes,
            IsActive = true,
            From = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
        Success("ServiceCreated");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAvailability([Bind(Prefix = "NewAvailability")] ProviderAvailabilityFormViewModel model)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return RedirectToAction("Index", "Dashboard");
        }

        ValidateAvailabilityRange(model);

        if (!ModelState.IsValid)
        {
            var vm = await BuildIndexViewModelAsync(newAvailability: model);
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
            ModelState.AddModelError("NewAvailability.Date", T("AvailabilityAlreadyExists"));
            var vm = await BuildIndexViewModelAsync(newAvailability: model);
            return View("Index", vm);
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
    public async Task<IActionResult> DeleteAvailability(Guid id)
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

    public async Task<IActionResult> Details(Guid id)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return NotFound();

        var service = await Context.Services
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Id == id && s.BeautyProviderId == provider.Id);

        if (service == null) return NotFound();

        return View(new ProviderServiceDetailsViewModel
        {
            Id = service.Id,
            Name = Translate(service.Name),
            Description = Translate(service.Description),
            CategoryName = Translate(service.ServiceCategory!.Name),
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        });
    }

    public async Task<IActionResult> Create()
    {
        var model = new ProviderServiceFormViewModel();
        await LoadCategoriesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderServiceFormViewModel model)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            await LoadCategoriesAsync(model);
            return View(model);
        }

        if (!await Context.ServiceCategories.AnyAsync(c => c.Id == model.ServiceCategoryId))
        {
            ModelState.AddModelError(nameof(model.ServiceCategoryId), T("SelectedCategoryNotFound"));
        }

        if (model.AddTimeRange)
        {
            ValidateTimeRange(model);
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        var service = new Service
        {
            BeautyProviderId = provider.Id,
            ServiceCategoryId = model.ServiceCategoryId,
            Name = BuildLangStr(model.NameEn, model.NameEt),
            Description = BuildOptionalLangStr(model.DescriptionEn, model.DescriptionEt),
            Price = model.Price,
            DurationMinutes = model.DurationMinutes,
            IsActive = model.IsActive,
            From = DateTime.UtcNow
        };
        Context.Services.Add(service);

        if (model.AddTimeRange)
        {
            var exists = await Context.Availabilities.AnyAsync(a =>
                a.BeautyProviderId == provider.Id &&
                a.Date == model.TimeRangeDate &&
                a.StartTime == model.TimeRangeStartTime &&
                a.EndTime == model.TimeRangeEndTime &&
                a.To == null);

            if (exists)
            {
                ModelState.AddModelError(nameof(model.TimeRangeDate), T("AvailabilityAlreadyExists"));
                await LoadCategoriesAsync(model);
                return View(model);
            }

            Context.Availabilities.Add(new Availability
            {
                BeautyProviderId = provider.Id,
                Date = model.TimeRangeDate,
                StartTime = model.TimeRangeStartTime,
                EndTime = model.TimeRangeEndTime,
                IsBooked = false,
                From = DateTime.UtcNow
            });
        }

        await Context.SaveChangesAsync();
        Success(model.AddTimeRange ? "ServiceAndTimeRangeCreated" : "ServiceCreated");
        return model.AddTimeRange
            ? RedirectToAction("Index", "Availability")
            : RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return NotFound();

        var service = await Context.Services.FirstOrDefaultAsync(s => s.Id == id && s.BeautyProviderId == provider.Id);
        if (service == null) return NotFound();

        var model = new ProviderServiceFormViewModel
        {
            Id = service.Id,
            ServiceCategoryId = service.ServiceCategoryId,
            NameEn = service.Name.Translate("en") ?? string.Empty,
            NameEt = service.Name.Translate("et") ?? string.Empty,
            DescriptionEn = service.Description?.Translate("en"),
            DescriptionEt = service.Description?.Translate("et"),
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        };
        await LoadCategoriesAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProviderServiceFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        var provider = await GetCurrentProviderAsync();
        if (provider == null) return NotFound();

        var service = await Context.Services.FirstOrDefaultAsync(s => s.Id == id && s.BeautyProviderId == provider.Id);
        if (service == null) return NotFound();

        if (!await Context.ServiceCategories.AnyAsync(c => c.Id == model.ServiceCategoryId))
        {
            ModelState.AddModelError(nameof(model.ServiceCategoryId), T("SelectedCategoryNotFound"));
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        service.ServiceCategoryId = model.ServiceCategoryId;
        service.Name = BuildLangStr(model.NameEn, model.NameEt);
        service.Description = BuildOptionalLangStr(model.DescriptionEn, model.DescriptionEt);
        service.Price = model.Price;
        service.DurationMinutes = model.DurationMinutes;
        service.IsActive = model.IsActive;
        service.To = model.IsActive ? null : DateTime.UtcNow;

        await Context.SaveChangesAsync();
        Success("ServiceUpdated");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return NotFound();

        var service = await Context.Services
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Id == id && s.BeautyProviderId == provider.Id);

        if (service == null) return NotFound();

        return View(new ProviderServiceDetailsViewModel
        {
            Id = service.Id,
            Name = Translate(service.Name),
            Description = Translate(service.Description),
            CategoryName = Translate(service.ServiceCategory!.Name),
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return NotFound();

        var service = await Context.Services.FirstOrDefaultAsync(s => s.Id == id && s.BeautyProviderId == provider.Id);
        if (service == null) return NotFound();

        service.IsActive = false;
        service.To = DateTime.UtcNow;

        await Context.SaveChangesAsync();
        Success("ServiceDeleted");
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProviderServiceIndexViewModel?> BuildIndexViewModelAsync(
        ProviderServiceFormViewModel? newService = null,
        ProviderAvailabilityFormViewModel? newAvailability = null)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null)
        {
            Error("NoProviderProfile");
            return null;
        }

        var services = await Context.Services
            .Include(s => s.ServiceCategory)
            .Where(s => s.BeautyProviderId == provider.Id && s.To == null)
            .ToListAsync();

        var categories = await Context.ServiceCategories.ToListAsync();
        var nowDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var availabilities = await Context.Availabilities
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

        var categoryOptions = categories
            .Select(c => new ServiceCategoryOptionViewModel
            {
                Id = c.Id,
                Name = Translate(c.Name)
            })
            .OrderBy(c => c.Name)
            .ToList();

        var serviceForm = newService ?? new ProviderServiceFormViewModel();
        serviceForm.Categories = categoryOptions;

        return new ProviderServiceIndexViewModel
        {
            Categories = categoryOptions,
            Services = services
                .Select(s => new ProviderServiceListItemViewModel
                {
                    Id = s.Id,
                    Name = Translate(s.Name),
                    CategoryName = Translate(s.ServiceCategory!.Name),
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    IsActive = s.IsActive
                })
                .OrderBy(s => s.Name)
                .ToList(),
            Availabilities = availabilities,
            NewService = serviceForm,
            NewAvailability = newAvailability ?? new ProviderAvailabilityFormViewModel()
        };
    }

    private void ValidateTimeRange(ProviderServiceFormViewModel model)
    {
        if (model.TimeRangeStartTime >= model.TimeRangeEndTime)
        {
            ModelState.AddModelError(nameof(model.TimeRangeEndTime), T("EndTimeAfterStartTime"));
        }

        if (model.TimeRangeDate < DateOnly.FromDateTime(DateTime.UtcNow) ||
            (model.TimeRangeDate == DateOnly.FromDateTime(DateTime.UtcNow) &&
             model.TimeRangeStartTime <= TimeOnly.FromDateTime(DateTime.UtcNow)))
        {
            ModelState.AddModelError(nameof(model.TimeRangeDate), T("ChooseFutureTimeSlot"));
        }
    }

    private void ValidateAvailabilityRange(ProviderAvailabilityFormViewModel model)
    {
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
    }
}
