using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

public sealed class UnitOfWorkBehaviorTests
{
    [Fact]
    public async Task Commands_RunInsideTheUnitOfWork_AndQueriesNeverReachIt()
    {
        await using var provider = TestServices.Build(cqrs => cqrs.AddOpenBehavior(typeof(RecordingUnitOfWorkBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));
        await mediator.SendAsync(new CreateOrderCommand("C-1000"));
        await mediator.QueryAsync(new GetOrderStatusQuery(Guid.NewGuid()));

        string[] expected =
        [
            "uow:begin", "handler:ArchiveOrderCommand", "uow:commit",
            "uow:begin", "handler:CreateOrderCommand", "uow:commit",
            "handler:GetOrderStatusQuery",
        ];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task ManuallyPersistedCommands_BypassTheUnitOfWork_ButNotOtherBehaviors()
    {
        await using var provider = TestServices.Build(cqrs => cqrs
            .AddOpenBehavior(typeof(OuterBehavior<,>))
            .AddOpenBehavior(typeof(RecordingUnitOfWorkBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new RebuildSearchIndexCommand());
        var imported = await mediator.SendAsync(new ImportPricesCommand(Rows: 250));

        Assert.Equal(250, imported);
        string[] expected =
        [
            "outer:before", "handler:RebuildSearchIndexCommand", "outer:after",
            "outer:before", "handler:ImportPricesCommand", "outer:after",
        ];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task ManuallyPersistedCommand_BypassesTheUnitOfWork_WhenTheBehaviorIsInvokedDirectly()
    {
        var log = new ExecutionLog();
        var behavior = new RecordingUnitOfWorkBehavior<ImportPricesCommand, int>(log);

        var result = await behavior.HandleAsync(new ImportPricesCommand(Rows: 3), _ => Task.FromResult(3), CancellationToken.None);

        Assert.Equal(3, result);
        Assert.Empty(log.Entries);
    }
}
