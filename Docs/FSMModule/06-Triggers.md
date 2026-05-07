# Triggers

`Brain.Triggers` is the branch-local event channel of the FSM module.

It is a deliberately small, synchronous, string-keyed surface used to express
that something just happened inside the current branch.

Use it for event pulses.

Do not use it as persistent shared state, authored tuning, or a replayable
message queue.

## Quick Mental Model

Triggers are a good fit when:

- one system needs to announce an event without a direct reference to the
  listener
- the event matters at the current moment, not as stored state
- listeners may come and go with state activation

Examples:

- `game.start`
- `damage.received`
- `ui.pause.opened`
- `combat.attack.confirmed`

Triggers are a poor fit when:

- the value must still exist later
- multiple states need to read the latest value over time
- the information is authored tuning data
- the information is sampled player input

In those cases, the better surfaces are usually:

- `Brain.Blackboard` for persistent shared runtime state
- `Brain.Stats` for authored tuning builds
- `Brain.Input` for sampled input state
- `Brain.Machine` for explicit state control

## Where Triggers Fit in the Brain Surface

`Brain.Triggers` remains a direct public surface on `FSMBrain`.

Unlike `Brain.Machine`, `Brain.States`, `Brain.Input`, `Brain.Stats`,
`Brain.Blackboard`, and `Brain.CCPro`, it is not wrapped in a dedicated
`FSMBrain*Domain` type.

That does not make it secondary or deprecated. It is still part of the
supported branch API.

## What the Provider Actually Does

`Brain.Triggers` exposes a `TriggersProvider`.

That provider owns two independent channels:

- callbacks without payloads, keyed by `string`
- callbacks with `TriggerData` payloads, also keyed by `string`

This distinction is important.

The following are separate:

- `Squeeze("combat.start")`
- `Squeeze("combat.start", data)`

If one listener was registered for the dataless overload and another listener
was registered for the payload overload, firing one overload will not invoke the
other listener.

## Dispatch Semantics

Trigger dispatch is:

- immediate
- synchronous
- branch-local
- not queued
- not replayed later

If no callback is registered for a key at the time of dispatch, nothing happens.

The provider copies the current callback list before invocation. That means a
callback can register or unregister other callbacks during dispatch without
invalidating the active iteration.

Duplicate registrations are allowed. If the same callback is registered more
than once for the same key, it will be invoked more than once.

Callbacks are invoked in registration order.

## Public API

### Register a dataless callback

```csharp
Brain.Triggers.RegisterCallback("combat.start", OnCombatStart);
```

### Register a payload callback

```csharp
Brain.Triggers.RegisterCallback("damage.received", OnDamageReceived);
```

### Fire a dataless trigger

```csharp
Brain.Triggers.Squeeze("combat.start");
```

### Fire a payload trigger

```csharp
Brain.Triggers.Squeeze("damage.received", new IntTriggerData(10));
```

### Remove a registration

```csharp
Brain.Triggers.UnregisterCallback("combat.start", OnCombatStart);
Brain.Triggers.UnregisterCallback("damage.received", OnDamageReceived);
```

## Built-In Payload Types

The module already includes these payload wrappers:

- `FloatTriggerData`
- `IntTriggerData`
- `StringTriggerData`
- `BoolTriggerData`
- `ObjectTriggerData`
- `StateTriggerData`

`ObjectTriggerData` also exposes:

- `ValueAs<T>()`
- `TryValueAs<T>(out T value)`

These built-in types are enough for many gameplay cases. Use a custom
`TriggerData` subclass only when the event contract benefits from a dedicated
shape.

## Registration Lifetime Patterns

### Pattern 1: Active-state listener

Register in `OnEnter` and unregister in `OnExit`.

This is the default pattern and the safest one for most states.

```csharp
private void OnEnter()
{
    Brain.Triggers.RegisterCallback("game.start", OnGameStart);
}

private void OnExit()
{
    Brain.Triggers.UnregisterCallback("game.start", OnGameStart);
}
```

Use this when the callback should only be valid while the current state is
active.

### Pattern 2: Whole-lifetime listener

Register in `OnInit` only when the callback is valid for the full lifetime of
the loaded state instance, including while the state is inactive.

If receiving the trigger while inactive would be a bug, do not use this
pattern.

### Pattern 3: External component listener

Non-state components usually pair registration with `OnEnable` and
unregistration with `OnDisable`.

