namespace Cqrs.Infrastructure.Internal;

internal static class RequestKind
{
    public const string Command = "command";
    public const string Query = "query";
    public const string Notification = "notification";
}

/// <summary>Per-request-type values computed once, so tracing allocates no strings per dispatch.</summary>
internal sealed class RequestMetadata(string kind, Type requestType)
{
    public string Kind { get; } = kind;

    public string RequestTypeName { get; } = TypeNameFormatter.FormatFull(requestType);

    public string ActivityName { get; } = $"{kind} {TypeNameFormatter.Format(requestType)}";
}
