using Base.Domain;

namespace App.Domain;

public class BeautyProvider : BaseEntity
{
    public Guid UserId { get; set; }
    public Identity.AppUser? User { get; set; }

    public string ProfileName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = default!;
    public DateTime From { get; set; }
    public DateTime? To { get; set; }

    public ICollection<Service>? Services { get; set; }
    public ICollection<Availability>? Availabilities { get; set; }
    public ICollection<ProviderSchedule>? Schedules { get; set; }
    public ICollection<ProviderBreak>? Breaks { get; set; }
    public ICollection<ProviderPortfolio>? Portfolio { get; set; }
    public ICollection<SalonStaff>? SalonStaff { get; set; }
    public ICollection<FavoriteProvider>? FavoritedBy { get; set; }
}
