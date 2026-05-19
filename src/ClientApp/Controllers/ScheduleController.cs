using App.DTO.v1;
using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class ScheduleController : Controller
{
    private readonly ScheduleApiService _scheduleApi;
    private readonly AvailabilityApiService _availabilityApi;
    private readonly ProvidersApiService _providersApi;
    private readonly BookingsApiService _bookingsApi;

    public ScheduleController(
        ScheduleApiService scheduleApi,
        AvailabilityApiService availabilityApi,
        ProvidersApiService providersApi,
        BookingsApiService bookingsApi)
    {
        _scheduleApi = scheduleApi;
        _availabilityApi = availabilityApi;
        _providersApi = providersApi;
        _bookingsApi = bookingsApi;
    }

    public async Task<IActionResult> Index(string? weekStart)
    {
        BeautyProviderResponse? provider;
        try { provider = await _providersApi.GetMineAsync(); }
        catch (HttpRequestException) { provider = null; }

        if (provider == null)
        {
            TempData["SetupRequired"] = "true";
            return RedirectToAction("EditProfile", "Providers");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        try { await _scheduleApi.GenerateSlotsAsync(today, today.AddDays(56)); }
        catch (HttpRequestException) { }

        var model = await BuildWeekViewModelAsync(provider, weekStart);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWorkingDay(int day, string weekStart)
    {
        var dow = (DayOfWeek)day;
        try
        {
            WeekBundleResponse? bundle = null;
            try { bundle = await _scheduleApi.GetWeekBundleAsync(weekStart); } catch { }
            var current = bundle?.Days.FirstOrDefault(d => d.DayOfWeek == dow);
            var isWorking = !(current?.IsWorkingDay ?? false);

            var result = await _scheduleApi.PatchDayAsync(new PatchDayRequest
            {
                DayOfWeek = dow,
                IsWorkingDay = isWorking
            });
            if (result?.Warnings.Count > 0)
                TempData["Warnings"] = string.Join("|", result.Warnings);

            TempData["SuccessMessage"] = "ScheduleSaved";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { weekStart });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ShiftTime(int day, string which, int deltaMinutes, string weekStart)
    {
        var dow = (DayOfWeek)day;
        try
        {
            WeekBundleResponse? bundle = null;
            try { bundle = await _scheduleApi.GetWeekBundleAsync(weekStart); } catch { }
            var current = bundle?.Days.FirstOrDefault(d => d.DayOfWeek == dow);
            var startTime = current?.StartTime ?? new TimeSpan(9, 0, 0);
            var endTime = current?.EndTime ?? new TimeSpan(17, 0, 0);

            if (which == "start")
                startTime = Clamp(startTime + TimeSpan.FromMinutes(deltaMinutes));
            else
                endTime = Clamp(endTime + TimeSpan.FromMinutes(deltaMinutes));

            if (startTime >= endTime)
            {
                TempData["ErrorMessage"] = "StartTime must be before EndTime.";
                return RedirectToAction(nameof(Index), new { weekStart });
            }

            var result = await _scheduleApi.PatchDayAsync(new PatchDayRequest
            {
                DayOfWeek = dow,
                StartTime = which == "start" ? startTime : null,
                EndTime = which == "end" ? endTime : null
            });
            if (result?.Warnings.Count > 0)
                TempData["Warnings"] = string.Join("|", result.Warnings);
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { weekStart });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBlock(AddBlockViewModel model)
    {
        try
        {
            var current = await _scheduleApi.GetBreaksAsync() ?? [];
            var updated = current
                .Select(b => new BreakUpsertRequest
                {
                    DayOfWeek = b.DayOfWeek,
                    BreakStart = b.BreakStart,
                    BreakEnd = b.BreakEnd,
                    Label = b.Label
                })
                .Append(new BreakUpsertRequest
                {
                    DayOfWeek = model.DayOfWeek,
                    BreakStart = model.BreakStart,
                    BreakEnd = model.BreakEnd,
                    Label = string.IsNullOrWhiteSpace(model.Label) ? null : model.Label
                })
                .ToList();
            await _scheduleApi.SaveBreaksAsync(updated);
            TempData["SuccessMessage"] = "BreakAdded";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { weekStart = model.WeekStart });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBlock(Guid id, string weekStart)
    {
        try
        {
            var current = await _scheduleApi.GetBreaksAsync() ?? [];
            var updated = current
                .Where(b => b.Id != id)
                .Select(b => new BreakUpsertRequest
                {
                    DayOfWeek = b.DayOfWeek,
                    BreakStart = b.BreakStart,
                    BreakEnd = b.BreakEnd,
                    Label = b.Label
                })
                .ToList();
            await _scheduleApi.SaveBreaksAsync(updated);
            TempData["SuccessMessage"] = "BreakRemoved";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { weekStart });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBooking(Guid id, string weekStart)
    {
        try
        {
            await _bookingsApi.UpdateStatusAsync(id, "Confirmed");
            TempData["SuccessMessage"] = "BookingConfirmed";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { weekStart });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(Guid id, string weekStart)
    {
        try
        {
            await _bookingsApi.UpdateStatusAsync(id, "Cancelled");
            TempData["SuccessMessage"] = "BookingRejected";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { weekStart });
    }

    private async Task<WeekCalendarViewModel> BuildWeekViewModelAsync(
        BeautyProviderResponse provider, string? weekStart)
    {
        WeekBundleResponse? bundle = null;
        try { bundle = await _scheduleApi.GetWeekBundleAsync(weekStart); } catch { }

        var weekStartDate = bundle?.WeekStart ?? ThisWeekMonday();

        var days = new List<WeekDayCalendarViewModel>();
        for (int i = 0; i < 7; i++)
        {
            var date = weekStartDate.AddDays(i);
            var bundleDay = bundle?.Days.FirstOrDefault(d => d.Date == date);

            days.Add(new WeekDayCalendarViewModel
            {
                DayOfWeek = date.DayOfWeek,
                Date = date,
                IsWorkingDay = bundleDay?.IsWorkingDay ?? false,
                StartTime = bundleDay?.StartTime ?? new TimeSpan(9, 0, 0),
                EndTime = bundleDay?.EndTime ?? new TimeSpan(17, 0, 0),
                Breaks = (bundleDay?.Breaks ?? []).Select(b => new CalendarBreakViewModel
                {
                    Id = b.Id,
                    BreakStart = b.BreakStart,
                    BreakEnd = b.BreakEnd,
                    Label = b.Label
                }).ToList(),
                Bookings = (bundleDay?.Bookings ?? []).Select(b => new CalendarBookingViewModel
                {
                    Id = b.Id,
                    ClientName = b.ClientName,
                    ServiceName = b.ServiceName,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status
                }).ToList()
            });
        }

        var warnings = TempData["Warnings"]?.ToString()?.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()
                       ?? [];

        return new WeekCalendarViewModel
        {
            WeekStart = weekStartDate,
            ProviderName = provider.ProfileName,
            Days = days,
            PendingBookings = (bundle?.PendingBookings ?? []).Select(p => new PendingBookingCalendarViewModel
            {
                Id = p.Id,
                ClientName = p.ClientName,
                ServiceName = p.ServiceName,
                BookingDate = p.BookingDate,
                StartTime = p.StartTime,
                EndTime = p.EndTime
            }).ToList(),
            Warnings = warnings,
            ErrorMessage = TempData["ErrorMessage"]?.ToString(),
            SuccessMessage = TempData["SuccessMessage"]?.ToString()
        };
    }

    private static DateOnly ThisWeekMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-diff);
    }

    private static TimeSpan Clamp(TimeSpan t)
    {
        var min = TimeSpan.Zero;
        var max = new TimeSpan(23, 45, 0);
        if (t < min) return min;
        if (t > max) return max;
        return t;
    }
}
