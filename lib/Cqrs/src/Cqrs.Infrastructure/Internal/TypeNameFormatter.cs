namespace Cqrs.Infrastructure.Internal;

/// <summary>Formats types the way they are written in C#: <c>ICommandHandler&lt;CreateOrder, Guid&gt;</c>.</summary>
internal static class TypeNameFormatter
{
    public static string Format(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name;
        var arityIndex = name.IndexOf('`', StringComparison.Ordinal);
        if (arityIndex >= 0)
        {
            name = name[..arityIndex];
        }

        var arguments = type.IsGenericTypeDefinition
            ? string.Join(",", type.GetGenericArguments().Select(static _ => string.Empty))
            : string.Join(", ", type.GetGenericArguments().Select(Format));

        return $"{name}<{arguments}>";
    }

    public static string FormatFull(Type type) =>
        string.IsNullOrEmpty(type.Namespace) ? Format(type) : $"{type.Namespace}.{Format(type)}";
}
