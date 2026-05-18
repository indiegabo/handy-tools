# Command Pattern Guide

This document covers the first shipped vertical slice of the HandyTools
`Command Pattern` module.

The slice is infrastructure-first. It owns command orchestration, scheduling,
queue arbitration, undo and redo history, runtime diagnostics, and a dedicated
play-mode monitor window. It does not own project-specific gameplay commands.

## Module Identity

- Module id: `command-pattern`
- Activation mode: Optional
- Active by default: Yes
- Load order: `172`
- Runtime assembly: `IndieGabo.HandyTools.CommandPattern`
- Editor assembly: `IndieGabo.HandyTools.CommandPattern.Editor`

Because the module is auto-activated, projects that need to disable it should
store an explicit override in `HandyModuleSettings.asset`.

## Runtime Entry Points

- `Runtime/Scripts/CommandPattern/CommandPatternModuleDefinition.cs`
- `Runtime/Scripts/CommandPattern/CommandPatternModuleBootstrapper.cs`
- `Runtime/Scripts/CommandPattern/Service/ICommandService.cs`
- `Runtime/Scripts/CommandPattern/Service/CommandService.cs`
- `Runtime/Scripts/CommandPattern/Contracts/*`
- `Runtime/Scripts/CommandPattern/Requests/*`
- `Runtime/Scripts/CommandPattern/Diagnostics/*`

The bootstrapper creates a persistent runtime service object and registers both
`ICommandService` and `CommandService` in the Service Locator.

## Quickstart In Five Minutes

Use this sequence when you need one new command flow and do not want to read
the whole runtime first.

1. Define one project-owned command type that implements `IHandyCommand`.
2. Return one `CommandExecutionResult` from `ExecuteAsync`.
3. Emit one `ICommandUndoOperation` only if the execution should participate in
   undo and redo history.
4. Resolve `ICommandService` through the Service Locator.
5. Wrap the command in one `CommandRequest` with an explicit `scope`, `queue`,
   and `ownerId`.
6. Call `Execute` for immediate work or `Schedule` for delayed work.
7. Await `CommandExecutionHandle.Completion` when the caller needs the final
   result.
8. Query `GetSnapshot` or subscribe to `LifecycleEventPublished` when tooling,
   UI, or tests need runtime visibility.

If you need one concrete reference before writing code, inspect these in order:

1. `Samples/Command Pattern Starter Kit/Scripts/CommandPatternSampleController.cs`
2. `Samples/Command Pattern Starter Kit/Scripts/SampleMoveGridCommand.cs`
3. `Tests/CommandPatternEditMode/CommandServiceEditModeTests.cs`
4. `Tests/CommandPatternPlayMode/CommandServicePlayModeTests.cs`

## Quickstart Decision Table

Use this table when choosing which surface to call.

| Need                           | Use                                  | Why                                                                             |
| ------------------------------ | ------------------------------------ | ------------------------------------------------------------------------------- |
| Run immediately                | `ICommandService.Execute`            | Returns one execution handle and enters queue arbitration right away.           |
| Run later                      | `ICommandService.Schedule`           | Registers one pending schedule and promotes it when the delay condition is met. |
| Cancel pending scheduled work  | `ICommandService.TryCancelScheduled` | Only applies before the scheduled request is promoted into execution.           |
| Undo prior work                | `ICommandService.UndoAsync`          | Reverts the latest undoable execution in one scope.                             |
| Redo prior undo                | `ICommandService.RedoAsync`          | Replays the original request in redo mode for one scope.                        |
| Inspect runtime state          | `ICommandService.GetSnapshot`        | Returns the filtered journal and queue view used by tooling.                    |
| Inject cross-cutting behaviour | `RegisterMiddleware`                 | Wraps every request without modifying command classes.                          |

## Authoring Checklist

Before calling a command done, verify these points:

- the command owns only project-specific behaviour, not orchestration state;
- the descriptor name is stable and readable in diagnostics;
- the request uses a deliberate `scope`, `queue`, and `ownerId` rather than
  relying on accidental defaults;
- undo returns the system to a coherent state, not just a visually similar one;
- redo is acceptable for the same request semantics;
- metadata and diagnostics summary expose the minimum useful context for tools;
- one test covers the expected queue or history behaviour.

