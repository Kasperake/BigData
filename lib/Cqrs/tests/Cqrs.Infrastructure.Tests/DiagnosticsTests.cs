using System.Collections.Concurrent;
using System.Diagnostics;
using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

public sealed class DiagnosticsTests
{
    [Fact]
    public async Task Dispatch_RecordsActivities_NestedUnderTheCurrentActivity()
    {
        using var testSource = new ActivitySource("Cqrs.Infrastructure.Tests");
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is CqrsDiagnostics.ActivitySourceName or "Cqrs.Infrastructure.Tests",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);

        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        ActivityTraceId traceId;
        using (var root = testSource.StartActivity("test-root"))
        {
            Assert.NotNull(root);
            traceId = root.TraceId;

            await mediator.SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));
            await mediator.PublishAsync(new OrderPlaced(Guid.NewGuid()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new LockedOrderCommand()));
        }

        // Filter on this test's trace: other tests may dispatch concurrently while the listener is attached.
        var activities = stopped
            .Where(activity => activity.TraceId == traceId && activity.Source.Name == CqrsDiagnostics.ActivitySourceName)
            .ToDictionary(activity => activity.DisplayName);

        var command = activities["command ArchiveOrderCommand"];
        Assert.Equal("command", command.GetTagItem(CqrsDiagnostics.RequestKindTag));
        Assert.Equal(typeof(ArchiveOrderCommand).FullName, command.GetTagItem(CqrsDiagnostics.RequestTypeTag));
        Assert.Equal(ActivityStatusCode.Unset, command.Status);

        var notification = activities["notification OrderPlaced"];
        Assert.Equal(2, notification.GetTagItem(CqrsDiagnostics.HandlerCountTag));

        var failed = activities["command LockedOrderCommand"];
        Assert.Equal(ActivityStatusCode.Error, failed.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, failed.GetTagItem(CqrsDiagnostics.ErrorTypeTag));
    }
}
