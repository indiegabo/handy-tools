# FSMBrain and Machine Flow

`FSMBrain` is the composition root for one FSM branch.

It owns the machine lifecycle, state provider, input-source binding, trigger
provider, optional integration bridges, and the public domain API that states
and composition code are expected to use.

If you only remember one thing, remember this: modern HandyTools FSM code talks
to the brain through domains such as `Brain.Machine`, `Brain.States`,
`Brain.Input`, `Brain.Stats`, `Brain.Blackboard`, and `Brain.CCPro`.

## One-Minute Mental Model

- `FSMBrain` is not just a state switcher. It is the runtime owner of one FSM branch.
- states do not reach sideways through random scene references when the brain
  already exposes a supported surface
- `Machine` decides lifecycle and transitions
- `States` decides what is loaded and how to look it up
- `Input` is the cache that state logic reads from
- `Stats` resolves authored builds and runtime overrides
- `Blackboard` is optional shared data access when Simple Blackboard is present
- `CCPro` is optional runtime support for Character Controller Pro branches
- `Triggers` remains a direct provider on the brain because it is a tiny event
  channel, not a full delegated domain class

## What The Brain Owns

The brain owns:

- startup and shutdown of the machine
- the currently active, previous, and default state references
- state initialization timing
- transition evaluation in the enabled Unity loops
- binding and unbinding the configured `FSMInputSource`
- optional Simple Blackboard and CCPro bridge wiring
- per-branch `TriggersProvider` creation
- editor-visible status and state-change events

The brain does not own:

- project-wide dependency injection
- save/load policy
- the meaning of your gameplay rules
- authoring-time creation of state assets
- external systems that choose a `PlayerInput` in custom runtime composition

## Brain Lifecycle

### `Awake`

In `Awake`, the brain builds the branch runtime core.

It resets runtime status, creates the provider objects, binds the input source,
loads the configured scriptable states, creates the trigger provider, runs the
derived-brain initialization hooks, and initializes all loaded states.

This means `OnInit` code can assume the state provider and delegated domains are
already available.

### `OnEnable`

When the component becomes enabled, the brain rebinds the input source and, if
CCPro is active, subscribes to the actor callbacks again.

### `Start`

When initialization mode is `Automatic`, `Start` tries to enter the resolved
default state.

If the default state cannot be resolved, the machine logs an error and stays
off. The failure is explicit because silently starting in an undefined state is
garbage behavior.

### `Update`, `LateUpdate`, and `FixedUpdate`

When CCPro is not active:

- `Update` evaluates transitions only if `Transitions On Update` is enabled,
  then runs `Tick()`
- `LateUpdate` evaluates transitions only if `Transitions On Late Update` is
  enabled, then runs `LateTick()`
- `FixedUpdate` evaluates transitions only if `Transitions On Fixed Update` is
  enabled, then runs `FixedTick()`

When CCPro is active, the relevant runtime flow is concentrated in
`FixedUpdate`, along with the CCPro simulation callbacks.

### `OnDisable`

On disable, the brain unsubscribes CCPro callbacks, unbinds the input source,
clears input runtime state, and stops the machine.

## Inspector Workflow

The custom inspector is organized by responsibility instead of throwing every
reference into one monolithic wall.

### General

Use this section for the machine owner, animator, initialization mode, update
loop strategy, default state, state list, and history capture.

### Input

Use the `Input Source` field when the branch already has a dedicated
`FSMInputSource` implementation or when your composition code assigns one.

### Third Party

This section exposes optional integrations.

- `Use Simple Blackboard?` enables blackboard access when the package is present
- `Use Character Controller Pro?` enables CCPro support when the package is present
- `Setup CCPro FSM` adds the HandyTools-owned support components for a CCPro
  branch, such as `FSMPlayerInputSource`, `FSMStatsRegistry`, and
  `CCProEnvironmentSource`, and resolves or creates the `CharacterActor` when
  necessary

The setup button is idempotent. If one component already exists, the inspector
keeps it and moves on.

### Debug

This section is for history and visualizer-oriented tooling, not for gameplay
composition.

## Domain Map

### `Machine`

Use `Brain.Machine` when you need lifecycle control, transition requests,
completion, failure, or transition reports.

Read [12 - Machine Domain](12-Machine-Domain.md).

### `States`

Use `Brain.States` when you need to load states, resolve loaded states, or
enumerate the provider cache.

Read [13 - States Domain](13-States-Domain.md).

### `Input`

Use `Brain.Input` when state logic should read already-resolved input values or
when a custom input source should publish values into the branch cache.

Read [14 - Input Domain](14-Input-Domain.md).

### `Stats`

Use `Brain.Stats` when a state needs authored tuning or when runtime systems
must swap an entire stats build for the branch.

Read [15 - Stats Domain](15-Stats-Domain.md).

