namespace Cqrs.Abstractions;

/// <summary>
/// Persists the changes a command made. When an implementation is registered in the container, every dispatched
/// command commits through it automatically after its handler succeeds; no pipeline behavior has to be registered.
/// </summary>
/// <remarks>
/// <para>
/// Register it with the same lifetime as the state it commits, typically scoped alongside a DbContext:
/// <c>services.AddScoped&lt;IUnitOfWork&gt;(sp =&gt; sp.GetRequiredService&lt;OrdersDbContext&gt;())</c>.
/// </para>
/// <para>
/// The automatic commit is skipped for commands implementing <see cref="IManuallyPersistedCommand"/>, for queries,
/// and when a <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> is registered for the command, because that
/// behavior then owns the unit of work.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Persists the pending changes.</summary>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>A task that completes when the changes are persisted.</returns>
    Task CommitAsync(CancellationToken cancellationToken);
}