## Core Contracts

Projects define commands through `IHandyCommand`.

Optional capabilities extend that baseline:

- `ICanExecuteCommand` gates execution.
- `ICommandUndoOperation` reverses successful executions.
- `ICommandMiddleware` wraps cross-cutting pipeline behavior.
- `ICommandDiagnosticsSummaryProvider` emits lightweight metadata.
- `ICommandOwner` supplies owner metadata when the request does not override
  it.

Requests and handles keep orchestration concerns outside reusable command
definitions:

- `CommandRequest`
- `CommandScheduleRequest`
- `CommandUndoRequest`
- `CommandRedoRequest`
- `CommandExecutionHandle`
- `CommandScheduleHandle`

## Resolve The Service

Most consumers should resolve the runtime service through the global Service
Locator.

```csharp
using IndieGabo.HandyTools.CommandPatternModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;

ICommandService commandService = ServiceLocator.GetRequired<ICommandService>();
```

The bootstrapper also registers the concrete `CommandService`, but new code
should prefer `ICommandService` unless it needs implementation-specific
behaviour for a controlled package-internal reason.

## Minimal Authoring Flow

The smallest useful command implements `IHandyCommand`, exposes one stable
descriptor, and returns one `CommandExecutionResult`.

```csharp
using System.Collections.Generic;
using System.Threading;
using IndieGabo.HandyTools.CommandPatternModule;
using UnityEngine;

public sealed class SetHealthCommand :
  IHandyCommand,
  ICommandOwner,
  ICommandDiagnosticsSummaryProvider
{
  private readonly Health _health;
  private readonly int _targetValue;

  public SetHealthCommand(Health health, int targetValue)
  {
    _health = health;
    _targetValue = targetValue;
  }

  public CommandDescriptor Descriptor { get; } =
    CommandDescriptor.Create<SetHealthCommand>(
      "Set Health",
      "Sets one health value and emits one undo operation.");

  public string OwnerId => "combat";

  public Awaitable<CommandExecutionResult> ExecuteAsync(
    ICommandExecutionContext context,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (_health == null)
    {
      return Complete(new CommandExecutionResult(
        false,
        failureReason: "The target health component is missing."));
    }

    int previousValue = _health.Value;
    _health.SetValue(_targetValue);

    return Complete(new CommandExecutionResult(
      true,
      isUndoable: true,
      allowRedo: true,
      undoOperation: new RestoreHealthUndo(_health, previousValue),
      metadata: new Dictionary<string, string>
      {
        ["previous"] = previousValue.ToString(),
        ["current"] = _health.Value.ToString(),
      }));
  }

  public IReadOnlyDictionary<string, string> GetDiagnosticsSummary()
  {
    return new Dictionary<string, string>
    {
      ["targetValue"] = _targetValue.ToString(),
    };
  }

  private static Awaitable<CommandExecutionResult> Complete(
    CommandExecutionResult result)
  {
    AwaitableCompletionSource<CommandExecutionResult> completionSource = new();
    completionSource.SetResult(result);
    return completionSource.Awaitable;
  }

  private sealed class RestoreHealthUndo : ICommandUndoOperation
  {
    private readonly Health _health;
    private readonly int _previousValue;

    public RestoreHealthUndo(Health health, int previousValue)
    {
      _health = health;
      _previousValue = previousValue;
    }

    public Awaitable UndoAsync(
      ICommandExecutionContext context,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (_health != null)
      {
        _health.SetValue(_previousValue);
      }

      AwaitableCompletionSource completionSource = new();
      completionSource.SetResult();
      return completionSource.Awaitable;
    }
  }
}
```

Use `AwaitableCompletionSource` when the command body finishes synchronously.
Use `async Awaitable<...>` only when the body actually awaits runtime work.

## Dispatch Patterns

Immediate execution wraps one command body in one `CommandRequest`.

```csharp
ICommandService commandService = ServiceLocator.GetRequired<ICommandService>();

CommandExecutionHandle handle = commandService.Execute(new CommandRequest(
  new SetHealthCommand(health, 75),
  scope: "player-health",
  queue: "mutations",
  ownerId: "combat-ui",
  tags: new[] { "combat", "health" },
  queuePolicy: CommandQueuePolicy.Serial,
  displayNameOverride: "Set Player Health"));

CommandExecutionResult result = await handle.Completion;
if (!result.Succeeded)
{
  Debug.LogWarning(result.FailureReason);
}
```

