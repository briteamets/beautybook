namespace App.BLL.Contracts;

public record RegenerateResult(int SlotsAdded, IReadOnlyList<string> Warnings);

public interface ISlotGenerationService
{
    Task<int> GenerateSlotsAsync(Guid providerId, DateOnly fromDate, DateOnly toDate);
    Task<RegenerateResult> RegenerateUpcomingSlotsAsync(Guid providerId, int weeksAhead = 8);
}
