using App.Contracts;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.DAL.EF;

internal sealed class EfTransaction : ITransaction
{
    private readonly IDbContextTransaction _tx;

    public EfTransaction(IDbContextTransaction tx) => _tx = tx;

    public Task CommitAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);

    public ValueTask DisposeAsync() => _tx.DisposeAsync();
}
