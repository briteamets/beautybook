using App.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class AvailabilityRepository : IAvailabilityRepository
{
    private readonly AppDbContext _context;

    public AvailabilityRepository(AppDbContext context) => _context = context;

    public Task<Availability?> GetByIdTrackedAsync(Guid id) =>
        _context.Availabilities
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.To == null);

    public Task<bool> SalonExistsAsync(Guid salonId) =>
        _context.Salons.AnyAsync(s => s.Id == salonId && s.To == null);
}
