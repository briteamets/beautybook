using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class FavoritesController : ClientControllerBase
{
    public FavoritesController(AppDbContext context) : base(context)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var favorites = await Context.FavoriteProviders
            .Where(f => f.UserId == userId)
            .Include(f => f.BeautyProvider!)
                .ThenInclude(p => p.Services!)
                    .ThenInclude(s => s.Bookings!)
                        .ThenInclude(b => b.Review)
            .ToListAsync();

        return View(new FavoritesIndexViewModel
        {
            Providers = favorites.Select(f =>
            {
                var reviews = f.BeautyProvider?.Services?
                    .SelectMany(s => s.Bookings ?? [])
                    .Where(b => b.Review != null)
                    .Select(b => b.Review!)
                    .ToList() ?? [];
                return new FavoriteProviderCardViewModel
                {
                    FavoriteId = f.Id,
                    ProviderId = f.BeautyProviderId,
                    ProfileName = f.BeautyProvider?.ProfileName ?? string.Empty,
                    Address = f.BeautyProvider?.Address ?? string.Empty,
                    ProfilePictureUrl = f.BeautyProvider?.ProfilePictureUrl,
                    AverageRating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(r => r.Rating), 2)
                };
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid providerId, string? returnUrl = null)
    {
        var userId = GetCurrentUserId();
        var exists = await Context.FavoriteProviders.AnyAsync(f => f.UserId == userId && f.BeautyProviderId == providerId);
        if (!exists)
        {
            Context.FavoriteProviders.Add(new FavoriteProvider
            {
                UserId = userId,
                BeautyProviderId = providerId,
                From = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();
            SetSuccessMessage(T("ProviderAddedToFavorites"));
        }

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var favorite = await Context.FavoriteProviders.FirstOrDefaultAsync(f => f.Id == id && f.UserId == GetCurrentUserId());
        if (favorite == null) return NotFound();

        Context.FavoriteProviders.Remove(favorite);
        await Context.SaveChangesAsync();
        SetSuccessMessage(T("FavoriteRemoved"));
        return RedirectToAction(nameof(Index));
    }
}
