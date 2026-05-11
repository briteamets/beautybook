using System.ComponentModel.DataAnnotations;

namespace ClientApp.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Firstname { get; set; } = default!;

    [Required]
    public string Lastname { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    public string Password { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = default!;

    public string? ErrorMessage { get; set; }
}

public class RegisterProviderViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Firstname { get; set; } = default!;

    [Required]
    public string Lastname { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    public string Password { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = default!;

    public string? ErrorMessage { get; set; }
}

public class ProviderIndexViewModel
{
    public ICollection<ProviderListItemViewModel> Providers { get; set; } = [];
}

public class ProviderListItemViewModel
{
    public Guid Id { get; set; }
    public string ProfileName { get; set; } = default!;
    public string Address { get; set; } = default!;
    public decimal AverageRating { get; set; }
    public int TodayAvailabilityCount { get; set; }
}

public class ProviderDetailsViewModel : ProviderFormViewModel
{
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public ICollection<ReviewListItemViewModel> Reviews { get; set; } = [];
    public bool IsFavorited { get; set; }
    public Guid? FavoriteId { get; set; }
}

public class ProviderFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string ProfileName { get; set; } = default!;

    public string? Bio { get; set; }

    [Required]
    public string Address { get; set; } = default!;

    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class ServiceIndexViewModel
{
    public ICollection<ServiceListItemViewModel> Services { get; set; } = [];
}

public class ServiceListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = default!;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceFormViewModel
{
    public Guid? Id { get; set; }
    public Guid BeautyProviderId { get; set; }
    public Guid ServiceCategoryId { get; set; }
    public Guid? ServiceTypeId { get; set; }

    [Required]
    public string NameEn { get; set; } = default!;

    [Required]
    public string NameEt { get; set; } = default!;

    public string? DescriptionEn { get; set; }
    public string? DescriptionEt { get; set; }

    [Range(0.01, 10000)]
    public decimal Price { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<SelectOptionViewModel> Providers { get; set; } = [];
    public ICollection<SelectOptionViewModel> Categories { get; set; } = [];
    public ICollection<SelectOptionViewModel> ServiceTypes { get; set; } = [];
}

public class MyServicesViewModel
{
    public bool HasProviderProfile { get; set; }
    public Guid ProviderId { get; set; }
    public ICollection<ServiceListItemViewModel> Services { get; set; } = [];
}

public class BookingIndexViewModel
{
    public ICollection<BookingListItemViewModel> Bookings { get; set; } = [];
}

public class BookingListItemViewModel
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string? SalonName { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
    public bool HasReview { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BookingCreateViewModel
{
    public Guid ServiceId { get; set; }
    public Guid AvailabilityId { get; set; }
    public string? Notes { get; set; }
    public ICollection<SelectOptionViewModel> Services { get; set; } = [];
    public ICollection<SelectOptionViewModel> AvailabilitySlots { get; set; } = [];
}

public class BookingEditViewModel
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = default!;
    public string? Notes { get; set; }
}

public class SelectOptionViewModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = default!;
}

public class HomeIndexViewModel
{
    public string Filter { get; set; } = "all";
    public Guid? ServiceTypeId { get; set; }
    public string? Search { get; set; }
    public ICollection<ServiceTypeOptionViewModel> ServiceTypes { get; set; } = [];
    public ICollection<ProviderListItemViewModel> Providers { get; set; } = [];
}

public class ProviderBookingItemViewModel
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
    public string? Notes { get; set; }
}

public class ProviderBookingsViewModel
{
    public bool HasProviderProfile { get; set; }
    public ICollection<ProviderBookingItemViewModel> Bookings { get; set; } = [];
}

public class MyReviewsViewModel
{
    public bool HasProviderProfile { get; set; }
    public ICollection<ReviewListItemViewModel> Reviews { get; set; } = [];
}

public class ReviewListItemViewModel
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string UserName { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class BookingWizardState
{
    public Guid? ProviderId { get; set; }
    public string? ProviderName { get; set; }
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public decimal ServicePrice { get; set; }
    public int ServiceDurationMinutes { get; set; }
    public DateOnly? Date { get; set; }
}

public class WizardStep1ViewModel
{
    public ICollection<ProviderListItemViewModel> Providers { get; set; } = [];
}

public class ServiceTypeOptionViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}

public class WizardServiceViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public string CategoryName { get; set; } = default!;
}

public class WizardStep2ViewModel
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = default!;
    public Guid? ServiceTypeId { get; set; }
    public ICollection<ServiceTypeOptionViewModel> ServiceTypes { get; set; } = [];
    public ICollection<WizardServiceViewModel> Services { get; set; } = [];
}

public class WizardStep3ViewModel
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = default!;
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public DateOnly? Date { get; set; }
}

public class WizardSlotViewModel
{
    public Guid Id { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class WizardStep4ViewModel
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = default!;
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public decimal ServicePrice { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
    public ICollection<WizardSlotViewModel> Slots { get; set; } = [];
}

public class AvailabilitySlotViewModel
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBooked { get; set; }
}

public class AvailabilityCreateViewModel
{
    public Guid BeautyProviderId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public DateOnly Date { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public TimeOnly StartTime { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public TimeOnly EndTime { get; set; }
}

public class AvailabilityIndexViewModel
{
    public bool HasProviderProfile { get; set; }
    public Guid ProviderId { get; set; }
    public DateOnly SelectedDate { get; set; }
    public ICollection<AvailabilitySlotViewModel> Slots { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class ReviewCreateViewModel
{
    public Guid BookingId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public DateTime AppointmentDateTime { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int Rating { get; set; } = 5;

    public string? Comment { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NotificationListItemViewModel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationsIndexViewModel
{
    public ICollection<NotificationListItemViewModel> Notifications { get; set; } = [];
}

public class FavoriteListItemViewModel
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ProfileName { get; set; } = default!;
    public string Address { get; set; } = default!;
    public decimal AverageRating { get; set; }
}

public class FavoritesIndexViewModel
{
    public ICollection<FavoriteListItemViewModel> Favorites { get; set; } = [];
}

public class PaymentListItemViewModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime? PaidAt { get; set; }
}

public class PaymentsIndexViewModel
{
    public ICollection<PaymentListItemViewModel> Payments { get; set; } = [];
}

public class SalonListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public int StaffCount { get; set; }
}

public class SalonStaffViewModel
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
}

public class SalonDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Address { get; set; } = default!;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public ICollection<SalonStaffViewModel> Staff { get; set; } = [];
}

public class SalonsIndexViewModel
{
    public ICollection<SalonListItemViewModel> Salons { get; set; } = [];
}

public class ScheduleIndexViewModel
{
    public List<DayScheduleViewModel> Days { get; set; } = [];
    public List<BreakViewModel> Breaks { get; set; } = [];
    public GenerateSlotsFormViewModel GenerateForm { get; set; } = new();
    public List<AvailabilitySlotViewModel> UpcomingSlots { get; set; } = [];
    public string? GenerateResult { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class DayScheduleViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class BreakViewModel
{
    public Guid? Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan BreakStart { get; set; }
    public TimeSpan BreakEnd { get; set; }
}

public class GenerateSlotsFormViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
}
