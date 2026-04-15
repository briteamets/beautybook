using App.Domain;
using Base.Contracts;
using Microsoft.AspNetCore.Identity;

namespace App.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsProviderApplicant { get; set; }

    public ICollection<AppRefreshToken>? RefreshTokens { get; set; }
    public ICollection<SalonOwner>? SalonOwners { get; set; }
    public ICollection<BeautyProvider>? BeautyProviders { get; set; }
    public ICollection<Booking>? AdminCancelledBookings { get; set; }
    public ICollection<Booking>? Bookings { get; set; }
    public ICollection<BookingHistory>? BookingHistoryEntries { get; set; }
    public ICollection<Payment>? Payments { get; set; }
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<FavoriteProvider>? FavoriteProviders { get; set; }
    public ICollection<Notification>? Notifications { get; set; }
}
