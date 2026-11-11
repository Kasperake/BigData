namespace Cqrs.Abstractions;

/// <summary>Handles a query.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Handles <paramref name="query"/>.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the result.</param>
    /// <returns>The result of the query.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
