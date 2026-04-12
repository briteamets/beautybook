using Base.Domain;

namespace App.Domain;

public class Availability : BaseEntity
{
    public Guid BeautyProviderId { get; set; }
    public BeautyProvider? BeautyProvider { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBooked { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }

    public ICollection<Booking>? Bookings { get; set; }
}
