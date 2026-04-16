using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class ServicesController : AdminControllerBase
{
    public ServicesController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Index(Guid? providerId, Guid? categoryId, bool? isActive, int page = 1)
    {
        page = Math.Max(1, page);
        var query = Context.Services
            .Include(s => s.BeautyProvider)
            .Include(s => s.ServiceCategory)
            .AsQueryable();

        if (providerId.HasValue)
            query = query.Where(s => s.BeautyProviderId == providerId.Value);
        if (categoryId.HasValue)
            query = query.Where(s => s.ServiceCategoryId == categoryId.Value);
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.BeautyProvider!.ProfileName)
            .ThenBy(s => s.Name)
            .Skip((page - 1) * PageSize)
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
            .Select(p => new SelectListItem(p.ProfileName, p.Id.ToString(), p.Id == providerId))
            .ToListAsync();

        var categories = await Context.ServiceCategories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name.Translate() ?? string.Empty, c.Id.ToString(), c.Id == categoryId))
            .ToListAsync();

        return View(new AdminServiceIndexViewModel
        {
            Filter = new AdminServiceIndexFilterViewModel
            {
                ProviderId = providerId,
                CategoryId = categoryId,
                IsActive = isActive
            },
            Providers = providers,
            Categories = categories,
            Page = new PagedViewModel<AdminServiceIndexItem>
            {
                Items = items,
                PageNumber = page,
                TotalItems = totalItems,
                TotalPages = (int) Math.Ceiling(totalItems / (double) PageSize)
            }
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var service = await Context.Services
            .Include(s => s.BeautyProvider).ThenInclude(p => p!.User)
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (service == null) return NotFound();

        return View(new AdminServiceDetailsViewModel
        {
            Id = service.Id,
            ServiceName = service.Name.Translate() ?? string.Empty,
            CategoryName = service.ServiceCategory?.Name.Translate() ?? string.Empty,
            ProviderName = service.BeautyProvider?.ProfileName ?? string.Empty,
            ProviderId = service.BeautyProviderId,
            ProviderEmail = service.BeautyProvider?.User?.Email,
            Description = service.Description?.Translate(),
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive,
            From = service.From,
            To = service.To
        });
    }
}
