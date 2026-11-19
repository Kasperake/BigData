namespace BigData.Application.Abstractions;

/// <summary>
/// Persists pending changes to the SQL Server data warehouse.
/// Command handlers that write to SQL call <see cref="SaveChangesAsync"/> explicitly; nothing is committed automatically.
/// </summary>
/// <remarks>
/// Intentionally does not derive from <c>Cqrs.Abstractions.IUnitOfWork</c>: registering an <c>IUnitOfWork</c> would make
/// the Cqrs pipeline wrap every command in a transaction again.
/// </remarks>
public interface ISqlUnitOfWork
{
    /// <summary>Saves all pending changes in one atomic operation.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
