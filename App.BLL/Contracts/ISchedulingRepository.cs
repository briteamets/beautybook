namespace App.BLL.Contracts;

public interface ISchedulingRepository
{
    Task<SlotGenerationInput?> GetSlotGenerationInputAsync(Guid providerServiceId, DateOnly date, CancellationToken cancellationToken = default);
}
