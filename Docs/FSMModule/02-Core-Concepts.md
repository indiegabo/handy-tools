# Core Concepts

Before you bury the project under ten states and twenty conditionals, you need to understand which pieces exist and what responsibility belongs to each one.

## 1. `FSMBrain`

`FSMBrain` owns the machine.

It is responsible for:

- loading states
- initializing states
- turning the machine on and off
- tracking the current state, previous state, and first successfully entered state
- executing transitions
- exposing the public API for state changes, pause, resume, and controlled failure
- exposing extension points for project-owned integrations when you decide to build them outside the module

Think of it as the orchestrator. A state should not decide machine infrastructure. A state should decide local behavior.

## 2. `State`

`State` is the base type for states implemented as regular C# classes.

Important characteristics:

- they are instantiated at runtime
- `DisplayName` defaults to the type name
- `Key` defaults to an empty string
- they usually show up when you use `GenericHandyFSMBrain`

Use `State` when the workflow is strongly code-driven.

## 3. `ScriptableState`

`ScriptableState` is the base type for asset-authored states.

Important characteristics:

- they appear in the `FSMBrain` inspector
- they serialize `_name` and `_key`
- the provider instantiates a clone of the asset at runtime
- that means the original asset is not the live runtime state instance

That last point matters. Do not assume the asset in the Project window is the instance currently running. The package clones it to isolate runtime state from authored data.

## 4. `StateProvider`

`StateProvider` performs the loading and lookup work for states.

It keeps lookup tables by:

- type
- string key

It is also responsible for:

- cloning `ScriptableState`
- discovering derived runtime states for `State`
- calling `Initialize` on each state

## 5. `StateTransition`

Each transition contains:

- a `Func<bool>` condition
- a target state
- an integer priority

Evaluation happens inside the current state. The brain asks the active state, "Do you want to transition?" The state walks its transition list and returns the first valid target.

## 6. `TriggersProvider`

The trigger provider is a lightweight bus of named callbacks.

It is useful for:

- firing events without direct coupling between emitter and listener
- notifying active states
- carrying simple payloads through `TriggerData`

## 7. State Lifecycle

These lifecycle method names are recognized automatically by the package:

- `OnInit`
- `OnEnter`
- `OnExit`
- `OnTick`
- `OnFixedTick`
- `OnLateTick`

You do not implement an interface for those. The package uses reflection once during initialization, creates delegates, and then executes those delegates for the lifetime of the machine.

## 8. `CanEnter(IState from)`

Every transition passes through two filters:

1. the transition condition must return `true`
2. the target state must accept the transition through `CanEnter(from)`

This is useful when:

- the raw condition only says it would be desirable to enter the target
- but the target still needs to validate additional preconditions

## 9. Current State, Previous State, and First Entered State

The brain tracks three important references:

- `CurrentState`
- `PreviousState`
- `FirstEnteredState`

`FirstEnteredState` is especially important in the error recovery flow because it can be used as a fallback if the default state is no longer valid.

## 10. `DisplayName` and `Key`

In `State`:

- `DisplayName` starts as the type name
- `Key` defaults to an empty string

In `ScriptableState`:

- `DisplayName` uses `_name` when filled; otherwise it uses the asset name
- `Key` uses `_key`

Use `Key` when you want to resolve a state by string.

## 11. `Owner`

`FSMBrain.Owner` returns:

- the `_owner` configured in the inspector, if any
- otherwise the `transform` of the GameObject that owns the component

That allows states to use `Brain.Owner` without assuming the logical machine owner must be the same object as the component host.

## 12. Legacy Integrations

Older revisions of HandyFSM exposed integration paths for Simple Blackboard and
Character Controller Pro.

Those integrations are no longer supported by this module. If your project still
needs them, keep the integration code in project-owned layers outside
`Assets/HandyFSM`.

## 13. Debugging and History

The package includes editor-side tooling to:

- visualize the loaded state tree
- highlight the path to the current active state
- capture the last play-mode history session
- show the reason behind each transition

That part is detailed in [09 - Debug, History, and Visualizer](09-Debug-History-and-Visualizer.md).

## 14. The Rule of Thumb for Choosing Between `State` and `ScriptableState`

If your doubt is genuine, use this heuristic:

- if the state needs to be configured as an asset and reused from the inspector, use `ScriptableState`
- if the state makes more sense as a pure class discovered by inheritance, use `State`

Neither approach is automatically more correct. What exists is technical context.
