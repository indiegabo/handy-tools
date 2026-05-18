# Cutscenes Module

The `Cutscenes` module ships a scene-authored cutscene runtime, the graph
editor authoring surface, the optional Dialogue System bridge, a built-in
Timeline playback node, and package samples.

## Current Package State

- Runtime code lives under `Runtime/Scripts/Cutscenes`.
- Editor code lives under `Editor/Scripts/Cutscenes`.
- The shared `Handy Tools/Modules` entry is available.
- `CutsceneDirectorInspector` opens the graph window and surfaces validation
  issues directly on the component.
- `CutsceneGraphWindow` uses `GraphView` for node creation, connection
  authoring, persistent edge-color authoring, property editing, and play-mode
  trace visualization.
- `CutsceneGraphBlackboardView` uses UI Toolkit to edit graph blackboard
  entries inside the graph window sidebar.
- The graph window persists the bound `CutsceneDirector` across recompiles.
- Graph nodes ship with stable category palettes, header icons, and one
  visual-only comment node for authoring-only annotations.
- Package samples include both scene-authored graphs and installer-backed
  setups depending on the sample workflow.

## Runtime Surface

- `CutsceneDirector`
- `CutsceneGraph`
- `CutsceneGraphBlackboard`
- `CutsceneGraphBlackboardEntry`
- `CutsceneRun`
- `CutsceneExecutionContext`
- `CutsceneRuntimeStateStore`
- `CutsceneRunTrace`
- `CutsceneTrigger`
- `CutsceneTriggerMode`
- `CutsceneNodeMenuAttribute`
- `CutsceneBusEventAttribute`
- `CutsceneBusEventRegistry`
- `ICutsceneService`
- built-in flow, action, signal, trigger, Timeline, and Dialogue nodes

## Runtime Reference Cards

### `CutsceneDirector`

- Scene-facing authoring root for one cutscene instance.
- Owns the serialized `CutsceneGraph`, play policy, time mode, autoplay, and
  one-shot configuration.
- Use it when scene code needs to play, restart, cancel, or inspect one
  specific cutscene.
- Do not store transient execution state here; that belongs to the active
  `CutsceneRun` and node runtime state storage.

### `CutsceneRun`

- Runtime execution object created for one active play session of one
  `CutsceneDirector`.
- Owns active executions, trace data, signal counts, runtime state storage,
  status, failure reason, and the graph blackboard access point.
- Use it when runtime systems need to inspect what is running now, which node
  is active, or why a run failed.
- Do not serialize or cache it across scene reload assumptions; it is a live
  runtime object, not one persisted authoring asset.

### `CutsceneExecutionContext`

- Per-node runtime contract passed into `OnEnter`, `Tick`, and `OnExit`.
- Exposes the current director, service, execution id, delta time, blackboard,
  signals, per-node runtime state, and completion helpers.
- Use it inside nodes instead of reaching outward for runtime globals.
- Do not bypass it by resolving ad-hoc cutscene state from unrelated singletons
  when the data already lives on the context.

### `ICutsceneService`

- Runtime orchestration boundary used by directors and higher-level systems.
- Starts and stops directors, resolves active runs, reports whether one
  director is currently running, and exposes the optional Dialogue System
  bridge.
- Depend on this interface when other systems need to coordinate cutscene runs
  without knowing the concrete service implementation.
- Prefer the interface over the concrete `CutsceneService` in consumers unless
  the package itself is extending the service internals.

### `CutsceneTrigger`

- Small scene component that turns one lifecycle moment or manual button press
  into `CutsceneDirector.Play()`.
- Supports `Manual`, `Awake`, `OnEnable`, and `Start` trigger modes plus one
  gate and one-shot guard.
- Use it for scene-authored startup and interaction wiring when no custom code
  needs to decide the exact play moment.
- Do not turn it into a replacement for runtime orchestration logic; if the
  start conditions become stateful or systemic, move that decision back into
  gameplay code and call the director or service directly.

