using App.Domain;
using App.DTO.v1;

namespace App.Contracts;

public interface IReviewService
{
    Task<IEnumerable<Review>> GetByProviderIdAsync(Guid providerId);
    Task<Review> CreateAsync(Guid userId, ReviewCreateRequest request);
    Task<Review> DeleteCommentAsync(Guid reviewId, Guid providerUserId);
}
