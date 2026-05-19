using App.Domain;

namespace App.Contracts;

public interface IAvailabilityRepository
{
    Task<Availability?> GetByIdTrackedAsync(Guid id);
    Task<bool> SalonExistsAsync(Guid salonId);
}