## Editor Surface

- `CutscenesModuleConfigurationPanel`
- `CutsceneDirectorInspector`
- `CutsceneGraphWindow`
- `CutsceneGraphBlackboardView`
- `CutsceneGraphView`
- `CutsceneNodeCreationRegistry`
- `CutsceneGraphValidator`

## Five-Minute Quickstart

Use this path when the goal is to prove that the module is wired correctly
before authoring custom nodes or expanding the package.

1. Enable `Cutscenes` in `Handy Tools/Modules` when the module is inactive.
2. Add `CutsceneDirector` to one scene object.
3. Optionally add `CutsceneTrigger` to the same object when the scene should
   start the graph automatically or through one button press.
4. Open the graph through the `CutsceneDirector` inspector `Open Graph`
   button or through `HandyTools/Cutscenes/Graph Editor`.
5. Bind the target director in the graph window toolbar if the window opened
   without one selected director.
6. Create `Entry`, `Wait`, and `Finish`.
7. Connect `Entry -> Wait -> Finish`.
8. Set the wait duration on the `Wait` node inspector.
9. If `CutsceneTrigger` is present, assign the director when it is not on the
   same object and choose `Manual`, `Awake`, `OnEnable`, or `Start`.
10. Enter Play Mode and either call `Trigger()` from the component button or
    let the selected trigger mode fire automatically.

Expected result:

- The graph window highlights the currently running node.
- Traversed edges and completed nodes update during play mode.
- Validation stays empty for the minimal graph.
- The same setup can be switched between scaled and unscaled time through the
  director time mode plus the authored wait configuration.

## Graph Blackboard

`CutsceneGraph` now owns one native blackboard that stores graph-level values
shared across node executions.

- Author entries in the `Blackboard` foldout on the graph window sidebar.
- Supported value types are `int`, `float`, `string`, `bool`, and
  `UnityEngine.Object` references.
- Read and write values through `CutsceneRun.Blackboard`,
  `CutsceneExecutionContext.Blackboard`, or the convenience helpers
  `TryGetBlackboardValue`, `SetBlackboardValue`,
  `GetOrCreateBlackboardValue`, and `RemoveBlackboardValue`.
- Every node that is actually executing receives the same graph blackboard
  through `CutsceneExecutionContext`, so `OnEnter`, `Tick`, and `OnExit` can
  all use `context.Blackboard` without resolving separate graph state.
- Existing graphs remain valid; an empty blackboard is created lazily.

Built-in blackboard authoring nodes now ship in the base module:

- `Actions/Blackboard/Set Blackboard Values`
- `Actions/Blackboard/Log Blackboard Value`

Blackboard-bound value sources are also available across the base node catalog,
including `Flow/Branch`, `Actions/Set GameObject Active`, and
`Actions/Set Behaviour Enabled`, so one graph can consume authored values
without requiring dedicated per-node blackboard variants.

## Graph Presentation

- Nodes use category-driven header palettes and icons in both the graph and
  the node-creation search UI.
- `Meta/Comment` is one visual-only annotation node with no ports; it stays
  out of auto-arrange and topology validation.
- Edge context menus expose persistent authored connection colors.
- Play-mode traversed-edge highlighting preserves authored edge hue when one
  custom connection color is present.

Minimal blackboard read example:

```csharp
using IndieGabo.HandyTools.CutscenesModule.Core;

namespace Game.Cutscenes
{
  [System.Serializable]
  [CutsceneNodeMenu("Gameplay/Log Blackboard Message")]
  public sealed class LogBlackboardMessageNode : CutsceneNodeBase
  {
    [UnityEngine.SerializeField]
    private string _key = "sample.message";

    public override void OnEnter(CutsceneExecutionContext context)
    {
      if (!context.TryGetBlackboardValue(_key, out string message))
      {
        context.TryComplete(CutsceneNodeResult.Failure(
          $"Missing blackboard key '{_key}'."));
        return;
      }

      UnityEngine.Debug.Log(message);
      context.TryComplete(CutsceneNodeResult.Success());
    }
  }
}
```

