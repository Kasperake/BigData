namespace Cqrs.Abstractions;

/// <summary>Publishes notifications to their handlers.</summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes <paramref name="notification"/> to every handler of its runtime type.
    /// Completes immediately when there are no handlers.
    /// </summary>
    /// <param name="notification">
    /// The notification. It is dispatched on its runtime type, so publishing a collection of domain events
    /// typed as a base interface reaches the handlers of each concrete event.
    /// </param>
    /// <param name="cancellationToken">Signals that the publisher no longer needs the operation.</param>
    /// <returns>A task that completes when the notification has been handled.</returns>
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
