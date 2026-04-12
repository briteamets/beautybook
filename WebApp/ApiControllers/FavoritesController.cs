using System.Net;
using App.DAL.EF;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/favorites")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class FavoritesController : BaseApiController
{
    private readonly AppDbContext _context;

    public FavoritesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FavoriteProviderResponse>>> GetFavorites()
    {
        var userId = GetCurrentUserId();
        var favorites = await _context.FavoriteProviders
            .Where(f => f.UserId == userId)
            .Include(f => f.BeautyProvider)
            .ThenInclude(p => p!.Services!)
            .ThenInclude(s => s.Bookings!)
            .ThenInclude(b => b.Review)
            .ToListAsync();

        return Ok(favorites.Select(f =>
        {
            var reviews = f.BeautyProvider?.Services?
                .SelectMany(s => s.Bookings ?? [])
                .Where(b => b.Review != null)
                .Select(b => b.Review!)
                .ToList() ?? [];
            var averageRating = reviews.Count == 0 ? 0 : Math.Round((decimal) reviews.Average(r => r.Rating), 2);
            return f.ToResponse(averageRating);
        }));
    }

    [HttpPost]
    public async Task<ActionResult<FavoriteProviderResponse>> PostFavorite([FromBody] FavoriteProviderCreateRequest request)
    {
        var userId = GetCurrentUserId();
        var provider = await _context.BeautyProviders
            .Include(p => p.Services!)
            .ThenInclude(s => s.Bookings!)
            .ThenInclude(b => b.Review)
            .FirstOrDefaultAsync(p => p.Id == request.BeautyProviderId && p.To == null);
        if (provider == null) return ApiError(HttpStatusCode.BadRequest, "Provider not found.");

        var existing = await _context.FavoriteProviders
            .FirstOrDefaultAsync(f => f.UserId == userId && f.BeautyProviderId == request.BeautyProviderId);
        if (existing != null)
        {
            var existingReviews = provider.Services?
                .SelectMany(s => s.Bookings ?? [])
                .Where(b => b.Review != null)
                .Select(b => b.Review!)
                .ToList() ?? [];
            var existingAverage = existingReviews.Count == 0 ? 0 : Math.Round((decimal) existingReviews.Average(r => r.Rating), 2);
            return Ok(existing.ToResponse(existingAverage));
        }

        var favorite = new App.Domain.FavoriteProvider
        {
            UserId = userId,
            BeautyProviderId = request.BeautyProviderId,
            From = DateTime.UtcNow
        };

        _context.FavoriteProviders.Add(favorite);
        await _context.SaveChangesAsync();

        favorite = await _context.FavoriteProviders
            .Include(f => f.BeautyProvider)
            .ThenInclude(p => p!.Services!)
            .ThenInclude(s => s.Bookings!)
            .ThenInclude(b => b.Review)
            .FirstAsync(f => f.Id == favorite.Id);

        var reviews = favorite.BeautyProvider?.Services?
            .SelectMany(s => s.Bookings ?? [])
            .Where(b => b.Review != null)
            .Select(b => b.Review!)
            .ToList() ?? [];
        var averageRating = reviews.Count == 0 ? 0 : Math.Round((decimal) reviews.Average(r => r.Rating), 2);

        return Ok(favorite.ToResponse(averageRating));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteFavorite(Guid id)
    {
        var favorite = await _context.FavoriteProviders
            .AsTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
        if (favorite == null) return NotFound();
        if (!IsCurrentUser(favorite.UserId)) return Forbid();

        _context.FavoriteProviders.Remove(favorite);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
