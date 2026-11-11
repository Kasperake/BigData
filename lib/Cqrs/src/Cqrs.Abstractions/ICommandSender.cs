namespace Cqrs.Abstractions;

/// <summary>Sends commands to their handlers through the pipeline.</summary>
public interface ICommandSender
{
    /// <summary>Sends a command that produces no result.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>Sends a command that produces a result.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Signals that the caller no longer needs the operation.</param>
    /// <returns>The result of the command.</returns>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
