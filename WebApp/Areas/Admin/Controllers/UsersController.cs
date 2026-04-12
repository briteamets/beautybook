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

    public UsersController(App.DAL.EF.AppDbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager) : base(context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(string? search, string? role, int page = 1)
    {
        page = Math.Max(1, page);
        var query = Context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(search.ToLower())) ||
                (u.UserName != null && u.UserName.ToLower().Contains(search.ToLower())));
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
                Role = roles.Contains("Client") ? "Client" : roles.FirstOrDefault() ?? string.Empty,
                RegisteredAt = user.CreatedAt,
                IsActive = user.IsActive
            });
        }

        return View(new AdminUserIndexViewModel
        {
            Search = search,
            Role = role,
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
            Roles = userRoles
        });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return View(new AdminUserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty,
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

        vm.Email = user.Email ?? string.Empty;
        vm.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
        vm.IsSelf = IsCurrentUser(user.Id);

        if (vm.IsSelf && !vm.IsActive)
        {
            ModelState.AddModelError(nameof(vm.IsActive), T("CannotDeactivateSelf"));
        }

        if (!await _roleManager.RoleExistsAsync(vm.Role))
        {
            ModelState.AddModelError(nameof(vm.Role), T("RoleNotFound"));
        }

        if (!ModelState.IsValid) return View(vm);

        var roles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, roles);
        if (!removeResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join(" ", removeResult.Errors.Select(e => e.Description)));
            return View(vm);
        }

        var addResult = await _userManager.AddToRoleAsync(user, vm.Role);
        if (!addResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join(" ", addResult.Errors.Select(e => e.Description)));
            return View(vm);
        }

        user.IsActive = vm.IsActive;
        await _userManager.UpdateAsync(user);
        SetSuccessMessage(T("UserUpdated"));
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

    private bool IsCurrentUser(Guid userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(currentUserId, out var id) && id == userId;
    }
}
