using System.Reflection;
using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cqrs.Infrastructure.Internal;

/// <summary>
/// Discovers handlers by assembly scanning and validates them before anything is registered.
/// </summary>
internal static class HandlerRegistrar
{
    public static HandlerRegistrations Scan(
        IServiceCollection services,
        IReadOnlyList<Assembly> assemblies,
        ServiceLifetime lifetime)
    {
        var registered = IndexExistingRequestHandlers(services);
        var requestHandlers = new List<ServiceDescriptor>();
        var notificationHandlers = new List<ServiceDescriptor>();
        var errors = new List<string>();

        foreach (var implementationType in assemblies.SelectMany(LoadTypes))
        {
            if (!IsHandlerCandidate(implementationType))
            {
                continue;
            }

            foreach (var serviceType in implementationType.GetInterfaces())
            {
                if (!serviceType.IsGenericType)
                {
                    continue;
                }

                var definition = serviceType.GetGenericTypeDefinition();

                if (definition == typeof(INotificationHandler<>))
                {
                    notificationHandlers.Add(ServiceDescriptor.Describe(serviceType, implementationType, lifetime));
                    continue;
                }

                if (!IsRequestHandlerDefinition(definition))
                {
                    continue;
                }

                if (!registered.TryGetValue(serviceType, out var existingImplementation))
                {
                    registered.Add(serviceType, implementationType);
                    requestHandlers.Add(ServiceDescriptor.Describe(serviceType, implementationType, lifetime));
                }
                else if (existingImplementation != implementationType)
                {
                    // The same type found twice (e.g. a module scanned twice) is fine; two different types are not.
                    errors.Add(DuplicateHandlerError(serviceType, existingImplementation, implementationType));
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new CqrsConfigurationException(errors);
        }

        return new HandlerRegistrations(requestHandlers, notificationHandlers);
    }

    private static Dictionary<Type, Type?> IndexExistingRequestHandlers(IServiceCollection services)
    {
        var index = new Dictionary<Type, Type?>();

        foreach (var descriptor in services)
        {
            // Accessing ImplementationType on a keyed descriptor throws, and keyed handlers are never dispatched to.
            if (descriptor.IsKeyedService ||
                !descriptor.ServiceType.IsGenericType ||
                descriptor.ServiceType.ContainsGenericParameters ||
                !IsRequestHandlerDefinition(descriptor.ServiceType.GetGenericTypeDefinition()))
            {
                continue;
            }

            // Null when registered through a factory or instance.
            index[descriptor.ServiceType] = descriptor.ImplementationType;
        }

        return index;
    }

    private static bool IsRequestHandlerDefinition(Type definition) =>
        definition == typeof(ICommandHandler<>) ||
        definition == typeof(ICommandHandler<,>) ||
        definition == typeof(IQueryHandler<,>);

    private static bool IsHandlerCandidate(Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false };

    private static Type[] LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            // Fail fast: silently skipping types that failed to load would silently skip their handlers.
            var reasons = exception.LoaderExceptions
                .Where(static loaderException => loaderException is not null)
                .Select(static loaderException => loaderException!.Message)
                .Distinct(StringComparer.Ordinal);

            throw new CqrsConfigurationException(
                $"Could not load types from assembly '{assembly.FullName}' while scanning for handlers: " +
                string.Join(" ", reasons),
                exception);
        }
    }

    private static string DuplicateHandlerError(Type serviceType, Type? existingImplementation, Type duplicateImplementation)
    {
        var requestType = serviceType.GetGenericArguments()[0];
        var existing = existingImplementation is null
            ? "an existing factory or instance registration"
            : $"'{TypeNameFormatter.FormatFull(existingImplementation)}'";

        return $"'{TypeNameFormatter.FormatFull(requestType)}' has more than one handler: {existing} and " +
               $"'{TypeNameFormatter.FormatFull(duplicateImplementation)}'. Every command and query must have exactly one handler.";
    }
}

internal sealed class HandlerRegistrations(
    IReadOnlyList<ServiceDescriptor> requestHandlers,
    IReadOnlyList<ServiceDescriptor> notificationHandlers)
{
    public void AddTo(IServiceCollection services)
    {
        foreach (var descriptor in requestHandlers)
        {
            services.Add(descriptor);
        }

        foreach (var descriptor in notificationHandlers)
        {
            // Idempotent when an assembly is scanned more than once.
            services.TryAddEnumerable(descriptor);
        }
    }
}
