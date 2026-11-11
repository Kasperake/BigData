using Cqrs.Abstractions;
using Cqrs.Tests.Application;

namespace Cqrs.Infrastructure.Tests;

internal sealed class OuterBehavior<TRequest, TResponse>(ExecutionLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add("outer:before");
        var response = await next(cancellationToken);
        log.Add("outer:after");
        return response;
    }
}

internal sealed class InnerBehavior<TRequest, TResponse>(ExecutionLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add("inner:before");
        var response = await next(cancellationToken);
        log.Add("inner:after");
        return response;
    }
}

/// <summary>Shape of the future unit-of-work behavior: constrained to commands, skipped for queries.</summary>
internal sealed class CommandOnlyBehavior<TRequest, TResponse>(ExecutionLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add($"command-only:{typeof(TRequest).Name}");
        return next(cancellationToken);
    }
}

internal sealed class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        Task.FromResult(default(TResponse)!);
}

internal sealed class RetryOnceBehavior<TRequest, TResponse>(ExecutionLog log) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        log.Add("retry:attempt");
        await next(cancellationToken);
        log.Add("retry:attempt");
        return await next(cancellationToken);
    }
}

internal sealed class TokenReplacingBehavior<TRequest, TResponse>(ReplacementToken replacement) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        next(replacement.Token);
}

internal sealed class ReplacementToken : IDisposable
{
    private readonly CancellationTokenSource _source = new();

    public CancellationToken Token => _source.Token;

    public void Dispose() => _source.Dispose();
}

/// <summary>Type parameters in the wrong order: the container would close it incorrectly, so it must be rejected.</summary>
internal sealed class SwappedBehavior<TResponse, TRequest> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        next(cancellationToken);
}

/// <summary>Open generic, so it is never picked up by scanning; used to simulate a manual registration.</summary>
internal sealed class NoOpCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public Task HandleAsync(TCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Stands in for a real transaction behavior by recording where the unit of work begins and commits.</summary>
internal sealed class RecordingUnitOfWorkBehavior<TRequest, TResponse>(ExecutionLog log) : UnitOfWorkBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    protected override async Task<TResponse> HandleInUnitOfWorkAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        log.Add("uow:begin");
        var response = await next(cancellationToken);
        log.Add("uow:commit");
        return response;
    }
}
