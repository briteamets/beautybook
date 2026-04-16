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
