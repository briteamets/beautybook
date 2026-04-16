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

    public async Task<IActionResult> Index(Guid? categoryId, bool? isActive, int page = 1)
    {
        page = Math.Max(1, page);
        var query = Context.ServiceTypes
            .Include(t => t.ServiceCategory)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.ServiceCategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var totalItems = await query.CountAsync();
        var serviceTypes = await query.OrderBy(t => t.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new ServiceTypeIndexViewModel
        {
            Filter = new ServiceTypeIndexFilterViewModel
            {
                CategoryId = categoryId,
                IsActive = isActive
            },
            Categories = await GetCategoryOptions(categoryId),
            Page = new PagedViewModel<ServiceTypeIndexItem>
            {
                Items = serviceTypes.Select(t => new ServiceTypeIndexItem
                {
                    Id = t.Id,
                    Name = t.Name.Translate() ?? string.Empty,
                    CategoryName = t.ServiceCategory?.Name.Translate() ?? string.Empty,
                    IsActive = t.IsActive
                }).ToList(),
                PageNumber = page,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize)
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
