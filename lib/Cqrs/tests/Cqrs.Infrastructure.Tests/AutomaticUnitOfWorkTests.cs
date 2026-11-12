using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

public sealed class AutomaticUnitOfWorkTests
{
    private static ServiceProvider BuildWithUnitOfWork(Action<CqrsConfiguration>? configure = null) =>
        TestServices.Build(configure, services =>
        {
            services.AddScoped<RecordingUnitOfWork>();
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RecordingUnitOfWork>());
        });

    [Fact]
    public async Task Commands_RunInsideATransaction_InnermostInThePipeline_AndQueriesDoNot()
    {
        await using var provider = BuildWithUnitOfWork(cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));
        await mediator.SendAsync(new CreateOrderCommand("C-1000"));
        await mediator.QueryAsync(new GetOrderStatusQuery(Guid.NewGuid()));

        string[] expected =
        [
            "outer:before", "uow:begin", "handler:ArchiveOrderCommand", "uow:commit", "outer:after",
            "outer:before", "uow:begin", "handler:CreateOrderCommand", "uow:commit", "outer:after",
            "outer:before", "handler:GetOrderStatusQuery", "outer:after",
        ];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task Commands_RunInsideATransaction_WithoutAnyBehaviorRegistered()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        string[] expected = ["uow:begin", "handler:ArchiveOrderCommand", "uow:commit"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task FailedHandler_RollsBack_AndRethrowsTheOriginalException()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new LockedOrderCommand()));

        Assert.Equal("Order is locked.", exception.Message);
        string[] expected = ["uow:begin", "uow:rollback"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
        Assert.False(scope.ServiceProvider.GetRequiredService<IUnitOfWork>().HasActiveTransaction);
    }

    [Fact]
    public async Task FailedCommit_RollsBack()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RecordingUnitOfWork>().FailCommit = true;

        await Assert.ThrowsAsync<TimeoutException>(
            () => scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid())));

        string[] expected = ["uow:begin", "handler:ArchiveOrderCommand", "uow:rollback"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task CancelledCommand_IsRolledBack_WithAnUncancelledToken()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<RecordingUnitOfWork>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scope.ServiceProvider.GetRequiredService<IMediator>()
                .SendAsync(new ArchiveOrderCommand(Guid.NewGuid()), cancellation.Token));

        Assert.Contains("uow:rollback", scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
        Assert.False(unitOfWork.RollbackToken.CanBeCanceled);
    }

    [Fact]
    public async Task FailedRollback_SurfacesBothExceptions()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RecordingUnitOfWork>().FailRollback = true;

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new LockedOrderCommand()));

        Assert.Collection(
            exception.InnerExceptions,
            inner => Assert.IsType<InvalidOperationException>(inner),
            inner => Assert.IsType<IOException>(inner));
    }

    [Fact]
    public async Task CommandsDispatchedInsideAnActiveTransaction_JoinIt()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(CancellationToken.None);

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        string[] expected = ["uow:begin", "handler:ArchiveOrderCommand"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
        Assert.True(unitOfWork.HasActiveTransaction);
    }

    [Fact]
    public async Task ManuallyPersistedCommands_SkipTheUnitOfWork()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new RebuildSearchIndexCommand());
        await mediator.SendAsync(new ImportPricesCommand(Rows: 1));

        string[] expected = ["handler:RebuildSearchIndexCommand", "handler:ImportPricesCommand"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task RegisteredUnitOfWorkBehavior_OwnsTheUnitOfWork_SoTheAutomaticOneIsSkipped()
    {
        await using var provider = BuildWithUnitOfWork(cqrs => cqrs.AddOpenBehavior(typeof(RecordingUnitOfWorkBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        // Logged by RecordingUnitOfWorkBehavior; RecordingUnitOfWork would log the same, so check it never began.
        string[] expected = ["uow:begin", "handler:ArchiveOrderCommand", "uow:commit"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
        Assert.Equal(0, scope.ServiceProvider.GetRequiredService<RecordingUnitOfWork>().BeginCount);
    }

    [Fact]
    public async Task WithoutARegisteredUnitOfWork_CommandsRunAsBefore()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        string[] expected = ["handler:ArchiveOrderCommand"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    private sealed class RecordingUnitOfWork(ExecutionLog log) : IUnitOfWork
    {
        public bool FailCommit { get; set; }

        public bool FailRollback { get; set; }

        public int BeginCount { get; private set; }

        public CancellationToken RollbackToken { get; private set; }

        public bool HasActiveTransaction { get; private set; }

        public Task BeginAsync(CancellationToken cancellationToken)
        {
            log.Add("uow:begin");
            BeginCount++;
            HasActiveTransaction = true;
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (FailCommit)
            {
                throw new TimeoutException("Commit timed out.");
            }

            log.Add("uow:commit");
            HasActiveTransaction = false;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            RollbackToken = cancellationToken;

            if (FailRollback)
            {
                throw new IOException("Connection lost.");
            }

            log.Add("uow:rollback");
            HasActiveTransaction = false;
            return Task.CompletedTask;
        }
    }
}
