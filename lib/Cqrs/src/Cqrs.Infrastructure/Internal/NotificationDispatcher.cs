using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Internal;

internal abstract class NotificationDispatcherBase
{
    public abstract Task DispatchAsync(
        INotification notification,
        IServiceProvider serviceProvider,
        INotificationPublishStrategy publishStrategy,
        CancellationToken cancellationToken);
}

internal sealed class NotificationDispatcher<TNotification> : NotificationDispatcherBase
    where TNotification : INotification
{
    private static readonly RequestMetadata Metadata = new(RequestKind.Notification, typeof(TNotification));

    public override async Task DispatchAsync(
        INotification notification,
        IServiceProvider serviceProvider,
        INotificationPublishStrategy publishStrategy,
        CancellationToken cancellationToken)
    {
        using var activity = CqrsDiagnostics.StartActivity(Metadata);

        try
        {
            var resolved = serviceProvider.GetServices<INotificationHandler<TNotification>>();
            var handlers = resolved as INotificationHandler<TNotification>[] ?? [.. resolved];

            if (activity is { IsAllDataRequested: true })
            {
                activity.SetTag(CqrsDiagnostics.HandlerCountTag, handlers.Length);
            }

            if (handlers.Length == 0)
            {
                return;
            }

            var executors = new NotificationHandlerExecutor[handlers.Length];
            for (var i = 0; i < handlers.Length; i++)
            {
                var handler = handlers[i];
                executors[i] = new NotificationHandlerExecutor(
                    handler,
                    (published, token) => handler.HandleAsync((TNotification)published, token));
            }

            await publishStrategy.PublishAsync(executors, notification, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsDiagnostics.RecordFailure(activity, exception, cancellationToken);
            throw;
        }
    }
}
