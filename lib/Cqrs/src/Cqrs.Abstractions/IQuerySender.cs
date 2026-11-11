namespace Cqrs.Abstractions;

/// <summary>Sends queries to their handlers through the pipeline.</summary>
public interface IQuerySender
{
    /// <summary>Sends a query.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the result.</param>
    /// <returns>The result of the query.</returns>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
