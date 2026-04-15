using App.Domain.Identity;
using Base.Domain;

namespace App.Domain;

public class SalonOwner : BaseEntity
{
    public Guid SalonId { get; set; }
    public Salon? Salon { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}
