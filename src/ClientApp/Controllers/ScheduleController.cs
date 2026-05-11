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

    public ScheduleController(
        ScheduleApiService scheduleApi,
        AvailabilityApiService availabilityApi,
        ProvidersApiService providersApi)
    {
        _scheduleApi = scheduleApi;
        _availabilityApi = availabilityApi;
        _providersApi = providersApi;
    }

    public async Task<IActionResult> Index()
    {
        BeautyProviderResponse? provider;
        try { provider = await _providersApi.GetMineAsync(); }
        catch (HttpRequestException) { provider = null; }

        if (provider == null)
        {
            TempData["SetupRequired"] = "true";
            return RedirectToAction("EditProfile", "Providers");
        }

        var model = await BuildViewModelAsync(provider.Id);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSchedule(ScheduleIndexViewModel model)
    {
        try
        {
            await _scheduleApi.SaveScheduleAsync(model.Days.Select(d => new ScheduleDayUpsertRequest
            {
                DayOfWeek = d.DayOfWeek,
                IsWorkingDay = d.IsWorkingDay,
                StartTime = d.StartTime,
                EndTime = d.EndTime
            }).ToList());
            TempData["SuccessMessage"] = "ScheduleSaved";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBreak(BreakViewModel model)
    {
        try
        {
            var current = await _scheduleApi.GetBreaksAsync() ?? [];
            var updated = current
                .Select(b => new BreakUpsertRequest
                    { DayOfWeek = b.DayOfWeek, BreakStart = b.BreakStart, BreakEnd = b.BreakEnd })
                .Append(new BreakUpsertRequest
                    { DayOfWeek = model.DayOfWeek, BreakStart = model.BreakStart, BreakEnd = model.BreakEnd })
                .ToList();
            await _scheduleApi.SaveBreaksAsync(updated);
            TempData["SuccessMessage"] = "BreaksSaved";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBreak(Guid id)
    {
        try
        {
            var current = await _scheduleApi.GetBreaksAsync() ?? [];
            var updated = current
                .Where(b => b.Id != id)
                .Select(b => new BreakUpsertRequest
                    { DayOfWeek = b.DayOfWeek, BreakStart = b.BreakStart, BreakEnd = b.BreakEnd })
                .ToList();
            await _scheduleApi.SaveBreaksAsync(updated);
            TempData["SuccessMessage"] = "BreaksSaved";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSlots(GenerateSlotsFormViewModel form)
    {
        try
        {
            var result = await _scheduleApi.GenerateSlotsAsync(form.FromDate, form.ToDate);
            TempData["GenerateResult"] = (result?.SlotsGenerated ?? 0).ToString();
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSlot(Guid id)
    {
        try { await _availabilityApi.DeleteAsync(id); }
        catch (HttpRequestException) { }
        return RedirectToAction(nameof(Index));
    }

    private async Task<ScheduleIndexViewModel> BuildViewModelAsync(Guid providerId)
    {
        List<ScheduleDayResponse> schedule;
        try { schedule = await _scheduleApi.GetScheduleAsync() ?? []; }
        catch (HttpRequestException) { schedule = []; }

        List<BreakResponse> breaks;
        try { breaks = await _scheduleApi.GetBreaksAsync() ?? []; }
        catch (HttpRequestException) { breaks = []; }

        ICollection<AvailabilityResponse> slots;
        try { slots = await _availabilityApi.GetMineAsync(); }
        catch (HttpRequestException) { slots = []; }

        var days = Enum.GetValues<DayOfWeek>().Select(dow =>
        {
            var s = schedule.FirstOrDefault(x => x.DayOfWeek == dow);
            return new DayScheduleViewModel
            {
                DayOfWeek = dow,
                IsWorkingDay = s?.IsWorkingDay ?? (dow is >= DayOfWeek.Monday and <= DayOfWeek.Friday),
                StartTime = s?.StartTime ?? new TimeSpan(9, 0, 0),
                EndTime = s?.EndTime ?? new TimeSpan(17, 0, 0)
            };
        }).ToList();

        return new ScheduleIndexViewModel
        {
            Days = days,
            Breaks = breaks.Select(b => new BreakViewModel
            {
                Id = b.Id,
                DayOfWeek = b.DayOfWeek,
                BreakStart = b.BreakStart,
                BreakEnd = b.BreakEnd
            }).ToList(),
            GenerateForm = new GenerateSlotsFormViewModel(),
            UpcomingSlots = slots
                .Where(s => s.Date >= DateOnly.FromDateTime(DateTime.Today))
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new AvailabilitySlotViewModel
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsBooked = s.IsBooked
                }).ToList(),
            GenerateResult = TempData["GenerateResult"]?.ToString(),
            ErrorMessage = TempData["ErrorMessage"]?.ToString(),
            SuccessMessage = TempData["SuccessMessage"]?.ToString()
        };
    }
}
