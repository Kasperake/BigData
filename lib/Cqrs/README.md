# Cqrs

An in-process CQRS mediator for .NET 8 and .NET 10: commands, queries, notifications, ordered pipeline behaviors,
assembly scanning from a single marker type, fail-fast configuration and OpenTelemetry-compatible tracing.

## Projects

| Project               | Reference it from                                  | Depends on                                             |
|-----------------------|----------------------------------------------------|--------------------------------------------------------|
| `Cqrs.Abstractions`   | Application / Domain / module `.Contracts`         | nothing                                                |
| `Cqrs.Infrastructure` | Composition roots only (`Shop.Api`, `Shop.Worker`) | `Cqrs.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions` |

Application code never references `Cqrs.Infrastructure`. Enforce it with NetArchTest:

```csharp
Assert.True(Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
    .ShouldNot().HaveDependencyOn("Cqrs.Infrastructure")
    .GetResult().IsSuccessful);
```

## Quick start

```csharp
// Application project ------------------------------------------------------------
public sealed class ApplicationAssemblyMarker;

public sealed record CreateOrderCommand(string CustomerNumber) : ICommand<Guid>;

internal sealed class CreateOrderCommandHandler(OrdersDbContext db) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = Order.Create(command.CustomerNumber);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}

// Composition root ---------------------------------------------------------------
builder.Services.AddCqrs<ApplicationAssemblyMarker>();

// Consumer -----------------------------------------------------------------------
app.MapPost("/orders", async (CreateOrderRequest request, ICommandSender commands, CancellationToken ct) =>
    Results.Ok(await commands.SendAsync(new CreateOrderCommand(request.CustomerNumber), ct)));
```

| Contract                                | Handler                                      | Dispatch                       |
|-----------------------------------------|----------------------------------------------|--------------------------------|
| `ICommand`                              | `ICommandHandler<TCommand>` (exactly one)    | `ICommandSender.SendAsync`     |
| `ICommand<TResult>`                     | `ICommandHandler<TCommand, TResult>` (one)   | `ICommandSender.SendAsync`     |
| `IQuery<TResult>`                       | `IQueryHandler<TQuery, TResult>` (one)       | `IQuerySender.QueryAsync`      |
| `INotification`                         | `INotificationHandler<T>` (zero or more)     | `IPublisher.PublishAsync`      |

`IMediator` combines all three. Prefer the narrow interfaces: they make intent explicit and let an architecture test
assert, for example, that query handlers never depend on `ICommandSender`.

## Registration

```csharp
builder.Services.AddCqrs<ApplicationAssemblyMarker>(cqrs =>
{
    cqrs.AddOpenBehavior(typeof(LoggingBehavior<,>));      // outermost
    cqrs.AddOpenBehavior(typeof(ValidationBehavior<,>));
    // cqrs.AddOpenBehavior(typeof(OrdersUnitOfWorkBehavior<,>)); // innermost; see "Unit of work"
});
```

- **Scanning.** Every concrete, non-generic class in the marker's assembly that implements a handler interface is
  registered, including `internal` ones, so handlers stay hidden behind a module's public contracts.
- **Fail fast, atomically.** Two handlers for one command or query (or a clash with an existing manual registration)
  throw `CqrsConfigurationException` at startup, listing every problem, and nothing is registered.
- **Modular monolith.** Each module may call `services.AddCqrs<OrdersModuleMarker>()` from its own `AddOrdersModule()`.
  Repeated calls are idempotent: core services are added once, re-scanned assemblies do not duplicate handlers, and a
  behavior type is registered once. Register behaviors once, in the host, so their order is defined in one place.
- **Lifetimes.** The mediator is scoped, so handlers resolve from the caller's scope and share its `DbContext`.
  Handlers and behaviors default to transient (`cqrs.HandlerLifetime` and the `lifetime` argument change that).
- **Hosted services / Service Bus processors** have no ambient scope. Create one per message:

  ```csharp
  await using var scope = scopeFactory.CreateAsyncScope();
  await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, ct);
  ```

  Resolving the mediator from the root provider is rejected when scope validation is on (the default in Development).

## Pipeline behaviors

```csharp
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        return await next(cancellationToken);
    }
}
```

- **Order** is registration order; the first registered behavior is the outermost.
- **Scope a behavior with a generic constraint.** Behaviors whose constraint a request does not satisfy are skipped:

  | Constraint on `TRequest`              | Runs for                                        |
  |---------------------------------------|-------------------------------------------------|
  | `notnull`                             | every command and query                         |
  | `IBaseCommand`                        | commands, with and without a result             |
  | `IBaseQuery`                          | queries                                         |
  | your own marker, e.g. `IOrdersCommand`| only requests that implement it                 |

- **Commands without a result** flow through the pipeline with `TResponse = Unit`, so one behavior covers both kinds.
- **`next` takes the cancellation token explicitly.** Pass the one you received, or a linked token to enforce a timeout.
  `next` may be called more than once (retries) or not at all (short-circuit, e.g. a cache hit).
- The handler is resolved **before** the first behavior runs, so a missing handler fails before side effects such as
  opening a transaction.
- Notifications do not flow through the pipeline.

## Unit of work

