using App.BLL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class SlotGenerationRepository : ISlotGenerationRepository
{
    private readonly AppDbContext _context;

    public SlotGenerationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<int?> GetShortestServiceDurationAsync(Guid providerId) =>
        _context.Services
            .Where(s => s.BeautyProviderId == providerId && s.IsActive && s.To == null)
            .MinAsync(s => (int?)s.DurationMinutes);

    public async Task<IReadOnlyList<ProviderScheduleData>> GetScheduleAsync(Guid providerId) =>
        await _context.ProviderSchedules
            .Where(s => s.ProviderId == providerId)
            .Select(s => new ProviderScheduleData(s.ProviderId, s.DayOfWeek, s.StartTime, s.EndTime, s.IsWorkingDay))
            .ToListAsync();

    public async Task<IReadOnlyList<ProviderBreakData>> GetBreaksAsync(Guid providerId) =>
        await _context.ProviderBreaks
            .Where(b => b.ProviderId == providerId)
            .Select(b => new ProviderBreakData(b.ProviderId, b.DayOfWeek, b.BreakStart, b.BreakEnd))
            .ToListAsync();

    public async Task<HashSet<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)>> GetExistingSlotKeysAsync(
        Guid providerId, DateOnly fromDate, DateOnly toDate)
    {
        var rows = await _context.Availabilities
            .Where(a => a.BeautyProviderId == providerId && a.To == null
                        && a.Date >= fromDate && a.Date <= toDate)
            .Select(a => new { a.Date, a.StartTime, a.EndTime })
            .ToListAsync();
        return rows.Select(r => (r.Date, r.StartTime, r.EndTime)).ToHashSet();
    }

    public async Task<int> InsertSlotsAsync(Guid providerId, List<(DateOnly Date, TimeOnly Start, TimeOnly End)> slots)
    {
        var now = DateTime.UtcNow;
        var entities = slots.Select(s => new Availability
        {
            BeautyProviderId = providerId,
            Date = s.Date,
            StartTime = s.Start,
            EndTime = s.End,
            IsBooked = false,
            From = now
        }).ToList();

        _context.Availabilities.AddRange(entities);
        await _context.SaveChangesAsync();
        return entities.Count;
    }
}
