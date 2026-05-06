using Base.Contracts;

namespace Base.Domain;

public abstract class BaseEntity : IBaseEntity
{
    // annab igale andmeobjektile automaatselt unikaalse Guid Id
    public Guid Id { get; set; } =  Guid.NewGuid();
}