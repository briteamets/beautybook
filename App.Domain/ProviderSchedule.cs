using Base.Domain;

namespace App.Domain;

public class ProviderSchedule : BaseEntity
{
    public Guid ProviderId { get; set; }
    public BeautyProvider? Provider { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkingDay { get; set; }
}
