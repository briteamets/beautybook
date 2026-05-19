using App.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    public ReviewRepository(AppDbContext context) => _context = context;

    public Task<IEnumerable<Review>> GetByProviderIdAsync(Guid providerId) =>
        _context.Reviews
            .Where(r => r.To == null && r.Booking!.Service!.BeautyProviderId == providerId)
            .Include(r => r.User)
            .OrderByDescending(r => r.From)
            .ToListAsync()
            .ContinueWith(t => (IEnumerable<Review>)t.Result);

    public Task<Booking?> GetBookingWithReviewAsync(Guid bookingId) =>
        _context.Bookings
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.To == null);

    public Task<Review?> GetByIdTrackedWithDetailsAsync(Guid id) =>
        _context.Reviews
            .AsTracking()
            .Include(r => r.User)
            .Include(r => r.Booking!)
                .ThenInclude(b => b.Service!)
                    .ThenInclude(s => s.BeautyProvider)
            .FirstOrDefaultAsync(r => r.Id == id && r.To == null);

    public Task<Review> GetByIdWithUserAsync(Guid id) =>
        _context.Reviews
            .Include(r => r.User)
            .FirstAsync(r => r.Id == id);

    public void Add(Review review) => _context.Reviews.Add(review);
}
