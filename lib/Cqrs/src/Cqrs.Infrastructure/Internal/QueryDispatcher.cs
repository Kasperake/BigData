using Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Infrastructure.Internal;

internal abstract class QueryDispatcherBase<TResult>
{
    public abstract Task<TResult> DispatchAsync(
        IQuery<TResult> query,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class QueryDispatcher<TQuery, TResult> : QueryDispatcherBase<TResult>
    where TQuery : IQuery<TResult>
{
    private static readonly RequestMetadata Metadata = new(RequestKind.Query, typeof(TQuery));

    public override async Task<TResult> DispatchAsync(
        IQuery<TResult> query,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using var activity = CqrsDiagnostics.StartActivity(Metadata);

        try
        {
            var typedQuery = (TQuery)query;

            var handler = serviceProvider.GetService<IQueryHandler<TQuery, TResult>>()
                ?? throw new HandlerNotFoundException(typeof(TQuery), typeof(IQueryHandler<TQuery, TResult>));

            var behaviors = RequestPipeline.ResolveBehaviors<TQuery, TResult>(serviceProvider);
            if (behaviors.Length == 0)
            {
                return await handler.HandleAsync(typedQuery, cancellationToken).ConfigureAwait(false);
            }

            return await RequestPipeline
                .InvokeAsync(typedQuery, behaviors, token => handler.HandleAsync(typedQuery, token), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsDiagnostics.RecordFailure(activity, exception, cancellationToken);
            throw;
        }
    }
}
