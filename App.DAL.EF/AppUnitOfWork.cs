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
    }

    public IBookingRepository Bookings { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