Scheduled execution wraps the same `CommandRequest` in one
`CommandScheduleRequest`.

```csharp
CommandRequest request = new(
  new SetHealthCommand(health, 75),
  scope: "player-health",
  queue: "mutations",
  ownerId: "combat-ui",
  tags: new[] { "combat", "health" },
  queuePolicy: CommandQueuePolicy.Serial);

CommandScheduleHandle scheduleHandle = commandService.Schedule(
  new CommandScheduleRequest(
    request,
    CommandDelayMode.UnscaledDelay,
    delaySeconds: 0.25d,
    requestedFrame: Time.frameCount));

bool cancelled = commandService.TryCancelScheduled(scheduleHandle);
```

Undo and redo are scope-first operations with an optional owner filter.

```csharp
CommandUndoResult undoResult = await commandService.UndoAsync(
  new CommandUndoRequest(
    scope: "player-health",
    ownerId: "combat-ui",
    reason: "UI undo"));

CommandRedoResult redoResult = await commandService.RedoAsync(
  new CommandRedoRequest(
    scope: "player-health",
    ownerId: "combat-ui",
    reason: "UI redo"));
```

## Routing Model

Use the routing fields deliberately. They decide history behaviour, queue
arbitration, and diagnostics readability.

- `scope`: the history lane. Undo and redo are resolved per scope.
- `queue`: the execution lane inside one scope. Queue policy arbitration is
  keyed by `(Scope, Queue)`.
- `ownerId`: the logical caller or subsystem. It is useful for diagnostics and
  targeted undo or redo.
- `tags`: free-form diagnostic labels for snapshot filtering.
- `displayNameOverride`: one request-local name when the descriptor label is too
  generic.
- `historyLimitOverride`: one per-scope retention override for undoable entries.
- `journalLimitOverride`: one per-scope retention override for journal entries.

Practical rule:

- different undo domains should use different scopes;
- independent execution lanes inside one domain should use different queues;
- unrelated callers sharing one domain should still keep distinct owner ids.

## Middleware

Middleware is the right place for cross-cutting policies such as tracing,
timing, permission checks, or request decoration.

```csharp
public sealed class LoggingMiddleware : ICommandMiddleware
{
  public async Awaitable<CommandExecutionResult> InvokeAsync(
    CommandRequest request,
    CommandPipelineDelegate next,
    CancellationToken cancellationToken = default)
  {
    Debug.Log($"Starting {request.Command.Descriptor.DisplayName}");
    CommandExecutionResult result = await next(request, cancellationToken);
    Debug.Log($"Completed {request.Command.Descriptor.DisplayName}: {result.Succeeded}");
    return result;
  }
}

commandService.RegisterMiddleware(new LoggingMiddleware());
```

Middleware should stay generic. Do not use it to inject project-specific game
logic into the package runtime slice.

## Diagnostics Workflow

Snapshots are the primary read surface for tools, runtime HUDs, and automated
checks.

```csharp
CommandJournalSnapshot snapshot = commandService.GetSnapshot(new CommandQuery(
  scope: "player-health",
  queue: "mutations",
  ownerId: "combat-ui",
  tag: "combat",
  commandType: nameof(SetHealthCommand),
  maxEntriesPerGroup: 20));

foreach (CommandJournalEntry entry in snapshot.Completed)
{
  Debug.Log($"{entry.DisplayName} -> {entry.Status}");
}
```

For live tooling, combine snapshots with the lifecycle event stream.

```csharp
commandService.LifecycleEventPublished += lifecycleEvent =>
{
  Debug.Log($"{lifecycleEvent.Kind} -> {lifecycleEvent.Entry.DisplayName}");
};
```

## Canonical References In This Repository

When implementing or expanding the slice, these files are the best package
local examples to inspect first:

- `Samples/Command Pattern Starter Kit/Scripts/SampleMoveGridCommand.cs`
- `Samples/Command Pattern Starter Kit/Scripts/CommandPatternSampleController.cs`
- `Tests/CommandPatternEditMode/CommandServiceEditModeTests.cs`
- `Tests/CommandPatternPlayMode/CommandServicePlayModeTests.cs`
- `Tests/CommandPatternEditMode/CommandPatternMonitorWindowTests.cs`

