# Creating States

This is the heart of the matter. If you understand the state authoring surface, you understand most of HandyFSM.

## Two State Families

HandyFSM works with two main base types:

- `State`
- `ScriptableState`

Both share the same conceptual lifecycle and transition model.

## Lifecycle Methods Recognized Automatically

These method names are detected through reflection and converted into delegates during `Initialize`:

- `OnInit`
- `OnEnter`
- `OnExit`
- `OnTick`
- `OnFixedTick`
- `OnLateTick`

Expected signatures in these common bases:

- no parameters
- `void` return type
- they may be `private`, `protected`, or `public`

Simple example:

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class PlayerIdleState : State
    {
        private void OnInit()
        {
            SetName("Player Idle");
        }

        private void OnEnter()
        {
            Debug.Log("Idle entered");
        }

        private void OnTick()
        {
        }

        private void OnExit()
        {
            Debug.Log("Idle exited");
        }
    }
}
```

## When to Use `OnInit`

Use `OnInit` for configuration work that needs to happen once per state instance.

Good examples:

- resolve references to other states through `Brain.States.Get<T>()`
- register fixed transitions
- adjust the displayed name through `SetName`
- cache stable references

Bad examples:

- logic that depends on the state being active right now
- registering something that must be unregistered every time the state enters and exits
- reading information that only exists after another system begins simulation and has not been prepared yet

## When to Use `OnEnter`

`OnEnter` is where you prepare the activation of that state.

Examples:

- reset state timers
- start animations
- register temporary callbacks
- enable flags associated with the current behavior

## When to Use `OnExit`

`OnExit` is for local cleanup.

Examples:

- unregister temporary triggers or event listeners
- disable state-specific effects
- clear transient references

## `State`: Class-Based Authoring

### Complete Example

These examples use `ExampleInput.Horizontal` as a stand-in for a project-owned
adapter that reads from the New Input System.

```csharp
using UnityEngine;
using IndieGabo.HandyTools.FSMModule.Implementations;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public abstract class PlayerState : State
    {
        protected Transform Owner => Brain.Owner;
    }

    public sealed class PlayerIdleState : PlayerState
    {
        private PlayerMoveState _moveState;

        private void OnInit()
        {
            _moveState = Brain.States.Get<PlayerMoveState>();

            AddTransition(
                () => ExampleInput.Horizontal != 0f,
                _moveState,
                100);

            SortTransitions();
        }
    }

    public sealed class PlayerMoveState : PlayerState
    {
        private PlayerIdleState _idleState;

        private void OnInit()
        {
            _idleState = Brain.States.Get<PlayerIdleState>();

            AddTransition(
                () => ExampleInput.Horizontal == 0f,
                _idleState,
                100);

            SortTransitions();
        }

        private void OnTick()
        {
            Vector3 step = Vector3.right * ExampleInput.Horizontal * Time.deltaTime;
            Owner.position += step;
        }
    }

    public sealed class PlayerBrain : GenericHandyFSMBrain<PlayerState, PlayerIdleState>
    {
    }
}
```

### What This Example Shows

- class-based states are discovered by the generic brain
- the default state comes from the generic `TDefaultState` parameter
- the machine does not need an asset list in the inspector

## `ScriptableState`: Asset-Based Authoring

### Complete Example

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    [CreateAssetMenu(
        fileName = "PlayerIdleAssetState",
        menuName = "HandyTools/FSM/Examples/Player Idle Asset State")]
    public sealed class PlayerIdleAssetState : ScriptableState
    {
        private PlayerMoveAssetState _moveState;

        private void OnInit()
        {
            _moveState = Brain.States.Get<PlayerMoveAssetState>();

            AddTransition(
                () => ExampleInput.Horizontal != 0f,
                _moveState,
                100);

            SortTransitions();
        }
    }

    [CreateAssetMenu(
        fileName = "PlayerMoveAssetState",
        menuName = "HandyTools/FSM/Examples/Player Move Asset State")]
    public sealed class PlayerMoveAssetState : ScriptableState
    {
        private PlayerIdleAssetState _idleState;

        private void OnInit()
        {
            _idleState = Brain.States.Get<PlayerIdleAssetState>();

            AddTransition(
                () => ExampleInput.Horizontal == 0f,
                _idleState,
                100);

            SortTransitions();
        }
    }
}
```

## `CanEnter(IState from)`

This method validates entry on the target state itself.

Example:

```csharp
public override bool CanEnter(IState from)
{
    return Brain.IsOn && Time.timeScale > 0f;
}
```

Use `CanEnter` for target-side restrictions. Use the transition condition for source-state local rules.

## Helpers Already Exposed by `State` and `ScriptableState`

Both base types already give you access to:

- `Brain`
- `Brain.States`
- `Brain.Machine`
- `Brain.Input`
- `Brain.Blackboard`
- `Brain.CCPro`
- `ThrowStateFailure()`

## How to Finish a State Correctly

If the state ended normally, use:

```csharp
Brain.Machine.CompleteState();
```

ou

```csharp
CompleteState(targetState);
```

If the state found a gameplay or business-level failure and needs to move through an error path, use:

```csharp
FailState(null, "The target dependency is missing.");
```

If you are deeper in the execution flow and want to abort the state immediately, use:

```csharp
ThrowStateFailure("The state entered an invalid runtime condition.");
```

## What Happens If I Throw a Regular `Exception`?

The brain's special recovery mechanism is designed for `StateFailureException`.

If you throw generic exceptions and do not catch them, behavior falls back to normal Unity and C# exception handling at that point. Do not use that as your routine state control-flow mechanism.

## Practical Rule for Modeling States

A good state usually:

- has one clear responsibility
- knows only a small set of dependencies
- decides only a small set of transitions
- keeps `OnEnter` and `OnExit` symmetric
- does not mix rules from too many different subsystems

A bad state usually:

- turns into a warehouse of `if` statements
- stores far too much global data
- registers callbacks on every `OnEnter` and never removes them
- creates too many transitions in too many directions
