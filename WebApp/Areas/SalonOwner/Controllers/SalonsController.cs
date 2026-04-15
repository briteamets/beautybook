using App.DAL.EF;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.SalonOwner.ViewModels;

namespace WebApp.Areas.SalonOwner.Controllers;

public class SalonsController : SalonOwnerControllerBase
{
    public SalonsController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer) : base(context, localizer)
    {
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        if (!await OwnsSalonAsync(id)) return NotFound();

        var salon = await Context.Salons.FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();

        return View(new SalonOwnerSalonEditViewModel
        {
            Id = salon.Id,
            Name =
            {
                En = salon.Name.Translate("en") ?? string.Empty,
                Et = salon.Name.Translate("et") ?? string.Empty
            },
            Description =
            {
                En = salon.Description?.Translate("en"),
                Et = salon.Description?.Translate("et")
            },
            Address = salon.Address,
            Latitude = salon.Latitude,
            Longitude = salon.Longitude,
            ProfilePictureUrl = salon.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SalonOwnerSalonEditViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!await OwnsSalonAsync(id)) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var salon = await Context.Salons.AsTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();

        salon.Name = BuildLangStr(model.Name.En, model.Name.Et);
        salon.Description = BuildOptionalLangStr(model.Description.En, model.Description.Et);
        salon.Address = model.Address;
        salon.Latitude = model.Latitude;
        salon.Longitude = model.Longitude;
        salon.ProfilePictureUrl = model.ProfilePictureUrl;

        await Context.SaveChangesAsync();
        Success("SalonUpdated");
        return RedirectToAction("Index", "Dashboard");
    }
}
