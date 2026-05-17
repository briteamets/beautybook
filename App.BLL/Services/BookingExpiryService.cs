using App.Contracts;
using App.Domain;

namespace App.BLL.Services;

public class BookingExpiryService
{
    private readonly IAppUnitOfWork _uow;

    public BookingExpiryService(IAppUnitOfWork uow) => _uow = uow;

    public async Task<int> ExpirePastPendingBookingsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _uow.Bookings.GetPastPendingAsync(now, ct);

        foreach (var booking in expired)
        {
            booking.Status = Booking.StatusCancelled;
            booking.CancellationReason = Booking.ReasonAutoCancelledPendingExpired;

            _uow.Bookings.AddHistory(new BookingHistory
            {
                BookingId = booking.Id,
                OldStatus = Booking.StatusPending,
                NewStatus = Booking.StatusCancelled,
                UserId = booking.UserId,
                ChangedAt = now,
                Reason = Booking.ReasonAutoCancelledPendingExpired,
                From = now
            });

            _uow.Bookings.AddNotification(new Notification
            {
                UserId = booking.UserId,
                BookingId = booking.Id,
                Type = Notification.TypeBookingCancelled,
                Title = "Booking cancelled",
                Body = "Your pending booking was automatically cancelled because the appointment time has passed.",
                IsRead = false,
                From = now
            });
        }

        if (expired.Count > 0)
            await _uow.SaveChangesAsync(ct);

        return expired.Count;
    }
}
