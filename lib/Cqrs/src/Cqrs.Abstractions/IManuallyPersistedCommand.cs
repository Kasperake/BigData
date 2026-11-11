namespace Cqrs.Abstractions;

/// <summary>
/// Opts a command out of the unit of work. <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> passes it straight
/// to the next step of the pipeline, and its handler is responsible for persisting its own changes.
/// </summary>
/// <remarks>
/// <para>
/// Implement it alongside <see cref="ICommand"/> or <see cref="ICommand{TResult}"/>:
/// <c>public sealed record ImportPricesCommand(...) : ICommand&lt;int&gt;, IManuallyPersistedCommand;</c>
/// </para>
/// <para>
/// Use it for handlers that commit in batches, need their own transaction or isolation level, or persist through
/// something other than the module's DbContext. Every other pipeline behavior, such as logging and validation, still runs.
/// </para>
/// <para>
/// It does not detach the command from a unit of work that an outer command has already started: a manually persisted
/// command sent from inside another command's handler still runs in that transaction. Nor does it affect the commands its
/// own handler sends; they get their own unit of work unless they also implement this interface.
/// </para>
/// </remarks>
public interface IManuallyPersistedCommand : IBaseCommand
{
}
