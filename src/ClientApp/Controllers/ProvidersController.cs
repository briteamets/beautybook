using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class ProvidersController : Controller
{
    private readonly ProvidersApiService _providersApiService;

    public ProvidersController(ProvidersApiService providersApiService)
    {
        _providersApiService = providersApiService;
    }

    public async Task<IActionResult> Index()
    {
        var providers = await _providersApiService.GetAllAsync();
        return View(new ProviderIndexViewModel
        {
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

    public async Task<IActionResult> Details(Guid id)
    {
        var provider = await _providersApiService.GetAsync(id);
        if (provider == null) return NotFound();

        return View(new ProviderDetailsViewModel
        {
            Id = provider.Id,
            ProfileName = provider.ProfileName,
            Bio = provider.Bio,
            Address = provider.Address,
            Lat = provider.Lat,
            Lng = provider.Lng,
            ProfilePictureUrl = provider.ProfilePictureUrl,
            AverageRating = provider.AverageRating,
            ReviewCount = provider.ReviewCount
        });
    }

    public IActionResult Create()
    {
        return View(new ProviderFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var created = await _providersApiService.CreateAsync(ToCreateRequest(model));
        return RedirectToAction(nameof(Details), new { id = created!.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var provider = await _providersApiService.GetAsync(id);
        if (provider == null) return NotFound();

        return View(new ProviderFormViewModel
        {
            Id = provider.Id,
            ProfileName = provider.ProfileName,
            Bio = provider.Bio,
            Address = provider.Address,
            Lat = provider.Lat,
            Lng = provider.Lng,
            ProfilePictureUrl = provider.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProviderFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        await _providersApiService.UpdateAsync(id, new BeautyProviderUpdateRequest
        {
            ProfileName = model.ProfileName,
            Bio = model.Bio,
            Address = model.Address,
            Lat = model.Lat,
            Lng = model.Lng,
            ProfilePictureUrl = model.ProfilePictureUrl
        });
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var provider = await _providersApiService.GetAsync(id);
        if (provider == null) return NotFound();
        return View(new ProviderDetailsViewModel
        {
            Id = provider.Id,
            ProfileName = provider.ProfileName,
            Address = provider.Address,
            Bio = provider.Bio,
            Lat = provider.Lat,
            Lng = provider.Lng
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _providersApiService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private static BeautyProviderCreateRequest ToCreateRequest(ProviderFormViewModel model) => new()
    {
        ProfileName = model.ProfileName,
        Bio = model.Bio,
        Address = model.Address,
        Lat = model.Lat,
        Lng = model.Lng,
        ProfilePictureUrl = model.ProfilePictureUrl
    };
}
