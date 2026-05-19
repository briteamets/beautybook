using App.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;

    public ServiceRepository(AppDbContext context) => _context = context;

    public Task<Service?> GetActiveWithProviderAsync(Guid serviceId) =>
        _context.Services
            .Include(s => s.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.To == null && s.IsActive);

    public async Task<IEnumerable<Service>> GetPublicAsync(Guid? providerId, Guid? categoryId, Guid? serviceTypeId)
    {
        var query = _context.Services
            .Where(s => s.To == null && s.IsActive && s.BeautyProvider!.To == null)
            .Include(s => s.ServiceCategory)
            .Include(s => s.BeautyProvider)
            .AsQueryable();

        if (providerId.HasValue) query = query.Where(s => s.BeautyProviderId == providerId.Value);
        if (categoryId.HasValue) query = query.Where(s => s.ServiceCategoryId == categoryId.Value);
        if (serviceTypeId.HasValue) query = query.Where(s => s.ServiceTypeId == serviceTypeId.Value);

        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public Task<IEnumerable<Service>> GetByProviderUserIdAsync(Guid userId) =>
        _context.Services
            .Where(s => s.To == null && s.BeautyProvider!.UserId == userId)
            .Include(s => s.ServiceCategory)
            .Include(s => s.BeautyProvider)
            .OrderBy(s => s.Name)
            .ToListAsync()
            .ContinueWith(t => (IEnumerable<Service>)t.Result);

    public Task<BeautyProvider?> GetProviderByIdAsync(Guid providerId) =>
        _context.BeautyProviders.FirstOrDefaultAsync(p => p.Id == providerId && p.To == null);

    public Task<bool> CategoryExistsAsync(Guid categoryId) =>
        _context.ServiceCategories.AnyAsync(c => c.Id == categoryId);

    public Task<Service?> GetByIdTrackedAsync(Guid id) =>
        _context.Services
            .AsTracking()
            .Include(s => s.BeautyProvider)
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Id == id && s.To == null);

    public Task<Service> GetByIdWithIncludesAsync(Guid id) =>
        _context.Services
            .Include(s => s.ServiceCategory)
            .Include(s => s.BeautyProvider)
            .FirstAsync(s => s.Id == id);

    public void Add(Service service) => _context.Services.Add(service);
}