## Authoring Custom Nodes

Projects that consume HandyTools can define their own cutscene nodes without
modifying the package editor code.

### Discovery Rules

- Create a non-abstract type that inherits `CutsceneNodeBase`.
- Mark the type with `[System.Serializable]`.
- Mark the type with `[CutsceneNodeMenu("Folder/Node Name")]`.
- Optionally register one default graph title with
  `[CutsceneNodeMenu("Folder/Node Name", "Default Title")]`.
- Keep the node type in a runtime assembly that is loaded by Unity.
- If the project uses asmdefs, reference `IndieGabo.HandyTools.Cutscenes`.
- The graph window discovers eligible node types automatically through
  `TypeCache`; no manual registration step is required.

`Runtime assembly` means the node script must compile into the game runtime,
not into an editor-only assembly.

- If the project does not use asmdefs, put the script outside any `Editor`
  folder. Unity will compile it into `Assembly-CSharp`, which is enough.
- If the project uses asmdefs, put the script under one project asmdef that is
  included in runtime builds and add a reference to
  `IndieGabo.HandyTools.Cutscenes`.
- Do not place custom node runtime classes inside `Editor` folders or editor
  asmdefs. The graph may see the type in the editor, but the node will not be
  valid for runtime execution or player builds.

Example runtime locations:

- `Assets/Game/Cutscenes/Nodes/MyCustomNode.cs`
- `Assets/_Project/Runtime/Cutscenes/Nodes/MyCustomNode.cs`

Example asmdef setup when the project uses asmdefs:

```json
{
  "name": "Game.Runtime",
  "references": ["IndieGabo.HandyTools.Cutscenes"]
}
```

With that setup, a project-defined class derived from `CutsceneNodeBase`
becomes available in the graph automatically after Unity recompiles.

## Authoring Registered Cutscene Events

The built-in `Emit HandyBus Event` and `Wait For Event` nodes now support two
authoring modes:

- `Custom Name`: uses the legacy string-based HandyBus channel backed by
  `CutsceneExternalEventRaisedEvent`.
- `Registered Event`: lets the node pick one concrete HandyBus event type from
  a registry discovered automatically at runtime and in the editor.

Use `Custom Name` when the cutscene should coordinate with arbitrary string
events. Use `Registered Event` when the project wants stronger discovery,
picker support, and a stable event catalog.

### Discovery Rules For Registered Events

- Create a non-abstract class that implements `IEvent`.
- Mark the class with `[CutsceneBusEvent("Folder/Logical Path")]`.
- Keep the type in a runtime assembly that is loaded by Unity.
- Ensure the type exposes a parameterless constructor.
- The cutscene event picker discovers eligible types automatically after Unity
  recompiles.

Example registered cutscene event:

```csharp
using System;
using IndieGabo.HandyTools.CutscenesModule.Events;
using IndieGabo.HandyTools.HandyBusModule;

namespace Game.Cutscenes
{
    [Serializable]
    [CutsceneBusEvent(
        "Gameplay/Cutscenes/Alarm Triggered",
        DisplayName = "Alarm Triggered")]
    public sealed class AlarmTriggeredCutsceneEvent : IEvent
    {
    }
}
```

After Unity recompiles, the event appears in the `Registered Event` picker for
both cutscene nodes.

### Inspector And Serialization Rules

- The graph node inspector draws serialized fields automatically through the
  `CutsceneDirector` serialized graph data.
- Private `[SerializeField]` fields appear exactly like built-in node fields.
- Unity object references such as `GameObject`, `Transform`,
  `PlayableDirector`, and custom `Component` types persist with the graph.
