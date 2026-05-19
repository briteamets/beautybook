using App.Domain;

namespace App.Contracts;

public interface IBookingService
{
    Task<IEnumerable<Booking>> GetUserBookingsAsync(Guid userId);
    Task<IEnumerable<Booking>> GetProviderBookingsAsync(Guid providerUserId);
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking> CreateBookingAsync(Guid userId, Guid serviceId, Guid availabilityId, Guid? salonId, string? notes);
    // Extra method (beyond spec): needed to eliminate all _context references from
    // BookingsController.CreateDynamicBooking while keeping that method on the controller.
    Task<Booking> CreateDynamicBookingAsync(Guid userId, Guid providerServiceId, DateOnly date, TimeSpan startTime, TimeSpan endTime, string? notes);
    Task<Booking> CancelBookingAsync(Guid bookingId, Guid requestingUserId, string? reason);
    Task<Booking> UpdateStatusAsync(Guid bookingId, Guid providerUserId, string newStatus, string? reason);
    Task<Booking> UpdateNotesAsync(Guid bookingId, Guid requestingUserId, string? notes);
}
