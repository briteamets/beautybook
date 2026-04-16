using App.DAL.EF;
using App.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.ServiceProvider.ViewModels;

namespace WebApp.Areas.ServiceProvider.Controllers;

public class ProfileController : ServiceProviderControllerBase
{
    public ProfileController(AppDbContext context, IStringLocalizer<SharedUiResource> localizer)
        : base(context, localizer)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var provider = await GetCurrentProviderAsync();
        if (provider == null) return RedirectToAction("Index", "Dashboard");

        return View(new ProviderProfileEditViewModel
        {
            ProfileName = provider.ProfileName,
            Bio = provider.Bio,
            ProfilePictureUrl = provider.ProfilePictureUrl,
            Address = provider.Address
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProviderProfileEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var provider = await Context.BeautyProviders
            .AsTracking()
            .FirstOrDefaultAsync(p => p.UserId == CurrentUserId && p.To == null);
        if (provider == null) return RedirectToAction("Index", "Dashboard");

        provider.ProfileName = vm.ProfileName.Trim();
        provider.Bio = string.IsNullOrWhiteSpace(vm.Bio) ? null : vm.Bio.Trim();
        provider.ProfilePictureUrl = string.IsNullOrWhiteSpace(vm.ProfilePictureUrl) ? null : vm.ProfilePictureUrl.Trim();
        provider.Address = vm.Address.Trim();

        await Context.SaveChangesAsync();
        Success("ProfileUpdated");
        return RedirectToAction(nameof(Edit));
    }
}
