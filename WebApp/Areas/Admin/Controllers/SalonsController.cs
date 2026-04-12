using App.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class SalonsController : AdminControllerBase
{
    public SalonsController(App.DAL.EF.AppDbContext context) : base(context)
    {
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = Context.Salons.Include(s => s.Staff!).OrderBy(s => s.Name);
        var totalItems = await query.CountAsync();
        var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new PagedViewModel<Salon>
        {
            Items = items,
            PageNumber = page,
            TotalItems = totalItems,
            TotalPages = (int) Math.Ceiling(totalItems / (double) PageSize)
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var salon = await Context.Salons
            .Include(s => s.Staff!)
            .ThenInclude(st => st.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();
        return View(salon);
    }

    public IActionResult Create()
    {
        return View(new SalonFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalonFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var salon = new Salon
        {
            Name = BuildLangStr(vm.Name.En, vm.Name.Et),
            Description = BuildLangStr(vm.Description.En, vm.Description.Et),
            Address = vm.Address,
            Latitude = vm.Latitude,
            Longitude = vm.Longitude,
            ProfilePictureUrl = vm.ProfilePictureUrl,
            From = DateTime.UtcNow
        };

        Context.Salons.Add(salon);
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("SalonCreated"));
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var salon = await Context.Salons.FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();

        return View(new SalonFormViewModel
        {
            Id = salon.Id,
            Name = new TranslationFormViewModel
            {
                En = salon.Name.Translate("en") ?? string.Empty,
                Et = salon.Name.Translate("et") ?? string.Empty
            },
            Description = new TranslationFormViewModel
            {
                En = salon.Description?.Translate("en") ?? string.Empty,
                Et = salon.Description?.Translate("et") ?? string.Empty
            },
            Address = salon.Address,
            Latitude = salon.Latitude,
            Longitude = salon.Longitude,
            ProfilePictureUrl = salon.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SalonFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        if (!ModelState.IsValid) return View(vm);

        var salon = await Context.Salons.AsTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();

        salon.Name = BuildLangStr(vm.Name.En, vm.Name.Et);
        salon.Description = BuildLangStr(vm.Description.En, vm.Description.Et);
        salon.Address = vm.Address;
        salon.Latitude = vm.Latitude;
        salon.Longitude = vm.Longitude;
        salon.ProfilePictureUrl = vm.ProfilePictureUrl;

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("SalonUpdated"));
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var salon = await Context.Salons.FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();
        return View(salon);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var salon = await Context.Salons.FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();
        Context.Salons.Remove(salon);
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("SalonDeleted"));
        return RedirectToAction(nameof(Index));
    }
}
