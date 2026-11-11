namespace Cqrs.Abstractions;

/// <summary>
/// Marker shared by every query.
/// </summary>
/// <remarks>
/// Do not implement this interface directly; implement <see cref="IQuery{TResult}"/>.
/// It exists so pipeline behaviors can be scoped to queries with a generic constraint,
/// for example <c>where TRequest : IBaseQuery</c>.
/// </remarks>
public interface IBaseQuery
{
}

/// <summary>
/// A request for data that must not change state. It is handled by exactly one
/// <see cref="IQueryHandler{TQuery, TResult}"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface IQuery<TResult> : IBaseQuery
{
}
