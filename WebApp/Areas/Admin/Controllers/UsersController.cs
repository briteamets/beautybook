using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

public class UsersController : AdminControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IServiceScopeFactory _scopeFactory;

    public UsersController(App.DAL.EF.AppDbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IServiceScopeFactory scopeFactory) : base(context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _scopeFactory = scopeFactory;
    }

    public async Task<IActionResult> Index(string? search, string? role, bool pendingOnly = false, int page = 1)
    {
        page = Math.Max(1, page);
        var query = Context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(search.ToLower())) ||
                (u.UserName != null && u.UserName.ToLower().Contains(search.ToLower())));
        }

        if (pendingOnly)
        {
            query = query.Where(u => u.IsProviderApplicant);
        }

        var users = await query.OrderBy(u => u.Email).ToListAsync();
        var availableRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
        var roleFilteredUsers = new List<AppUser>();

        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(role) || await _userManager.IsInRoleAsync(user, role))
            {
                roleFilteredUsers.Add(user);
            }
        }

        var totalItems = roleFilteredUsers.Count;
        var pagedUsers = roleFilteredUsers.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        var items = new List<AdminUserIndexItem>();
        foreach (var user in pagedUsers)
        {
            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            items.Add(new AdminUserIndexItem
            {
                Id = user.Id,
                Name = user.UserName ?? user.Email ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roles.Count == 0 ? string.Empty : string.Join(", ", roles.OrderBy(roleName => roleName)),
                RegisteredAt = user.CreatedAt,
                IsActive = user.IsActive,
                IsProviderApplicant = user.IsProviderApplicant
            });
        }

        return View(new AdminUserIndexViewModel
        {
            Search = search,
            Role = role,
            PendingOnly = pendingOnly,
            AvailableRoles = availableRoles,
            Page = new PagedViewModel<AdminUserIndexItem>
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
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var userRoles = (await _userManager.GetRolesAsync(user)).ToList();

        return View(new AdminUserDetailsViewModel
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            IsProviderApplicant = user.IsProviderApplicant,
            Roles = userRoles
        });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = GetPrimaryRole(roles);
        return View(new AdminUserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = currentRole,
            CurrentRole = currentRole,
            IsActive = user.IsActive,
            IsSelf = IsCurrentUser(user.Id),
            AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminUserEditViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        // Capture the posted role immediately so nothing below can accidentally
        // clobber it before the role-change logic runs.
        var requestedRole = vm.Role;

        var isSelf = IsCurrentUser(user.Id);
        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentPrimaryRole = GetPrimaryRole(currentRoles);

        // Self-edit guard: admins cannot change their own role.
        if (isSelf)
        {
            requestedRole = currentPrimaryRole;
        }

        // Refresh VM fields used by the view in case we need to re-render.
        vm.Email = user.Email ?? string.Empty;
        vm.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
        vm.IsSelf = isSelf;
        vm.CurrentRole = currentPrimaryRole;
        vm.Role = requestedRole;

        if (isSelf && !vm.IsActive)
        {
            ModelState.AddModelError(nameof(vm.IsActive), T("CannotDeactivateSelf"));
        }

        if (string.IsNullOrWhiteSpace(requestedRole) || !await _roleManager.RoleExistsAsync(requestedRole))
        {
            ModelState.AddModelError(nameof(vm.Role), T("RoleNotFound"));
        }

        // ModelState may still carry the original posted Role value; since we
        // have intentionally normalised it above (self-edit guard), clear any
        // stale validation state so re-rendering shows the correct value.
        ModelState.Remove(nameof(vm.Role));

        if (!ModelState.IsValid) return View(vm);

        // Apply role change FIRST so that a failure here aborts before we
        // mutate the AppUser entity. This mirrors the original (working)
        // ordering and avoids any interaction between UpdateAsync and the
        // subsequent UserManager role-store calls.
        var rolesToRemove = currentRoles.Where(role => role != requestedRole).ToList();
        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", removeResult.Errors.Select(e => e.Description)));
                return View(vm);
            }
        }

        if (!await _userManager.IsInRoleAsync(user, requestedRole))
        {
            var addResult = await _userManager.AddToRoleAsync(user, requestedRole);
            if (!addResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.Join(" ", addResult.Errors.Select(e => e.Description)));
                return View(vm);
            }
        }

        user.IsActive = vm.IsActive;
        if (requestedRole == "ServiceProvider")
        {
            user.IsProviderApplicant = false;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join(" ", updateResult.Errors.Select(e => e.Description)));
            return View(vm);
        }

        SetSuccessMessage(T("UserUpdated"));
        return RedirectToAction(nameof(Details), "Users", new { area = "Admin", id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAsProvider(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains("ServiceProvider"))
        {
            var rolesToRemove = currentRoles.Where(r => r != "ServiceProvider").ToList();
            if (rolesToRemove.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            await _userManager.AddToRoleAsync(user, "ServiceProvider");
        }

        user.IsProviderApplicant = false;
        await _userManager.UpdateAsync(user);

        // Use a fresh scope so UserManager's tracked entities don't interfere with SaveChanges.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<App.DAL.EF.AppDbContext>();
        db.Notifications.Add(new App.Domain.Notification
        {
            UserId = id,
            Type = "ProviderApproved",
            Title = "Provider application approved",
            Body = "Your service provider application has been approved. Log out and back in to activate your provider account.",
            IsRead = false,
            From = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        SetSuccessMessage(T("ProviderApproved"));
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        return View(new AdminUserResetPasswordViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, AdminUserResetPasswordViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        vm.Email = user.Email ?? string.Empty;

        if (!ModelState.IsValid) return View(vm);

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, vm.TemporaryPassword);
        await _userManager.UpdateSecurityStampAsync(user);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join(" ", result.Errors.Select(e => e.Description)));
            return View(vm);
        }

        SetSuccessMessage(T("PasswordReset"));
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> ForceLogout(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        return View(new AdminUserConfirmActionViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            ActionName = "ForceLogout",
            ConfirmText = T("ForceLogoutConfirmation")
        });
    }

    [HttpPost, ActionName("ForceLogout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceLogoutConfirmed(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        await Context.RefreshTokens.Where(t => t.AppUserId == id).ExecuteDeleteAsync();
        SetSuccessMessage(T("RefreshTokensInvalidated"));
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Profiles()
    {
        var users = await Context.Users.OrderBy(u => u.Email).ToListAsync();

        var providers = await Context.BeautyProviders
            .Where(p => p.To == null)
            .Select(p => new { p.UserId, p.Id, p.ProfileName })
            .ToListAsync();

        var salonOwners = await Context.SalonOwners
            .Where(so => so.To == null)
            .Include(so => so.Salon)
            .ToListAsync();

        var providerByUser = providers.ToDictionary(p => p.UserId);
        var salonByUser = salonOwners
            .GroupBy(so => so.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var items = new List<AdminProfileMapItem>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            providerByUser.TryGetValue(user.Id, out var provider);
            salonByUser.TryGetValue(user.Id, out var salonOwner);
            items.Add(new AdminProfileMapItem
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Role = GetPrimaryRole(roles),
                ProviderProfileName = provider?.ProfileName,
                ProviderProfileId = provider?.Id,
                SalonName = salonOwner?.Salon?.Name.Translate(),
                SalonId = salonOwner?.SalonId
            });
        }

        return View(new AdminProfileMapViewModel { Items = items });
    }

    private bool IsCurrentUser(Guid userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(currentUserId, out var id) && id == userId;
    }

    private static string GetPrimaryRole(IEnumerable<string> roles)
    {
        var roleList = roles.ToList();
        if (roleList.Contains("Admin")) return "Admin";
        if (roleList.Contains("SalonOwner")) return "SalonOwner";
        if (roleList.Contains("ServiceProvider")) return "ServiceProvider";
        if (roleList.Contains("Client")) return "Client";
        return roleList.FirstOrDefault() ?? string.Empty;
    }
}
