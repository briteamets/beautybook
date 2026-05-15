namespace App.DTO.v1;

public class ScheduleDayResponse
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class ScheduleDayUpsertRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class BreakResponse
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan BreakStart { get; set; }
    public TimeSpan BreakEnd { get; set; }
    public string? Label { get; set; }
}

public class BreakUpsertRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan BreakStart { get; set; }
    public TimeSpan BreakEnd { get; set; }
    public string? Label { get; set; }
}

public class GenerateSlotsRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public class GenerateSlotsResponse
{
    public int SlotsGenerated { get; set; }
}

public class ScheduleSaveResponse
{
    public List<ScheduleDayResponse> Days { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class PatchDayRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool? IsWorkingDay { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

public class WeekBundleResponse
{
    public DateOnly WeekStart { get; set; }
    public List<WeekDayBundle> Days { get; set; } = [];
    public List<OotelBookingItem> PendingBookings { get; set; } = [];
}

public class WeekDayBundle
{
    public DayOfWeek DayOfWeek { get; set; }
    public DateOnly Date { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<BreakResponse> Breaks { get; set; } = [];
    public List<WeekBookingItem> Bookings { get; set; } = [];
}

public class WeekBookingItem
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = default!;
}

public class OotelBookingItem
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
