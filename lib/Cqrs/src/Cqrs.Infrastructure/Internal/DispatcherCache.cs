using System.Collections.Concurrent;

namespace Cqrs.Infrastructure.Internal;

/// <summary>
/// Caches one strongly typed dispatcher per request type. Reflection happens once per type; every later dispatch
/// is a dictionary lookup followed by fully typed, allocation-light code. Dispatchers are stateless, so the cache
/// is process-wide and shared by every service provider.
/// </summary>
internal static class DispatcherCache
{
    private static readonly ConcurrentDictionary<Type, VoidCommandDispatcherBase> VoidCommands = new();
    private static readonly ConcurrentDictionary<Type, NotificationDispatcherBase> Notifications = new();

    public static VoidCommandDispatcherBase ForVoidCommand(Type commandType) =>
        VoidCommands.GetOrAdd(
            commandType,
            static type => Create<VoidCommandDispatcherBase>(typeof(VoidCommandDispatcher<>), type));

    public static CommandDispatcherBase<TResult> ForCommand<TResult>(Type commandType) =>
        ResultDispatchers<TResult>.Commands.GetOrAdd(
            commandType,
            static type => Create<CommandDispatcherBase<TResult>>(typeof(CommandDispatcher<,>), type, typeof(TResult)));

    public static QueryDispatcherBase<TResult> ForQuery<TResult>(Type queryType) =>
        ResultDispatchers<TResult>.Queries.GetOrAdd(
            queryType,
            static type => Create<QueryDispatcherBase<TResult>>(typeof(QueryDispatcher<,>), type, typeof(TResult)));

    public static NotificationDispatcherBase ForNotification(Type notificationType) =>
        Notifications.GetOrAdd(
            notificationType,
            static type => Create<NotificationDispatcherBase>(typeof(NotificationDispatcher<>), type));

    private static TDispatcher Create<TDispatcher>(Type openDispatcherType, params Type[] typeArguments) =>
        (TDispatcher)Activator.CreateInstance(openDispatcherType.MakeGenericType(typeArguments))!;

    // Keyed by result type as well as request type, so a request implementing more than one
    // ICommand<TResult>/IQuery<TResult> can never collide.
    private static class ResultDispatchers<TResult>
    {
        public static readonly ConcurrentDictionary<Type, CommandDispatcherBase<TResult>> Commands = new();
        public static readonly ConcurrentDictionary<Type, QueryDispatcherBase<TResult>> Queries = new();
    }
}
