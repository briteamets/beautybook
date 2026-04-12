using Base.Domain;

namespace App.Domain;

public class Review : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid UserId { get; set; }
    public Identity.AppUser? User { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}
