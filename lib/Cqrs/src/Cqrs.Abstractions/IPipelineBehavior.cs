using System.Diagnostics.CodeAnalysis;

namespace Cqrs.Abstractions;

/// <summary>
/// Wraps the handling of commands and queries to add cross-cutting concerns such as logging, validation,
/// authorization, retries or transactions.
/// </summary>
/// <typeparam name="TRequest">The command or query type.</typeparam>
/// <typeparam name="TResponse">
/// The response type. For an <see cref="ICommand"/> (no result) this is <see cref="Unit"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Behaviors run in registration order; the first registered behavior is the outermost.
/// </para>
/// <para>
/// A behavior applies to every command and query unless its implementation constrains <typeparamref name="TRequest"/>.
/// <c>where TRequest : IBaseCommand</c> limits it to commands, <c>where TRequest : IBaseQuery</c> to queries,
/// and a custom marker interface to any subset. Behaviors whose constraints a request does not satisfy are skipped
/// for that request.
/// </para>
/// <para>
/// Notifications do not flow through the pipeline.
/// </para>
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles <paramref name="request"/>, typically by doing work before and/or after invoking <paramref name="next"/>.
    /// </summary>
    /// <param name="request">The command or query.</param>
    /// <param name="next">
    /// The rest of the pipeline. Invoke it to continue; return without invoking it to short-circuit.
    /// It may be invoked more than once, for example by a retry behavior.
    /// </param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>The response for the request.</returns>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "'next' is the established name in middleware pipelines; VB.NET consumers are not a target.")]
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
