using Base.Domain;

namespace App.Domain;

public class ProviderBreak : BaseEntity
{
    public Guid ProviderId { get; set; }
    public BeautyProvider? Provider { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan BreakStart { get; set; }
    public TimeSpan BreakEnd { get; set; }
    public string? Label { get; set; }
}
