namespace Cqrs.Abstractions;

/// <summary>
/// A transactional unit of work. When an implementation is registered in the container, every dispatched command runs
/// inside it automatically: <see cref="BeginAsync"/> before the handler, <see cref="CommitAsync"/> after it succeeds,
/// and <see cref="RollbackAsync"/> if the handler or the commit throws. No pipeline behavior has to be registered.
/// </summary>
/// <remarks>
/// <para>
/// Register it scoped, alongside the state it persists (typically a DbContext), so a command's handler and its unit
/// of work share that state: <c>services.AddScoped&lt;IUnitOfWork, OrdersUnitOfWork&gt;()</c>.
/// </para>
/// <para>
/// A command dispatched while a transaction is already active (for example from inside another command's handler)
/// joins that transaction; only the outermost command begins, commits or rolls back.
/// </para>
/// <para>
/// The unit of work is skipped for commands implementing <see cref="IManuallyPersistedCommand"/>, for queries, and
/// when a <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> is registered for the command, because that behavior
/// then owns the unit of work.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Whether a transaction has begun and has not yet been committed or rolled back.</summary>
    bool HasActiveTransaction { get; }

    /// <summary>Begins a transaction.</summary>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>A task that completes when the transaction has begun.</returns>
    Task BeginAsync(CancellationToken cancellationToken);

    /// <summary>Persists the pending changes and commits the transaction.</summary>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>A task that completes when the transaction is committed.</returns>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Rolls back the transaction and discards the pending changes. Called with <see cref="CancellationToken.None"/>,
    /// so a cancelled command is still rolled back.
    /// </summary>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>A task that completes when the transaction is rolled back.</returns>
    Task RollbackAsync(CancellationToken cancellationToken);
}
