using System.Net;
using App.Contracts;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/reviews")]
public class ReviewsController : BaseApiController
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetReviews([FromQuery] Guid providerId)
    {
        var reviews = await _reviewService.GetByProviderIdAsync(providerId);
        return Ok(reviews.Select(r => r.ToResponse()));
    }

    [HttpDelete("{id:guid}/comment")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ReviewResponse>> DeleteComment(Guid id)
    {
        try
        {
            var review = await _reviewService.DeleteCommentAsync(id, GetCurrentUserId());
            return Ok(review.ToResponse());
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ApiError(HttpStatusCode.Forbidden, ex.Message); }
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ReviewResponse>> PostReview([FromBody] ReviewCreateRequest request)
    {
        try
        {
            var review = await _reviewService.CreateAsync(GetCurrentUserId(), request);
            return Ok(review.ToResponse());
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return ApiError(HttpStatusCode.BadRequest, ex.Message); }
    }
}
