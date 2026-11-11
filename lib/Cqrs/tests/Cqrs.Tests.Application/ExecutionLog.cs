using System.Collections.Concurrent;

namespace Cqrs.Tests.Application;

/// <summary>Scoped test probe that records what ran, in order.</summary>
public sealed class ExecutionLog
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyList<string> Entries => [.. _entries];

    public void Add(string entry) => _entries.Enqueue(entry);
}
