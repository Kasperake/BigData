namespace Cqrs.Infrastructure;

/// <summary>
/// Thrown by <c>AddCqrs</c> when the handler configuration is invalid, for example when a command or query
/// has more than one handler. Nothing is registered when this exception is thrown.
/// </summary>
public sealed class CqrsConfigurationException : InvalidOperationException
{
    /// <summary>Creates the exception from a list of configuration errors.</summary>
    /// <param name="errors">The configuration errors.</param>
    public CqrsConfigurationException(IReadOnlyList<string> errors)
        : base(CreateMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>Creates the exception for a single error caused by another exception.</summary>
    /// <param name="message">The error.</param>
    /// <param name="innerException">The exception that caused the error.</param>
    public CqrsConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = [message];
    }

    /// <summary>Every configuration error that was found, so all of them can be fixed in one go.</summary>
    public IReadOnlyList<string> Errors { get; }

    private static string CreateMessage(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return $"The CQRS configuration is invalid:{Environment.NewLine}- " +
               string.Join($"{Environment.NewLine}- ", errors);
    }
}
