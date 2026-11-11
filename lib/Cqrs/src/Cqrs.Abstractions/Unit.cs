namespace Cqrs.Abstractions;

/// <summary>
/// Represents "no value". It is the <c>TResponse</c> of an <see cref="ICommand"/> (a command without a result)
/// while it flows through the pipeline, so a single <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// implementation serves commands with and without results.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>The one and only value of <see cref="Unit"/>.</summary>
    public static readonly Unit Value;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>Always <see langword="true"/>: all <see cref="Unit"/> values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>Always <see langword="false"/>: all <see cref="Unit"/> values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    public static bool operator !=(Unit left, Unit right) => false;
}
