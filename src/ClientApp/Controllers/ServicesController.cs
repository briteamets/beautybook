using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class ServicesController : Controller
{
    private readonly ServicesApiService _servicesApiService;
    private readonly ProvidersApiService _providersApiService;
    private readonly CategoriesApiService _categoriesApiService;

    public ServicesController(
        ServicesApiService servicesApiService,
        ProvidersApiService providersApiService,
        CategoriesApiService categoriesApiService)
    {
        _servicesApiService = servicesApiService;
        _providersApiService = providersApiService;
        _categoriesApiService = categoriesApiService;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _servicesApiService.GetAllAsync();
        return View(new ServiceIndexViewModel
        {
            Services = services.Select(ToListItem).ToList()
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var service = await _servicesApiService.GetAsync(id);
        if (service == null) return NotFound();
        return View(ToListItem(service));
    }

    public async Task<IActionResult> Create()
    {
        var model = new ServiceFormViewModel();
        await LoadOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(model);
            return View(model);
        }

        var created = await _servicesApiService.CreateAsync(ToCreateRequest(model));
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var service = await _servicesApiService.GetAsync(id);
        if (service == null) return NotFound();

        var model = new ServiceFormViewModel
        {
            Id = service.Id,
            ServiceCategoryId = service.CategoryId,
            NameEn = service.Name,
            NameEt = service.Name,
            DescriptionEn = service.Description,
            DescriptionEt = service.Description,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        };
        await LoadOptionsAsync(model);
        return View(model);
    }

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

    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _servicesApiService.GetAsync(id);
        if (service == null) return NotFound();
        return View(ToListItem(service));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _servicesApiService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
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
        Name = new Dictionary<string, string> { ["en"] = model.NameEn, ["et"] = model.NameEt },
        Description = new Dictionary<string, string>
        {
            ["en"] = model.DescriptionEn ?? string.Empty,
            ["et"] = model.DescriptionEt ?? string.Empty
        },
        Price = model.Price,
        DurationMinutes = model.DurationMinutes,
        IsActive = model.IsActive
    };

    private static ServiceUpdateRequest ToUpdateRequest(ServiceFormViewModel model) => new()
    {
        ServiceCategoryId = model.ServiceCategoryId,
        Name = new Dictionary<string, string> { ["en"] = model.NameEn, ["et"] = model.NameEt },
        Description = new Dictionary<string, string>
        {
            ["en"] = model.DescriptionEn ?? string.Empty,
            ["et"] = model.DescriptionEt ?? string.Empty
        },
        Price = model.Price,
        DurationMinutes = model.DurationMinutes,
        IsActive = model.IsActive
    };
}