Register an `IUnitOfWork` and every dispatched command commits through it automatically, after its handler succeeds
(innermost, so outer behaviors such as validation run first and a failed handler never commits). Queries never commit.

```csharp
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options), IUnitOfWork
{
    Task IUnitOfWork.CommitAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
```

Handlers then only change tracked state; they do not call `SaveChangesAsync` themselves.

### Custom unit of work behavior

When you need more than a commit (explicit transactions, retries, per-module contexts), derive a behavior from
`UnitOfWorkBehavior<TRequest, TResponse>` (in `Cqrs.Abstractions`). When one is registered for a command, it owns the
unit of work and the automatic commit is skipped:

```csharp
internal sealed class OrdersUnitOfWorkBehavior<TRequest, TResponse>(OrdersDbContext db)
    : UnitOfWorkBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand          // or a module marker: where TRequest : IBaseCommand, IOrdersCommand
{
    protected override async Task<TResponse> HandleInUnitOfWorkAsync(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // begin (only when no transaction is active) -> next -> SaveChanges + commit; roll back on exception
        return await next(cancellationToken);
    }
}
```

### Opting out: `IManuallyPersistedCommand`

```csharp
public sealed record ImportPricesCommand(IReadOnlyList<PriceRow> Rows) : ICommand<int>, IManuallyPersistedCommand;
```

A command that implements `IManuallyPersistedCommand` skips the unit-of-work behavior: it goes straight to the next
step and its handler owns persistence. Use it for handlers that commit in batches, need their own transaction or
isolation level, or persist through something other than the module's DbContext.

- The bypass lives in the base class's `HandleAsync`, which is not virtual, so a derived behavior cannot forget it.
- Only the unit of work is skipped; logging, validation and every other behavior still run.
- It does not detach the command from a transaction an outer command already opened. A manually persisted command
  sent from inside another command's handler still runs in that transaction.
- It does not affect the commands its handler sends; they get their own unit of work unless they also opt out.

### Points the implementation must settle

- **Nesting.** A handler that sends another command re-enters the pipeline in the same scope. Only the outermost
  command may own the transaction (check `db.Database.CurrentTransaction`).
- **One DbContext per module.** A single global behavior cannot know which context to use. Either one behavior per
  module constrained to a module marker interface, or a behavior that resolves an `IUnitOfWork` belonging to the
  command's module.
- **Domain events** published from a handler with the default sequential strategy run inside the transaction, and a
  failing handler rolls it back. Anything that must leave the process belongs in the outbox, not in a notification.
- Keep it innermost so logging and validation wrap it and validation failures never open a transaction.

## Notifications

- Dispatched on the **runtime type**, so publishing a list of `INotification` domain events reaches each concrete
  event's handlers.
- Matched on the **exact** type: a handler of a base class or interface is not invoked. This is deliberate; fan-out is
  always visible in the handler declarations.
- **Strategies** (`cqrs.UseNotificationPublishStrategy<T>()`):
  - `SequentialNotificationPublishStrategy` (default): one at a time, in registration order, stops at the first failure.
  - `ParallelNotificationPublishStrategy`: all at once; every handler runs; one failure is rethrown as is, several as an
    `AggregateException`. Handlers share the scope, so never use it when a handler touches a `DbContext`.
  - Implement `INotificationPublishStrategy` for anything else, such as log-and-continue.
- Notifications are in-process and not durable. Use the outbox for integration events.

## Observability

```csharp
builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(CqrsDiagnostics.ActivitySourceName));
```

Every dispatch becomes an activity named `command CreateOrderCommand`, `query GetOrderStatusQuery` or
`notification OrderPlaced`, nested under the current HTTP request or message. Tags: `cqrs.request.kind`,
`cqrs.request.type` and, for notifications, `cqrs.notification.handler_count`. Failures set status `Error` and the
`error.type` tag; exception messages are not recorded because they may contain personal data. Cancellation is not a
failure. With no listener attached, no activity is created.

## Catching missing handlers in CI

A command without a handler throws `HandlerNotFoundException` when it is dispatched. Copy
`tests/Cqrs.Infrastructure.Tests/HandlerCoverageTests.cs` into your solution, point it at the assembly that defines
your commands and queries, and assert that nothing is missing.

## Design decisions and limitations

- **`Task`, not `ValueTask`.** Dispatch cost is dominated by DI resolution; `Task` avoids `ValueTask` misuse in
  application code.
- **Reflection once per request type.** A strongly typed dispatcher is created on first use and cached; every later
  dispatch is a dictionary lookup plus typed code. Because it uses `MakeGenericType`, the library is not Native AOT or
  trimming compatible.
- **Behaviors are registered explicitly, never scanned,** because their order matters.
- **Out of scope:** streaming requests (`IAsyncEnumerable`), polymorphic notification handlers, request
  pre/post-processors (use a behavior).

## Build, test, pack

```bash
dotnet build -c Release
dotnet test  -c Release
dotnet pack  -c Release -o artifacts
```

Warnings are errors and the recommended .NET analyzers are on. Both libraries target `net8.0` and `net10.0`;
.NET 8 reaches end of support in November 2026, so drop `net8.0` from `Directory.Build.props` once every consumer runs
on .NET 10. Package versions are pinned centrally in `Directory.Packages.props`; update the test packages to their
latest versions when you adopt the repository.
