using App.DTO.v1;

namespace ClientApp.Services;

public class ScheduleApiService
{
    private readonly BackendApiClient _api;

    public ScheduleApiService(BackendApiClient api)
    {
        _api = api;
    }

    public Task<List<ScheduleDayResponse>?> GetScheduleAsync()
        => _api.GetAsync<List<ScheduleDayResponse>>("/api/v1/schedule");

    public Task<ScheduleSaveResponse?> SaveScheduleAsync(List<ScheduleDayUpsertRequest> days)
        => _api.PutAsync<ScheduleSaveResponse>("/api/v1/schedule", days);

    public Task<List<BreakResponse>?> GetBreaksAsync()
        => _api.GetAsync<List<BreakResponse>>("/api/v1/schedule/breaks");

    public Task<List<BreakResponse>?> SaveBreaksAsync(List<BreakUpsertRequest> breaks)
        => _api.PutAsync<List<BreakResponse>>("/api/v1/schedule/breaks", breaks);

    public Task<GenerateSlotsResponse?> GenerateSlotsAsync(DateOnly fromDate, DateOnly toDate)
        => _api.PostAsync<GenerateSlotsResponse>("/api/v1/schedule/generate-slots",
            new GenerateSlotsRequest { FromDate = fromDate, ToDate = toDate });

    public Task<WeekBundleResponse?> GetWeekBundleAsync(string? weekStart = null)
    {
        var url = "/api/v1/schedule/week";
        if (!string.IsNullOrEmpty(weekStart)) url += $"?weekStart={weekStart}";
        return _api.GetAsync<WeekBundleResponse>(url);
    }

    public Task<ScheduleSaveResponse?> PatchDayAsync(PatchDayRequest request)
        => _api.PatchAsync<ScheduleSaveResponse>("/api/v1/schedule/day", request);
}
