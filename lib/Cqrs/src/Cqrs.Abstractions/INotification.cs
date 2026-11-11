namespace Cqrs.Abstractions;

/// <summary>
/// An in-process notification that something has happened, such as a domain event.
/// It is delivered to zero or more <see cref="INotificationHandler{TNotification}"/> implementations.
/// </summary>
/// <remarks>
/// Notifications are in-process and not durable. Use an outbox for anything that must survive a crash
/// or cross a process boundary.
/// </remarks>
public interface INotification
{
}
