using Cqrs.Abstractions;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

/// <summary>
/// A missing handler only surfaces when the request is dispatched. Copy this test into your solution (pointed at the
/// assembly that defines your commands and queries) to catch it in CI instead.
/// </summary>
public sealed class HandlerCoverageTests
{
    [Fact]
    public void EveryCommandAndQuery_HasARegisteredHandler()
    {
        var registered = new ServiceCollection()
            .AddCqrs<ApplicationAssemblyMarker>()
            .Select(descriptor => descriptor.ServiceType)
            .ToHashSet();

        var missing = typeof(ApplicationAssemblyMarker).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, ContainsGenericParameters: false })
            .Select(type => (Request: type, Handler: ExpectedHandler(type)))
            .Where(pair => pair.Handler is not null && !registered.Contains(pair.Handler))
            .Select(pair => pair.Request)
            .ToList();

        // In a real solution: Assert.Empty(missing). The fixture deliberately contains one unhandled command.
        Assert.Equal(typeof(UnhandledCommand), Assert.Single(missing));
    }

    private static Type? ExpectedHandler(Type request) =>
        request.GetInterfaces()
            .Select(contract =>
                contract == typeof(ICommand) ? typeof(ICommandHandler<>).MakeGenericType(request) :
                !contract.IsGenericType ? null :
                contract.GetGenericTypeDefinition() == typeof(ICommand<>) ? typeof(ICommandHandler<,>).MakeGenericType(request, contract.GetGenericArguments()[0]) :
                contract.GetGenericTypeDefinition() == typeof(IQuery<>) ? typeof(IQueryHandler<,>).MakeGenericType(request, contract.GetGenericArguments()[0]) :
                null)
            .FirstOrDefault(handler => handler is not null);
}
