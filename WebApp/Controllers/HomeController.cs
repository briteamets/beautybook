using System.Diagnostics;
using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class HomeController : ClientControllerBase
{
    public HomeController(AppDbContext context) : base(context)
    {
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Browse));
    }

    [HttpGet]
    public async Task<IActionResult> Browse(string category = "All", string? search = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow);
        var providersQuery = Context.BeautyProviders
            .Where(p => p.To == null)
            .Include(p => p.Availabilities!.Where(a =>
                a.To == null &&
                !a.IsBooked &&
                (a.Date > today || (a.Date == today && a.StartTime > nowTime))))
            .Include(p => p.Services!.Where(s => s.To == null && s.IsActive))
                .ThenInclude(s => s.ServiceCategory)
            .Include(p => p.Services!)
                .ThenInclude(s => s.Bookings!)
                    .ThenInclude(b => b.Review)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            providersQuery = providersQuery.Where(p =>
                p.ProfileName.ToLower().Contains(term) ||
                p.Address.ToLower().Contains(term));
        }

        var providers = await providersQuery
            .OrderBy(p => p.ProfileName)
            .ToListAsync();

        if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(category, "Beauty Salon", StringComparison.OrdinalIgnoreCase))
        {
            providers = providers.Where(p =>
                p.Services!.Any(s => string.Equals(s.ServiceCategory!.Name.Translate("en"), category, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var salonsQuery = Context.Salons
            .Where(s => s.To == null)
            .Include(s => s.Staff!)
            .OrderBy(s => s.Name)
            .AsQueryable();

        var salons = await salonsQuery.ToListAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            salons = salons.Where(s =>
                s.Address.ToLower().Contains(term) ||
                (s.Name.Translate("en") ?? string.Empty).ToLower().Contains(term) ||
                (s.Name.Translate("et") ?? string.Empty).ToLower().Contains(term))
                .ToList();
        }

        var vm = new BrowseHomeViewModel
        {
            Search = search,
            Category = category,
            Providers = providers.Select(provider =>
            {
                var reviews = provider.Services?
                    .SelectMany(s => s.Bookings ?? [])
                    .Where(b => b.Review != null)
                    .Select(b => b.Review!)
                    .ToList() ?? [];

                var avg = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(r => r.Rating), 2);

                return new BrowseProviderCardViewModel
                {
                    Id = provider.Id,
                    ProfileName = provider.ProfileName,
                    ProfilePictureUrl = provider.ProfilePictureUrl,
                    Address = provider.Address,
                    AverageRating = avg,
                    TodayAvailabilityCount = provider.Availabilities?.Count() ?? 0
                };
            }).ToList(),
            Salons = salons.Select(s => new BrowseSalonCardViewModel
            {
                Id = s.Id,
                Name = s.Name.Translate() ?? string.Empty,
                ProfilePictureUrl = s.ProfilePictureUrl,
                Address = s.Address,
                StaffCount = s.Staff?.Count ?? 0
            }).ToList()
        };

        return View("Index", vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions()
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            }
        );
        return LocalRedirect(returnUrl);
    }
}
