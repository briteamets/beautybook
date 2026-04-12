using System.Security.Claims;
using App.DAL.EF;
using App.Domain;
using App.Resources;
using Base.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApp.Areas.ServiceProvider.ViewModels;
using WebApp;

namespace WebApp.Areas.ServiceProvider.Controllers;

[Area("ServiceProvider")]
[Authorize(Roles = "ServiceProvider")]
public abstract class ServiceProviderControllerBase : Controller
{
    protected readonly AppDbContext Context;
    private readonly IStringLocalizer<SharedUiResource> _localizer;

    protected ServiceProviderControllerBase(AppDbContext context, IStringLocalizer<SharedUiResource> localizer)
    {
        Context = context;
        _localizer = localizer;
    }

    protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected string T(string key) => _localizer[key];

    protected async Task<BeautyProvider?> GetCurrentProviderAsync()
    {
        var provider = await Context.BeautyProviders
            .FirstOrDefaultAsync(p => p.UserId == CurrentUserId && p.To == null);

        if (provider != null)
        {
            await EnsureDefaultScheduleAsync(provider.Id);
            return provider;
        }

        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user == null) return null;

        provider = new BeautyProvider
        {
            UserId = CurrentUserId,
            ProfileName = user.Email ?? "Service Provider",
            Bio = T("DefaultProviderBio"),
            Address = T("DefaultProviderAddress"),
            Latitude = 0,
            Longitude = 0,
            From = DateTime.UtcNow
        };

        Context.BeautyProviders.Add(provider);
        await Context.SaveChangesAsync();
        await EnsureDefaultScheduleAsync(provider.Id);

        Success("ProviderProfileCreated");
        return provider;
    }

    private async Task EnsureDefaultScheduleAsync(Guid providerId)
    {
        if (await Context.ProviderSchedules.AnyAsync(s => s.ProviderId == providerId))
        {
            return;
        }

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            Context.ProviderSchedules.Add(new ProviderSchedule
            {
                ProviderId = providerId,
                DayOfWeek = day,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsWorkingDay = day is >= DayOfWeek.Monday and <= DayOfWeek.Friday
            });
        }

        await Context.SaveChangesAsync();
    }

    protected string Translate(LangStr? value)
    {
        return value?.Translate(Thread.CurrentThread.CurrentUICulture.Name) ?? string.Empty;
    }

    protected async Task LoadCategoriesAsync(ProviderServiceFormViewModel model)
    {
        var categories = await Context.ServiceCategories.ToListAsync();

        model.Categories = categories
            .Select(c => new ServiceCategoryOptionViewModel
            {
                Id = c.Id,
                Name = Translate(c.Name)
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    protected LangStr BuildLangStr(string en, string et)
    {
        return new LangStr(en, "en")
        {
            ["et"] = et
        };
    }

    protected LangStr? BuildOptionalLangStr(string? en, string? et)
    {
        if (string.IsNullOrWhiteSpace(en) && string.IsNullOrWhiteSpace(et)) return null;

        return new LangStr(en ?? string.Empty, "en")
        {
            ["et"] = et ?? string.Empty
        };
    }

    protected void Success(string key) => TempData["SuccessMessage"] = T(key);
    protected void Error(string key) => TempData["ErrorMessage"] = T(key);
}
