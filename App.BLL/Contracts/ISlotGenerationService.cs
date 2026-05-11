namespace App.BLL.Contracts;

public interface ISlotGenerationService
{
    Task<int> GenerateSlotsAsync(Guid providerId, DateOnly fromDate, DateOnly toDate);
}
