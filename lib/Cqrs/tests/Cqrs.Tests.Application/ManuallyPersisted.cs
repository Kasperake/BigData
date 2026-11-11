using Cqrs.Abstractions;

namespace Cqrs.Tests.Application;

/// <summary>Commits in batches itself, so it opts out of the unit of work.</summary>
public sealed record ImportPricesCommand(int Rows) : ICommand<int>, IManuallyPersistedCommand;

internal sealed class ImportPricesCommandHandler(ExecutionLog log) : ICommandHandler<ImportPricesCommand, int>
{
    public Task<int> HandleAsync(ImportPricesCommand command, CancellationToken cancellationToken)
    {
        log.Add($"handler:{nameof(ImportPricesCommand)}");
        return Task.FromResult(command.Rows);
    }
}

public sealed record RebuildSearchIndexCommand : ICommand, IManuallyPersistedCommand;

internal sealed class RebuildSearchIndexCommandHandler(ExecutionLog log) : ICommandHandler<RebuildSearchIndexCommand>
{
    public Task HandleAsync(RebuildSearchIndexCommand command, CancellationToken cancellationToken)
    {
        log.Add($"handler:{nameof(RebuildSearchIndexCommand)}");
        return Task.CompletedTask;
    }
}