- `CutsceneGraphValidator` automatically flags null Unity object references on
  custom nodes when the field looks mandatory.
- The runtime manages `_id` and `_position`; custom nodes should not expose or
  overwrite those values directly.

### Runtime Lifecycle

- Override `OnEnter` for immediate work or to bind asynchronous callbacks.
- Override `RequiresTick` and `Tick` when the node needs frame-by-frame polling.
- Override `OnExit` to clean up listeners, handles, or long-lived state.
- Treat `CutsceneExecutionContext` as the canonical runtime surface passed into
  every node callback.
- Call `context.TryComplete(CutsceneNodeResult.Success())` for synchronous
  completion.
- Call `context.TryCompleteNode(context.CurrentNodeExecutionId, result)` or a
  captured execution id from asynchronous callbacks.
- Use `context.GetOrCreateNodeState`, `context.TryGetNodeState`, and
  `context.SetNodeState` to persist per-node runtime state safely.
- Use `context.Blackboard` or the blackboard helper methods when the node needs
  graph-shared runtime state. Do not cache a separate blackboard reference on
  the serialized node definition.

### Blackboard Access In Custom Nodes

Every node that runs inside one cutscene execution already receives the graph
blackboard through `CutsceneExecutionContext`.

- Prefer `context.Blackboard` when the node needs the full blackboard surface.
- Prefer `context.TryGetBlackboardValue`, `context.SetBlackboardValue`, and
  related helpers when the node only needs typed reads or writes.
- Prefer the execution context over reaching back through
  `context.Director.Graph.Blackboard`, because the context is the explicit
  runtime contract passed to every node callback.

This means custom nodes do not need a special injection hook or extra
constructor argument to use graph-shared state; the blackboard is already part
of the execution contract.

### Minimal Immediate Node Example

```csharp
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace Game.Cutscenes
{
    [System.Serializable]
    [CutsceneNodeMenu("Gameplay/Debug Message", "Debug Message")]
    public sealed class DebugMessageNode : CutsceneNodeBase
    {
        [SerializeField]
        private string _message = "Hello from a custom cutscene node.";

        public override string GetSummary()
        {
            return _message;
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            Debug.Log(_message);
            context.TryComplete(CutsceneNodeResult.Success());
        }
    }
}
```

### Multi-Output Node Example

```csharp
using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace Game.Cutscenes
{
    [System.Serializable]
    [CutsceneNodeMenu("Gameplay/Random Branch")]
    public sealed class RandomBranchNode : CutsceneNodeBase
    {
        private static readonly IReadOnlyList<CutsceneNodePort> _ports = new[]
        {
            new CutsceneNodePort(CutsceneNodePorts.True, "True"),
            new CutsceneNodePort(CutsceneNodePorts.False, "False"),
        };

        [Range(0f, 1f)]
        [SerializeField]
        private float _trueThreshold = 0.5f;

        public override IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            return _ports;
        }

        public override string GetSummary()
        {
            return $"True when Random.value <= {_trueThreshold:0.##}";
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            bool useTrueOutput = Random.value <= _trueThreshold;

            context.TryComplete(CutsceneNodeResult.Success(
                useTrueOutput ? CutsceneNodePorts.True : CutsceneNodePorts.False));
        }
    }
}
```

### Asynchronous Node Pattern Example

The built-in `Play Timeline` node follows the standard asynchronous pattern:
bind a callback in `OnEnter`, capture the current execution id, and detach the
callback in `OnExit`.

