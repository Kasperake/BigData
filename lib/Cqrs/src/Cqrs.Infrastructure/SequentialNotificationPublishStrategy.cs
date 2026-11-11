using Cqrs.Abstractions;

namespace Cqrs.Infrastructure;

/// <summary>
/// Invokes handlers one at a time, in registration order, and stops at the first failure. This is the default.
/// </summary>
/// <remarks>
/// Safe with scoped dependencies such as a DbContext, which the handlers share with the publisher. When publishing
/// inside a transaction, the first failure propagates to the caller so the transaction can roll back.
/// </remarks>
public sealed class SequentialNotificationPublishStrategy : INotificationPublishStrategy
{
    /// <inheritdoc />
    public async Task PublishAsync(
        IReadOnlyList<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(notification);

        for (var i = 0; i < handlers.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handlers[i].HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
