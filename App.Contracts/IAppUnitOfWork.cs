namespace App.Contracts;

public interface IAppUnitOfWork
{
    IBookingRepository Bookings { get; }
    IServiceRepository Services { get; }
    IAvailabilityRepository Availabilities { get; }
    IReviewRepository Reviews { get; }
    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
