using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

public class ProvidersController : Controller
{
    private readonly ProvidersApiService _providersApiService;
    private readonly ReviewsApiService _reviewsApiService;
    private readonly FavoritesApiService _favoritesApiService;
    private readonly SalonsApiService _salonsApiService;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(
        ProvidersApiService providersApiService,
        ReviewsApiService reviewsApiService,
        FavoritesApiService favoritesApiService,
        SalonsApiService salonsApiService,
        ILogger<ProvidersController> logger)
    {
        _providersApiService = providersApiService;
        _reviewsApiService = reviewsApiService;
        _favoritesApiService = favoritesApiService;
        _salonsApiService = salonsApiService;
        _logger = logger;
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

    public async Task<IActionResult> Details(Guid id, Guid? salonId = null, string? salonName = null)
    {
        var provider = await _providersApiService.GetPublicAsync(id);
        if (provider == null) return NotFound();

        if (salonId == null)
        {
            try
            {
                var salons = await _salonsApiService.GetAllAsync();
                var salon = salons.FirstOrDefault(s => s.StaffMembers.Any(m => m.ProviderId == id));
                if (salon != null)
                {
                    salonId = salon.Id;
                    salonName = salon.Name;
                }
            }
            catch (HttpRequestException) { }
        }

        var reviews = await _reviewsApiService.GetByProviderIdAsync(id);

        ICollection<FavoriteProviderResponse> favorites = [];
        if (HttpContext.Session.HasUsableToken())
        {
            try
            {
                favorites = await _favoritesApiService.GetAsync();
            }
            catch (HttpRequestException)
            {
                favorites = [];
            }
        }

        var favorite = favorites.FirstOrDefault(f => f.ProviderId == id);

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
            ReviewCount = provider.ReviewCount,
            Reviews = reviews.Select(r => new ReviewListItemViewModel
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                UserName = r.UserName,
                CreatedAt = r.CreatedAt
            }).ToList(),
            IsFavorited = favorite != null,
            FavoriteId = favorite?.Id,
            SalonId = salonId,
            SalonName = salonName
        });
    }

    [ClientAuthorize]
    public IActionResult Create()
    {
        return View(new ProviderFormViewModel());
    }

    [ClientAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var created = await _providersApiService.CreateAsync(ToCreateRequest(model));
            return RedirectToAction(nameof(Details), new { id = created!.Id });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Save failed.");
            return View(model);
        }
    }

    [ClientAuthorize]
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

    [ClientAuthorize]
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

    [ClientAuthorize]
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

    [ClientAuthorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _providersApiService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [ClientAuthorize]
    public async Task<IActionResult> EditProfile()
    {
        BeautyProviderResponse? provider;
        try
        {
            provider = await _providersApiService.GetMineAsync();
        }
        catch (HttpRequestException)
        {
            provider = null;
        }

        if (provider == null)
            return View("EditProfile", new ProviderFormViewModel());

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

    [ClientAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(ProviderFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _providersApiService.SaveMineAsync(new BeautyProviderCreateRequest
            {
                ProfileName = model.ProfileName,
                Bio = model.Bio,
                Address = model.Address,
                Lat = model.Lat,
                Lng = model.Lng,
                ProfilePictureUrl = model.ProfilePictureUrl
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("EditProfile POST /api/v1/providers failed — status={Status} message={Message}", ex.StatusCode, ex.Message);
            ModelState.AddModelError(string.Empty, $"Save failed [{ex.StatusCode}]: {ex.Message}");
            return View(model);
        }

        TempData["SuccessMessage"] = "ProfileSaved";
        return RedirectToAction(nameof(EditProfile));
    }

    [ClientAuthorize]
    public async Task<IActionResult> MyReviews()
    {
        BeautyProviderResponse? provider;
        try
        {
            provider = await _providersApiService.GetMineAsync();
        }
        catch (HttpRequestException)
        {
            provider = null;
        }

        if (provider == null)
        {
            TempData["SetupRequired"] = "true";
            return RedirectToAction(nameof(EditProfile));
        }

        var reviews = await _reviewsApiService.GetByProviderIdAsync(provider.Id);
        return View(new MyReviewsViewModel
        {
            HasProviderProfile = true,
            Reviews = reviews.Select(r => new ReviewListItemViewModel
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                UserName = r.UserName,
                CreatedAt = r.CreatedAt
            }).ToList()
        });
    }

    [ClientAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReviewComment(Guid id)
    {
        await _reviewsApiService.DeleteCommentAsync(id);
        return RedirectToAction(nameof(MyReviews));
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
