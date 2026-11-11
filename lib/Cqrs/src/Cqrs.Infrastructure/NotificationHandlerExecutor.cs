using Cqrs.Abstractions;

namespace Cqrs.Infrastructure;

/// <summary>A resolved notification handler, ready to be invoked by an <see cref="INotificationPublishStrategy"/>.</summary>
/// <param name="HandlerInstance">The handler instance, for example to log which handler failed.</param>
/// <param name="HandlerCallback">Invokes the handler with a notification and a cancellation token.</param>
public readonly record struct NotificationHandlerExecutor(
    object HandlerInstance,
    Func<INotification, CancellationToken, Task> HandlerCallback);
