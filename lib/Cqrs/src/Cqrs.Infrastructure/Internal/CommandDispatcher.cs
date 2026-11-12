using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Internal;

internal abstract class CommandDispatcherBase<TResult>
{
    public abstract Task<TResult> DispatchAsync(
        ICommand<TResult> command,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class CommandDispatcher<TCommand, TResult> : CommandDispatcherBase<TResult>
    where TCommand : ICommand<TResult>
{
    private static readonly RequestMetadata Metadata = new(RequestKind.Command, typeof(TCommand));

    public override async Task<TResult> DispatchAsync(
        ICommand<TResult> command,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using var activity = CqrsDiagnostics.StartActivity(Metadata);

        try
        {
            var typedCommand = (TCommand)command;

            var handler = serviceProvider.GetService<ICommandHandler<TCommand, TResult>>()
                ?? throw new HandlerNotFoundException(typeof(TCommand), typeof(ICommandHandler<TCommand, TResult>));

            var behaviors = RequestPipeline.ResolveBehaviors<TCommand, TResult>(serviceProvider);
            var handle = RequestPipeline.WithUnitOfWork(
                typedCommand,
                behaviors,
                token => handler.HandleAsync(typedCommand, token),
                serviceProvider);

            if (behaviors.Length == 0)
            {
                return await handle(cancellationToken).ConfigureAwait(false);
            }

            return await RequestPipeline
                .InvokeAsync(typedCommand, behaviors, handle, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsDiagnostics.RecordFailure(activity, exception, cancellationToken);
            throw;
        }
    }
}
