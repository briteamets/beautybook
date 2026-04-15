using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.SalonOwner.ViewModels;

public class SalonOwnerDashboardViewModel
{
    public ICollection<SalonOwnerDashboardSalonViewModel> Salons { get; set; } = [];
}

public class SalonOwnerDashboardSalonViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public int StaffCount { get; set; }
    public int ServiceCount { get; set; }
    public int UpcomingBookingCount { get; set; }
}

public class SalonOwnerTranslationViewModel
{
    [Required]
    [MaxLength(128)]
    public string En { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Et { get; set; } = string.Empty;
}

public class SalonOwnerOptionalTranslationViewModel
{
    [MaxLength(512)]
    public string? En { get; set; }

    [MaxLength(512)]
    public string? Et { get; set; }
}

public class SalonOwnerSalonEditViewModel
{
    public Guid Id { get; set; }
    public SalonOwnerTranslationViewModel Name { get; set; } = new();
    public SalonOwnerOptionalTranslationViewModel Description { get; set; } = new();

    [Required]
    [MaxLength(256)]
    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [MaxLength(512)]
    public string? ProfilePictureUrl { get; set; }
}

public class SalonOwnerStaffIndexViewModel
{
    public Guid? SalonId { get; set; }
    public ICollection<SelectListItem> SalonOptions { get; set; } = [];
    public ICollection<SalonOwnerStaffItemViewModel> Staff { get; set; } = [];
    public SalonOwnerAddStaffViewModel AddStaff { get; set; } = new();
}

public class SalonOwnerStaffItemViewModel
{
    public Guid SalonStaffId { get; set; }
    public string SalonName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string ProviderEmail { get; set; } = default!;
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; }
    public int ServiceCount { get; set; }
}

public class SalonOwnerAddStaffViewModel
{
    [Required]
    public Guid SalonId { get; set; }

    [Required]
    public Guid BeautyProviderId { get; set; }

    public ICollection<SelectListItem> ProviderOptions { get; set; } = [];
}

public class SalonOwnerBookingIndexViewModel
{
    public Guid? SalonId { get; set; }
    public string? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public ICollection<SelectListItem> SalonOptions { get; set; } = [];
    public ICollection<SelectListItem> StatusOptions { get; set; } = [];
    public ICollection<SalonOwnerBookingItemViewModel> Bookings { get; set; } = [];
}

public class SalonOwnerBookingItemViewModel
{
    public Guid Id { get; set; }
    public string SalonName { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string ServiceName { get; set; } = default!;
    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class SalonOwnerServiceIndexViewModel
{
    public Guid? SalonId { get; set; }
    public ICollection<SelectListItem> SalonOptions { get; set; } = [];
    public ICollection<SalonOwnerServiceItemViewModel> Services { get; set; } = [];
}

public class SalonOwnerServiceItemViewModel
{
    public string SalonName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string? ServiceTypeName { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}
