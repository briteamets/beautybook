using App.Domain;

namespace App.Contracts;

public interface IBookingRepository
{
    Task<List<Booking>> GetPastPendingAsync(DateTime now, CancellationToken ct = default);
    void AddHistory(BookingHistory history);
    void AddNotification(Notification notification);
}
