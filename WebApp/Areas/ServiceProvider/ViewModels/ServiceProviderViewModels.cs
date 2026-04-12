using System.ComponentModel.DataAnnotations;

namespace WebApp.Areas.ServiceProvider.ViewModels;

public class DashboardViewModel
{
    public string ProviderName { get; set; } = default!;
    public int ActiveServicesCount { get; set; }
    public int PendingBookingsCount { get; set; }
    public int CompletedBookingsCount { get; set; }
}

public class ProviderServiceIndexViewModel
{
    public ICollection<ServiceCategoryOptionViewModel> Categories { get; set; } = [];
    public ICollection<ProviderServiceListItemViewModel> Services { get; set; } = [];
    public ICollection<ProviderAvailabilityListItemViewModel> Availabilities { get; set; } = [];
    public ProviderServiceFormViewModel NewService { get; set; } = new();
    public ProviderAvailabilityFormViewModel NewAvailability { get; set; } = new();
}

public class ProviderServiceListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class ProviderServiceDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = default!;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class ProviderServiceFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid ServiceCategoryId { get; set; }

    [Required]
    [MaxLength(128)]
    public string NameEn { get; set; } = default!;

    [Required]
    [MaxLength(128)]
    public string NameEt { get; set; } = default!;

    [MaxLength(512)]
    public string? DescriptionEn { get; set; }

    [MaxLength(512)]
    public string? DescriptionEt { get; set; }

    [Range(0.01, 10000)]
    public decimal Price { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;
    public bool AddTimeRange { get; set; }
    public DateOnly TimeRangeDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
    public TimeOnly TimeRangeStartTime { get; set; } = new(9, 0);
    public TimeOnly TimeRangeEndTime { get; set; } = new(10, 0);
    public ICollection<ServiceCategoryOptionViewModel> Categories { get; set; } = [];
}

public class ServiceCategoryOptionViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}

public class ProviderAvailabilityIndexViewModel
{
    public ICollection<ProviderAvailabilityListItemViewModel> Availabilities { get; set; } = [];
    public ProviderAvailabilityFormViewModel NewAvailability { get; set; } = new();
}

public class ProviderAvailabilityListItemViewModel
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBooked { get; set; }
}

public class ProviderAvailabilityFormViewModel
{
    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));

    [Required]
    public TimeOnly StartTime { get; set; } = new(9, 0);

    [Required]
    public TimeOnly EndTime { get; set; } = new(10, 0);
}

public class ProviderBookingIndexViewModel
{
    public ICollection<ProviderBookingListItemViewModel> Bookings { get; set; } = [];
}

public class ProviderBookingListItemViewModel
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public DateTime AppointmentDateTime { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = default!;
}

public class ProviderBookingDetailsViewModel
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public string? SalonName { get; set; }
    public DateTime BookedAt { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = default!;
    public string? Notes { get; set; }
    public decimal TotalPrice { get; set; }
    public ICollection<ProviderBookingHistoryItemViewModel> History { get; set; } = [];
}

public class ProviderBookingHistoryItemViewModel
{
    public string OldStatus { get; set; } = default!;
    public string NewStatus { get; set; } = default!;
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}

public class ProviderScheduleIndexViewModel
{
    public ICollection<ProviderScheduleDayViewModel> Days { get; set; } = [];
    public ProviderBreakFormViewModel NewBreak { get; set; } = new();
}

public class ProviderScheduleDayViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
    public ICollection<ProviderBreakListItemViewModel> Breaks { get; set; } = [];
}

public class ProviderBreakListItemViewModel
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan BreakStart { get; set; }
    public TimeSpan BreakEnd { get; set; }
}

public class ProviderBreakFormViewModel
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;

    [Required]
    public TimeSpan BreakStart { get; set; } = new(12, 0, 0);

    [Required]
    public TimeSpan BreakEnd { get; set; } = new(13, 0, 0);
}
