using App.Domain;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class SalonsController : AdminControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public SalonsController(App.DAL.EF.AppDbContext context, UserManager<AppUser> userManager) : base(context)
    {
        _userManager = userManager;
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
            .Include(s => s.Owners!)
            .ThenInclude(o => o.User)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (salon == null) return NotFound();

        return View(new SalonDetailsViewModel
        {
            Id = salon.Id,
            NameEn = salon.Name.Translate("en") ?? string.Empty,
            NameEt = salon.Name.Translate("et") ?? string.Empty,
            DescriptionEn = salon.Description?.Translate("en"),
            DescriptionEt = salon.Description?.Translate("et"),
            Address = salon.Address,
            Latitude = salon.Latitude,
            Longitude = salon.Longitude,
            Staff = salon.Staff?
                .Where(st => st.To == null)
                .Select(st => new SalonStaffDetailsViewModel
                {
                    ProviderName = st.BeautyProvider?.ProfileName ?? string.Empty,
                    Role = st.Role
                })
                .OrderBy(st => st.ProviderName)
                .ToList() ?? new List<SalonStaffDetailsViewModel>(),
            Owners = salon.Owners?
                .Where(o => o.To == null)
                .Select(o => new SalonOwnerDetailsViewModel
                {
                    SalonOwnerId = o.Id,
                    Email = o.User?.Email ?? string.Empty,
                    UserName = o.User?.UserName ?? string.Empty,
                    From = o.From
                })
                .OrderBy(o => o.Email)
                .ToList() ?? new List<SalonOwnerDetailsViewModel>(),
            AssignOwner = new AssignSalonOwnerViewModel
            {
                SalonId = salon.Id,
                UserOptions = await GetAssignableOwnerOptionsAsync(salon.Id)
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignOwner(Guid id, [Bind(Prefix = "AssignOwner")] AssignSalonOwnerViewModel vm)
    {
        if (id != vm.SalonId) return NotFound();

        var salonExists = await Context.Salons.AnyAsync(s => s.Id == id);
        if (!salonExists) return NotFound();

        var user = await _userManager.FindByIdAsync(vm.UserId.ToString());
        if (user == null) return NotFound();

        if (!await _userManager.IsInRoleAsync(user, "SalonOwner"))
        {
            SetErrorMessage(T("UserMustHaveSalonOwnerRole"));
            return RedirectToAction(nameof(Details), new { id });
        }

        var existingOwner = await Context.SalonOwners
            .AsTracking()
            .FirstOrDefaultAsync(o => o.SalonId == id && o.UserId == vm.UserId);

        if (existingOwner == null)
        {
            Context.SalonOwners.Add(new App.Domain.SalonOwner
            {
                SalonId = id,
                UserId = vm.UserId,
                From = DateTime.UtcNow
            });
        }
        else
        {
            existingOwner.To = null;
        }

        await Context.SaveChangesAsync();
        SetSuccessMessage(T("SalonOwnerAssigned"));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveOwner(Guid id, Guid salonOwnerId)
    {
        var owner = await Context.SalonOwners
            .AsTracking()
            .FirstOrDefaultAsync(o => o.Id == salonOwnerId && o.SalonId == id);

        if (owner == null) return NotFound();

        owner.To = DateTime.UtcNow;
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("SalonOwnerRemoved"));
        return RedirectToAction(nameof(Details), new { id });
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

    private async Task<List<SelectListItem>> GetAssignableOwnerOptionsAsync(Guid salonId)
    {
        var ownerUsers = await _userManager.GetUsersInRoleAsync("SalonOwner");
        var assignedUserIds = await Context.SalonOwners
            .Where(o => o.SalonId == salonId && o.To == null)
            .Select(o => o.UserId)
            .ToListAsync();

        return ownerUsers
            .Where(u => !assignedUserIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .Select(u => new SelectListItem(u.Email ?? u.UserName ?? u.Id.ToString(), u.Id.ToString()))
            .ToList();
    }
}
