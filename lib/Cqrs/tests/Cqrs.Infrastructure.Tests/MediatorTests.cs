using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

public sealed class MediatorTests
{
    [Fact]
    public async Task SendAsync_CommandWithoutResult_InvokesItsHandler()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(new ArchiveOrderCommand(Guid.NewGuid()));

        Assert.Equal("handler:ArchiveOrderCommand", Assert.Single(scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries));
    }

    [Fact]
    public async Task SendAsync_CommandWithResult_ReturnsTheHandlerResult()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();

        var orderId = await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(new CreateOrderCommand("C-1000"));

        Assert.NotEqual(Guid.Empty, orderId);
    }

    [Fact]
    public async Task QueryAsync_ReturnsTheHandlerResult()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();

        var status = await scope.ServiceProvider.GetRequiredService<IQuerySender>().QueryAsync(new GetOrderStatusQuery(Guid.NewGuid()));

        Assert.Equal("Shipped", status);
    }

    [Fact]
    public async Task SendAsync_WithoutHandler_ThrowsHandlerNotFoundException()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => mediator.SendAsync(new UnhandledCommand()));

        Assert.Equal(typeof(UnhandledCommand), exception.RequestType);
        Assert.Equal(typeof(ICommandHandler<UnhandledCommand>), exception.HandlerType);
        Assert.Contains("ICommandHandler<UnhandledCommand>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_HandlerThrows_PropagatesTheOriginalException()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new LockedOrderCommand()));

        Assert.Equal("Order is locked.", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_InvokesEveryHandler()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new OrderPlaced(Guid.NewGuid()));

        var entries = scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries;
        Assert.Equal(2, entries.Count);
        Assert.Contains("notification:confirmation", entries);
        Assert.Contains("notification:statistics", entries);
    }

    [Fact]
    public async Task PublishAsync_ThroughBaseTypedReference_DispatchesOnTheRuntimeType()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();
        IReadOnlyList<INotification> domainEvents = [new OrderPlaced(Guid.NewGuid())];

        foreach (var domainEvent in domainEvents)
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(domainEvent);
        }

        Assert.Equal(2, scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries.Count);
    }

    [Fact]
    public async Task PublishAsync_WithoutHandlers_Completes()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new OrderCancelled(Guid.NewGuid()));

        Assert.Empty(scope.ServiceProvider.GetRequiredService<ExecutionLog>().Entries);
    }

    [Fact]
    public async Task Dispatch_NullRequest_ThrowsArgumentNullException()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.SendAsync((ICommand)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.SendAsync((ICommand<Guid>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.QueryAsync((IQuery<string>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.PublishAsync(null!));
    }

    [Fact]
    public async Task Mediator_IsScoped_AndEveryFacadeResolvesTheSameInstance()
    {
        await using var provider = TestServices.Build();
        await using var scope = provider.CreateAsyncScope();
        await using var otherScope = provider.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Assert.Same(mediator, scope.ServiceProvider.GetRequiredService<ICommandSender>());
        Assert.Same(mediator, scope.ServiceProvider.GetRequiredService<IQuerySender>());
        Assert.Same(mediator, scope.ServiceProvider.GetRequiredService<IPublisher>());
        Assert.NotSame(mediator, otherScope.ServiceProvider.GetRequiredService<IMediator>());
    }

    [Fact]
    public async Task Mediator_ResolvedFromTheRootProvider_IsRejectedByScopeValidation()
    {
        await using var provider = TestServices.Build();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IMediator>());
    }
}
