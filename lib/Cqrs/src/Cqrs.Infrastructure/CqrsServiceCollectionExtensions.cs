using Cqrs.Abstractions;
using Cqrs.Infrastructure;
using Cqrs.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Lives in the DI namespace so AddCqrs is discoverable wherever services are configured.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the CQRS mediator in an <see cref="IServiceCollection"/>.
/// </summary>
public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and every command, query and notification handler found in the assembly that
    /// contains <typeparamref name="TAssemblyMarker"/>.
    /// </summary>
    /// <typeparam name="TAssemblyMarker">Any type in the assembly to scan, typically <c>ApplicationAssemblyMarker</c>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional additional configuration, such as pipeline behaviors.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="CqrsConfigurationException">
    /// A command or query has more than one handler, or the assembly cannot be scanned.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddCqrs&lt;ApplicationAssemblyMarker&gt;(cqrs =&gt;
    /// {
    ///     cqrs.AddOpenBehavior(typeof(LoggingBehavior&lt;,&gt;));
    ///     cqrs.AddOpenBehavior(typeof(ValidationBehavior&lt;,&gt;));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddCqrs<TAssemblyMarker>(
        this IServiceCollection services,
        Action<CqrsConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddCqrs(configuration =>
        {
            configuration.RegisterHandlersFromAssemblyContaining<TAssemblyMarker>();
            configure?.Invoke(configuration);
        });
    }

    /// <summary>
    /// Registers the mediator, the handlers of the configured assemblies and the configured pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures assemblies to scan, behaviors and notification publishing.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Safe to call more than once, for example once per module of a modular monolith: core services are added
    /// once, re-scanning an assembly does not duplicate its handlers, and a behavior type is registered once.
    /// Behaviors keep the order in which they were first added.
    /// </para>
    /// <para>
    /// The call is atomic: if validation fails, a <see cref="CqrsConfigurationException"/> is thrown and
    /// <paramref name="services"/> is left unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="CqrsConfigurationException">
    /// A command or query has more than one handler, or an assembly cannot be scanned.
    /// </exception>
    public static IServiceCollection AddCqrs(this IServiceCollection services, Action<CqrsConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new CqrsConfiguration();
        configure(configuration);

        // Scan and validate before touching the collection so a failed call leaves it unchanged.
        var handlers = HandlerRegistrar.Scan(services, configuration.Assemblies, configuration.HandlerLifetime);

        AddMediator(services);
        AddNotificationPublishStrategy(services, configuration.NotificationPublishStrategyType);

        foreach (var behavior in configuration.Behaviors)
        {
            services.TryAddEnumerable(behavior);
        }

        handlers.AddTo(services);

        return services;
    }

    private static void AddMediator(IServiceCollection services)
    {
        // Scoped, so handlers resolve from the caller's scope and scoped dependencies (e.g. a DbContext) are shared
        // with the rest of the unit of work. Resolving it from the root provider is a bug that scope validation catches.
        services.TryAddScoped<Mediator>();
        services.TryAddScoped<IMediator>(static provider => provider.GetRequiredService<Mediator>());
        services.TryAddScoped<ICommandSender>(static provider => provider.GetRequiredService<Mediator>());
        services.TryAddScoped<IQuerySender>(static provider => provider.GetRequiredService<Mediator>());
        services.TryAddScoped<IPublisher>(static provider => provider.GetRequiredService<Mediator>());
    }

    private static void AddNotificationPublishStrategy(IServiceCollection services, Type? strategyType)
    {
        if (strategyType is null)
        {
            services.TryAddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
            return;
        }

        services.Replace(ServiceDescriptor.Singleton(typeof(INotificationPublishStrategy), strategyType));
    }
}
