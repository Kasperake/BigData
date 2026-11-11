using System.Diagnostics;
using Cqrs.Infrastructure.Internal;

namespace Cqrs.Infrastructure;

/// <summary>
/// Tracing integration. Every dispatched command, query and notification is recorded as an <see cref="Activity"/>
/// on the <see cref="ActivitySourceName"/> source, nested under the current activity (e.g. the incoming HTTP request
/// or Service Bus message). Activities are only created while a listener is attached, so the cost is negligible otherwise.
/// </summary>
/// <remarks>
/// With OpenTelemetry: <c>tracing.AddSource(CqrsDiagnostics.ActivitySourceName)</c>.
/// Failed requests get <see cref="ActivityStatusCode.Error"/> and an <see cref="ErrorTypeTag"/> tag. Exception messages
/// are deliberately not recorded, because they may contain personal data. Cancellation is not recorded as a failure.
/// </remarks>
public static class CqrsDiagnostics
{
    /// <summary>The name of the <see cref="ActivitySource"/> used by the mediator.</summary>
    public const string ActivitySourceName = "Cqrs";

    /// <summary>Tag with the kind of request: <c>command</c>, <c>query</c> or <c>notification</c>.</summary>
    public const string RequestKindTag = "cqrs.request.kind";

    /// <summary>Tag with the full type name of the request.</summary>
    public const string RequestTypeTag = "cqrs.request.type";

    /// <summary>Tag with the number of handlers a notification was published to.</summary>
    public const string HandlerCountTag = "cqrs.notification.handler_count";

    /// <summary>Tag (OpenTelemetry semantic convention) with the exception type of a failed request.</summary>
    public const string ErrorTypeTag = "error.type";

    private static readonly ActivitySource Source =
        new(ActivitySourceName, typeof(CqrsDiagnostics).Assembly.GetName().Version?.ToString());

    internal static Activity? StartActivity(RequestMetadata metadata)
    {
        var activity = Source.StartActivity(metadata.ActivityName, ActivityKind.Internal);

        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(RequestKindTag, metadata.Kind);
            activity.SetTag(RequestTypeTag, metadata.RequestTypeName);
        }

        return activity;
    }

    internal static void RecordFailure(Activity? activity, Exception exception, CancellationToken cancellationToken)
    {
        if (activity is null || (exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error);
        activity.SetTag(ErrorTypeTag, exception.GetType().FullName);
    }
}
