namespace Cqrs.Abstractions;

/// <summary>Handles a notification.</summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <remarks>
/// Handlers are matched on the exact runtime type of the published notification. Handlers registered for a
/// base class or interface of that type are not invoked.
/// </remarks>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Handles <paramref name="notification"/>.</summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Signals that the publisher no longer needs the operation.</param>
    /// <returns>A task that completes when the notification has been handled.</returns>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
