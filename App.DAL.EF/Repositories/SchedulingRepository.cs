using App.BLL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class SchedulingRepository : ISchedulingRepository
{
    private readonly AppDbContext _context;

    public SchedulingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SlotGenerationInput?> GetSlotGenerationInputAsync(
        Guid providerServiceId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var service = await _context.Services
            .Include(s => s.BeautyProvider)
            .FirstOrDefaultAsync(s => s.Id == providerServiceId && s.To == null && s.IsActive, cancellationToken);

        if (service == null) return null;

        var dayOfWeek = date.DayOfWeek;
        var schedule = await _context.ProviderSchedules
            .Where(s => s.ProviderId == service.BeautyProviderId && s.DayOfWeek == dayOfWeek)
            .Select(s => new ProviderScheduleData(s.ProviderId, s.DayOfWeek, s.StartTime, s.EndTime, s.IsWorkingDay))
            .FirstOrDefaultAsync(cancellationToken);

        var breaks = await _context.ProviderBreaks
            .Where(b => b.ProviderId == service.BeautyProviderId && b.DayOfWeek == dayOfWeek)
            .Select(b => new ProviderBreakData(b.ProviderId, b.DayOfWeek, b.BreakStart, b.BreakEnd))
            .ToListAsync(cancellationToken);

        var bookings = await _context.Bookings
            .Where(b => b.Service!.BeautyProviderId == service.BeautyProviderId &&
                        b.BookingDate == date &&
                        b.To == null &&
                        (b.Status == Booking.StatusPending || b.Status == Booking.StatusConfirmed))
            .Select(b => new SlotBooking(b.Id, b.UserId, b.StartTime, b.EndTime))
            .ToListAsync(cancellationToken);

        return new SlotGenerationInput(
            service.BeautyProviderId,
            service.Id,
            date,
            service.DurationMinutes,
            schedule,
            breaks,
            bookings,
            DateTime.UtcNow);
    }
}
