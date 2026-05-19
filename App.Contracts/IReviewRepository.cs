using App.Domain;

namespace App.Contracts;

public interface IReviewRepository
{
    Task<IEnumerable<Review>> GetByProviderIdAsync(Guid providerId);
    Task<Booking?> GetBookingWithReviewAsync(Guid bookingId);
    Task<Review?> GetByIdTrackedWithDetailsAsync(Guid id);
    Task<Review> GetByIdWithUserAsync(Guid id);
    void Add(Review review);
}
