using App.BLL.Contracts;

namespace App.BLL.Services;

public class SlotGenerationService : ISlotGenerationService
{
    private readonly ISlotGenerationRepository _repo;

    public SlotGenerationService(ISlotGenerationRepository repo)
    {
        _repo = repo;
    }

    public async Task<RegenerateResult> RegenerateUpcomingSlotsAsync(Guid providerId, int weeksAhead = 8)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var toDate = today.AddDays(weeksAhead * 7);

        var schedule = await _repo.GetScheduleAsync(providerId);
        var breaks = await _repo.GetBreaksAsync(providerId);
        var bookedSlots = await _repo.GetBookedSlotRangesAsync(providerId, today, toDate);

        var warnings = new List<string>();
        foreach (var slot in bookedSlots)
        {
            var day = schedule.FirstOrDefault(s => s.DayOfWeek == slot.Date.DayOfWeek);
            var slotStart = slot.StartTime.ToTimeSpan();
            var slotEnd = slot.EndTime.ToTimeSpan();

            bool outside = day == null || !day.IsWorkingDay
                || slotStart < day.StartTime || slotEnd > day.EndTime
                || breaks.Where(b => b.DayOfWeek == slot.Date.DayOfWeek)
                         .Any(b => slotStart < b.BreakEnd && slotEnd > b.BreakStart);

            if (outside)
                warnings.Add($"{slot.Date:yyyy-MM-dd} {slot.StartTime:HH\\:mm}–{slot.EndTime:HH\\:mm}");
        }

        await _repo.SoftDeleteUnbookedSlotsAsync(providerId, today, toDate);
        var added = await GenerateSlotsAsync(providerId, today, toDate);

        return new RegenerateResult(added, warnings);
    }

    public async Task<int> GenerateSlotsAsync(Guid providerId, DateOnly fromDate, DateOnly toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowTime = DateTime.UtcNow.TimeOfDay;

        var schedule = await _repo.GetScheduleAsync(providerId);
        var breaks = await _repo.GetBreaksAsync(providerId);
        var durationMinutes = await _repo.GetShortestServiceDurationAsync(providerId) ?? 30;
        var existing = await _repo.GetExistingSlotKeysAsync(providerId, fromDate, toDate);


        var duration = TimeSpan.FromMinutes(durationMinutes);
        var newSlots = new List<(DateOnly Date, TimeOnly Start, TimeOnly End)>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (date < today) continue;

            var daySchedule = schedule.FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek);
            if (daySchedule == null || !daySchedule.IsWorkingDay) continue;

            var dayBreaks = breaks.Where(b => b.DayOfWeek == date.DayOfWeek).ToList();

            for (var start = daySchedule.StartTime; start + duration <= daySchedule.EndTime; start += duration)
            {
                var end = start + duration;

                if (date == today && end <= nowTime) continue;

                if (dayBreaks.Any(b => start < b.BreakEnd && end > b.BreakStart)) continue;

                var startTime = TimeOnly.FromTimeSpan(start);
                var endTime = TimeOnly.FromTimeSpan(end);

                if (existing.Contains((date, startTime, endTime))) continue;

                newSlots.Add((date, startTime, endTime));
            }
        }

        if (newSlots.Count == 0) return 0;
        return await _repo.InsertSlotsAsync(providerId, newSlots);
    }
}
