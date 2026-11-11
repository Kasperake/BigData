using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Internal;

internal static class RequestPipeline
{
    public static IPipelineBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>(IServiceProvider serviceProvider)
        where TRequest : notnull
    {
        // The container skips open generic behaviors whose constraints TRequest does not satisfy.
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        return behaviors as IPipelineBehavior<TRequest, TResponse>[] ?? [.. behaviors];
    }

    /// <summary>
    /// Wraps <paramref name="handler"/> in <paramref name="behaviors"/>; the first behavior is the outermost.
    /// The resulting chain is re-invocable, so a behavior may call <c>next</c> more than once (e.g. to retry).
    /// </summary>
    public static Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        IPipelineBehavior<TRequest, TResponse>[] behaviors,
        RequestHandlerDelegate<TResponse> handler,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var next = handler;

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var inner = next;
            next = token => behavior.HandleAsync(request, inner, token);
        }

        return next(cancellationToken);
    }
}