### `Blackboard`

Use `Brain.Blackboard` when the optional Simple Blackboard integration is both
available and enabled for the branch.

Read [07 - Blackboard with Simple Blackboard](07-Blackboard-with-Simple-Blackboard.md).

### `CCPro`

Use `Brain.CCPro` when the branch is CCPro-driven and you need movement
reference data, root-motion flags, or direct access to the configured actor.

Read [08 - Character Controller Pro](08-Character-Controller-Pro.md).

### `Triggers`

Use `Brain.Triggers` when you need a lightweight immediate event channel inside
the branch.

Read [06 - Triggers](06-Triggers.md).

## Example: One State Using Several Brain Surfaces

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
        [CreateAssetMenu(
                fileName = "ChaseTargetState",
                menuName = "HandyTools/FSM/Examples/Chase Target State")]
        public sealed class ChaseTargetState : ScriptableState
        {
                [SerializeField]
                private InputActionReference _moveAction;

                private PatrolState _patrolState;
                private ChaseStats _stats;

                private void OnInit()
                {
                        _patrolState = Brain.States.Get<PatrolState>();
                        _stats = Brain.Stats.Get<ChaseStats>();
                }

                private void OnTick()
                {
                        if (_stats == null)
                        {
                                Brain.Machine.FailState(
                                        _patrolState,
                                        "ChaseStats was not resolved for the branch.");
                                return;
                        }

                        if (!Brain.Input.TryGetVector2(_moveAction, out Vector2 moveInput))
                        {
                                Brain.Machine.FailState(
                                        _patrolState,
                                        "Movement input is unavailable.");
                                return;
                        }

                        if (!Brain.Blackboard.TryGetValue("combat.target", out Transform target)
                                || target == null)
                        {
                                Brain.Machine.CompleteState(_patrolState);
                                return;
                        }

                        Vector3 direction = (target.position - Brain.Owner.position).normalized;
                        Brain.Owner.position += direction * _stats.MoveSpeed * Time.deltaTime;
                }
        }

        public sealed class ChaseStats : FSMStatsAsset
        {
                [field: SerializeField]
                public float MoveSpeed { get; private set; } = 4f;
        }
}
```

The important part is not the exact movement code. The important part is the
ownership split:

- `States` resolved the fallback state
- `Stats` resolved authored tuning
- `Input` provided cached player intent
- `Blackboard` provided shared branch context
- `Machine` handled success or failure transitions

## Guidance For Humans

- think in branch ownership, not in random scene lookups
- read from the domain that already owns the concern instead of re-deriving it
- let the brain stay the composition root; keep project-specific glue in your
  own installers, bootstrap components, or services
- make states small and explicit about which domain surfaces they depend on

## Guidance For AI Agents

- when the task mentions transitions or lifecycle, start at `Brain.Machine`
- when the task mentions loaded state lookup or initialization, start at
  `Brain.States`
- when the task mentions player intent, action caching, or fixed-step button
  windows, start at `Brain.Input`
- when the task mentions sample tuning assets or runtime swap of a full build,
  start at `Brain.Stats`
- do not reintroduce flat helper wrappers onto `FSMBrain`; the supported surface
  is the delegated domain model
- if a state needs data from multiple systems, prefer one small composition step
  through the brain over ad-hoc scene traversal inside the state
- `ExternalRequest`
- `ConditionTransition`
- `NaturalTransition`
- `ErrorTransition`
- `Unknown`

Those reasons feed the debugging history.

## Error Fallback

When a state fails with `StateFailureException`, the brain tries to recover without crashing the application.

The flow is:

1. mark the state as problematic for the current session
2. try to go to the default state if it exists and is not faulted
3. otherwise try the first state that entered successfully
4. if no safe fallback exists, shut the machine down

That means state failure is treated as a controlled runtime event, not as an inevitable crash.

## When to Derive from `FSMBrain`

You derive from `FSMBrain` when you need to:

- load class-based states automatically through `GenericHandyFSMBrain`
- add custom bootstrap logic before initialization
- encapsulate integration with other game components

Example of a simple class-based brain:

```csharp
using IndieGabo.HandyTools.FSMModule.Implementations;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public abstract class PlayerState : State
    {
    }

    public sealed class PlayerBrain : GenericHandyFSMBrain<PlayerState, PlayerIdleState>
    {
    }
}
```

## Recommended Modeling Rule

Use a dedicated base type when working with runtime states.

Good:

```csharp
public abstract class PlayerState : State
{
}

public sealed class PlayerBrain : GenericHandyFSMBrain<PlayerState, PlayerIdleState>
{
}
```

Bad:

```csharp
public sealed class PlayerBrain : GenericHandyFSMBrain<State>
{
}
```

The second case can pull types into the machine that you never intended to load, as long as they are in the same assembly and inherit from `State`.
