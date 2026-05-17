namespace App.Contracts;

public interface ISlotGeneratorService
{
    Task<IReadOnlyCollection<TimeSlot>> GenerateSlotsAsync(Guid providerServiceId, DateOnly date, CancellationToken cancellationToken = default);
    IReadOnlyCollection<TimeSlot> GenerateSlots(SlotGenerationInput input);
}
