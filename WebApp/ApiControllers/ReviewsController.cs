using System.Net;
using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/reviews")]
public class ReviewsController : BaseApiController
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetReviews([FromQuery] Guid providerId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.To == null && r.Booking!.Service!.BeautyProviderId == providerId)
            .Include(r => r.User)
            .OrderByDescending(r => r.From)
            .ToListAsync();

        return Ok(reviews.Select(r => r.ToResponse()));
    }

    [HttpDelete("{id:guid}/comment")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ReviewResponse>> DeleteComment(Guid id)
    {
        var review = await _context.Reviews
            .AsTracking()
            .Include(r => r.User)
            .Include(r => r.Booking!)
                .ThenInclude(b => b.Service!)
                    .ThenInclude(s => s.BeautyProvider)
            .FirstOrDefaultAsync(r => r.Id == id && r.To == null);

        if (review == null) return NotFound();

        var providerUserId = review.Booking?.Service?.BeautyProvider?.UserId;
        if (providerUserId == null || !IsCurrentUser(providerUserId.Value))
            return ApiError(HttpStatusCode.Forbidden, "Only the provider of this service may remove the comment.");

        review.Comment = null;
        await _context.SaveChangesAsync();

        return Ok(review.ToResponse());
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ReviewResponse>> PostReview([FromBody] ReviewCreateRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            return ApiError(HttpStatusCode.BadRequest, "Rating must be between 1 and 5.");
        }

        var userId = GetCurrentUserId();
        var booking = await _context.Bookings
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.To == null);
        if (booking == null) return ApiError(HttpStatusCode.BadRequest, "Booking not found.");
        if (!IsCurrentUser(booking.UserId)) return Forbid();
        if (booking.Status != Booking.StatusCompleted)
        {
            return ApiError(HttpStatusCode.BadRequest, "Review can only be added to completed bookings.");
        }
        if (booking.Review != null)
        {
            return ApiError(HttpStatusCode.BadRequest, "Booking already has a review.");
        }

        var review = new Review
        {
            BookingId = booking.Id,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment,
            From = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        review = await _context.Reviews.Include(r => r.User).FirstAsync(r => r.Id == review.Id);
        return Ok(review.ToResponse());
    }
}
