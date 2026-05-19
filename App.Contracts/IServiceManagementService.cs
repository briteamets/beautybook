using App.DTO.v1;

namespace App.Contracts;

public interface IServiceManagementService
{
    Task<IEnumerable<App.Domain.Service>> GetPublicAsync(Guid? providerId, Guid? categoryId, Guid? serviceTypeId);
    Task<IEnumerable<App.Domain.Service>> GetByProviderUserIdAsync(Guid userId);
    Task<App.Domain.Service> CreateAsync(Guid providerUserId, ServiceCreateRequest request);
    Task<App.Domain.Service> UpdateAsync(Guid id, Guid providerUserId, ServiceUpdateRequest request);
    Task SoftDeleteAsync(Guid id, Guid providerUserId);
}
