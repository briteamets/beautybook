using App.Domain;

namespace App.Contracts;

public interface IBookingRepository
{
    // --- existing (used by BookingExpiryService) ---
    Task<List<Booking>> GetPastPendingAsync(DateTime now, CancellationToken ct = default);
    void AddHistory(BookingHistory history);
    void AddNotification(Notification notification);

    // --- new (used by BookingService) ---
    Task<Booking?> GetByIdWithDetailsAsync(Guid id);
    Task<List<Booking>> GetByUserIdAsync(Guid userId);
    Task<List<Booking>> GetByProviderUserIdAsync(Guid providerUserId);
    Task<Booking> AddAsync(Booking booking);
    void Update(Booking booking);
    Task<bool> HasOverlapAsync(Guid userId, Guid providerId, DateOnly date, TimeSpan startTime, TimeSpan endTime);
}