```csharp
using System;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.Playables;

namespace Game.Cutscenes
{
    [System.Serializable]
    [CutsceneNodeMenu("Gameplay/Wait For Director")]
    public sealed class WaitForDirectorNode : CutsceneNodeBase
    {
        private const string RuntimeStateKey = "RuntimeState";

        [SerializeField]
        private PlayableDirector _director;

        public override string GetSummary()
        {
            return _director == null ? "No PlayableDirector" : _director.name;
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            if (_director == null)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Wait For Director requires a PlayableDirector."));
                return;
            }

            RuntimeState state = context.GetOrCreateNodeState(
                RuntimeStateKey,
                static () => new RuntimeState());

            if (state.Director != null && state.StoppedHandler != null)
            {
                state.Director.stopped -= state.StoppedHandler;
            }

            SerializableGuid executionId = context.CurrentNodeExecutionId;
            PlayableDirector targetDirector = _director;

            state.Director = targetDirector;
            state.StoppedHandler = _ => context.TryCompleteNode(
                executionId,
                CutsceneNodeResult.Success());

            targetDirector.stopped += state.StoppedHandler;
            context.SetNodeState(RuntimeStateKey, state);
            targetDirector.Play();
        }

        public override void OnExit(CutsceneExecutionContext context)
        {
            if (!context.TryGetNodeState(RuntimeStateKey, out RuntimeState state))
            {
                return;
            }

            if (state.Director != null && state.StoppedHandler != null)
            {
                state.Director.stopped -= state.StoppedHandler;
            }

            state.Director = null;
            state.StoppedHandler = null;
            context.SetNodeState(RuntimeStateKey, state);
        }

        private sealed class RuntimeState
        {
            public PlayableDirector Director;
            public Action<PlayableDirector> StoppedHandler;
        }
    }
}
```

### Recommended Workflow For Project Nodes

1. Put the node class in the project runtime assembly.
2. Keep all authoring data in serialized fields.
3. Use `GetSummary()` to surface the most relevant state in the graph body.
4. Prefer explicit failure messages when required references are missing.
5. Use runtime state rather than static fields for asynchronous bindings.
6. Add a graph validation expectation for any mandatory scene reference.

## Workflow For Expanding The Package

Use this workflow when the goal is not only to consume the API, but to add new
built-in nodes, editor behavior, or integration points to HandyTools itself.

1. Decide whether the new behavior is project-specific or package-owned.
   Project-specific behavior should usually be implemented as one consumer
   node derived from `CutsceneNodeBase` instead of changing package code.
2. When the feature is package-owned, put the runtime node in the owning
   category folder under `Runtime/Scripts/Cutscenes/Nodes/Actions`, `Flow`,
   `Signals`, or `Meta`.
3. Keep scene-owned authoring data on `CutsceneDirector` and keep run state in
   `CutsceneRun`, `CutsceneExecutionContext`, or node runtime state storage.
   Do not push transient runtime state back into serialized node definitions.
4. Rely on the default inspector and type discovery first. A new built-in node
   does not require editor registration when serialized fields and
   `CutsceneNodeMenu` metadata are enough.
5. Touch editor code only when the feature needs one new validation rule,
   custom drag-and-drop behavior, custom graph presentation, or a dedicated
   authoring affordance.
6. If the node introduces new mandatory outputs, scene references, or binding
   rules, extend `CutsceneGraphValidator` so authoring failures are visible
   before play mode.
7. If the node introduces one new category or needs different visual language,
   update `CutsceneNodePresentationRegistry` so the graph and creation search
   stay visually coherent.
8. Keep optional Dialogue System behavior isolated. Serialized dialogue node
   data may live in the base cutscenes runtime, but typed Pixel Crushers code
   must remain in `IndieGabo.HandyTools.Cutscenes.DialogueSystem` and
   `IndieGabo.HandyTools.Cutscenes.DialogueSystem.Editor` behind the
   synchronized define.
9. Add or update EditMode smoke coverage under `Assets/Tests/CutscenesEditMode`
   when the feature changes execution flow, validation, or graph serialization.
10. Update the module guide and sample README files when the new feature
    changes the recommended authoring workflow or becomes part of the public
    built-in node catalog.

Ownership map for common expansion points:

- `CutsceneDirector`: scene-owned authoring root, runtime policy, and graph
  ownership.
