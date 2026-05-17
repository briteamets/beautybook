namespace App.Contracts;

public interface IAppUnitOfWork
{
    IBookingRepository Bookings { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
