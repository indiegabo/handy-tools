# Blackboard with Simple Blackboard

`Brain.Blackboard` is the optional shared-state surface of the FSM module.

It is available when the Simple Blackboard package is installed, the current
brain is configured to use it, and the assigned container resolves a valid
runtime blackboard.

The core FSM module keeps compiling without the package because the integration
is resolved through a runtime reflection bridge instead of a hard dependency.

## Quick Mental Model

Use the blackboard for persistent shared runtime context that multiple states or
branch systems may need to read over time.

Good blackboard candidates:

- the current combat target
- a branch-wide status flag such as `combat.active`
- a shared runtime modifier such as `movement.speedMultiplier`
- a reference discovered by one state and consumed by another

Poor blackboard candidates:

- a private local value that only one state uses
- authored tuning that belongs in `Brain.Stats`
- event pulses that belong in `Brain.Triggers`
- sampled input that belongs in `Brain.Input`

The blackboard is shared runtime state, not a replacement for every other data
owner in the branch.

## Why This Integration Is Optional

The FSM module resolves Simple Blackboard through `SimpleBlackboardRuntimeBridge`.

This design has practical consequences:

- the core FSM module still compiles when the package is absent
- the inspector can show an install prompt instead of a hard compile error
- the public blackboard domain returns safe false or null style results when the
  integration is unavailable or not configured

That optionality is intentional. Core FSM code should prefer `Brain.Blackboard`
instead of hard-referencing package types directly.

## Enabling It in the Inspector

When the Simple Blackboard package is present:

1. select the `FSMBrain`
2. open the `Third Party` section
3. enable `Use Simple Blackboard?`
4. assign the Simple Blackboard container component

If the package is missing, the brain inspector shows an install prompt.

If the package was just installed and the prompt is still visible, Unity likely
has not finished recompiling or resolving the package yet.

## Public Surface

The relevant blackboard-facing surface is:

- `UseSimpleBlackboard`
- `Brain.Blackboard.IsEnabled`
- `Brain.Blackboard.HasBlackboard`
- `Brain.Blackboard.Container`
- `Brain.Blackboard.Value`
- `Brain.Blackboard.TryGetValue<T>(...)`
- `Brain.Blackboard.SetValue<T>(...)`
- `Brain.Blackboard.TryGetObjectValue(...)`
- `Brain.Blackboard.ContainsValue(...)`

### `IsEnabled`

This indicates that the optional integration is enabled and the runtime bridge
is available.

### `HasBlackboard`

This indicates that the branch currently resolves a valid runtime blackboard
instance from the configured container.

It is possible for `IsEnabled` to be true while `HasBlackboard` is false if the
container is missing or invalid.

### `Container`

This exposes the assigned Simple Blackboard container component.

### `Value`

This exposes the raw runtime blackboard object.

Most gameplay code should prefer the typed helpers instead of using `Value`
directly.

## Safe Failure Behavior

When the integration is unavailable, disabled, or misconfigured:

- `HasBlackboard` is false
- `Value` is null
- `TryGetValue<T>(...)` returns false
- `SetValue<T>(...)` returns false
- `TryGetObjectValue(...)` returns false
- `ContainsValue(...)` returns false

This allows states to treat missing blackboard access as a normal runtime check
instead of handling package-specific failures.

## Typed and Untyped Access

### Typed read

```csharp
if (Brain.Blackboard.TryGetValue("movement.speed", out float speed))
{
    Debug.Log(speed);
}
```

### Typed write

```csharp
Brain.Blackboard.SetValue("movement.speed", 4.5f);
```

### Existence check

```csharp
if (Brain.Blackboard.ContainsValue("movement.speed"))
{
    Debug.Log("movement.speed exists");
}
```

### Untyped read

```csharp
if (Brain.Blackboard.TryGetObjectValue("combat.target", out object value))
{
    Debug.Log(value);
}
```

Prefer typed reads whenever the expected type is known.

## Example: One state writes, another state consumes

This is the canonical blackboard pattern: one authoritative writer and one or
more readers.

