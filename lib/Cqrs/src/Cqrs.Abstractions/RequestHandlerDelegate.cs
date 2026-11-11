using System.Diagnostics.CodeAnalysis;

namespace Cqrs.Abstractions;

/// <summary>
/// Invokes the next step of the pipeline: the next <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// or, at the end of the pipeline, the handler.
/// </summary>
/// <typeparam name="TResponse">The response type of the request.</typeparam>
/// <param name="cancellationToken">
/// The token passed on to the next step. Pass the token your behavior received, unless you deliberately
/// substitute another one, such as a linked token that enforces a timeout.
/// </param>
/// <returns>The response produced by the rest of the pipeline.</returns>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Mirrors ASP.NET Core's RequestDelegate and the name most .NET developers know for this concept.")]
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
