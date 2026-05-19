using App.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context) => _context = context;

    public Task<List<Booking>> GetPastPendingAsync(DateTime now, CancellationToken ct = default) =>
        _context.Bookings
            .AsTracking()
            .Where(b => b.To == null && b.Status == Booking.StatusPending && b.AppointmentDateTime < now)
            .ToListAsync(ct);

    public void AddHistory(BookingHistory history) => _context.BookingHistories.Add(history);

    public void AddNotification(Notification notification) => _context.Notifications.Add(notification);

    public Task<Booking?> GetByIdWithDetailsAsync(Guid id) =>
        BookingQuery().FirstOrDefaultAsync(b => b.Id == id && b.To == null);

    public Task<List<Booking>> GetByUserIdAsync(Guid userId) =>
        BookingQuery()
            .Where(b => b.UserId == userId && b.To == null)
            .OrderByDescending(b => b.AppointmentDateTime)
            .ToListAsync();

    public Task<List<Booking>> GetByProviderUserIdAsync(Guid providerUserId) =>
        _context.Bookings
            .Include(b => b.Service).ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .Include(b => b.BookingPayments!).ThenInclude(bp => bp.Payment)
            .Include(b => b.Review)
            .Include(b => b.User)
            .Where(b => b.Service!.BeautyProvider!.UserId == providerUserId && b.To == null)
            .OrderByDescending(b => b.AppointmentDateTime)
            .ToListAsync();

    public Task<Booking> AddAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        return Task.FromResult(booking);
    }

    public void Update(Booking booking) =>
        _context.Entry(booking).State = EntityState.Modified;

    public Task<bool> HasOverlapAsync(
        Guid userId, Guid providerId, DateOnly date, TimeSpan startTime, TimeSpan endTime) =>
        _context.Bookings.AnyAsync(b =>
            b.UserId == userId &&
            b.Service!.BeautyProviderId == providerId &&
            b.BookingDate == date &&
            b.To == null &&
            (b.Status == Booking.StatusPending || b.Status == Booking.StatusConfirmed) &&
            startTime < b.EndTime &&
            endTime > b.StartTime);

    private IQueryable<Booking> BookingQuery() =>
        _context.Bookings
            .Include(b => b.Service).ThenInclude(s => s!.BeautyProvider)
            .Include(b => b.Salon)
            .Include(b => b.BookingPayments!).ThenInclude(bp => bp.Payment)
            .Include(b => b.Review)
            .AsSplitQuery()
            .AsQueryable();
}
