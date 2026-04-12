using Base.Domain;

namespace App.Domain;

public class ServiceType : BaseEntity
{
    public Guid ServiceCategoryId { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }

    public LangStr Name { get; set; } = default!;
    public LangStr? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Service>? Services { get; set; }
}
