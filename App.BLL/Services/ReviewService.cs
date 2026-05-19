using App.Contracts;
using App.Domain;
using App.DTO.v1;

namespace App.BLL.Services;

public class ReviewService : IReviewService
{
    private readonly IAppUnitOfWork _uow;

    public ReviewService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IEnumerable<Review>> GetByProviderIdAsync(Guid providerId) =>
        _uow.Reviews.GetByProviderIdAsync(providerId);

    public async Task<Review> CreateAsync(Guid userId, ReviewCreateRequest request)
    {
        if (request.Rating is < 1 or > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var booking = await _uow.Reviews.GetBookingWithReviewAsync(request.BookingId)
            ?? throw new ArgumentException("Booking not found.");

        if (booking.UserId != userId)
            throw new UnauthorizedAccessException();

        if (booking.Status != Booking.StatusCompleted)
            throw new ArgumentException("Review can only be added to completed bookings.");

        if (booking.Review != null)
            throw new ArgumentException("Booking already has a review.");

        var review = new Review
        {
            BookingId = booking.Id,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment,
            From = DateTime.UtcNow
        };

        _uow.Reviews.Add(review);
        await _uow.SaveChangesAsync();

        return await _uow.Reviews.GetByIdWithUserAsync(review.Id);
    }

    public async Task<Review> DeleteCommentAsync(Guid reviewId, Guid providerUserId)
    {
        var review = await _uow.Reviews.GetByIdTrackedWithDetailsAsync(reviewId)
            ?? throw new KeyNotFoundException("Review not found.");

        var serviceProviderUserId = review.Booking?.Service?.BeautyProvider?.UserId;
        if (serviceProviderUserId == null || serviceProviderUserId != providerUserId)
            throw new UnauthorizedAccessException("Only the provider of this service may remove the comment.");

        review.Comment = null;
        await _uow.SaveChangesAsync();

        return review;
    }
}
