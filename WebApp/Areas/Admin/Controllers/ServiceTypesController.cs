using App.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class ServiceTypesController : AdminControllerBase
{
    public ServiceTypesController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Index(
        string tab = "catalog",
        Guid? ct_categoryId = null, bool? ct_isActive = null, int ct_page = 1,
        Guid? svc_providerId = null, Guid? svc_categoryId = null, bool? svc_isActive = null, int svc_page = 1,
        int cat_page = 1)
    {
        ct_page = Math.Max(1, ct_page);
        svc_page = Math.Max(1, svc_page);
        cat_page = Math.Max(1, cat_page);

        // --- Catalog tab data ---
        var catalogQuery = Context.ServiceTypes
            .Include(t => t.ServiceCategory)
            .AsQueryable();

        if (ct_categoryId.HasValue)
            catalogQuery = catalogQuery.Where(t => t.ServiceCategoryId == ct_categoryId.Value);
        if (ct_isActive.HasValue)
            catalogQuery = catalogQuery.Where(t => t.IsActive == ct_isActive.Value);

        var catalogTotal = await catalogQuery.CountAsync();
        var catalogItems = await catalogQuery.OrderBy(t => t.Name)
            .Skip((ct_page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // --- Provider Services tab data ---
        var svcQuery = Context.Services
            .Include(s => s.BeautyProvider)
            .Include(s => s.ServiceCategory)
            .AsQueryable();

        if (svc_providerId.HasValue)
            svcQuery = svcQuery.Where(s => s.BeautyProviderId == svc_providerId.Value);
        if (svc_categoryId.HasValue)
            svcQuery = svcQuery.Where(s => s.ServiceCategoryId == svc_categoryId.Value);
        if (svc_isActive.HasValue)
            svcQuery = svcQuery.Where(s => s.IsActive == svc_isActive.Value);

        var svcTotal = await svcQuery.CountAsync();
        var svcItems = await svcQuery
            .OrderBy(s => s.BeautyProvider!.ProfileName)
            .ThenBy(s => s.Name)
            .Skip((svc_page - 1) * PageSize)
            .Take(PageSize)
            .Select(s => new AdminServiceIndexItem
            {
                Id = s.Id,
                ServiceName = s.Name.Translate() ?? string.Empty,
                CategoryName = s.ServiceCategory!.Name.Translate() ?? string.Empty,
                ProviderName = s.BeautyProvider!.ProfileName,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            })
            .ToListAsync();

        var providers = await Context.BeautyProviders
            .OrderBy(p => p.ProfileName)
            .Select(p => new SelectListItem(p.ProfileName, p.Id.ToString(), p.Id == svc_providerId))
            .ToListAsync();

        var allCategories = await Context.ServiceCategories
            .Include(c => c.ServiceTypes!)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var catTotal = allCategories.Count;
        var catItems = allCategories
            .Skip((cat_page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new ServiceCategoryIndexItem
            {
                Id = c.Id,
                Name = c.Name.Translate() ?? string.Empty,
                IsActive = c.IsActive,
                ServiceTypeCount = c.ServiceTypes?.Count ?? 0
            })
            .ToList();

        return View(new AdminServicesPageViewModel
        {
            ActiveTab = tab,
            Catalog = new ServiceTypeIndexViewModel
            {
                Filter = new ServiceTypeIndexFilterViewModel { CategoryId = ct_categoryId, IsActive = ct_isActive },
                Categories = allCategories.Select(c => new SelectListItem(
                    c.Name.Translate() ?? string.Empty, c.Id.ToString(), c.Id == ct_categoryId)).ToList(),
                Page = new PagedViewModel<ServiceTypeIndexItem>
                {
                    Items = catalogItems.Select(t => new ServiceTypeIndexItem
                    {
                        Id = t.Id,
                        Name = t.Name.Translate() ?? string.Empty,
                        CategoryName = t.ServiceCategory?.Name.Translate() ?? string.Empty,
                        IsActive = t.IsActive
                    }).ToList(),
                    PageNumber = ct_page,
                    TotalItems = catalogTotal,
                    TotalPages = (int)Math.Ceiling(catalogTotal / (double)PageSize)
                }
            },
            Services = new AdminServiceIndexViewModel
            {
                Filter = new AdminServiceIndexFilterViewModel
                    { ProviderId = svc_providerId, CategoryId = svc_categoryId, IsActive = svc_isActive },
                Providers = providers,
                Categories = allCategories.Select(c => new SelectListItem(
                    c.Name.Translate() ?? string.Empty, c.Id.ToString(), c.Id == svc_categoryId)).ToList(),
                Page = new PagedViewModel<AdminServiceIndexItem>
                {
                    Items = svcItems,
                    PageNumber = svc_page,
                    TotalItems = svcTotal,
                    TotalPages = (int)Math.Ceiling(svcTotal / (double)PageSize)
                }
            },
            CategoryCatalog = new ServiceCategoryIndexViewModel
            {
                Page = new PagedViewModel<ServiceCategoryIndexItem>
                {
                    Items = catItems,
                    PageNumber = cat_page,
                    TotalItems = catTotal,
                    TotalPages = (int)Math.Ceiling(catTotal / (double)PageSize)
                }
            }
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var serviceType = await Context.ServiceTypes
            .Include(t => t.ServiceCategory)
            .Include(t => t.Services!)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (serviceType == null) return NotFound();

        return View(new ServiceTypeDetailsViewModel
        {
            Id = serviceType.Id,
            NameEn = serviceType.Name.Translate("en") ?? string.Empty,
            NameEt = serviceType.Name.Translate("et") ?? string.Empty,
            DescriptionEn = serviceType.Description?.Translate("en"),
            DescriptionEt = serviceType.Description?.Translate("et"),
            CategoryName = serviceType.ServiceCategory?.Name.Translate() ?? string.Empty,
            IsActive = serviceType.IsActive,
            ServiceCount = serviceType.Services?.Count ?? 0
        });
    }

    public async Task<IActionResult> Create()
    {
        return View(new ServiceTypeFormViewModel
        {
            Categories = await GetCategoryOptions(null),
            FormAction = "Create",
            SubmitLabel = T("Save")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceTypeFormViewModel vm)
    {
        await ValidateForm(vm);
        if (!ModelState.IsValid)
        {
            vm.Categories = await GetCategoryOptions(vm.ServiceCategoryId);
            vm.FormAction = "Create";
            vm.SubmitLabel = T("Save");
            return View(vm);
        }

        Context.ServiceTypes.Add(new ServiceType
        {
            ServiceCategoryId = vm.ServiceCategoryId,
            Name = BuildLangStr(vm.Name.En, vm.Name.Et),
            Description = string.IsNullOrWhiteSpace(vm.Description.En) && string.IsNullOrWhiteSpace(vm.Description.Et)
                ? null
                : BuildLangStr(vm.Description.En, vm.Description.Et),
            IsActive = vm.IsActive
        });

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ServiceTypeCreated"));
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var serviceType = await Context.ServiceTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (serviceType == null) return NotFound();

        return View(new ServiceTypeFormViewModel
        {
            Id = serviceType.Id,
            ServiceCategoryId = serviceType.ServiceCategoryId,
            Name = new TranslationFormViewModel
            {
                En = serviceType.Name.Translate("en") ?? string.Empty,
                Et = serviceType.Name.Translate("et") ?? string.Empty
            },
            Description = new TranslationFormViewModel
            {
                En = serviceType.Description?.Translate("en") ?? string.Empty,
                Et = serviceType.Description?.Translate("et") ?? string.Empty
            },
            IsActive = serviceType.IsActive,
            Categories = await GetCategoryOptions(serviceType.ServiceCategoryId),
            FormAction = "Edit",
            SubmitLabel = T("SaveChanges")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceTypeFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        await ValidateForm(vm);
        if (!ModelState.IsValid)
        {
            vm.Categories = await GetCategoryOptions(vm.ServiceCategoryId);
            vm.FormAction = "Edit";
            vm.SubmitLabel = T("SaveChanges");
            return View(vm);
        }

        var serviceType = await Context.ServiceTypes.AsTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (serviceType == null) return NotFound();

        serviceType.ServiceCategoryId = vm.ServiceCategoryId;
        serviceType.Name = BuildLangStr(vm.Name.En, vm.Name.Et);
        serviceType.Description = string.IsNullOrWhiteSpace(vm.Description.En) && string.IsNullOrWhiteSpace(vm.Description.Et)
            ? null
            : BuildLangStr(vm.Description.En, vm.Description.Et);
        serviceType.IsActive = vm.IsActive;

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ServiceTypeUpdated"));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var serviceType = await Context.ServiceTypes.AsTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (serviceType == null) return NotFound();

        serviceType.IsActive = !serviceType.IsActive;
        await Context.SaveChangesAsync();
        SetSuccessMessage(serviceType.IsActive ? T("ServiceTypeActivated") : T("ServiceTypeDeactivated"));
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var serviceType = await Context.ServiceTypes
            .Include(t => t.ServiceCategory)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (serviceType == null) return NotFound();
        return View(serviceType);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var serviceType = await Context.ServiceTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (serviceType == null) return NotFound();

        if (await Context.Services.AnyAsync(s => s.ServiceTypeId == id))
        {
            SetErrorMessage(T("CannotDeleteServiceTypeWithServices"));
            return RedirectToAction(nameof(Delete), new { id });
        }

        Context.ServiceTypes.Remove(serviceType);
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ServiceTypeDeleted"));
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateForm(ServiceTypeFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name.En))
        {
            ModelState.AddModelError("Name.En", T("RequiredField"));
        }

        if (string.IsNullOrWhiteSpace(vm.Name.Et))
        {
            ModelState.AddModelError("Name.Et", T("RequiredField"));
        }

        if (!await Context.ServiceCategories.AnyAsync(c => c.Id == vm.ServiceCategoryId))
        {
            ModelState.AddModelError(nameof(vm.ServiceCategoryId), T("SelectedCategoryNotFound"));
        }
    }

    private async Task<List<SelectListItem>> GetCategoryOptions(Guid? selectedId)
    {
        return await Context.ServiceCategories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name.Translate() ?? string.Empty,
                Selected = selectedId == c.Id
            })
            .ToListAsync();
    }
}
