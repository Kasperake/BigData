using Cqrs.Abstractions;

namespace Cqrs.Infrastructure.Tests;

// Only scanned by RegistrationTests: an invalid configuration with two handlers for one command.

public sealed record DuplicateCommand : ICommand;

internal sealed class FirstDuplicateCommandHandler : ICommandHandler<DuplicateCommand>
{
    public Task HandleAsync(DuplicateCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class SecondDuplicateCommandHandler : ICommandHandler<DuplicateCommand>
{
    public Task HandleAsync(DuplicateCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}
