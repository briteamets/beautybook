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
}
