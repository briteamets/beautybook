using App.Domain;

namespace App.Contracts;

public interface IServiceRepository
{
    Task<Service?> GetActiveWithProviderAsync(Guid serviceId);
    Task<IEnumerable<Service>> GetPublicAsync(Guid? providerId, Guid? categoryId, Guid? serviceTypeId);
    Task<IEnumerable<Service>> GetByProviderUserIdAsync(Guid userId);
    Task<BeautyProvider?> GetProviderByIdAsync(Guid providerId);
    Task<bool> CategoryExistsAsync(Guid categoryId);
    Task<Service?> GetByIdTrackedAsync(Guid id);
    Task<Service> GetByIdWithIncludesAsync(Guid id);
    void Add(Service service);
}
