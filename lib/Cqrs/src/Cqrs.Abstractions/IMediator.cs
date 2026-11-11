namespace Cqrs.Abstractions;

/// <summary>
/// Sends commands and queries and publishes notifications.
/// </summary>
/// <remarks>
/// Prefer depending on the narrower <see cref="ICommandSender"/>, <see cref="IQuerySender"/> or
/// <see cref="IPublisher"/>. They make a consumer's intent explicit and let architecture tests enforce rules
/// such as "query handlers never send commands".
/// </remarks>
public interface IMediator : ICommandSender, IQuerySender, IPublisher
{
}
