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
    private readonly SalonsApiService _salonsApiService;

    public HomeController(
        ProvidersApiService providersApiService,
        ServiceTypesApiService serviceTypesApiService,
        SalonsApiService salonsApiService)
    {
        _providersApiService = providersApiService;
        _serviceTypesApiService = serviceTypesApiService;
        _salonsApiService = salonsApiService;
    }

    public async Task<IActionResult> Index(string? filter = null, Guid? serviceTypeId = null, string? search = null)
    {
        var normalizedFilter = filter == "independent" ? "independent" : null;
        var providersTask = _providersApiService.GetAllAsync("independent", serviceTypeId, search);
        var serviceTypesTask = _serviceTypesApiService.GetAsync();

        ICollection<App.DTO.v1.SalonResponse> salons;
        try
        {
            salons = await _salonsApiService.GetAllAsync();
        }
        catch (HttpRequestException)
        {
            salons = [];
        }

        var providers = await providersTask;
        var serviceTypes = await serviceTypesTask;
        var lang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        return View(new HomeIndexViewModel
        {
            Filter = normalizedFilter ?? "all",
            ServiceTypeId = serviceTypeId,
            Search = search,
            ServiceTypes = serviceTypes.Select(t => new ServiceTypeOptionViewModel
            {
                Id = t.Id,
                Name = lang == "et" ? t.NameEt : t.NameEn
            }).ToList(),
            Providers = providers.Select(p => new ProviderListItemViewModel
            {
                Id = p.Id,
                ProfileName = p.ProfileName,
                Address = p.Address,
                AverageRating = p.AverageRating,
                TodayAvailabilityCount = p.TodayAvailability.Count
            }).ToList(),
            Salons = salons.Select(s => new SalonListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                ProfilePictureUrl = s.ProfilePictureUrl,
                StaffCount = s.StaffMembers.Count
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
