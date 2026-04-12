using Base.Domain;

namespace App.Domain;

public class Salon : BaseEntity
{
    public LangStr Name { get; set; } = default!;
    public LangStr? Description { get; set; }
    public string Address { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }

    public ICollection<SalonStaff>? Staff { get; set; }
    public ICollection<Booking>? Bookings { get; set; }
}
