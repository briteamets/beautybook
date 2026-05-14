using App.Domain;
using App.Domain.Identity;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels;

public class PagedViewModel<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
}

public class AdminUserIndexItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
    public DateTime RegisteredAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsProviderApplicant { get; set; }
}

public class AdminUserIndexViewModel
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool PendingOnly { get; set; }
    public List<string> AvailableRoles { get; set; } = new();
    public PagedViewModel<AdminUserIndexItem> Page { get; set; } = new();
}

public class AdminUserDetailsViewModel
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsProviderApplicant { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class AdminUserEditViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    [Required]
    public string Role { get; set; } = default!;
    public string CurrentRole { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSelf { get; set; }
    public List<string> AvailableRoles { get; set; } = new();
}

public class AdminUserResetPasswordViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    [Required]
    [DataType(DataType.Password)]
    public string TemporaryPassword { get; set; } = string.Empty;
}

public class AdminUserConfirmActionViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string ActionName { get; set; } = default!;
    public string ConfirmText { get; set; } = default!;
}

public class AdminDashboardViewModel
{
    public int TotalRegisteredUsers { get; set; }
    public int TotalServiceProviders { get; set; }
    public int TotalBookingsToday { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingApprovals { get; set; }
    public List<AdminActivityFeedItem> RecentActivity { get; set; } = new();
}

public class AdminActivityFeedItem
{
    public string ActivityType { get; set; } = default!;
    public string PrimaryText { get; set; } = default!;
    public string SecondaryText { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}

public class TranslationFormViewModel
{
    public string? En { get; set; }
    public string? Et { get; set; }
}

public class SalonFormViewModel
{
    public Guid Id { get; set; }
    public TranslationFormViewModel Name { get; set; } = new();
    public TranslationFormViewModel Description { get; set; } = new();
    [Required]
    public string Address { get; set; } = default!;
    [Range(-90, 90)]
    public double Latitude { get; set; }
    [Range(-180, 180)]
    public double Longitude { get; set; }
    [Url]
    public string? ProfilePictureUrl { get; set; }
}

public class SalonDetailsViewModel
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionEt { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<SalonStaffDetailsViewModel> Staff { get; set; } = new();
    public List<SalonOwnerDetailsViewModel> Owners { get; set; } = new();
    public AssignSalonOwnerViewModel AssignOwner { get; set; } = new();
}

public class SalonStaffDetailsViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class SalonOwnerDetailsViewModel
{
    public Guid SalonOwnerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime From { get; set; }
}

public class AssignSalonOwnerViewModel
{
    public Guid SalonId { get; set; }
    [Required]
    public Guid UserId { get; set; }
    public List<SelectListItem> UserOptions { get; set; } = new();
}

public class ServiceCategoryFormViewModel
{
    public Guid Id { get; set; }
    public TranslationFormViewModel Name { get; set; } = new();
    public TranslationFormViewModel Description { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class ServiceCategoryIndexItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public int ServiceTypeCount { get; set; }
}

public class ServiceCategoryIndexViewModel
{
    public PagedViewModel<ServiceCategoryIndexItem> Page { get; set; } = new();
}

public class ServiceCategoryDetailsViewModel
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionEt { get; set; }
    public bool IsActive { get; set; }
    public int ServiceTypeCount { get; set; }
    public int ServiceCount { get; set; }
}

public class ServiceTypeIndexFilterViewModel
{
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

public class ServiceTypeIndexItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class ServiceTypeIndexViewModel
{
    public ServiceTypeIndexFilterViewModel Filter { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    public PagedViewModel<ServiceTypeIndexItem> Page { get; set; } = new();
}

public class ServiceTypeFormViewModel
{
    public Guid Id { get; set; }
    [Required]
    public Guid ServiceCategoryId { get; set; }
    public TranslationFormViewModel Name { get; set; } = new();
    public TranslationFormViewModel Description { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public List<SelectListItem> Categories { get; set; } = new();
    public string FormAction { get; set; } = "Create";
    public string SubmitLabel { get; set; } = "Save";
}

public class ServiceTypeDetailsViewModel
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameEt { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionEt { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ServiceCount { get; set; }
}

public class ProviderIndexItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; }
    public int ServiceCount { get; set; }
    public int BookingCount { get; set; }
}

public class ProviderDetailsViewModel
{
    public Guid Id { get; set; }
    public string ProfileName { get; set; } = default!;
    public string? Email { get; set; }
    public string Address { get; set; } = default!;
    public string? Bio { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
    public bool IsSuspended { get; set; }
    public double? AverageRating { get; set; }
    public List<ProviderServiceItem> Services { get; set; } = new();
    public List<ProviderScheduleItem> Schedules { get; set; } = new();
    public List<ProviderBookingItem> RecentBookings { get; set; } = new();
}

public class ProviderServiceItem
{
    public string ServiceType { get; set; } = default!;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class ProviderScheduleItem
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsWorkingDay { get; set; }
}

public class ProviderBookingItem
{
    public string ClientName { get; set; } = default!;
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = default!;
}

public class BookingIndexFilterViewModel
{
    public string? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? ProviderId { get; set; }
    public string? ProviderSearch { get; set; }
    public Guid? ServiceTypeId { get; set; }
}

public class BookingIndexItem
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class BookingIndexViewModel
{
    public BookingIndexFilterViewModel Filter { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> ServiceTypes { get; set; } = new();
    public PagedViewModel<BookingIndexItem> Page { get; set; } = new();
}

public class BookingDetailsViewModel
{
    public Booking Booking { get; set; } = default!;
    public List<BookingHistory> HistoryEntries { get; set; } = new();
    public bool CanCancel { get; set; }
}

public class BookingCancelViewModel
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public DateTime AppointmentDateTime { get; set; }
    [Required]
    [StringLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class AdminServiceIndexFilterViewModel
{
    public Guid? ProviderId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

public class AdminServiceIndexItem
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class AdminServiceIndexViewModel
{
    public AdminServiceIndexFilterViewModel Filter { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    public PagedViewModel<AdminServiceIndexItem> Page { get; set; } = new();
}

public class AdminServicesPageViewModel
{
    public string ActiveTab { get; set; } = "catalog";
    public ServiceTypeIndexViewModel Catalog { get; set; } = new();
    public AdminServiceIndexViewModel Services { get; set; } = new();
    public ServiceCategoryIndexViewModel CategoryCatalog { get; set; } = new();
}

public class ReviewIndexFilterViewModel
{
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public Guid? ProviderId { get; set; }
}

public class ReviewIndexViewModel
{
    public ReviewIndexFilterViewModel Filter { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
    public PagedViewModel<ReviewIndexItem> Page { get; set; } = new();
}

public class ReviewIndexItem
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminServiceDetailsViewModel
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public Guid ProviderId { get; set; }
    public string? ProviderEmail { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}
