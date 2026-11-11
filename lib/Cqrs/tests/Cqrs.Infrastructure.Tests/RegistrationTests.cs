using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

public sealed class RegistrationTests
{
    [Fact]
    public void AddCqrs_RegistersInternalHandlersFromTheMarkerAssembly()
    {
        var services = new ServiceCollection().AddCqrs<ApplicationAssemblyMarker>();

        Assert.Single(services, d => d.ServiceType == typeof(ICommandHandler<CreateOrderCommand, Guid>));
        Assert.Single(services, d => d.ServiceType == typeof(ICommandHandler<ArchiveOrderCommand>));
        Assert.Single(services, d => d.ServiceType == typeof(IQueryHandler<GetOrderStatusQuery, string>));
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(INotificationHandler<OrderPlaced>)));
    }

    [Fact]
    public void AddCqrs_CalledTwice_IsIdempotent()
    {
        var services = new ServiceCollection();

        services.AddCqrs<ApplicationAssemblyMarker>(cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<,>)));
        services.AddCqrs<ApplicationAssemblyMarker>(cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<,>)));

        Assert.Single(services, d => d.ServiceType == typeof(IMediator));
        Assert.Single(services, d => d.ServiceType == typeof(INotificationPublishStrategy));
        Assert.Single(services, d => d.ServiceType == typeof(IPipelineBehavior<,>));
        Assert.Single(services, d => d.ServiceType == typeof(ICommandHandler<CreateOrderCommand, Guid>));
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(INotificationHandler<OrderPlaced>)));
    }

    [Fact]
    public void AddCqrs_TwoHandlersForOneCommand_ThrowsAndRegistersNothing()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<CqrsConfigurationException>(() => services.AddCqrs<RegistrationTests>());

        Assert.Empty(services);
        var error = Assert.Single(exception.Errors);
        Assert.Contains(nameof(DuplicateCommand), error, StringComparison.Ordinal);
        Assert.Contains(nameof(FirstDuplicateCommandHandler), error, StringComparison.Ordinal);
        Assert.Contains(nameof(SecondDuplicateCommandHandler), error, StringComparison.Ordinal);
    }

    [Fact]
    public void AddCqrs_HandlerAlreadyRegisteredByFactory_Throws()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICommandHandler<ArchiveOrderCommand>>(_ => new NoOpCommandHandler<ArchiveOrderCommand>());

        var exception = Assert.Throws<CqrsConfigurationException>(() => services.AddCqrs<ApplicationAssemblyMarker>());

        Assert.Contains(nameof(ArchiveOrderCommand), Assert.Single(exception.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void AddCqrs_IgnoresKeyedRegistrations()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<ICommandHandler<ArchiveOrderCommand>, NoOpCommandHandler<ArchiveOrderCommand>>("legacy");

        services.AddCqrs<ApplicationAssemblyMarker>();

        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(ICommandHandler<ArchiveOrderCommand>)));
    }

    [Fact]
    public void AddCqrs_UsesTheConfiguredHandlerLifetime()
    {
        var services = new ServiceCollection().AddCqrs<ApplicationAssemblyMarker>(cqrs => cqrs.HandlerLifetime = ServiceLifetime.Scoped);

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IQueryHandler<GetOrderStatusQuery, string>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddOpenBehavior_RejectsTypesTheContainerCannotCloseCorrectly()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddCqrs<ApplicationAssemblyMarker>(
            cqrs => cqrs.AddOpenBehavior(typeof(OuterBehavior<CreateOrderCommand, Guid>))));
        Assert.Throws<ArgumentException>(() => services.AddCqrs<ApplicationAssemblyMarker>(
            cqrs => cqrs.AddOpenBehavior(typeof(List<>))));
        Assert.Throws<ArgumentException>(() => services.AddCqrs<ApplicationAssemblyMarker>(
            cqrs => cqrs.AddOpenBehavior(typeof(SwappedBehavior<,>))));
        Assert.Empty(services);
    }

    [Fact]
    public async Task NotificationPublishStrategy_DefaultsToSequential_AndCanBeReplaced()
    {
        await using var defaults = TestServices.Build();
        await using var parallel = TestServices.Build(cqrs => cqrs.UseNotificationPublishStrategy<ParallelNotificationPublishStrategy>());

        Assert.IsType<SequentialNotificationPublishStrategy>(defaults.GetRequiredService<INotificationPublishStrategy>());
        Assert.IsType<ParallelNotificationPublishStrategy>(parallel.GetRequiredService<INotificationPublishStrategy>());
    }
}
