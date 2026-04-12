using Base.Domain;

namespace App.Domain;

public class SalonStaff : BaseEntity
{
    public const string RoleOwner = "Owner";
    public const string RoleEmployee = "Employee";

    public Guid SalonId { get; set; }
    public Salon? Salon { get; set; }

    public Guid BeautyProviderId { get; set; }
    public BeautyProvider? BeautyProvider { get; set; }

    public string Role { get; set; } = RoleEmployee;
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}
