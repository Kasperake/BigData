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
    /// Makes <paramref name="handler"/> commit the registered <see cref="IUnitOfWork"/> once it succeeds, so the commit
    /// is the innermost step of the pipeline. Returns <paramref name="handler"/> unchanged when the command opts out
    /// (<see cref="IManuallyPersistedCommand"/>), when a <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> already
    /// owns the unit of work, or when no <see cref="IUnitOfWork"/> is registered.
    /// </summary>
    public static RequestHandlerDelegate<TResponse> WithUnitOfWork<TCommand, TResponse>(
        TCommand command,
        IPipelineBehavior<TCommand, TResponse>[] behaviors,
        RequestHandlerDelegate<TResponse> handler,
        IServiceProvider serviceProvider)
        where TCommand : IBaseCommand
    {
        if (command is IManuallyPersistedCommand ||
            Array.Exists(behaviors, static behavior => behavior is UnitOfWorkBehavior<TCommand, TResponse>))
        {
            return handler;
        }

        var unitOfWork = serviceProvider.GetService<IUnitOfWork>();
        if (unitOfWork is null)
        {
            return handler;
        }

        return async token =>
        {
            var response = await handler(token).ConfigureAwait(false);
            await unitOfWork.CommitAsync(token).ConfigureAwait(false);
            return response;
        };
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
