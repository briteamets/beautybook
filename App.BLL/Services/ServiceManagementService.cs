using App.Contracts;
using App.Domain;
using App.DTO.v1;
using Base.Domain;

namespace App.BLL.Services;

public class ServiceManagementService : IServiceManagementService
{
    private readonly IAppUnitOfWork _uow;

    public ServiceManagementService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Service?> GetByIdAsync(Guid id)
    {
        try { return await _uow.Services.GetByIdWithIncludesAsync(id); }
        catch { return null; }
    }

    public Task<IEnumerable<Service>> GetPublicAsync(Guid? providerId, Guid? categoryId, Guid? serviceTypeId) =>
        _uow.Services.GetPublicAsync(providerId, categoryId, serviceTypeId);

    public Task<IEnumerable<Service>> GetByProviderUserIdAsync(Guid userId) =>
        _uow.Services.GetByProviderUserIdAsync(userId);

    public async Task<Service> CreateAsync(Guid providerUserId, ServiceCreateRequest request)
    {
        var provider = await _uow.Services.GetProviderByIdAsync(request.BeautyProviderId)
            ?? throw new KeyNotFoundException("Provider not found.");

        if (provider.UserId != providerUserId)
            throw new UnauthorizedAccessException();

        if (!await _uow.Services.CategoryExistsAsync(request.ServiceCategoryId))
            throw new ArgumentException("Service category not found.");

        var service = new Service
        {
            BeautyProviderId = request.BeautyProviderId,
            ServiceCategoryId = request.ServiceCategoryId,
            ServiceTypeId = request.ServiceTypeId,
            Name = ToLangStr(request.Name),
            Description = ToLangStrOrNull(request.Description),
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            IsActive = request.IsActive,
            From = DateTime.UtcNow
        };

        _uow.Services.Add(service);
        await _uow.SaveChangesAsync();

        return await _uow.Services.GetByIdWithIncludesAsync(service.Id);
    }

    public async Task<Service> UpdateAsync(Guid id, Guid providerUserId, ServiceUpdateRequest request)
    {
        var service = await _uow.Services.GetByIdTrackedAsync(id)
            ?? throw new KeyNotFoundException("Service not found.");

        if (service.BeautyProvider!.UserId != providerUserId)
            throw new UnauthorizedAccessException();

        service.ServiceCategoryId = request.ServiceCategoryId;
        service.ServiceTypeId = request.ServiceTypeId;
        service.Name = ToLangStr(request.Name);
        service.Description = ToLangStrOrNull(request.Description);
        service.Price = request.Price;
        service.DurationMinutes = request.DurationMinutes;
        service.IsActive = request.IsActive;

        await _uow.SaveChangesAsync();

        return await _uow.Services.GetByIdWithIncludesAsync(id);
    }

    public async Task SoftDeleteAsync(Guid id, Guid providerUserId)
    {
        var service = await _uow.Services.GetByIdTrackedAsync(id)
            ?? throw new KeyNotFoundException("Service not found.");

        if (service.BeautyProvider!.UserId != providerUserId)
            throw new UnauthorizedAccessException();

        service.IsActive = false;
        await _uow.SaveChangesAsync();
    }

    private static LangStr ToLangStr(Dictionary<string, string> values)
    {
        var result = new LangStr();
        foreach (var pair in values) result[pair.Key] = pair.Value;
        return result;
    }

    private static LangStr? ToLangStrOrNull(Dictionary<string, string>? values) =>
        values == null || values.Count == 0 ? null : ToLangStr(values);
}
