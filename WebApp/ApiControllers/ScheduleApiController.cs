using System.Net;
using App.BLL.Contracts;
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
[Route("/api/v{version:apiVersion}/schedule")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ScheduleApiController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly ISlotGenerationService _slotGenerationService;

    public ScheduleApiController(AppDbContext context, ISlotGenerationService slotGenerationService)
    {
        _context = context;
        _slotGenerationService = slotGenerationService;
    }

    private async Task<BeautyProvider?> GetProviderAsync()
    {
        var userId = GetCurrentUserId();
        return await _context.BeautyProviders
            .FirstOrDefaultAsync(p => p.UserId == userId && p.To == null);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleDayResponse>>> GetSchedule()
    {
        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        var rows = await _context.ProviderSchedules
            .Where(s => s.ProviderId == provider.Id)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();

        return Ok(rows.Select(s => new ScheduleDayResponse
        {
            DayOfWeek = s.DayOfWeek,
            IsWorkingDay = s.IsWorkingDay,
            StartTime = s.StartTime,
            EndTime = s.EndTime
        }));
    }

    [HttpPut]
    public async Task<ActionResult<IEnumerable<ScheduleDayResponse>>> PutSchedule(
        [FromBody] List<ScheduleDayUpsertRequest> request)
    {
        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        foreach (var day in request)
        {
            if (day.IsWorkingDay && day.StartTime >= day.EndTime)
                return ApiError(HttpStatusCode.BadRequest,
                    $"{day.DayOfWeek}: StartTime must be before EndTime.");

            var row = await _context.ProviderSchedules
                .AsTracking()
                .FirstOrDefaultAsync(s => s.ProviderId == provider.Id && s.DayOfWeek == day.DayOfWeek);

            if (row == null)
            {
                _context.ProviderSchedules.Add(new ProviderSchedule
                {
                    ProviderId = provider.Id,
                    DayOfWeek = day.DayOfWeek,
                    IsWorkingDay = day.IsWorkingDay,
                    StartTime = day.StartTime,
                    EndTime = day.EndTime
                });
            }
            else
            {
                row.IsWorkingDay = day.IsWorkingDay;
                row.StartTime = day.StartTime;
                row.EndTime = day.EndTime;
            }
        }

        await _context.SaveChangesAsync();
        return await GetSchedule();
    }

    [HttpGet("breaks")]
    public async Task<ActionResult<IEnumerable<BreakResponse>>> GetBreaks()
    {
        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        var rows = await _context.ProviderBreaks
            .Where(b => b.ProviderId == provider.Id)
            .OrderBy(b => b.DayOfWeek)
            .ThenBy(b => b.BreakStart)
            .ToListAsync();

        return Ok(rows.Select(b => new BreakResponse
        {
            Id = b.Id,
            DayOfWeek = b.DayOfWeek,
            BreakStart = b.BreakStart,
            BreakEnd = b.BreakEnd
        }));
    }

    [HttpPut("breaks")]
    public async Task<ActionResult<IEnumerable<BreakResponse>>> PutBreaks(
        [FromBody] List<BreakUpsertRequest> request)
    {
        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        foreach (var b in request)
        {
            if (b.BreakStart >= b.BreakEnd)
                return ApiError(HttpStatusCode.BadRequest,
                    $"{b.DayOfWeek}: BreakStart must be before BreakEnd.");
        }

        var existing = await _context.ProviderBreaks
            .AsTracking()
            .Where(b => b.ProviderId == provider.Id)
            .ToListAsync();

        _context.ProviderBreaks.RemoveRange(existing);
        _context.ProviderBreaks.AddRange(request.Select(b => new ProviderBreak
        {
            ProviderId = provider.Id,
            DayOfWeek = b.DayOfWeek,
            BreakStart = b.BreakStart,
            BreakEnd = b.BreakEnd
        }));

        await _context.SaveChangesAsync();
        return await GetBreaks();
    }

    [HttpPost("generate-slots")]
    public async Task<ActionResult<GenerateSlotsResponse>> GenerateSlots(
        [FromBody] GenerateSlotsRequest request)
    {
        if (request.ToDate < request.FromDate)
            return ApiError(HttpStatusCode.BadRequest, "ToDate must be on or after FromDate.");

        if (request.ToDate.DayNumber - request.FromDate.DayNumber > 90)
            return ApiError(HttpStatusCode.BadRequest, "Date range cannot exceed 90 days.");

        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        var count = await _slotGenerationService.GenerateSlotsAsync(
            provider.Id, request.FromDate, request.ToDate);

        return Ok(new GenerateSlotsResponse { SlotsGenerated = count });
    }
}
