using Base.Domain;

namespace App.Domain;

public class FavoriteProvider : BaseEntity
{
    public Guid UserId { get; set; }
    public Identity.AppUser? User { get; set; }

    public Guid BeautyProviderId { get; set; }
    public BeautyProvider? BeautyProvider { get; set; }

    public DateTime From { get; set; }
}
