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
}

public class BreakUpsertRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan BreakStart { get; set; }
    public TimeSpan BreakEnd { get; set; }
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
