using App.Contracts;

namespace App.BLL.Services;

public interface ISlotGeneratorService
{
    Task<IReadOnlyCollection<TimeSlot>> GenerateSlotsAsync(Guid providerServiceId, DateOnly date, CancellationToken cancellationToken = default);
    IReadOnlyCollection<TimeSlot> GenerateSlots(SlotGenerationInput input);
}

public class SlotGeneratorService : ISlotGeneratorService
{
    private readonly ISchedulingRepository _schedulingRepository;

    public SlotGeneratorService(ISchedulingRepository schedulingRepository)
    {
        _schedulingRepository = schedulingRepository;
    }

    public async Task<IReadOnlyCollection<TimeSlot>> GenerateSlotsAsync(
        Guid providerServiceId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var input = await _schedulingRepository.GetSlotGenerationInputAsync(providerServiceId, date, cancellationToken);
        return input == null ? [] : GenerateSlots(input);
    }

    public IReadOnlyCollection<TimeSlot> GenerateSlots(SlotGenerationInput input)
    {
        if (input.Schedule == null || !input.Schedule.IsWorkingDay || input.DurationMinutes <= 0)
        {
            return [];
        }

        var duration = TimeSpan.FromMinutes(input.DurationMinutes);
        var nowDate = DateOnly.FromDateTime(input.NowUtc);
        var nowTime = TimeOnly.FromDateTime(input.NowUtc).ToTimeSpan();
        var result = new List<TimeSlot>();

        for (var start = input.Schedule.StartTime; start + duration <= input.Schedule.EndTime; start += duration)
        {
            var end = start + duration;

            if (input.Date == nowDate && start <= nowTime)
            {
                continue;
            }

            if (OverlapsAny(start, end, input.ExistingBookings.Select(b => (b.StartTime, b.EndTime))))
            {
                continue;
            }

            if (OverlapsAny(start, end, input.Breaks.Select(b => (b.BreakStart, b.BreakEnd))))
            {
                continue;
            }

            result.Add(new TimeSlot(start, end));
        }

        return result;
    }

    private static bool OverlapsAny(TimeSpan start, TimeSpan end, IEnumerable<(TimeSpan Start, TimeSpan End)> ranges)
    {
        return ranges.Any(range => start < range.End && end > range.Start);
    }
}
