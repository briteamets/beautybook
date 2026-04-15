using App.DAL.EF;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.SalonOwner.ViewModels;

namespace WebApp.Areas.SalonOwner.Controllers;

public class ServicesController : SalonOwnerControllerBase
{
    public ServicesController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Index(Guid? salonId)
    {
        var ownedSalonIds = await GetOwnedSalonIdsAsync();
        if (salonId.HasValue && !ownedSalonIds.Contains(salonId.Value)) return NotFound();

        var selectedSalonIds = salonId.HasValue ? new List<Guid> { salonId.Value } : ownedSalonIds;
        var staff = await Context.SalonStaff
            .Include(st => st.Salon)
            .Include(st => st.BeautyProvider!)
            .ThenInclude(p => p.Services!)
            .ThenInclude(s => s.ServiceCategory)
            .Include(st => st.BeautyProvider!)
            .ThenInclude(p => p.Services!)
            .ThenInclude(s => s.ServiceType)
            .Where(st => selectedSalonIds.Contains(st.SalonId) && st.To == null)
            .ToListAsync();

        var model = new SalonOwnerServiceIndexViewModel
        {
            SalonId = salonId,
            SalonOptions = await GetSalonOptionsAsync(salonId),
            Services = staff
                .SelectMany(st => st.BeautyProvider?.Services?
                    .Where(service => service.To == null)
                    .Select(service => new SalonOwnerServiceItemViewModel
                    {
                        SalonName = Translate(st.Salon?.Name),
                        ProviderName = st.BeautyProvider.ProfileName,
                        Name = Translate(service.Name),
                        CategoryName = Translate(service.ServiceCategory?.Name),
                        ServiceTypeName = Translate(service.ServiceType?.Name),
                        Price = service.Price,
                        DurationMinutes = service.DurationMinutes,
                        IsActive = service.IsActive
                    }) ?? Enumerable.Empty<SalonOwnerServiceItemViewModel>())
                .OrderBy(service => service.SalonName)
                .ThenBy(service => service.ProviderName)
                .ThenBy(service => service.Name)
                .ToList()
        };

        return View(model);
    }
}