```csharp
private void OnEnable()
{
    _brain.Triggers.RegisterCallback("ui.pause.opened", OnPauseOpened);
}

private void OnDisable()
{
    _brain.Triggers.UnregisterCallback("ui.pause.opened", OnPauseOpened);
}
```

## Example: Waiting for a start signal

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    [CreateAssetMenu(
        fileName = "WaitingForStartState",
        menuName = "HandyTools/FSM/Examples/Waiting For Start State")]
    public sealed class WaitingForStartState : ScriptableState
    {
        private PlayingState _playingState;

        private void OnInit()
        {
            _playingState = Brain.States.Get<PlayingState>();
        }

        private void OnEnter()
        {
            Brain.Triggers.RegisterCallback("game.start", OnGameStart);
        }

        private void OnExit()
        {
            Brain.Triggers.UnregisterCallback("game.start", OnGameStart);
        }

        private void OnGameStart()
        {
            Brain.Machine.CompleteState(_playingState);
        }
    }
}
```

An external component can fire the same trigger without knowing which state is
listening:

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class StartGameButtonDriver : MonoBehaviour
    {
        [SerializeField]
        private FSMBrain _brain;

        public void StartGame()
        {
            if (_brain != null)
            {
                _brain.Triggers.Squeeze("game.start");
            }
        }
    }
}
```

## Example: Payload-driven reaction

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class DamageReceiverState : State
    {
        private int _lastDamage;

        private void OnEnter()
        {
            Brain.Triggers.RegisterCallback("damage.received", OnDamageReceived);
        }

        private void OnExit()
        {
            Brain.Triggers.UnregisterCallback("damage.received", OnDamageReceived);
        }

        private void OnDamageReceived(TriggerData data)
        {
            if (data is not IntTriggerData damage)
            {
                return;
            }

            _lastDamage = damage.Value;
            Debug.Log($"Damage received: {_lastDamage}");
        }
    }
}
```

Sender:

```csharp
Brain.Triggers.Squeeze("damage.received", new IntTriggerData(10));
```

The defensive cast matters because trigger keys are runtime contracts, not
compile-time generic channels.

## Example: `ObjectTriggerData` handoff

```csharp
private void OnTargetLocked(TriggerData data)
{
    if (data is not ObjectTriggerData payload
        || !payload.TryValueAs<Transform>(out Transform target)
        || target == null)
    {
        return;
    }

    Brain.Blackboard.SetValue("combat.target", target);
}
```

This pattern is useful when an event reveals a reference and a persistent shared
owner should store it afterward.

## Key Design Rules

Treat trigger keys as part of the branch contract.

Good keys:

- `game.start`
- `combat.attack.confirmed`
- `ui.pause.opened`
- `damage.received`

Weak keys:

- `A`
- `thing`
- `event1`
- `myTrigger`

Prefer stable, namespaced, searchable keys that describe intent clearly.

## Choosing Between Triggers and Other Surfaces

Use `Brain.Triggers` when:

- the information is an event pulse
- listeners may safely ignore it if nobody is subscribed
- the sender should not require a direct reference to the receiver

Use `Brain.Blackboard` when:

- the value must remain available later
- several states or branch systems may read it over time

Use `Brain.Stats` when:

- the data is authored tuning and should be swapped as a coherent build

Use `Brain.Input` when:

- the data is sampled input, not an event contract

## Guidance for Humans

- scope listeners to the exact lifetime in which they are valid
- keep trigger keys explicit and stable
- treat the payload type as part of the event contract
- move persistent branch data to the blackboard or another clear owner
- inspect duplicate registration first when a callback fires more than once

## Guidance for AI Agents

- start at `TriggersProvider` when the task mentions `RegisterCallback`,
  `UnregisterCallback`, or `Squeeze`
- remember that dataless and payload callbacks are separate registries
- do not propose triggers as a replacement for persistent shared state, input,
  or authored stats
- if behavior looks duplicated, inspect listener lifetime before changing
  unrelated systems
- if an event must be observable later, propose a persistent owner instead of
  assuming replay exists

## Common Mistakes

### Re-registering without unregistering

Because duplicate registrations are allowed, a repeated `OnEnter` registration
without a matching `OnExit` unregistration causes duplicate callback execution.

### Firing the wrong overload

If a listener was registered with payload, the dataless `Squeeze(key)` overload
will not reach it.

### Using triggers as memory

Triggers do not persist values. If a state needs the latest result later, store
that result in a persistent owner such as the blackboard.
