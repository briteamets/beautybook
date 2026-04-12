using Base.Domain;

namespace App.Domain;

public class ServiceCategory : BaseEntity
{
    public LangStr Name { get; set; } = default!;
    public LangStr? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ServiceType>? ServiceTypes { get; set; }
    public ICollection<Service>? Services { get; set; }
}
