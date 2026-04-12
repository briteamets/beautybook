using Base.Domain;

namespace App.Domain;

public class Service : BaseEntity
{
    public Guid BeautyProviderId { get; set; }
    public BeautyProvider? BeautyProvider { get; set; }

    public Guid ServiceCategoryId { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }

    public Guid? ServiceTypeId { get; set; }
    public ServiceType? ServiceType { get; set; }

    public LangStr Name { get; set; } = default!;
    public LangStr? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }

    public ICollection<Booking>? Bookings { get; set; }
}
