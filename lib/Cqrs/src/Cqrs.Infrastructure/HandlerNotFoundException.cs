using Cqrs.Abstractions;
using Cqrs.Infrastructure.Internal;

namespace Cqrs.Infrastructure;

/// <summary>
/// Thrown when a command or query is dispatched but no handler is registered for it.
/// This is a configuration error, not a runtime condition to recover from.
/// </summary>
public sealed class HandlerNotFoundException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="requestType">The command or query type that has no handler.</param>
    /// <param name="handlerType">The handler service type that was expected to be registered.</param>
    public HandlerNotFoundException(Type requestType, Type handlerType)
        : base(CreateMessage(requestType, handlerType))
    {
        RequestType = requestType;
        HandlerType = handlerType;
    }

    /// <summary>The command or query type that has no handler.</summary>
    public Type RequestType { get; }

    /// <summary>The handler service type that was expected to be registered.</summary>
    public Type HandlerType { get; }

    private static string CreateMessage(Type requestType, Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(handlerType);

        var kind = typeof(IBaseQuery).IsAssignableFrom(requestType) ? RequestKind.Query : RequestKind.Command;

        return $"No handler is registered for {kind} '{TypeNameFormatter.FormatFull(requestType)}'. " +
               $"Expected one registration of '{TypeNameFormatter.Format(handlerType)}'. " +
               "Make sure the assembly containing the handler is scanned by AddCqrs (e.g. AddCqrs<ApplicationAssemblyMarker>()) " +
               "and that the handler is a concrete, non-generic class.";
    }
}
