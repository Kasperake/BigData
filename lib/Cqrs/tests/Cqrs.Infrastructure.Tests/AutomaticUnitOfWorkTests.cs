using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

public sealed class AutomaticUnitOfWorkTests
{
    private static ServiceProvider BuildWithUnitOfWork(Action<CqrsConfiguration>? configure = null) =>
        TestServices.Build(configure, services => services.AddScoped<IUnitOfWork, RecordingUnitOfWork>());

    [Fact]
    public async Task Commands_CommitTheRegisteredUnitOfWork_AfterTheHandler_AndQueriesDoNot()
    {
        await using var provider = BuildWithUnitOfWork(cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));
        await mediator.SendAsync(new CreateOrderCommand("C-1000"));
        await mediator.QueryAsync(new GetOrderStatusQuery(Guid.NewGuid()));

        string[] expected =
        [
            "outer:before", "handler:ArchiveOrderCommand", "uow:commit", "outer:after",
            "outer:before", "handler:CreateOrderCommand", "uow:commit", "outer:after",
            "outer:before", "handler:GetOrderStatusQuery", "outer:after",
        ];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task Commands_CommitWithoutAnyBehaviorRegistered()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        string[] expected = ["handler:ArchiveOrderCommand", "uow:commit"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task FailedHandler_DoesNotCommit()
    {
        await using var provider = BuildWithUnitOfWork();
        await using var scope = provider.CreateAsyncScope();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new LockedOrderCommand()));

        Assert.Empty(scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task ManuallyPersistedCommands_DoNotCommit()
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
    public async Task RegisteredUnitOfWorkBehavior_OwnsTheUnitOfWork_SoTheAutomaticCommitIsSkipped()
    {
        await using var provider = BuildWithUnitOfWork(cqrs => cqrs.AddOpenBehavior(typeof(RecordingUnitOfWorkBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        string[] expected = ["uow:begin", "handler:ArchiveOrderCommand", "uow:commit"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
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
        public Task CommitAsync(CancellationToken cancellationToken)
        {
            log.Add("uow:commit");
            return Task.CompletedTask;
        }
    }
}
