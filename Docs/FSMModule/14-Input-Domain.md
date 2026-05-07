# Input Domain

`Brain.Input` is the runtime cache for branch input.

States should read from this cache. Input sources should publish into this cache.
That separation is the main reason the domain exists.

## Quick Mental Model

The input domain owns resolved values, not raw device polling.

- `FSMInputSource` implementations push values into the brain
- the brain stores those values in one branch-local cache
- states read typed values, snapshots, and recent-button windows from that cache
- optional CCPro movement-reference input is fed through the same input source layer

This is what keeps input access coherent across update loops and state types.

## What The Input Domain Owns

`Brain.Input` owns:

- `Source`
- `Has(...)`
- `TryGetButton(...)`
- `TryGetFloat(...)`
- `TryGetVector2(...)`
- `TryGetSnapshot(...)`
- `HasRecentButtonStart(...)`
- `TryConsumeRecentButtonStart(...)`
- `CopySnapshots(...)`
- `SetButtonValue(...)`, `SetFloatValue(...)`, `SetVector2Value(...)`
- `ClearValues()`

The `Set*Value(...)` methods are public because custom input-source or bridge
code may need them. Regular state logic should mostly read, not write.

## Read Workflow For States

### Read a button

```csharp
if (Brain.Input.TryGetButton(_jumpAction, out bool jumpPressed) && jumpPressed)
{
    Brain.Machine.RequestStateChange<JumpState>();
}
```

### Read a `Vector2`

```csharp
Vector2 movement = Brain.Input.TryGetVector2(_movementAction, out Vector2 value)
    ? value
    : Vector2.zero;
```

### Read a full snapshot

```csharp
if (Brain.Input.TryGetSnapshot(_jumpAction, out FSMInputSnapshot snapshot))
{
    if (snapshot.ButtonStarted)
    {
        Debug.Log("Jump started this frame.");
    }
}
```

Use snapshots when you need richer debug or timing context than a plain boolean.

## Fixed-Step Button Windows

The domain includes `HasRecentButtonStart(...)` and
`TryConsumeRecentButtonStart(...)` for a reason.

When an input source writes in `Update` but gameplay consumes in `FixedUpdate`, a
short button tap can occur entirely between two fixed steps. A frame-local
`ButtonStarted` check is not enough.

Use a recent press window instead.

```csharp
private const float JumpBufferSeconds = 0.12f;

private void OnFixedTick()
{
    if (Brain.Input.TryConsumeRecentButtonStart(
            _jumpAction,
            JumpBufferSeconds))
    {
        PerformJump();
    }
}
```

This is the correct pattern for jump buffering, double-jump windows, wall-jump
entry, and similar fixed-step gameplay.

## Example: Reading Input in a State

```csharp
private bool ReadDashPressed()
{
    return Brain.Input.TryGetButton(_dashAction, out bool value) && value;
}

private Vector2 ReadMovement()
{
    return Brain.Input.TryGetVector2(_movementAction, out Vector2 value)
        ? value
        : Vector2.zero;
}
```

This keeps the state independent from whichever concrete source currently owns
the branch, whether that is `FSMPlayerInputSource` or a custom project source.

## Example: Publishing Input From a Custom Source

```csharp
using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class SensorInputSource : FSMInputSource
    {
        [SerializeField]
        private string _moveIdString = "c9961ddd-e3f4-4efe-9f74-d9f9b96b0175";

        private Guid _moveId;

        private void Awake()
        {
            _moveId = Guid.Parse(_moveIdString);
        }

        private void Update()
        {
            Vector2 move = new(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            ReportVector2(_moveId, "Move", move);
            ReportMovementInput(move);
        }
    }
}
```

The key idea is not the legacy `Input.GetAxisRaw` part. The key idea is that a
custom source writes into the branch through the reporting helpers, and every
state continues reading from `Brain.Input`.

## Example: Copy Snapshots For Diagnostics

```csharp
private readonly List<FSMInputSnapshot> _snapshots = new();

private void LateUpdate()
{
    Brain.Input.CopySnapshots(_snapshots);

    for (int index = 0; index < _snapshots.Count; index++)
    {
        Debug.Log(_snapshots[index].EffectiveDisplayName + ": "
            + _snapshots[index].FormattedValue);
    }
}
```

This is useful for custom debug overlays, replay diagnostics, and AI-agent
inspection tooling.

## Guidance For Humans

- let sources write and states read; that split keeps the branch understandable
- when a gameplay action is consumed in `FixedUpdate`, use the recent-button
  helpers instead of pretending frame-local edge checks are enough
- keep action references on the state or source that semantically owns them

## Guidance For AI Agents

- when the code already has `InputActionReference` fields and a bound
  `FSMInputSource`, prefer `Brain.Input` over direct polling of
  `actionReference.action.ReadValue(...)`
- `FSMInputSnapshot.ButtonStarted` is frame-local; for fixed-step logic prefer
  `HasRecentButtonStart(...)` or `TryConsumeRecentButtonStart(...)`
- if the task is about authoring a new source, inspect `FSMInputSource` and use
  its reporting helpers instead of mutating private brain state
- do not forget the optional CCPro semantic movement feed; custom sources can
  call `ReportMovementInput(...)`

## Common Mistakes

### Polling raw actions in every state

That bypasses the branch cache and recreates the update/fixed-update timing mess
the domain exists to solve.

### Writing input from arbitrary gameplay systems

The write surface exists for dedicated bridges and sources. If any random system
can overwrite branch input, the data contract becomes incoherent fast.

### Forgetting to consume buffered presses

If the same recent button start is checked repeatedly without consumption, later
states can replay a press that should already be spent.
