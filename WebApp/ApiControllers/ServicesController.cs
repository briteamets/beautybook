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
[Route("/api/v{version:apiVersion}/services")]
public class ServicesController : BaseApiController
{
    private readonly AppDbContext _context;

    public ServicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetServices(
        [FromQuery] Guid? providerId,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? serviceTypeId)
    {
        var query = _context.Services
            .Where(s => s.To == null && s.IsActive && s.BeautyProvider!.To == null)
            .Include(s => s.ServiceCategory)
            .Include(s => s.BeautyProvider)
            .AsQueryable();

        if (providerId.HasValue) query = query.Where(s => s.BeautyProviderId == providerId.Value);
        if (categoryId.HasValue) query = query.Where(s => s.ServiceCategoryId == categoryId.Value);
        if (serviceTypeId.HasValue) query = query.Where(s => s.ServiceTypeId == serviceTypeId.Value);

        return Ok((await query.OrderBy(s => s.Name).ToListAsync()).Select(s => s.ToResponse()));
    }

    [HttpGet("mine")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetMyServices()
    {
        var userId = GetCurrentUserId();
        var services = await _context.Services
            .Where(s => s.To == null && s.BeautyProvider!.UserId == userId)
            .Include(s => s.ServiceCategory)
            .Include(s => s.BeautyProvider)
            .OrderBy(s => s.Name)
            .ToListAsync();
        return Ok(services.Select(s => s.ToResponse()));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ServiceResponse>> PostService([FromBody] ServiceCreateRequest request)
    {
        var provider = await _context.BeautyProviders
            .FirstOrDefaultAsync(p => p.Id == request.BeautyProviderId && p.To == null);
        if (provider == null) return NotFound();
        if (!IsCurrentUser(provider.UserId)) return Forbid();

        var categoryExists = await _context.ServiceCategories.AnyAsync(c => c.Id == request.ServiceCategoryId);
        if (!categoryExists) return ApiError(HttpStatusCode.BadRequest, "Service category not found.");

        var service = new App.Domain.Service
        {
            BeautyProviderId = request.BeautyProviderId,
            ServiceCategoryId = request.ServiceCategoryId,
            ServiceTypeId = request.ServiceTypeId,
            Name = request.Name.ToLangStr(),
            Description = request.Description.ToLangStrOrNull(),
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            IsActive = request.IsActive,
            From = DateTime.UtcNow
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        service = await _context.Services.Include(s => s.ServiceCategory).FirstAsync(s => s.Id == service.Id);
        return CreatedAtAction(nameof(GetServices), new { version = "1.0" }, service.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ServiceResponse>> PutService(Guid id, [FromBody] ServiceUpdateRequest request)
    {
        var service = await _context.Services
            .AsTracking()
            .Include(s => s.BeautyProvider)
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Id == id && s.To == null);

        if (service == null) return NotFound();
        if (!IsCurrentUser(service.BeautyProvider!.UserId)) return Forbid();

        service.ServiceCategoryId = request.ServiceCategoryId;
        service.ServiceTypeId = request.ServiceTypeId;
        service.Name = request.Name.ToLangStr();
        service.Description = request.Description.ToLangStrOrNull();
        service.Price = request.Price;
        service.DurationMinutes = request.DurationMinutes;
        service.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        service = await _context.Services.Include(s => s.ServiceCategory).FirstAsync(s => s.Id == id);
        return Ok(service.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult> DeleteService(Guid id)
    {
        var service = await _context.Services
            .AsTracking()
            .Include(s => s.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == id && s.To == null);
        if (service == null) return NotFound();
        if (!IsCurrentUser(service.BeautyProvider!.UserId)) return Forbid();

        service.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
