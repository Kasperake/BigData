using System.Collections.Concurrent;
using Cqrs.Abstractions;
using Cqrs.Tests.Application;

namespace Cqrs.Infrastructure.Tests;

public sealed class NotificationPublishStrategyTests
{
    private static readonly OrderPlaced Notification = new(Guid.NewGuid());

    [Fact]
    public async Task Sequential_RunsHandlersInOrder_AndStopsAtTheFirstFailure()
    {
        var invoked = new ConcurrentQueue<string>();
        NotificationHandlerExecutor[] handlers =
        [
            Executor((_, _) => { invoked.Enqueue("first"); return Task.CompletedTask; }),
            Executor((_, _) => { invoked.Enqueue("second"); throw new InvalidOperationException("second failed"); }),
            Executor((_, _) => { invoked.Enqueue("third"); return Task.CompletedTask; }),
        ];

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SequentialNotificationPublishStrategy().PublishAsync(handlers, Notification, CancellationToken.None));

        Assert.Equal("second failed", exception.Message);
        string[] expected = ["first", "second"];
        Assert.Equal(expected, invoked);
    }

    [Fact]
    public async Task Sequential_DoesNotStartHandlersOnceCancelled()
    {
        var invoked = new ConcurrentQueue<string>();
        using var source = new CancellationTokenSource();
        NotificationHandlerExecutor[] handlers =
        [
            Executor((_, _) => { invoked.Enqueue("first"); source.Cancel(); return Task.CompletedTask; }),
            Executor((_, _) => { invoked.Enqueue("second"); return Task.CompletedTask; }),
        ];

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new SequentialNotificationPublishStrategy().PublishAsync(handlers, Notification, source.Token));

        Assert.Equal("first", Assert.Single(invoked));
    }

    [Fact]
    public async Task Parallel_RunsEveryHandler_AndSurfacesEveryFailure()
    {
        var invoked = new ConcurrentQueue<string>();
        NotificationHandlerExecutor[] handlers =
        [
            Executor((_, _) => { invoked.Enqueue("sync-failure"); throw new InvalidOperationException("sync"); }),
            Executor((_, _) => { invoked.Enqueue("success"); return Task.CompletedTask; }),
            Executor(async (_, _) =>
            {
                invoked.Enqueue("async-failure");
                await Task.Yield();
                throw new InvalidOperationException("async");
            }),
        ];

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => new ParallelNotificationPublishStrategy().PublishAsync(handlers, Notification, CancellationToken.None));

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Equal(3, invoked.Count);
    }

    [Fact]
    public async Task Parallel_SingleFailure_IsRethrownUnwrapped()
    {
        NotificationHandlerExecutor[] handlers =
        [
            Executor((_, _) => Task.CompletedTask),
            Executor((_, _) => Task.FromException(new InvalidOperationException("only failure"))),
        ];

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ParallelNotificationPublishStrategy().PublishAsync(handlers, Notification, CancellationToken.None));

        Assert.Equal("only failure", exception.Message);
    }

    private static NotificationHandlerExecutor Executor(Func<INotification, CancellationToken, Task> callback) =>
        new(new object(), callback);
}
