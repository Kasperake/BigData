namespace BigData.Application.Abstractions;

/// <summary>
/// Persists pending changes to MongoDB (the data lake).
/// Command handlers that write to MongoDB call <see cref="SaveChangesAsync"/> explicitly; nothing is committed automatically.
/// </summary>
/// <remarks>
/// Intentionally does not derive from <c>Cqrs.Abstractions.IUnitOfWork</c>: registering an <c>IUnitOfWork</c> would make
/// the Cqrs pipeline wrap every command in a transaction again.
/// </remarks>
public interface IMongoUnitOfWork
{
    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
