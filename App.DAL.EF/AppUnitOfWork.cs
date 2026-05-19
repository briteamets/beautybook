using App.Contracts;
using App.DAL.EF.Repositories;

namespace App.DAL.EF;

public class AppUnitOfWork : IAppUnitOfWork
{
    private readonly AppDbContext _context;

    public AppUnitOfWork(AppDbContext context)
    {
        _context = context;
        Bookings = new BookingRepository(context);
        Services = new ServiceRepository(context);
        Availabilities = new AvailabilityRepository(context);
        Reviews = new ReviewRepository(context);
    }

    public IBookingRepository Bookings { get; }
    public IServiceRepository Services { get; }
    public IAvailabilityRepository Availabilities { get; }
    public IReviewRepository Reviews { get; }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        new EfTransaction(await _context.Database.BeginTransactionAsync(ct));

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
