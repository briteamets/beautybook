namespace App.BLL.Contracts;

public interface ISlotGenerationRepository
{
    Task<int?> GetShortestServiceDurationAsync(Guid providerId);
    Task<IReadOnlyList<ProviderScheduleData>> GetScheduleAsync(Guid providerId);
    Task<IReadOnlyList<ProviderBreakData>> GetBreaksAsync(Guid providerId);
    Task<HashSet<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)>> GetExistingSlotKeysAsync(
        Guid providerId, DateOnly fromDate, DateOnly toDate);
    Task<int> InsertSlotsAsync(Guid providerId, List<(DateOnly Date, TimeOnly Start, TimeOnly End)> slots);
    Task<int> SoftDeleteUnbookedSlotsAsync(Guid providerId, DateOnly from, DateOnly to);
    Task<IReadOnlyList<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)>> GetBookedSlotRangesAsync(
        Guid providerId, DateOnly from, DateOnly to);
}
