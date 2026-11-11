using Cqrs.Infrastructure;
using Cqrs.Tests.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Tests;

internal static class TestServices
{
    /// <summary>Builds a provider the way a host does, with scope and build-time validation switched on.</summary>
    public static ServiceProvider Build(
        Action<CqrsConfiguration>? configure = null,
        Action<IServiceCollection>? services = null)
    {
        var collection = new ServiceCollection();
        collection.AddScoped<ExecutionLog>();
        services?.Invoke(collection);
        collection.AddCqrs<ApplicationAssemblyMarker>(configure);

        return collection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
    }
}
