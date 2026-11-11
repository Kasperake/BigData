using System.Reflection;
using Cqrs.Abstractions;
using Cqrs.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure;

/// <summary>
/// Configures handler scanning, pipeline behaviors and notification publishing for <c>AddCqrs</c>.
/// </summary>
public sealed class CqrsConfiguration
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<ServiceDescriptor> _behaviors = [];

    internal CqrsConfiguration()
    {
    }

    /// <summary>
    /// The lifetime of handlers discovered by assembly scanning. Defaults to <see cref="ServiceLifetime.Transient"/>.
    /// Handlers may depend on scoped services, such as a DbContext, with any lifetime except singleton.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    internal IReadOnlyList<Assembly> Assemblies => _assemblies;

    internal IReadOnlyList<ServiceDescriptor> Behaviors => _behaviors;

    internal Type? NotificationPublishStrategyType { get; private set; }

    /// <summary>Scans the assembly containing <typeparamref name="TMarker"/> for handlers.</summary>
    /// <typeparam name="TMarker">Any type in the assembly to scan.</typeparam>
    /// <returns>This configuration, for chaining.</returns>
    public CqrsConfiguration RegisterHandlersFromAssemblyContaining<TMarker>() =>
        RegisterHandlersFromAssemblyContaining(typeof(TMarker));

    /// <summary>Scans the assembly containing <paramref name="markerType"/> for handlers.</summary>
    /// <param name="markerType">Any type in the assembly to scan.</param>
    /// <returns>This configuration, for chaining.</returns>
    public CqrsConfiguration RegisterHandlersFromAssemblyContaining(Type markerType)
    {
        ArgumentNullException.ThrowIfNull(markerType);
        return RegisterHandlersFromAssembly(markerType.Assembly);
    }

    /// <summary>
    /// Scans <paramref name="assembly"/> for handlers. Concrete, non-generic classes are registered, including
    /// <see langword="internal"/> ones, so handlers can stay hidden behind a module's public contracts.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>This configuration, for chaining.</returns>
    public CqrsConfiguration RegisterHandlersFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>
    /// Adds an open generic pipeline behavior, such as <c>typeof(LoggingBehavior&lt;,&gt;)</c>.
    /// Behaviors run in the order they are added; the first is the outermost.
    /// </summary>
    /// <param name="openBehaviorType">
    /// A non-abstract class with exactly two type parameters that implements
    /// <see cref="IPipelineBehavior{TRequest, TResponse}"/> with those parameters, in that order.
    /// Constrain <c>TRequest</c> (for example <c>where TRequest : IBaseCommand</c>) to target a subset of requests.
    /// </param>
    /// <param name="lifetime">The lifetime of the behavior. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This configuration, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="openBehaviorType"/> is not a valid open generic behavior.</exception>
    public CqrsConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!IsValidOpenBehavior(openBehaviorType))
        {
            throw new ArgumentException(
                $"'{TypeNameFormatter.FormatFull(openBehaviorType)}' is not a valid open generic pipeline behavior. " +
                "Expected a non-abstract class with two type parameters, <TRequest, TResponse>, implementing " +
                "IPipelineBehavior<TRequest, TResponse> in that order, registered as e.g. typeof(LoggingBehavior<,>).",
                nameof(openBehaviorType));
        }

        _behaviors.Add(ServiceDescriptor.Describe(typeof(IPipelineBehavior<,>), openBehaviorType, lifetime));
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="SequentialNotificationPublishStrategy"/>. The strategy is registered as a singleton.
    /// If <c>AddCqrs</c> is called more than once, the last configured strategy wins.
    /// </summary>
    /// <typeparam name="TStrategy">The strategy type.</typeparam>
    /// <returns>This configuration, for chaining.</returns>
    public CqrsConfiguration UseNotificationPublishStrategy<TStrategy>()
        where TStrategy : class, INotificationPublishStrategy
    {
        NotificationPublishStrategyType = typeof(TStrategy);
        return this;
    }

    private static bool IsValidOpenBehavior(Type type)
    {
        if (!type.IsClass || type.IsAbstract || !type.IsGenericTypeDefinition)
        {
            return false;
        }

        var typeParameters = type.GetGenericArguments();
        if (typeParameters.Length != 2)
        {
            return false;
        }

        // The container closes the behavior positionally, so its parameters must map 1:1 onto the interface's.
        return type.GetInterfaces().Any(contract =>
            contract.IsGenericType &&
            contract.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            contract.GetGenericArguments().SequenceEqual(typeParameters));
    }
}
