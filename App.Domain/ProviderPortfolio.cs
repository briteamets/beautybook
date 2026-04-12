using Base.Domain;

namespace App.Domain;

public class ProviderPortfolio : BaseEntity
{
    public Guid BeautyProviderId { get; set; }
    public BeautyProvider? BeautyProvider { get; set; }

    public Guid? ServiceId { get; set; }
    public Service? Service { get; set; }

    public string ImageUrl { get; set; } = default!;
    public DateTime From { get; set; }
}
