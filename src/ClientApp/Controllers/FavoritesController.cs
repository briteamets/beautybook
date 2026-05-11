using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class FavoritesController : Controller
{
    private readonly FavoritesApiService _favoritesApiService;

    public FavoritesController(FavoritesApiService favoritesApiService)
    {
        _favoritesApiService = favoritesApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ICollection<App.DTO.v1.FavoriteProviderResponse> favorites;
        try
        {
            favorites = await _favoritesApiService.GetAsync();
        }
        catch (HttpRequestException)
        {
            favorites = [];
        }

        return View(new FavoritesIndexViewModel
        {
            Favorites = favorites.Select(f => new FavoriteListItemViewModel
            {
                Id = f.Id,
                ProviderId = f.ProviderId,
                ProfileName = f.ProfileName,
                Address = f.Address,
                AverageRating = f.AverageRating
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid providerId)
    {
        try
        {
            await _favoritesApiService.AddAsync(providerId);
        }
        catch (HttpRequestException) { }

        return RedirectToAction("Details", "Providers", new { id = providerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id, Guid? returnProviderId)
    {
        try
        {
            await _favoritesApiService.RemoveAsync(id);
        }
        catch (HttpRequestException) { }

        if (returnProviderId.HasValue)
            return RedirectToAction("Details", "Providers", new { id = returnProviderId.Value });

        return RedirectToAction(nameof(Index));
    }
}
