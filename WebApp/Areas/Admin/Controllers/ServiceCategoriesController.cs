using App.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class ServiceCategoriesController : AdminControllerBase
{
    public ServiceCategoriesController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        page = Math.Max(1, page);
        var query = Context.ServiceCategories
            .Include(c => c.ServiceTypes)
            .OrderBy(c => c.Name);
        var totalItems = await query.CountAsync();
        var categories = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new ServiceCategoryIndexViewModel
        {
            Page = new PagedViewModel<ServiceCategoryIndexItem>
            {
                Items = categories.Select(c => new ServiceCategoryIndexItem
                {
                    Id = c.Id,
                    Name = c.Name.Translate() ?? string.Empty,
                    IsActive = c.IsActive,
                    ServiceTypeCount = c.ServiceTypes?.Count ?? 0
                }).ToList(),
                PageNumber = page,
                TotalItems = totalItems,
                TotalPages = (int) Math.Ceiling(totalItems / (double) PageSize)
            }
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var category = await Context.ServiceCategories.Include(c => c.Services!).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        return View(category);
    }

    public IActionResult Create()
    {
        return View(new ServiceCategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceCategoryFormViewModel vm)
    {
        ValidateRequiredTranslation(vm.Name, nameof(vm.Name));
        if (!ModelState.IsValid) return View(vm);

        Context.ServiceCategories.Add(new ServiceCategory
        {
            Name = BuildLangStr(vm.Name.En, vm.Name.Et),
            Description = BuildLangStr(vm.Description.En, vm.Description.Et),
            IsActive = vm.IsActive
        });
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ServiceCategoryCreated"));
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await Context.ServiceCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();

        return View(new ServiceCategoryFormViewModel
        {
            Id = category.Id,
            Name = new TranslationFormViewModel
            {
                En = category.Name.Translate("en") ?? string.Empty,
                Et = category.Name.Translate("et") ?? string.Empty
            },
            Description = new TranslationFormViewModel
            {
                En = category.Description?.Translate("en") ?? string.Empty,
                Et = category.Description?.Translate("et") ?? string.Empty
            },
            IsActive = category.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceCategoryFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        ValidateRequiredTranslation(vm.Name, nameof(vm.Name));
        if (!ModelState.IsValid) return View(vm);

        var category = await Context.ServiceCategories.AsTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();

        category.Name = BuildLangStr(vm.Name.En, vm.Name.Et);
        category.Description = BuildLangStr(vm.Description.En, vm.Description.Et);
        category.IsActive = vm.IsActive;

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("ServiceCategoryUpdated"));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var category = await Context.ServiceCategories.AsTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();

        if (category.IsActive && await Context.ServiceTypes.AnyAsync(t => t.ServiceCategoryId == id && t.IsActive))
        {
            SetErrorMessage(T("CannotDeactivateCategoryWithActiveTypes"));
            return RedirectToAction(nameof(Index));
        }

        category.IsActive = !category.IsActive;
        await Context.SaveChangesAsync();
        SetSuccessMessage(category.IsActive ? T("ServiceCategoryActivated") : T("ServiceCategoryDeactivated"));
        return RedirectToAction(nameof(Index));
    }

    private void ValidateRequiredTranslation(TranslationFormViewModel translation, string prefix)
    {
        if (string.IsNullOrWhiteSpace(translation.En))
        {
            ModelState.AddModelError($"{prefix}.En", T("RequiredField"));
        }

        if (string.IsNullOrWhiteSpace(translation.Et))
        {
            ModelState.AddModelError($"{prefix}.Et", T("RequiredField"));
        }
    }
}
