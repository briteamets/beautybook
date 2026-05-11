using App.DTO.v1;

namespace ClientApp.Services;

public class ReviewsApiService
{
    private readonly BackendApiClient _api;

    public ReviewsApiService(BackendApiClient api)
    {
        _api = api;
    }

    public async Task<ICollection<ReviewResponse>> GetByProviderIdAsync(Guid providerId)
        => await _api.AnonymousGetAsync<List<ReviewResponse>>($"/api/v1/reviews?providerId={providerId}") ?? [];

    public Task<ReviewResponse?> CreateAsync(ReviewCreateRequest request)
        => _api.PostAsync<ReviewResponse>("/api/v1/reviews", request);

    public Task DeleteCommentAsync(Guid reviewId)
        => _api.DeleteAsync($"/api/v1/reviews/{reviewId}/comment");
}
