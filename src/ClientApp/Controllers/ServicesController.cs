using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

public class ServicesController : Controller
{
    private readonly ServicesApiService _servicesApiService;
    private readonly ProvidersApiService _providersApiService;
    private readonly CategoriesApiService _categoriesApiService;
    private readonly ServiceTypesApiService _serviceTypesApiService;

    public ServicesController(
        ServicesApiService servicesApiService,
        ProvidersApiService providersApiService,
        CategoriesApiService categoriesApiService,
        ServiceTypesApiService serviceTypesApiService)
    {
        _servicesApiService = servicesApiService;
        _providersApiService = providersApiService;
        _categoriesApiService = categoriesApiService;
        _serviceTypesApiService = serviceTypesApiService;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _servicesApiService.GetPublicAsync();
        return View(new ServiceIndexViewModel
        {
            Services = services.Select(ToListItem).ToList()
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var service = await _servicesApiService.GetPublicAsync(id);
        if (service == null) return NotFound();
        return View(ToListItem(service));
    }

    [ClientAuthorize]
    public async Task<IActionResult> Create()
    {
        var model = new ServiceFormViewModel();
        await LoadOptionsAsync(model);
        return View(model);
    }

    [ClientAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(model);
            return View(model);
        }

        try
        {
            var created = await _servicesApiService.CreateAsync(ToCreateRequest(model));
            return RedirectToAction(nameof(Details), new { id = created!.Id });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            ModelState.AddModelError(string.Empty, "You can only create services for your own provider profile. Use My Services instead.");
            await LoadOptionsAsync(model);
            return View(model);
        }
    }

    [ClientAuthorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        var service = await _servicesApiService.GetAsync(id);
        if (service == null) return NotFound();

        var model = new ServiceFormViewModel
        {
            Id = service.Id,
            ServiceCategoryId = service.CategoryId,
            NameEn = service.NameEn,
            NameEt = service.NameEt,
            DescriptionEn = service.DescriptionEn,
            DescriptionEt = service.DescriptionEt,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        };
        await LoadOptionsAsync(model);
        return View(model);
    }

    [ClientAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(model);
            return View(model);
        }

        await _servicesApiService.UpdateAsync(id, ToUpdateRequest(model));
        return RedirectToAction(nameof(Details), new { id });
    }

    [ClientAuthorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _servicesApiService.GetAsync(id);
        if (service == null) return NotFound();
        return View(ToListItem(service));
    }

    [ClientAuthorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _servicesApiService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // ── Provider self-service ──────────────────────────────────────────────────

    [ClientAuthorize]
    public async Task<IActionResult> MyServices()
    {
        BeautyProviderResponse? provider;
        try { provider = await _providersApiService.GetMineAsync(); }
        catch (HttpRequestException) { provider = null; }

        if (provider == null)
        {
            TempData["SetupRequired"] = "true";
            return RedirectToAction("EditProfile", "Providers");
        }

        ICollection<ServiceResponse> services;
        try { services = await _servicesApiService.GetMyAsync(); }
        catch (HttpRequestException) { services = []; }

        return View(new MyServicesViewModel
        {
            HasProviderProfile = true,
            ProviderId = provider.Id,
            Services = services.Select(s => new ServiceListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                CategoryName = s.CategoryName,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            }).ToList()
        });
    }

    [ClientAuthorize]
    public async Task<IActionResult> MyServiceCreate(Guid providerId)
    {
        var model = new ServiceFormViewModel { BeautyProviderId = providerId };
        await LoadCategoriesAsync(model);
        return View(model);
    }

