using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Extensions;

public static class BookingQueryExtensions
{
    /// <summary>
    /// Filters to bookings that are genuinely pending: Status == Pending,
    /// not soft-deleted, and the appointment has not yet started.
    /// Use this on every query that surfaces "pending" bookings in the UI
    /// so stale past-pending rows never appear even if the sweeper hasn't run.
    /// </summary>
    public static IQueryable<Booking> WhereActivelyPending(this IQueryable<Booking> q)
    {
        var now = DateTime.UtcNow;
        return q.Where(b =>
            b.To == null &&
            b.Status == Booking.StatusPending &&
            b.AppointmentDateTime >= now);
    }
}
