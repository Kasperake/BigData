using Cqrs.Abstractions;

namespace Cqrs.Infrastructure;

/// <summary>
/// Decides how a notification is delivered to its handlers, for example one at a time or concurrently.
/// </summary>
/// <remarks>
/// Implementations are registered as singletons, so they must be stateless and thread-safe.
/// Select one with <see cref="CqrsConfiguration.UseNotificationPublishStrategy{TStrategy}"/>.
/// </remarks>
public interface INotificationPublishStrategy
{
    /// <summary>Delivers <paramref name="notification"/> to <paramref name="handlers"/>.</summary>
    /// <param name="handlers">The handlers, in registration order. Never empty.</param>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Signals that the publisher no longer needs the operation.</param>
    /// <returns>A task that completes when the notification has been delivered.</returns>
    Task PublishAsync(
        IReadOnlyList<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken);
}