    [ClientAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyServiceCreate(ServiceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        await _servicesApiService.CreateAsync(ToCreateRequest(model));
        return RedirectToAction(nameof(MyServices));
    }

    [ClientAuthorize]
    public async Task<IActionResult> MyServiceEdit(Guid id)
    {
        var service = await _servicesApiService.GetAsync(id);
        if (service == null) return NotFound();

        var model = new ServiceFormViewModel
        {
            Id = service.Id,
            ServiceCategoryId = service.CategoryId,
            ServiceTypeId = service.ServiceTypeId,
            NameEn = service.NameEn,
            NameEt = service.NameEt,
            DescriptionEn = service.DescriptionEn,
            DescriptionEt = service.DescriptionEt,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        };
        await LoadCategoriesAsync(model);
        return View(model);
    }

    [ClientAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyServiceEdit(Guid id, ServiceFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        await _servicesApiService.UpdateAsync(id, ToUpdateRequest(model));
        return RedirectToAction(nameof(MyServices));
    }

    [ClientAuthorize]
    public async Task<IActionResult> MyServiceDelete(Guid id)
    {
        var service = await _servicesApiService.GetAsync(id);
        if (service == null) return NotFound();
        return View(ToListItem(service));
    }

    [ClientAuthorize]
    [HttpPost, ActionName("MyServiceDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyServiceDeleteConfirmed(Guid id)
    {
        await _servicesApiService.DeleteAsync(id);
        return RedirectToAction(nameof(MyServices));
    }

    private async Task LoadOptionsAsync(ServiceFormViewModel model)
    {
        model.Providers = (await _providersApiService.GetAllAsync())
            .Select(p => new SelectOptionViewModel { Id = p.Id, Label = p.ProfileName })
            .ToList();
        model.Categories = (await _categoriesApiService.GetAllAsync())
            .Select(c => new SelectOptionViewModel { Id = c.Id, Label = c.Name })
            .ToList();
    }

    private async Task LoadCategoriesAsync(ServiceFormViewModel model)
    {
        var lang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        var categories = (await _categoriesApiService.GetAllAsync()).ToList();
        var serviceTypes = (await _serviceTypesApiService.GetAsync()).ToList();

        model.Categories = categories
            .Select(c => new SelectOptionViewModel { Id = c.Id, Label = c.Name })
            .ToList();

        model.CategoryPresets = categories
            .ToDictionary(
                cat => cat.Id,
                cat =>
                {
                    if (!ServiceCategoryPresets.ByCategory.TryGetValue(cat.NameEn, out var presets))
                        return new List<ServicePresetOption>();

                    return presets.Select(p => new ServicePresetOption
                    {
                        NameEn = p.En,
                        NameEt = p.Et,
                        IsOther = p.IsOther,
                        ServiceTypeId = serviceTypes
                            .FirstOrDefault(t => string.Equals(t.NameEn, p.En, StringComparison.OrdinalIgnoreCase))?.Id
                    }).ToList();
                });
    }

    private static ServiceListItemViewModel ToListItem(ServiceResponse service) => new()
    {
        Id = service.Id,
        Name = service.Name,
        Description = service.Description,
        CategoryName = service.CategoryName,
        Price = service.Price,
        DurationMinutes = service.DurationMinutes,
        IsActive = service.IsActive
    };

    private static ServiceCreateRequest ToCreateRequest(ServiceFormViewModel model) => new()
    {
        BeautyProviderId = model.BeautyProviderId,
        ServiceCategoryId = model.ServiceCategoryId,
        ServiceTypeId = model.ServiceTypeId,
        Name = new Dictionary<string, string> { ["en"] = model.NameEn, ["et"] = model.NameEt },
        Description = string.IsNullOrWhiteSpace(model.DescriptionEn) && string.IsNullOrWhiteSpace(model.DescriptionEt) ? null :
            new Dictionary<string, string> { ["en"] = model.DescriptionEn ?? string.Empty, ["et"] = model.DescriptionEt ?? string.Empty },
        Price = model.Price,
        DurationMinutes = model.DurationMinutes,
        IsActive = model.IsActive
    };

    private static ServiceUpdateRequest ToUpdateRequest(ServiceFormViewModel model) => new()
    {
        ServiceCategoryId = model.ServiceCategoryId,
        ServiceTypeId = model.ServiceTypeId,
        Name = new Dictionary<string, string> { ["en"] = model.NameEn, ["et"] = model.NameEt },
        Description = string.IsNullOrWhiteSpace(model.DescriptionEn) && string.IsNullOrWhiteSpace(model.DescriptionEt) ? null :
            new Dictionary<string, string> { ["en"] = model.DescriptionEn ?? string.Empty, ["et"] = model.DescriptionEt ?? string.Empty },
        Price = model.Price,
        DurationMinutes = model.DurationMinutes,
        IsActive = model.IsActive
    };
}
