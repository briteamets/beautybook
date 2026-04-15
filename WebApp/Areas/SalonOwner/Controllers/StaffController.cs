using App.DAL.EF;
using App.Domain;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.SalonOwner.ViewModels;

namespace WebApp.Areas.SalonOwner.Controllers;

public class StaffController : SalonOwnerControllerBase
{
    public StaffController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
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
            .ThenInclude(p => p.User)
            .Include(st => st.BeautyProvider!)
            .ThenInclude(p => p.Services)
            .Where(st => selectedSalonIds.Contains(st.SalonId) && st.To == null)
            .ToListAsync();

        var model = new SalonOwnerStaffIndexViewModel
        {
            SalonId = salonId,
            SalonOptions = await GetSalonOptionsAsync(salonId),
            Staff = staff
                .Select(st => new SalonOwnerStaffItemViewModel
                {
                    SalonStaffId = st.Id,
                    SalonName = Translate(st.Salon?.Name),
                    ProviderName = st.BeautyProvider?.ProfileName ?? string.Empty,
                    ProviderEmail = st.BeautyProvider?.User?.Email ?? string.Empty,
                    Role = st.Role,
                    IsActive = st.BeautyProvider != null && st.BeautyProvider.To == null && (st.BeautyProvider.User?.IsActive ?? false),
                    ServiceCount = st.BeautyProvider?.Services?.Count(s => s.To == null) ?? 0
                })
                .OrderBy(st => st.SalonName)
                .ThenBy(st => st.ProviderName)
                .ToList()
        };

        if (salonId.HasValue)
        {
            model.AddStaff.SalonId = salonId.Value;
            model.AddStaff.ProviderOptions = await GetProviderOptionsAsync(salonId.Value);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([Bind(Prefix = "AddStaff")] SalonOwnerAddStaffViewModel model)
    {
        if (!await OwnsSalonAsync(model.SalonId)) return NotFound();

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index), new { salonId = model.SalonId });
        }

        var providerExists = await Context.BeautyProviders.AnyAsync(p => p.Id == model.BeautyProviderId);
        if (!providerExists) return NotFound();

        var existingStaff = await Context.SalonStaff
            .AsTracking()
            .FirstOrDefaultAsync(st => st.SalonId == model.SalonId && st.BeautyProviderId == model.BeautyProviderId);

        if (existingStaff == null)
        {
            Context.SalonStaff.Add(new SalonStaff
            {
                SalonId = model.SalonId,
                BeautyProviderId = model.BeautyProviderId,
                Role = SalonStaff.RoleEmployee,
                From = DateTime.UtcNow
            });
        }
        else
        {
            existingStaff.To = null;
            existingStaff.Role = SalonStaff.RoleEmployee;
        }

        await Context.SaveChangesAsync();
        Success("StaffProviderAdded");
        return RedirectToAction(nameof(Index), new { salonId = model.SalonId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id)
    {
        var staff = await Context.SalonStaff.AsTracking().FirstOrDefaultAsync(st => st.Id == id);
        if (staff == null) return NotFound();
        if (!await OwnsSalonAsync(staff.SalonId)) return NotFound();

        staff.To = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        Success("StaffProviderRemoved");
        return RedirectToAction(nameof(Index), new { salonId = staff.SalonId });
    }

    private async Task<List<SelectListItem>> GetProviderOptionsAsync(Guid salonId)
    {
        if (salonId == Guid.Empty) return [];

        var currentProviderIds = await Context.SalonStaff
            .Where(st => st.SalonId == salonId && st.To == null)
            .Select(st => st.BeautyProviderId)
            .ToListAsync();

        var providers = await Context.BeautyProviders
            .Include(p => p.User)
            .Where(p => p.To == null && !currentProviderIds.Contains(p.Id))
            .ToListAsync();

        return providers
            .Select(p => new SelectListItem($"{p.ProfileName} ({p.User!.Email})", p.Id.ToString()))
            .OrderBy(p => p.Text)
            .ToList();
    }
}