The sample demonstrates consumer-facing usage. The tests demonstrate the
expected runtime invariants.

## How To Extend This Slice Safely

This module is intended to grow, but new surface area must stay generic enough
to justify package ownership.

Add runtime API when the capability improves cross-project orchestration, for
example:

- new generic diagnostics records,
- new generic scheduling modes,
- new generic queue policies,
- new query surfaces, or
- new cross-cutting middleware hooks.

Keep code out of the runtime slice when it is project-authored behaviour, for
example:

- gameplay abilities,
- inventory actions,
- UI-specific button handlers,
- level-editor object mutations, or
- bridges to one project-specific framework.

When expanding the slice:

- prefer capability interfaces over inheritance-heavy command bases;
- keep mutable execution state in the service, not on reusable command
  instances;
- update the monitor only when the new runtime state is generically useful;
- add edit-mode or play-mode regression coverage for every behavioural change;
- update this guide when one new contract, queue rule, or diagnostic surface
  changes how consumers should author commands.

## Execution Model

The runtime exposes one unified completion model through
`CommandExecutionHandle.Completion`.

Immediate execution can:

- run in parallel,
- wait in a serial queue, or
- fail immediately through `RejectWhenBusy`.

Lifecycle transitions publish typed `CommandLifecycleEvent` payloads for:

- queued work,
- scheduled work,
- start,
- completion,
- failure,
- cancellation,
- undo, and
- redo.

## Scheduling

The first shipped scheduler supports:

- `NextFrame`
- `ScaledDelay`
- `UnscaledDelay`

Pending scheduled commands remain visible in runtime snapshots until they run,
fail, or are cancelled.

Queue arbitration is keyed by `(Scope, Queue)`.

The first shipped queue policies are:

- `Parallel`
- `Serial`
- `RejectWhenBusy`

## Undo And Redo

Undo and redo are scope-based.

The current implementation stores one done stack and one redo stack per scope.
Only successful undoable executions enter the done stack. A new successful
execution in the same scope clears redo continuity.

Redo re-executes the original `CommandRequest` in `CommandExecutionMode.Redo`
instead of replaying stale output.

## Diagnostics

`ICommandService.GetSnapshot(...)` returns a grouped immutable journal snapshot
with these buckets:

- pending
- running
- completed
- failed
- cancelled
- undone
- redone

Each `CommandJournalEntry` exposes routing data, timestamps, status, ids,
undoability flags, and lightweight metadata.

## Editor Monitor

Open the play-mode monitor through:

- `HandyTools/Command Pattern/Monitor`

The window currently provides:

- filters for scope, queue, owner, tag, and command type,
- flattened grouped journal rows,
- a details panel with timestamps and metadata,
- cancel action for pending scheduled entries, and
- scope-level undo and redo actions.

The monitor is diagnostic-only. It is not a configuration surface, so the
module remains out of the shared modules window.

## Sample

The module now ships one authored sample under:

- `Samples/Command Pattern Starter Kit`

The starter kit includes:

- `Scenes/CommandPatternStarterKit.unity`,
- one discrete grid actor with a persistent trail,
- immediate and scheduled movement submission buttons,
- undo and redo controls, and
- one vertical request list rendered at runtime.

The sample code remains isolated inside
`IndieGabo.HandyTools.CommandPattern.Samples` so the runtime module assembly
does not absorb gameplay-specific commands.

## Test Coverage

The current shipped slice includes:

- edit-mode runtime coverage for immediate execution, undo, and redo,
- middleware ordering coverage for the execution pipeline,
- play-mode scheduling coverage for next-frame, scaled, and unscaled delays,
- pending-schedule cancellation coverage,
- redo invalidation and scope-isolation coverage,
- named-queue isolation and `RejectWhenBusy` coverage, and
- monitor-window coverage for edit-mode shell creation, play-mode refresh, and
  filter-driven detail updates.

## Current Gaps

The current vertical slice does not yet ship:

- bulk cancellation by scope, queue, owner, or tag,
- dedicated history snapshot APIs separate from the journal,
- composition helpers such as batches or transactions.
