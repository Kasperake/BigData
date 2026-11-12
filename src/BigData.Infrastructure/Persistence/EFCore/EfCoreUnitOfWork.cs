using Cqrs.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace BigData.Infrastructure.Persistence.EFCore;

/// <summary>
/// Runs each command in a database transaction on the scoped <see cref="WeatherDbContext"/>. The CQRS pipeline calls
/// it automatically: begin before the handler, save and commit after it, roll back if anything throws.
/// </summary>
public sealed class EfCoreUnitOfWork(WeatherDbContext dbContext) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public bool HasActiveTransaction => _transaction is not null;

    public async Task BeginAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("There is no active transaction to commit.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            // Drop the failed command's tracked changes so a later command in the same scope does not save them.
            dbContext.ChangeTracker.Clear();
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
