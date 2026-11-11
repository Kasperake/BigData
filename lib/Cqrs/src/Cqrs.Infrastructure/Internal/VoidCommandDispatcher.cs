using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Internal;

internal abstract class VoidCommandDispatcherBase
{
    public abstract Task DispatchAsync(ICommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class VoidCommandDispatcher<TCommand> : VoidCommandDispatcherBase
    where TCommand : ICommand
{
    private static readonly RequestMetadata Metadata = new(RequestKind.Command, typeof(TCommand));

    public override async Task DispatchAsync(ICommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var activity = CqrsDiagnostics.StartActivity(Metadata);

        try
        {
            var typedCommand = (TCommand)command;

            // Resolve the handler before any behavior runs, so a missing handler fails before side effects
            // such as opening a transaction.
            var handler = serviceProvider.GetService<ICommandHandler<TCommand>>()
                ?? throw new HandlerNotFoundException(typeof(TCommand), typeof(ICommandHandler<TCommand>));

            var behaviors = RequestPipeline.ResolveBehaviors<TCommand, Unit>(serviceProvider);
            if (behaviors.Length == 0)
            {
                await handler.HandleAsync(typedCommand, cancellationToken).ConfigureAwait(false);
                return;
            }

            await RequestPipeline
                .InvokeAsync(
                    typedCommand,
                    behaviors,
                    async token =>
                    {
                        await handler.HandleAsync(typedCommand, token).ConfigureAwait(false);
                        return Unit.Value;
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsDiagnostics.RecordFailure(activity, exception, cancellationToken);
            throw;
        }
    }
}
