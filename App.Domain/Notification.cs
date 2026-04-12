using Base.Domain;

namespace App.Domain;

public class Notification : BaseEntity
{
    public const string TypeBookingConfirmed = "BookingConfirmed";
    public const string TypeBookingCancelled = "BookingCancelled";
    public const string TypeReviewReceived = "ReviewReceived";

    public Guid UserId { get; set; }
    public Identity.AppUser? User { get; set; }

    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public bool IsRead { get; set; }
    public DateTime From { get; set; }
}
