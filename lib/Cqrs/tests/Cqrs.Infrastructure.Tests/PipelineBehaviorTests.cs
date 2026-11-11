using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task Behaviors_RunInRegistrationOrder_FirstRegisteredIsOutermost()
    {
        await using var provider = TestServices.Build(cqrs => cqrs
            .AddOpenBehavior(typeof(OuterBehavior<,>))
            .AddOpenBehavior(typeof(InnerBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        string[] expected = ["outer:before", "inner:before", "handler:ArchiveOrderCommand", "inner:after", "outer:after"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task CommandConstrainedBehavior_RunsForCommands_AndIsSkippedForQueries()
    {
        await using var provider = TestServices.Build(cqrs => cqrs.AddOpenBehavior(typeof(CommandOnlyBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));
        await mediator.SendAsync(new CreateOrderCommand("C-1000"));
        await mediator.QueryAsync(new GetOrderStatusQuery(Guid.NewGuid()));

        string[] expected =
        [
            "command-only:ArchiveOrderCommand", "handler:ArchiveOrderCommand",
            "command-only:CreateOrderCommand", "handler:CreateOrderCommand",
            "handler:GetOrderStatusQuery",
        ];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task Behavior_ThatDoesNotCallNext_ShortCircuitsTheHandler()
    {
        await using var provider = TestServices.Build(cqrs => cqrs.AddOpenBehavior(typeof(ShortCircuitBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();

        var status = await scope.ServiceProvider.GetRequiredService<IMediator>().QueryAsync(new GetOrderStatusQuery(Guid.NewGuid()));

        Assert.Null(status);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task Behavior_CanInvokeNextMoreThanOnce()
    {
        await using var provider = TestServices.Build(cqrs => cqrs.AddOpenBehavior(typeof(RetryOnceBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        string[] expected = ["retry:attempt", "handler:ArchiveOrderCommand", "retry:attempt", "handler:ArchiveOrderCommand"];
        Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task Behavior_CanReplaceTheCancellationTokenSeenByTheHandler()
    {
        await using var provider = TestServices.Build(
            cqrs => cqrs.AddOpenBehavior(typeof(TokenReplacingBehavior<,>)),
            services => services.AddSingleton<ReplacementToken>());
        await using var scope = provider.CreateAsyncScope();
        using var callerSource = new CancellationTokenSource();

        var seenByHandler = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .QueryAsync(new EchoCancellationTokenQuery(), callerSource.Token);

        Assert.Equal(provider.GetRequiredService<ReplacementToken>().Token, seenByHandler);
        Assert.NotEqual(callerSource.Token, seenByHandler);
    }

    [Fact]
    public async Task Behavior_WithoutReplacement_PassesTheCallerTokenToTheHandler()
    {
        await using var provider = TestServices.Build(cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();
        using var callerSource = new CancellationTokenSource();

        var seenByHandler = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .QueryAsync(new EchoCancellationTokenQuery(), callerSource.Token);

        Assert.Equal(callerSource.Token, seenByHandler);
    }

    [Fact]
    public async Task HandlerException_UnwindsThroughBehaviors()
    {
        await using var provider = TestServices.Build(cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new LockedOrderCommand()));

        Assert.Equal("outer:before", Assert.Single(scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries));
    }

    [Fact]
    public async Task MissingHandler_FailsBeforeAnyBehaviorRuns()
    {
        await using var provider = TestServices.Build(cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<,>)));
        await using var scope = provider.CreateAsyncScope();

        await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new UnhandledCommand()));

        Assert.Empty(scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }
}
