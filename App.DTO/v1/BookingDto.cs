namespace App.DTO.v1;

public class BookingResponse
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string? SalonName { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
    public string? Notes { get; set; }
    public bool HasReview { get; set; }
    public string? ClientName { get; set; }
}

public class BookingCreateRequest
{
    public Guid ServiceId { get; set; }
    public Guid AvailabilityId { get; set; }
    public Guid? ProviderServiceId { get; set; }
    public DateOnly? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public Guid? SalonId { get; set; }
    public string? Notes { get; set; }
}

public class BookingCancelRequest
{
    public string? Reason { get; set; }
}

public class AvailableSlotResponse
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class BookingStatusUpdateRequest
{
    public string Status { get; set; } = default!;
    public string? Reason { get; set; }
}

public class BookingUpdateRequest
{
    public string? Notes { get; set; }
}
