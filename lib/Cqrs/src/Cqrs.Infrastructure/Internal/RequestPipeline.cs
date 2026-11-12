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
    /// Runs <paramref name="handler"/> inside the registered <see cref="IUnitOfWork"/>: begin, handle, commit, and roll
    /// back if the handler or the commit throws. It is the innermost step of the pipeline, so outer behaviors such as
    /// validation run before a transaction opens. Joins an already active transaction instead of beginning a nested
    /// one. Returns <paramref name="handler"/> unchanged when the command opts out
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

        return token => RunInUnitOfWorkAsync(unitOfWork, handler, token);
    }

    private static async Task<TResponse> RunInUnitOfWorkAsync<TResponse>(
        IUnitOfWork unitOfWork,
        RequestHandlerDelegate<TResponse> handler,
        CancellationToken cancellationToken)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            // An outer command owns the transaction; it commits or rolls back this command's changes too.
            return await handler(cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await handler(cancellationToken).ConfigureAwait(false);
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch (Exception exception)
        {
            try
            {
                // Not the caller's token: a cancelled command must still be rolled back.
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "The command failed and rolling back its unit of work failed too.",
                    exception,
                    rollbackException);
            }

            throw;
        }
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
