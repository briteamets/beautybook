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
[Route("/api/v{version:apiVersion}/availability")]
public class AvailabilityController : BaseApiController
{
    private readonly AppDbContext _context;

    public AvailabilityController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvailabilityResponse>>> GetAvailability(
        [FromQuery] Guid providerId,
        [FromQuery] Guid? serviceId,
        [FromQuery] DateOnly? date)
    {
        var query = _context.Availabilities
            .Where(a => a.BeautyProviderId == providerId && a.To == null && !a.IsBooked)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .AsQueryable();

        if (date.HasValue) query = query.Where(a => a.Date == date.Value);

        return Ok((await query.ToListAsync()).Select(a => a.ToResponse()));
    }

    [HttpGet("mine")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<AvailabilityResponse>>> GetMyAvailability([FromQuery] DateOnly? date)
    {
        var userId = GetCurrentUserId();
        var provider = await _context.BeautyProviders
            .FirstOrDefaultAsync(p => p.UserId == userId && p.To == null);
        if (provider == null) return NotFound();

        var query = _context.Availabilities
            .Where(a => a.BeautyProviderId == provider.Id && a.To == null)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .AsQueryable();

        if (date.HasValue) query = query.Where(a => a.Date == date.Value);

        return Ok((await query.ToListAsync()).Select(a => a.ToResponse()));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AvailabilityResponse>> PostAvailability([FromBody] AvailabilityCreateRequest request)
    {
        if (request.StartTime >= request.EndTime)
        {
            return ApiError(HttpStatusCode.BadRequest, "StartTime must be before EndTime.");
        }

        var provider = await _context.BeautyProviders.FirstOrDefaultAsync(p => p.Id == request.BeautyProviderId && p.To == null);
        if (provider == null) return NotFound();
        if (!IsCurrentUser(provider.UserId)) return Forbid();

        var duplicateExists = await _context.Availabilities.AnyAsync(a =>
            a.BeautyProviderId == request.BeautyProviderId &&
            a.Date == request.Date &&
            a.StartTime == request.StartTime &&
            a.EndTime == request.EndTime &&
            a.To == null);
        if (duplicateExists)
        {
            return ApiError(HttpStatusCode.BadRequest, "Availability slot already exists.");
        }

        var availability = new Availability
        {
            BeautyProviderId = request.BeautyProviderId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsBooked = false,
            From = DateTime.UtcNow
        };

        _context.Availabilities.Add(availability);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAvailability), new { providerId = request.BeautyProviderId, version = "1.0" }, availability.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult> DeleteAvailability(Guid id)
    {
        var availability = await _context.Availabilities
            .AsTracking()
            .Include(a => a.BeautyProvider)
            .FirstOrDefaultAsync(a => a.Id == id && a.To == null);
        if (availability == null) return NotFound();
        if (!IsCurrentUser(availability.BeautyProvider!.UserId)) return Forbid();
        if (availability.IsBooked) return ApiError(HttpStatusCode.BadRequest, "Booked availability cannot be removed.");

        availability.To = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
