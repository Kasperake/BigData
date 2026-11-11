using Cqrs.Abstractions;

namespace Cqrs.Tests.Application;

// ---- Commands -------------------------------------------------------------------------------------------------

public sealed record CreateOrderCommand(string CustomerNumber) : ICommand<Guid>;

internal sealed class CreateOrderCommandHandler(ExecutionLog log) : ICommandHandler<CreateOrderCommand, Guid>
{
    public Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        log.Add($"handler:{nameof(CreateOrderCommand)}");
        return Task.FromResult(Guid.NewGuid());
    }
}

public sealed record ArchiveOrderCommand(Guid OrderId) : ICommand;

internal sealed class ArchiveOrderCommandHandler(ExecutionLog log) : ICommandHandler<ArchiveOrderCommand>
{
    public Task HandleAsync(ArchiveOrderCommand command, CancellationToken cancellationToken)
    {
        log.Add($"handler:{nameof(ArchiveOrderCommand)}");
        return Task.CompletedTask;
    }
}

public sealed record LockedOrderCommand : ICommand;

internal sealed class LockedOrderCommandHandler : ICommandHandler<LockedOrderCommand>
{
    public Task HandleAsync(LockedOrderCommand command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Order is locked.");
}

/// <summary>Deliberately has no handler.</summary>
public sealed record UnhandledCommand : ICommand;

// ---- Queries --------------------------------------------------------------------------------------------------

public sealed record GetOrderStatusQuery(Guid OrderId) : IQuery<string>;

internal sealed class GetOrderStatusQueryHandler(ExecutionLog log) : IQueryHandler<GetOrderStatusQuery, string>
{
    public Task<string> HandleAsync(GetOrderStatusQuery query, CancellationToken cancellationToken)
    {
        log.Add($"handler:{nameof(GetOrderStatusQuery)}");
        return Task.FromResult("Shipped");
    }
}

/// <summary>Returns the cancellation token its handler received.</summary>
public sealed record EchoCancellationTokenQuery : IQuery<CancellationToken>;

internal sealed class EchoCancellationTokenQueryHandler : IQueryHandler<EchoCancellationTokenQuery, CancellationToken>
{
    public Task<CancellationToken> HandleAsync(EchoCancellationTokenQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(cancellationToken);
}

// ---- Notifications --------------------------------------------------------------------------------------------

public sealed record OrderPlaced(Guid OrderId) : INotification;

internal sealed class SendOrderConfirmationHandler(ExecutionLog log) : INotificationHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken)
    {
        log.Add("notification:confirmation");
        return Task.CompletedTask;
    }
}

internal sealed class UpdateSalesStatisticsHandler(ExecutionLog log) : INotificationHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken)
    {
        log.Add("notification:statistics");
        return Task.CompletedTask;
    }
}

/// <summary>Deliberately has no handlers.</summary>
public sealed record OrderCancelled(Guid OrderId) : INotification;
