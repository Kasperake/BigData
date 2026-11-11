using Cqrs.Abstractions;

namespace Cqrs.Infrastructure.Internal;

/// <summary>
/// The mediator. Registered as scoped, so <paramref name="serviceProvider"/> is the caller's scope and handlers,
/// behaviors and their scoped dependencies are resolved from it.
/// </summary>
internal sealed class Mediator(IServiceProvider serviceProvider, INotificationPublishStrategy publishStrategy) : IMediator
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return DispatcherCache
            .ForVoidCommand(command.GetType())
            .DispatchAsync(command, serviceProvider, cancellationToken);
    }

    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return DispatcherCache
            .ForCommand<TResult>(command.GetType())
            .DispatchAsync(command, serviceProvider, cancellationToken);
    }

    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return DispatcherCache
            .ForQuery<TResult>(query.GetType())
            .DispatchAsync(query, serviceProvider, cancellationToken);
    }

    public Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Dispatch on the runtime type: a domain event published through a base-typed reference must still
        // reach the handlers of its concrete type.
        return DispatcherCache
            .ForNotification(notification.GetType())
            .DispatchAsync(notification, serviceProvider, publishStrategy, cancellationToken);
    }
}
