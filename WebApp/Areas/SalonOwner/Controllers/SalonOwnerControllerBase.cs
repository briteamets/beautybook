using System.Security.Claims;
using App.DAL.EF;
using App.Domain;
using App.Resources;
using Base.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace WebApp.Areas.SalonOwner.Controllers;

[Area("SalonOwner")]
[Authorize(Roles = "SalonOwner")]
public abstract class SalonOwnerControllerBase : Controller
{
    protected readonly AppDbContext Context;
    private readonly IStringLocalizer<SharedUiResource> _localizer;

    protected SalonOwnerControllerBase(AppDbContext context, IStringLocalizer<SharedUiResource> localizer)
    {
        Context = context;
        _localizer = localizer;
    }

    protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected string T(string key) => _localizer[key];

    protected string Translate(LangStr? value)
    {
        return value?.Translate(Thread.CurrentThread.CurrentUICulture.Name) ?? string.Empty;
    }

    protected LangStr BuildLangStr(string en, string et)
    {
        return new LangStr(en.Trim(), "en")
        {
            ["et"] = et.Trim()
        };
    }

    protected LangStr? BuildOptionalLangStr(string? en, string? et)
    {
        if (string.IsNullOrWhiteSpace(en) && string.IsNullOrWhiteSpace(et)) return null;

        return new LangStr(en?.Trim() ?? string.Empty, "en")
        {
            ["et"] = et?.Trim() ?? string.Empty
        };
    }

    protected async Task<List<Guid>> GetOwnedSalonIdsAsync()
    {
        return await Context.SalonOwners
            .Where(o => o.UserId == CurrentUserId && o.To == null)
            .Select(o => o.SalonId)
            .ToListAsync();
    }

    protected async Task<bool> OwnsSalonAsync(Guid salonId)
    {
        return await Context.SalonOwners
            .AnyAsync(o => o.UserId == CurrentUserId && o.SalonId == salonId && o.To == null);
    }

    protected async Task<List<SelectListItem>> GetSalonOptionsAsync(Guid? selectedSalonId = null)
    {
        var ownedSalonIds = await GetOwnedSalonIdsAsync();
        var salons = await Context.Salons
            .Where(s => ownedSalonIds.Contains(s.Id))
            .ToListAsync();

        return salons
            .Select(s => new SelectListItem(Translate(s.Name), s.Id.ToString(), selectedSalonId == s.Id))
            .OrderBy(s => s.Text)
            .ToList();
    }

    protected void Success(string key) => TempData["SuccessMessage"] = T(key);
    protected void Error(string key) => TempData["ErrorMessage"] = T(key);
}