- `CutsceneRun`: execution authority and active-branch orchestration.
- `CutsceneExecutionContext`: node runtime contract, completion, blackboard,
  and per-node state access.
- `ICutsceneService` and `CutsceneService`: cross-director run management and
  optional bridge lookup.
- `CutsceneGraphValidator`: authoring-time diagnostics.
- `CutsceneGraphWindow`, `CutsceneGraphInspectorView`, and
  `CutsceneGraphBlackboardView`: graph authoring UI.

## Built-In Timeline Node

`CutscenePlayTimelineNode` lives under `Actions/Play Timeline` and executes a
`PlayableDirector` that is already configured with a Timeline asset.

- The node fails immediately when the `PlayableDirector` reference is missing.
- The node fails immediately when the `PlayableDirector` has no playable asset.
- The node can restart playback from time zero on enter.
- The node reports success when the director raises its `stopped` callback.
- The node can stop playback on exit when the cutscene is cancelled or
  restarted mid-timeline.

Recommended authoring setup:

1. Add a `PlayableDirector` component to one scene object.
2. Assign the Timeline asset to that director.
3. Disable `Play On Awake` on the director when the cutscene should own the
   playback moment.
4. Assign that director to `Play Timeline` in the graph inspector.
5. Connect the node `Next` output to the continuation branch that should run
   after the Timeline finishes.

## Built-In Branch Nodes

The module now ships two distinct built-in branch nodes:

- `Boolean Branch` is the old true/false branch. It exposes exactly two outputs:
  `True` and `False`.
- `Branch` is the dynamic value-based branch. It lets you add as many outputs
  as needed and assign one match value to each output.

`Branch` authoring model:

1. Set the node `Value` field to the value that should be evaluated on enter.
2. Add one list element per possible output.
3. For each element, define the `Match Value` that selects it.
4. Give each element one readable `Display Name` for the graph port label.
5. Connect every generated output port to its destination node.

At runtime, `Branch` compares the current node value against the configured
match values and chooses the first matching output. If no output matches, the
node fails with an explicit runtime error.

## Optional Dialogue System Integration

- `CutsceneDialogueConversationNode` lives in the base cutscenes runtime
  assembly so serialized node data survives when Dialogue System is absent.
- Typed Pixel Crushers references live only in
  `IndieGabo.HandyTools.Cutscenes.DialogueSystem`.
- Editor-side authoring helpers live in
  `IndieGabo.HandyTools.Cutscenes.DialogueSystem.Editor`.
- `DialogueSystemIntegrationAvailability` probes the Dialogue System runtime
  types.
- The runtime and editor child asmdefs are constrained by
  `HANDY_DIALOGUE_SYSTEM_PRESENT`.
- `HandyScriptingDefineRegistry` now synchronizes that define automatically.
- Dialogue node creation stays hidden from the graph window when Dialogue
  System is unavailable, while existing serialized dialogue nodes remain
  intact and warn through validation.

## Current Limitations

- `CutsceneParallelNode` fans out into all connected branches and waits for all
  spawned branches to complete before resolving the owning fork, but it does
  not yet expose configurable join policies or partial-failure recovery.
- The current graph window focuses on one director at a time and does not yet
  provide richer authoring helpers such as dedicated conversation pickers.

## Samples

- `Cutscenes Base Sample`
- `Cutscenes Dialogue System Sample`

Open `Samples/Cutscenes Base Sample/Scenes/CutscenesBaseSample.unity` and
press Play to run the non-dialogue sample. The base sample starts from one
scene-authored `CutsceneTrigger`, includes one native writer node that writes
the initial payload, and uses blackboard-bound consumers later in the same
flow.

Open `Samples/Cutscenes Dialogue System Sample/Scenes/CutscenesDialogueSystemSample.unity`
and press Play in a project where Dialogue System is installed to run the
optional integration sample.
