using Base.Domain;

namespace App.Domain;

public class BookingHistory : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public string OldStatus { get; set; } = default!;
    public string NewStatus { get; set; } = default!;

    public Guid UserId { get; set; }
    public Identity.AppUser? User { get; set; }

    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
    public DateTime From { get; set; }
}
