using Base.Domain;

namespace App.Domain;

public class Booking : BaseEntity
{
    public const string StatusPending = "Pending";
    public const string StatusConfirmed = "Confirmed";
    public const string StatusCancelled = "Cancelled";
    public const string StatusCompleted = "Completed";

    public const string ReasonAutoCancelledPendingExpired = "AutoCancelled_PendingExpired";

    public Guid UserId { get; set; }
    public Identity.AppUser? User { get; set; }

    public Guid? CancelledByAdminId { get; set; }
    public Identity.AppUser? CancelledByAdmin { get; set; }

    public Guid ServiceId { get; set; }
    public Service? Service { get; set; }

    public Guid? AvailabilityId { get; set; }
    public Availability? Availability { get; set; }

    public Guid? SalonId { get; set; }
    public Salon? Salon { get; set; }

    public DateTime BookedAt { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = StatusPending;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }

    public ICollection<BookingHistory>? BookingHistory { get; set; }
    public ICollection<BookingPayment>? BookingPayments { get; set; }
    public Review? Review { get; set; }
    public ICollection<Notification>? Notifications { get; set; }
}
