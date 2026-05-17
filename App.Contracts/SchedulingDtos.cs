namespace App.Contracts;

public record TimeSlot(TimeSpan StartTime, TimeSpan EndTime, bool IsAvailable = true);

public record SlotBooking(Guid BookingId, Guid ClientId, TimeSpan StartTime, TimeSpan EndTime);

public record SlotGenerationInput(
    Guid ProviderId,
    Guid ProviderServiceId,
    DateOnly Date,
    int DurationMinutes,
    ProviderScheduleData? Schedule,
    IReadOnlyCollection<ProviderBreakData> Breaks,
    IReadOnlyCollection<SlotBooking> ExistingBookings,
    DateTime NowUtc);

public record ProviderScheduleData(
    Guid ProviderId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsWorkingDay);

public record ProviderBreakData(
    Guid ProviderId,
    DayOfWeek DayOfWeek,
    TimeSpan BreakStart,
    TimeSpan BreakEnd);
