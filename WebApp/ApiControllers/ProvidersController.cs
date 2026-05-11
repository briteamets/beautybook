using System.Net;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/providers")]
public class ProvidersController : BaseApiController
{
    private readonly AppDbContext _context;

    public ProvidersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BeautyProviderResponse>>> GetProviders(
        [FromQuery] Guid? serviceTypeId = null,
        [FromQuery] string? providerType = null,
        [FromQuery] string? search = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<BeautyProvider> query = _context.BeautyProviders
            .Where(p => p.To == null)
            .Include(p => p.Availabilities!.Where(a => a.To == null && a.Date == today))
            .Include(p => p.Services!)
                .ThenInclude(s => s.Bookings!)
                    .ThenInclude(b => b.Review);

        if (serviceTypeId.HasValue)
            query = query.Where(p => p.Services!.Any(s => s.ServiceTypeId == serviceTypeId.Value && s.IsActive && s.To == null));

        if (providerType == "independent")
            query = query.Where(p => !p.SalonStaff!.Any(ss => ss.To == null));
        else if (providerType == "salon")
            query = query.Where(p => p.SalonStaff!.Any(ss => ss.To == null));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.ProfileName, $"%{search}%"));

        var providers = await query.OrderBy(p => p.ProfileName).ToListAsync();

        return Ok(providers.Select(provider =>
        {
            var reviews = provider.Services?
                .SelectMany(s => s.Bookings ?? new List<Booking>())
                .Where(b => b.Review != null)
                .Select(b => b.Review!)
                .ToList() ?? new List<Review>();

            var averageRating = reviews.Count == 0 ? 0 : Math.Round((decimal) reviews.Average(r => r.Rating), 2);
            var availability = provider.Availabilities?
                .Where(a => !a.IsBooked)
                .OrderBy(a => a.StartTime)
                .Select(a => a.ToResponse())
                .ToList() ?? new List<AvailabilityResponse>();

            return provider.ToResponse(averageRating, reviews.Count, availability);
        }));
    }

    [HttpGet("mine")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BeautyProviderResponse>> GetMyProvider()
    {
        var userId = GetCurrentUserId();
        var provider = await _context.BeautyProviders
            .Where(p => p.UserId == userId && p.To == null)
            .FirstOrDefaultAsync();

        if (provider == null) return NotFound();

        return Ok(provider.ToResponse(0, 0, new List<AvailabilityResponse>()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BeautyProviderResponse>> GetProvider(Guid id)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var provider = await _context.BeautyProviders
            .Where(p => p.Id == id && p.To == null)
            .Include(p => p.Availabilities!)
            .Include(p => p.Services!)
            .ThenInclude(s => s.Bookings!)
            .ThenInclude(b => b.Review)
            .FirstOrDefaultAsync();

        if (provider == null) return NotFound();

        var reviews = provider.Services?
            .SelectMany(s => s.Bookings ?? new List<Booking>())
            .Where(b => b.Review != null)
            .Select(b => b.Review!)
            .ToList() ?? new List<Review>();

        var averageRating = reviews.Count == 0 ? 0 : Math.Round((decimal) reviews.Average(r => r.Rating), 2);
        var availability = provider.Availabilities?
            .Where(a => a.To == null && a.Date == today && !a.IsBooked)
            .OrderBy(a => a.StartTime)
            .Select(a => a.ToResponse())
            .ToList() ?? new List<AvailabilityResponse>();

        return Ok(provider.ToResponse(averageRating, reviews.Count, availability));
    }

    [HttpPut("mine")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BeautyProviderResponse>> UpsertMyProvider([FromBody] BeautyProviderUpdateRequest request)
    {
        Guid userId;
        try { userId = GetCurrentUserId(); }
        catch (Exception ex) { return ApiError(HttpStatusCode.Unauthorized, $"Cannot resolve user id: {ex.Message}"); }

        var provider = await _context.BeautyProviders
            .AsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.To == null);

        if (provider == null)
        {
            provider = new BeautyProvider
            {
                UserId = userId,
                From = DateTime.UtcNow
            };
            _context.BeautyProviders.Add(provider);
        }

        provider.ProfileName = request.ProfileName ?? string.Empty;
        provider.ProfilePictureUrl = request.ProfilePictureUrl;
        provider.Bio = request.Bio;
        provider.Latitude = request.Lat;
        provider.Longitude = request.Lng;
        provider.Address = request.Address ?? string.Empty;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return ApiError(HttpStatusCode.InternalServerError, $"Database error: {ex.Message}");
        }

        return Ok(provider.ToResponse(0, 0, new List<AvailabilityResponse>()));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BeautyProviderResponse>> PostProvider([FromBody] BeautyProviderCreateRequest request)
    {
        var userId = GetCurrentUserId();
        var provider = await _context.BeautyProviders
            .AsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.To == null);

        if (provider == null)
        {
            provider = new BeautyProvider { UserId = userId, From = DateTime.UtcNow };
            _context.BeautyProviders.Add(provider);
        }

        provider.ProfileName = request.ProfileName ?? string.Empty;
        provider.ProfilePictureUrl = request.ProfilePictureUrl;
        provider.Bio = request.Bio;
        provider.Latitude = request.Lat;
        provider.Longitude = request.Lng;
        provider.Address = request.Address ?? string.Empty;

        await _context.SaveChangesAsync();
        return Ok(provider.ToResponse(0, 0, new List<AvailabilityResponse>()));
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BeautyProviderResponse>> PutProvider(Guid id, [FromBody] BeautyProviderUpdateRequest request)
    {
        var provider = await _context.BeautyProviders
            .AsTracking()
            .Include(p => p.Services!)
            .ThenInclude(s => s.Bookings!)
            .ThenInclude(b => b.Review)
            .Include(p => p.Availabilities!)
            .FirstOrDefaultAsync(p => p.Id == id && p.To == null);

        if (provider == null) return NotFound();
        if (!IsCurrentUser(provider.UserId)) return Forbid();

        provider.ProfileName = request.ProfileName;
        provider.ProfilePictureUrl = request.ProfilePictureUrl;
        provider.Bio = request.Bio;
        provider.Latitude = request.Lat;
        provider.Longitude = request.Lng;
        provider.Address = request.Address;

        await _context.SaveChangesAsync();

        var reviews = provider.Services?
            .SelectMany(s => s.Bookings ?? new List<Booking>())
            .Where(b => b.Review != null)
            .Select(b => b.Review!)
            .ToList() ?? new List<Review>();
        var averageRating = reviews.Count == 0 ? 0 : Math.Round((decimal) reviews.Average(r => r.Rating), 2);
        var availability = provider.Availabilities?
            .Where(a => a.To == null && a.Date == DateOnly.FromDateTime(DateTime.UtcNow) && !a.IsBooked)
            .Select(a => a.ToResponse()).ToList() ?? new List<AvailabilityResponse>();

        return Ok(provider.ToResponse(averageRating, reviews.Count, availability));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult> DeleteProvider(Guid id)
    {
        var provider = await _context.BeautyProviders
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.To == null);

        if (provider == null) return NotFound();
        if (!IsCurrentUser(provider.UserId)) return Forbid();

        provider.To = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
