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

        return Ok(rows.Select(ToResponse));
    }

    [HttpPut]
    public async Task<ActionResult<ScheduleSaveResponse>> PutSchedule(
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

        var regenerate = await _slotGenerationService.RegenerateUpcomingSlotsAsync(provider.Id);
        var days = await GetScheduleDays(provider.Id);
        return Ok(new ScheduleSaveResponse { Days = days, Warnings = regenerate.Warnings.ToList() });
    }

    [HttpGet("breaks")]
    public async Task<ActionResult<IEnumerable<BreakResponse>>> GetBreaks()
    {
        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        return Ok(await FetchBreaks(provider.Id));
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
            BreakEnd = b.BreakEnd,
            Label = b.Label
        }));

        await _context.SaveChangesAsync();

        await _slotGenerationService.RegenerateUpcomingSlotsAsync(provider.Id);
        return Ok(await FetchBreaks(provider.Id));
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

    [HttpGet("week")]
    public async Task<ActionResult<WeekBundleResponse>> GetWeek([FromQuery] string? weekStart)
    {
        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        DateOnly weekStartDate;
        if (!DateOnly.TryParse(weekStart, out weekStartDate))
            weekStartDate = ThisWeekMonday();

        // Normalise to Monday
        while (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            weekStartDate = weekStartDate.AddDays(-1);

        var weekEnd = weekStartDate.AddDays(6);

        var scheduleRows = await _context.ProviderSchedules
            .Where(s => s.ProviderId == provider.Id)
            .ToListAsync();

        var breakRows = await _context.ProviderBreaks
            .Where(b => b.ProviderId == provider.Id)
            .ToListAsync();

        var bookingRows = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.User)
            .Where(b => b.Service!.BeautyProvider!.UserId == GetCurrentUserId()
                        && b.To == null
                        && b.BookingDate >= weekStartDate
                        && b.BookingDate <= weekEnd)
            .ToListAsync();

        var pendingRows = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.User)
            .Where(b => b.Service!.BeautyProvider!.UserId == GetCurrentUserId()
                        && b.To == null
                        && b.Status == Booking.StatusPending)
            .OrderBy(b => b.BookingDate).ThenBy(b => b.StartTime)
            .ToListAsync();

        var days = new List<WeekDayBundle>();
        for (int i = 0; i < 7; i++)
        {
            var date = weekStartDate.AddDays(i);
            var dow = date.DayOfWeek;
            var sched = scheduleRows.FirstOrDefault(s => s.DayOfWeek == dow);

            days.Add(new WeekDayBundle
            {
                DayOfWeek = dow,
                Date = date,
                IsWorkingDay = sched?.IsWorkingDay ?? false,
                StartTime = sched?.StartTime ?? new TimeSpan(9, 0, 0),
                EndTime = sched?.EndTime ?? new TimeSpan(17, 0, 0),
                Breaks = breakRows
                    .Where(b => b.DayOfWeek == dow)
                    .OrderBy(b => b.BreakStart)
                    .Select(b => new BreakResponse
                    {
                        Id = b.Id, DayOfWeek = b.DayOfWeek,
                        BreakStart = b.BreakStart, BreakEnd = b.BreakEnd, Label = b.Label
                    }).ToList(),
                Bookings = bookingRows
                    .Where(b => b.BookingDate == date)
                    .OrderBy(b => b.StartTime)
                    .Select(b => new WeekBookingItem
                    {
                        Id = b.Id,
                        ClientName = b.User?.Email ?? "—",
                        ServiceName = b.Service?.Name.Translate() ?? "—",
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        Status = b.Status
                    }).ToList()
            });
        }

        var pending = pendingRows.Select(b => new OotelBookingItem
        {
            Id = b.Id,
            ClientName = b.User?.Email ?? "—",
            ServiceName = b.Service?.Name.Translate() ?? "—",
            BookingDate = b.BookingDate,
            StartTime = b.StartTime,
            EndTime = b.EndTime
        }).ToList();

        return Ok(new WeekBundleResponse
        {
            WeekStart = weekStartDate,
            Days = days,
            PendingBookings = pending
        });
    }

    [HttpPatch("day")]
    public async Task<ActionResult<ScheduleSaveResponse>> PatchDay([FromBody] PatchDayRequest request)
    {
        var provider = await GetProviderAsync();
        if (provider == null) return NotFound();

        var row = await _context.ProviderSchedules
            .AsTracking()
            .FirstOrDefaultAsync(s => s.ProviderId == provider.Id && s.DayOfWeek == request.DayOfWeek);

        if (row == null)
        {
            row = new ProviderSchedule
            {
                ProviderId = provider.Id,
                DayOfWeek = request.DayOfWeek,
                IsWorkingDay = false,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            };
            _context.ProviderSchedules.Add(row);
        }

        if (request.IsWorkingDay.HasValue) row.IsWorkingDay = request.IsWorkingDay.Value;
        if (request.StartTime.HasValue) row.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue) row.EndTime = request.EndTime.Value;

        if (row.IsWorkingDay && row.StartTime >= row.EndTime)
            return ApiError(HttpStatusCode.BadRequest,
                $"{request.DayOfWeek}: StartTime must be before EndTime.");

        await _context.SaveChangesAsync();

        var regenerate = await _slotGenerationService.RegenerateUpcomingSlotsAsync(provider.Id);
        var days = await GetScheduleDays(provider.Id);
        return Ok(new ScheduleSaveResponse { Days = days, Warnings = regenerate.Warnings.ToList() });
    }

    private static DateOnly ThisWeekMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-diff);
    }

    private async Task<List<ScheduleDayResponse>> GetScheduleDays(Guid providerId)
    {
        var rows = await _context.ProviderSchedules
            .Where(s => s.ProviderId == providerId)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();
        return rows.Select(ToResponse).ToList();
    }

    private async Task<List<BreakResponse>> FetchBreaks(Guid providerId) =>
        await _context.ProviderBreaks
            .Where(b => b.ProviderId == providerId)
            .OrderBy(b => b.DayOfWeek).ThenBy(b => b.BreakStart)
            .Select(b => new BreakResponse
            {
                Id = b.Id, DayOfWeek = b.DayOfWeek,
                BreakStart = b.BreakStart, BreakEnd = b.BreakEnd, Label = b.Label
            })
            .ToListAsync();

    private static ScheduleDayResponse ToResponse(ProviderSchedule s) => new()
    {
        DayOfWeek = s.DayOfWeek,
        IsWorkingDay = s.IsWorkingDay,
        StartTime = s.StartTime,
        EndTime = s.EndTime
    };
}
