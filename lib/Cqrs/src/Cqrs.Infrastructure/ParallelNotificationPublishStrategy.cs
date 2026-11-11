using System.Diagnostics.CodeAnalysis;
using Cqrs.Abstractions;

namespace Cqrs.Infrastructure;

/// <summary>
/// Starts all handlers concurrently and waits for all of them. Every handler runs even if another fails.
/// A single failure is rethrown as is; multiple failures are thrown together as an <see cref="AggregateException"/>.
/// </summary>
/// <remarks>
/// Handlers share the publisher's DI scope. Only use this strategy when no handler of a published notification uses
/// a scoped dependency that is not thread-safe, such as an Entity Framework DbContext.
/// </remarks>
public sealed class ParallelNotificationPublishStrategy : INotificationPublishStrategy
{
    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A synchronous failure is captured into a faulted task and rethrown after all handlers ran.")]
    public async Task PublishAsync(
        IReadOnlyList<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(notification);

        var tasks = new Task[handlers.Count];
        for (var i = 0; i < handlers.Count; i++)
        {
            try
            {
                tasks[i] = handlers[i].HandlerCallback(notification, cancellationToken);
            }
            catch (Exception exception)
            {
                // A handler that throws before its first await must not prevent the other handlers from starting.
                tasks[i] = Task.FromException(exception);
            }
        }

        var whenAll = Task.WhenAll(tasks);
        try
        {
            await whenAll.ConfigureAwait(false);
        }
        catch (Exception) when (whenAll.Exception is { InnerExceptions.Count: > 1 } aggregate)
        {
            // Awaiting surfaces only the first failure; do not hide the others.
            throw aggregate;
        }
    }
}
