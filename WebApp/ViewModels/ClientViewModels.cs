using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApp.ViewModels;

public class BrowseHomeViewModel
{
    public string? Search { get; set; }
    public string Category { get; set; } = "All";
    public List<BrowseProviderCardViewModel> Providers { get; set; } = new();
    public List<BrowseSalonCardViewModel> Salons { get; set; } = new();
}

public class BrowseProviderCardViewModel
{
    public Guid Id { get; set; }
    public string ProfileName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string Address { get; set; } = default!;
    public decimal AverageRating { get; set; }
    public int TodayAvailabilityCount { get; set; }
}

public class BrowseSalonCardViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string Address { get; set; } = default!;
    public int StaffCount { get; set; }
}

public class ProviderProfileViewModel
{
    public Guid Id { get; set; }
    public Guid? BookingId { get; set; }
    public string ProfileName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public string Address { get; set; } = default!;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public Guid? SelectedServiceId { get; set; }
    public DateOnly SelectedDate { get; set; }
    public bool IsFavorite { get; set; }
    public List<ServiceCardViewModel> Services { get; set; } = new();
    public List<PortfolioImageViewModel> Portfolio { get; set; } = new();
    public List<ReviewItemViewModel> Reviews { get; set; } = new();
    public List<DateOnly> AvailableDates { get; set; } = new();
    public List<AvailabilitySlotViewModel> TimeSlots { get; set; } = new();
}

public class ServiceCardViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}

public class PortfolioImageViewModel
{
    public string ImageUrl { get; set; } = default!;
    public string? ServiceName { get; set; }
}

public class ReviewItemViewModel
{
    public string UserName { get; set; } = default!;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AvailabilitySlotViewModel
{
    public Guid Id { get; set; }
    public string StartTime { get; set; } = default!;
    public string EndTime { get; set; } = default!;
}

public class SalonDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Address { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public DateOnly SelectedDate { get; set; }
    public Guid? SelectedStaffId { get; set; }
    public decimal AverageRating { get; set; }
    public List<DateOnly> AvailableDates { get; set; } = new();
    public List<SalonStaffBookingViewModel> StaffMembers { get; set; } = new();
    public List<SalonAllServicesItemViewModel> AllServices { get; set; } = new();
}

public class SalonAllServicesItemViewModel
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string? Description { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = default!;
    public string? ProviderPictureUrl { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}

public class SalonStaffBookingViewModel
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public decimal AverageRating { get; set; }
    public List<ServiceCardViewModel> Services { get; set; } = new();
    public List<AvailabilitySlotViewModel> Slots { get; set; } = new();
}

public class BookingSummaryViewModel
{
    public Guid? BookingId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid AvailabilityId { get; set; }
    public Guid? SalonId { get; set; }
    [ValidateNever]
    public string ServiceName { get; set; } = default!;
    [ValidateNever]
    public string ProviderName { get; set; } = default!;
    [ValidateNever]
    public string? SalonName { get; set; }
    [ValidateNever]
    public DateTime AppointmentDateTime { get; set; }
    [ValidateNever]
    public decimal TotalPrice { get; set; }
    [ValidateNever]
    public int DurationMinutes { get; set; }
    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class DynamicBookingViewModel
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string ProviderAddress { get; set; } = default!;
    public string? ProviderPictureUrl { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [StringLength(2000)]
    public string? Notes { get; set; }
    public Guid? SalonId { get; set; }
    public List<SiblingProviderViewModel> SiblingProviders { get; set; } = new();
}

public class SiblingProviderViewModel
{
    public string ProviderName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public Guid ServiceId { get; set; }
    public decimal ServicePrice { get; set; }
}

public class BookingPaymentViewModel
{
    public Guid BookingId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string? SalonName { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public decimal TotalPrice { get; set; }
}

public class BookingConfirmationViewModel
{
    public Guid BookingId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string? SalonName { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = default!;
}

public class MyBookingsIndexViewModel
{
    public string Tab { get; set; } = "Upcoming";
    public List<MyBookingCardViewModel> Bookings { get; set; } = new();
}

public class MyBookingCardViewModel
{
    public Guid BookingId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid ServiceId { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string ProviderName { get; set; } = default!;
    public string? SalonName { get; set; }
    public string ServiceName { get; set; } = default!;
    public DateTime AppointmentDateTime { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = default!;
    public bool HasReview { get; set; }
}

public class ReviewCreateViewModel
{
    public Guid BookingId { get; set; }
    [ValidateNever]
    public string ProviderName { get; set; } = default!;
    [ValidateNever]
    public string ProviderAddress { get; set; } = default!;
    [ValidateNever]
    public string ServiceName { get; set; } = default!;
    [ValidateNever]
    public DateTime AppointmentDateTime { get; set; }
    [Range(1, 5)]
    public int Rating { get; set; }
    [StringLength(2000)]
    public string? Comment { get; set; }
}

public class FavoritesIndexViewModel
{
    public List<FavoriteProviderCardViewModel> Providers { get; set; } = new();
}

public class FavoriteProviderCardViewModel
{
    public Guid FavoriteId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProfileName { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public decimal AverageRating { get; set; }
}

public class ProviderManageViewModel
{
    public Guid ProviderId { get; set; }
    public string ProfileName { get; set; } = default!;
    public List<ServiceManageItemViewModel> Services { get; set; } = new();
    public List<AvailabilityManageItemViewModel> Availabilities { get; set; } = new();
    public List<CategoryOptionViewModel> Categories { get; set; } = new();
    public ProviderServiceCreateViewModel NewService { get; set; } = new();
    public ProviderAvailabilityCreateViewModel NewAvailability { get; set; } = new();
}

public class ServiceManageItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class AvailabilityManageItemViewModel
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string StartTime { get; set; } = default!;
    public string EndTime { get; set; } = default!;
    public bool IsBooked { get; set; }
}

public class CategoryOptionViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}

public class ProviderServiceCreateViewModel
{
    [Required]
    public Guid ServiceCategoryId { get; set; }
    [Required]
    public string NameEn { get; set; } = string.Empty;
    [Required]
    public string NameEt { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionEt { get; set; }
    [Range(0.01, 10000)]
    public decimal Price { get; set; }
    [Range(1, 1440)]
    public int DurationMinutes { get; set; }
}

public class ProviderAvailabilityCreateViewModel
{
    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
    [Required]
    public TimeOnly StartTime { get; set; } = new(10, 0);
    [Required]
    public TimeOnly EndTime { get; set; } = new(11, 0);
}
