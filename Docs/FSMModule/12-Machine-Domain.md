# Machine Domain

`Brain.Machine` is the public authority for machine lifecycle and state-change
decisions.

If you need to turn the machine on, stop it, pause it, resume it, request a
state change, complete a state naturally, fail a state explicitly, or inspect
the latest transition report, this is the surface you should touch.

## Quick Mental Model

The machine domain answers these questions:

- what state is active right now?
- what state was active before that?
- is the machine on, off, paused, or generally working?
- why did the latest successful transition happen?
- what should happen when an external system or the current state wants to move
  the machine elsewhere?

The machine domain does not load states and it does not discover input. It
assumes those concerns are already handled by `Brain.States` and `Brain.Input`.

## What The Machine Domain Owns

`Brain.Machine` owns:

- `IsInitialized`, `IsOn`, `IsPaused`, `IsOff`, `IsWorking`
- `Status`
- `CurrentState`, `PreviousState`, `DefaultState`, `FirstEnteredState`
- `LastTransitionReason`, `LastTransitionReport`
- `TurnOn(...)`
- `Pause()`, `Resume()`, `Stop()`, `ChangeStatus(...)`
- `RequestStateChange(...)`
- `CompleteState(...)`
- `FailState(...)`

## What It Does Not Own

`Brain.Machine` does not own:

- the state catalog or runtime state construction
- input bindings or input caching
- authored stats assets
- blackboard values
- CCPro movement reference calculation

When you need one of those, step sideways into the correct domain instead of
trying to overload `Machine` with responsibilities it should never carry.

## Read Surface

Use the properties when you need to inspect the branch.

```csharp
if (!Brain.Machine.IsOn)
{
    return;
}

IState activeState = Brain.Machine.CurrentState;
StateTransitionReport report = Brain.Machine.LastTransitionReport;
```

`LastTransitionReport` is the correct way to inspect why the latest successful
transition happened. Do not reverse-engineer that from history strings or by
guessing what the current state implies.

## Write Surface

### Start the machine manually

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class ManualBrainStarter : MonoBehaviour
    {
        [SerializeField]
        private FSMBrain _brain;

        private void Start()
        {
            if (_brain == null || _brain.DefaultState == null)
            {
                return;
            }

            _brain.Machine.TurnOn(_brain.DefaultState);
        }
    }
}
```

Use manual start when your project must finish some composition step before the
first state becomes active.

### Request an external transition

```csharp
Brain.Machine.RequestStateChange(_combatState);
```

or, for class-based runtime states loaded through the provider:

```csharp
Brain.Machine.RequestStateChange<CombatState>();
```

The generic overload is for runtime states derived from `State`. If the target
is a `ScriptableState`, resolve it first through `Brain.States` and then pass
the resulting `IState` instance.

### Complete the current state naturally

```csharp
Brain.Machine.CompleteState();
```

This means "the current state finished successfully; let the machine continue
through its natural success path."

You can also complete toward an explicit target:

```csharp
Brain.Machine.CompleteState(_idleState);
Brain.Machine.CompleteState("combat.idle");
Brain.Machine.CompleteState<IdleState>();
```

### Fail the current state explicitly

```csharp
Brain.Machine.FailState(
    _fallbackState,
    "Navigation target disappeared during attack windup.");
```

Use `FailState` when something is wrong or invalid for the current state, not
when the state simply finished its normal work.

## Typical Usage Patterns

### Pattern 1: State-owned natural completion

Inside a state, prefer `CompleteState` when the state reached its intended end.

```csharp
private void OnTick()
{
    if (_elapsed >= _duration)
    {
        Brain.Machine.CompleteState();
    }
}
```

### Pattern 2: State-owned failure with reason

```csharp
private void OnTick()
{
    if (_target == null)
    {
        Brain.Machine.FailState(
            _fallbackState,
            "Target became null during chase.");
    }
}
```

### Pattern 3: External orchestration from another component

```csharp
private void OnCutsceneStarted()
{
    _brain.Machine.Pause();
}

private void OnCutsceneEnded()
{
    _brain.Machine.Resume();
}
```

Use this when a system outside the current state owns a broader application
mode, such as UI, cutscenes, or flow-control screens.

## Decision Rules

Use `RequestStateChange(...)` when:

- the caller is outside the current state
- the transition is an external orchestration decision
- you are not expressing success or failure of the current state itself

Use `CompleteState(...)` when:

- the current state finished its intended job
- you want the result to be recorded as a normal completion

Use `FailState(...)` when:

- the current state cannot keep going safely
- the runtime condition is invalid or broken
- you want the transition report to retain failure context

## Guidance For Humans

- keep transition intent honest; success and failure are not the same thing
- do not bypass `Machine` by trying to mutate internal brain state
- log or attach useful failure messages when the state breaks for a meaningful reason

## Guidance For AI Agents

- before calling `RequestStateChange`, verify the target state is loaded
- if the change is being initiated from inside the active state, prefer
  `CompleteState` or `FailState` instead of `RequestStateChange`
- when documenting or debugging unexpected transitions, inspect
  `LastTransitionReport` before guessing
- do not propose `_status` mutation or direct private-field writes; the public
  control surface is `Brain.Machine`

## Common Mistakes

### Requesting a state that was never loaded

This is the most common self-inflicted wound. Load or resolve the state first
through `Brain.States`.

### Using failure for normal endings

If the state finished what it was supposed to do, that is completion, not
failure.

### Using the generic request overload for a `ScriptableState`

`RequestStateChange<T>()` is constrained to runtime `State` types. Resolve the
loaded scriptable instance first and pass the resulting `IState`.