Writer:

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    [CreateAssetMenu(
        fileName = "AcquireTargetState",
        menuName = "HandyTools/FSM/Examples/Acquire Target State")]
    public sealed class AcquireTargetState : ScriptableState
    {
        [SerializeField]
        private string _targetKey = "combat.target";

        private void OnTick()
        {
            GameObject target = GameObject.FindWithTag("Enemy");

            if (target == null)
            {
                return;
            }

            if (!Brain.Blackboard.SetValue(_targetKey, target))
            {
                Brain.Machine.FailState(null, "Blackboard target write failed.");
                return;
            }

            Brain.Machine.CompleteState();
        }
    }
}
```

Reader:

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    [CreateAssetMenu(
        fileName = "ChaseTargetState",
        menuName = "HandyTools/FSM/Examples/Chase Target State")]
    public sealed class ChaseTargetState : ScriptableState
    {
        [SerializeField]
        private string _targetKey = "combat.target";

        private void OnTick()
        {
            if (!Brain.Blackboard.TryGetValue(_targetKey, out GameObject target)
                || target == null)
            {
                Brain.Machine.FailState(null, "Blackboard target was missing.");
                return;
            }

            Vector3 direction =
                (target.transform.position - Brain.Owner.position).normalized;

            Brain.Owner.position += direction * Time.deltaTime;
        }
    }
}
```

This is a good blackboard use because the target is branch context rather than a
private detail of either state.

## Example: Shared runtime modifier

```csharp
private const string SpeedMultiplierKey = "movement.speedMultiplier";

private float ResolveSpeedMultiplier()
{
    return Brain.Blackboard.TryGetValue(SpeedMultiplierKey, out float value)
        ? value
        : 1f;
}

private void ApplySlowEffect(float multiplier)
{
    Brain.Blackboard.SetValue(SpeedMultiplierKey, multiplier);
}
```

Use the blackboard for this kind of live runtime modifier when several states may
need the same current value.

If the value is authored tuning rather than runtime context, prefer `Brain.Stats`.

## Key and Ownership Rules

Treat blackboard keys as stable branch contracts.

Good keys:

- `combat.target`
- `combat.active`
- `movement.speedMultiplier`
- `player.lastGroundedTime`

Weak keys:

- `a`
- `thing`
- `temp`
- `myData`

In addition to naming the key well, decide who writes it.

One authoritative writer per key is usually healthy.

Many readers are fine.

Many writers targeting the same key require an explicit contract or the branch
becomes difficult to reason about.

## Choosing Between Blackboard and Other Surfaces

Use `Brain.Blackboard` when:

- the value must remain available over time
- multiple states or branch systems may read it later
- the value is live runtime context

Use `Brain.Stats` when:

- the data is authored tuning
- the runtime should swap whole builds instead of mutating arbitrary fields

Use `Brain.Triggers` when:

- the information is an event pulse rather than persistent state

Use `Brain.Input` when:

- the information is sampled control intent

Use local state fields when:

- the value belongs only to one state and nobody else should depend on it

## Guard Patterns

### Guard the full integration

```csharp
if (!Brain.Blackboard.IsEnabled || !Brain.Blackboard.HasBlackboard)
{
    Brain.Machine.FailState(
        null,
        "Simple Blackboard is not available for this branch.");
    return;
}
```

Use this when the state cannot function without blackboard access.

### Guard one optional key

```csharp
if (!Brain.Blackboard.TryGetValue("combat.target", out GameObject target))
{
    return;
}
```

Use this when missing data is an expected runtime case.

## Guidance for Humans

- use the blackboard only for genuinely shared runtime state
- keep keys stable, searchable, and explicit
- prefer typed accessors over raw object access
- fail honestly when a required key or container is missing
- keep the writer and reader contract clear for every shared key

## Guidance for AI Agents

- if the task mentions shared runtime context across states, inspect
  `Brain.Blackboard` before proposing new branch components
- do not add hard references to `Zor.SimpleBlackboard` types in core FSM code
  unless the task explicitly targets the optional integration boundary
- `IsEnabled` and `HasBlackboard` are different checks and should not be
  treated as synonyms
- when a read fails, inspect availability, toggle state, container assignment,
  key spelling, and type mismatch before changing higher-level logic
- if the data is authored tuning, prefer `Brain.Stats`; if it is an event pulse,
  prefer `Brain.Triggers`

## Common Errors

### "My state cannot read anything"

Checklist:

- is the package installed?
- is `Use Simple Blackboard?` enabled on `FSMBrain`?
- is the container assigned?
- is the read key exactly the same as the write key?
- does the requested type match the stored value type?

### "The blackboard section does not appear"

That usually means the optional package is still unavailable to the project.

### "I duplicated the same value in local state and in the blackboard"

Pick a canonical owner. If several states depend on the value, the blackboard is
usually the cleaner owner. If only one state depends on it, keep it local.
