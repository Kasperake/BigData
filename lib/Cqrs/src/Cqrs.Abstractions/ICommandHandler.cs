namespace Cqrs.Abstractions;

/// <summary>Handles a command that produces no result.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Handles <paramref name="command"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Handles a command that produces a result.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Handles <paramref name="command"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>The result of the command.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
