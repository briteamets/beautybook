using System.Diagnostics;
using ClientApp.Models;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

public class HomeController : Controller
{
    private readonly ProvidersApiService _providersApiService;
    private readonly ServiceTypesApiService _serviceTypesApiService;

    public HomeController(ProvidersApiService providersApiService, ServiceTypesApiService serviceTypesApiService)
    {
        _providersApiService = providersApiService;
        _serviceTypesApiService = serviceTypesApiService;
    }

    public async Task<IActionResult> Index(string? filter = null, Guid? serviceTypeId = null, string? search = null)
    {
        var normalizedFilter = filter is "independent" or "salon" ? filter : null;
        var providers = await _providersApiService.GetAllAsync(normalizedFilter, serviceTypeId, search);
        var serviceTypes = await _serviceTypesApiService.GetAsync();
        return View(new HomeIndexViewModel
        {
            Filter = normalizedFilter ?? "all",
            ServiceTypeId = serviceTypeId,
            Search = search,
            ServiceTypes = serviceTypes.Select(t => new ServiceTypeOptionViewModel { Id = t.Id, Name = t.Name }).ToList(),
            Providers = providers.Select(p => new ProviderListItemViewModel
            {
                Id = p.Id,
                ProfileName = p.ProfileName,
                Address = p.Address,
                AverageRating = p.AverageRating,
                TodayAvailabilityCount = p.TodayAvailability.Count
            }).ToList()
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
        return LocalRedirect(returnUrl);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
