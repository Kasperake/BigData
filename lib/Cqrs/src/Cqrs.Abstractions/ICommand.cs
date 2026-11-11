namespace Cqrs.Abstractions;

/// <summary>
/// Marker shared by every command, with or without a result.
/// </summary>
/// <remarks>
/// Do not implement this interface directly; implement <see cref="ICommand"/> or <see cref="ICommand{TResult}"/>.
/// It exists so pipeline behaviors can be scoped to commands with a generic constraint, for example
/// <c>UnitOfWorkBehavior&lt;TRequest, TResponse&gt; ... where TRequest : IBaseCommand</c>.
/// </remarks>
public interface IBaseCommand
{
}

/// <summary>
/// A request to change state that produces no result. It is handled by exactly one
/// <see cref="ICommandHandler{TCommand}"/>.
/// </summary>
public interface ICommand : IBaseCommand
{
}

/// <summary>
/// A request to change state that produces a result, such as the identifier of a created aggregate.
/// It is handled by exactly one <see cref="ICommandHandler{TCommand, TResult}"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface ICommand<TResult> : IBaseCommand
{
}
