using System.Diagnostics.CodeAnalysis;

namespace Cqrs.Abstractions;

/// <summary>
/// Base class for the unit-of-work (transaction) pipeline behavior. It applies to commands only, and guarantees that
/// commands implementing <see cref="IManuallyPersistedCommand"/> bypass it.
/// </summary>
/// <typeparam name="TRequest">The command type.</typeparam>
/// <typeparam name="TResponse">The response type; <see cref="Unit"/> for commands without a result.</typeparam>
/// <remarks>
/// <para>
/// Derive from this class rather than implementing <see cref="IPipelineBehavior{TRequest, TResponse}"/> directly.
/// <see cref="HandleAsync"/> is not virtual, so a derived behavior cannot forget the bypass; it only implements
/// <see cref="HandleInUnitOfWorkAsync"/>.
/// </para>
/// <para>
/// Register the derived open generic type last, so it is the innermost behavior and validation failures never open a
/// transaction: <c>cqrs.AddOpenBehavior(typeof(OrdersUnitOfWorkBehavior&lt;,&gt;))</c>.
/// </para>
/// </remarks>
public abstract class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    /// <summary>
    /// Passes an <see cref="IManuallyPersistedCommand"/> straight to <paramref name="next"/>; runs every other command
    /// through <see cref="HandleInUnitOfWorkAsync"/>.
    /// </summary>
    /// <param name="request">The command.</param>
    /// <param name="next">The rest of the pipeline.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>The response for the command.</returns>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "'next' is the established name in middleware pipelines; VB.NET consumers are not a target.")]
    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        return request is IManuallyPersistedCommand
            ? next(cancellationToken)
            : HandleInUnitOfWorkAsync(request, next, cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="next"/> inside a unit of work: typically begin a transaction (unless one is already active),
    /// invoke <paramref name="next"/>, save changes and commit, and roll back on failure.
    /// </summary>
    /// <param name="request">The command. Never an <see cref="IManuallyPersistedCommand"/>.</param>
    /// <param name="next">The rest of the pipeline.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>The response for the command.</returns>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "'next' is the established name in middleware pipelines; VB.NET consumers are not a target.")]
    protected abstract Task<TResponse> HandleInUnitOfWorkAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
