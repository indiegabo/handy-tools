# Cutscenes Module

The `Cutscenes` module is the richest graph-backed module in HandyTools. It
ships a scene-authored cutscene runtime, composes the shared GraphCore
runtime and editor layer into the cutscene-specific graph experience, keeps
the optional Dialogue System bridge behind child asmdefs, includes one built-
in cross-module `Conversations/Start Conversation` node, includes a built-in
Timeline playback node, and ships package samples.

## Current Package State

- Runtime cutscene code lives under `Runtime/Scripts/Cutscenes`.
- Editor cutscene code lives under `Editor/Scripts/Cutscenes`.
- Shared graph-neutral runtime primitives now live under
  `Runtime/Scripts/GraphCore`.
- Shared graph-neutral editor primitives now live under
  `Editor/Scripts/GraphCore`.
- The shared `Handy Tools/Modules` entry is available.
- `CutsceneDirectorInspector` opens the graph window and surfaces validation
  issues directly on the component.
- `CutsceneGraph`, `CutsceneGraphBlackboard`, and `CutsceneValueSource` now sit
  on top of shared GraphCore authored containers instead of maintaining one
  fully separate graph substrate.
- `CutsceneGraphFamily` registers the stable family id that scopes cutscene
  node catalogs, blackboard descriptors, and authoring helpers.
- `CutsceneGraphWindow` still owns the cutscene-specific window composition,
  inspector, presentation, and play-mode trace visualization, but now composes
  shared GraphCore canvas and blackboard surfaces.
- `CutsceneGraphBlackboardView` and node inspector value drawers now share the
  same GraphCore blackboard or value-source registry and consistent direct
  versus blackboard authoring behavior.
- The graph window persists the bound `CutsceneDirector` across recompiles.
- Graph nodes ship with stable category palettes, header icons, and one
  visual-only comment node for authoring-only annotations.
- The built-in node catalog now includes `Conversations/Start Conversation`,
  which plays one authored `ConversationReference` and resumes the cutscene
  after the conversation completes or is canceled.
- Optional integration nodes stay hidden from the creation menu when their
  required package or module is unavailable, while existing serialized nodes
  remain intact and surface validation warnings.
- Existing authored graphs remain supported through cutscene compatibility
  normalization and the migration utilities that rebuild authored Cutscenes
  shapes from shared GraphCore data when needed.
- Package samples include both scene-authored graphs and installer-backed
  setups depending on the sample workflow.

## GraphCore Architecture

Cutscenes no longer owns one isolated graph stack.

- `CutsceneGraph` derives from `GraphDefinition`.
- `CutsceneNodeBase` derives from `GraphNodeBase`.
- `CutsceneGraphBlackboard` derives from `GraphBlackboard`.
- `CutsceneValueSource` derives from `GraphValueSource`.
- `CutsceneGraphCoreRuntimeMigrationUtility` bridges shared GraphCore shapes
  and cutscene-authored compatibility shapes when graphs need to move between
  the reusable substrate and cutscene-owned authoring containers.
- GraphCore owns the family registry, shared blackboard value wrappers,
  variable references, shared editor drawers, graph canvas shell, and
  blackboard overlay shell.
- Cutscenes still owns the scene host, run lifecycle, node catalog,
  validation semantics, graph-window composition, play-mode visualization,
  and optional Dialogue System bridge.

This split is the current package reference for graph-backed modules: keep the
shared substrate in GraphCore and keep family-specific behavior inside the
consumer.

## Runtime Surface

- `CutsceneDirector`
- `CutsceneGraph`
- `CutsceneGraphFamily`
- `CutsceneGraphBlackboard`
- `CutsceneGraphBlackboardEntry`
- `CutsceneValueSource`
- `CutsceneRun`
- `CutsceneExecutionContext`
- `CutsceneRuntimeStateStore`
- `CutsceneRunTrace`
- `CutsceneTrigger`
- `CutsceneTriggerMode`
- `CutsceneNodeMenuAttribute`
- `CutsceneBusEventAttribute`
- `CutsceneBusEventRegistry`
- `CutsceneGraphCoreRuntimeMigrationUtility`
- `ICutsceneService`
- built-in flow, action, signal, trigger, Timeline, Conversations, and
  Dialogue nodes

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

### `CutsceneNodeMenuAttribute`

- Registers one graph-creation menu path and optional default title for one
  cutscene node type.
- Supports availability flags such as `requiresDialogueSystem` and
  `requiresConversationsModule` for optional integration nodes.
- Use it to keep dependency-bound nodes out of the creation UI until their
  backing module or package is actually available.
- Do not treat it as the only runtime guard. Nodes that depend on optional
  systems should still fail with one explicit diagnostic when execution starts
  without that dependency.

## Editor Surface

- `CutscenesModuleConfigurationPanel`
- `CutsceneDirectorInspector`
- `CutsceneGraphWindow`
- `CutsceneGraphView`
- `CutsceneGraphInspectorView`
- `CutsceneGraphBlackboardView`
- `CutsceneNodeCreationRegistry`
- `CutsceneNodePresentationRegistry`
- `CutsceneBlackboardValueSourceDrawers`
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

`CutsceneGraph` still owns one cutscene-authored blackboard that stores
graph-level values shared across node executions, but the storage and
authoring model now ride on the shared GraphCore blackboard containers,
descriptors, variable references, and value-source system.

- Author entries in the blackboard overlay on the graph window sidebar.
- The picker exposes the GraphCore built-in wrappers available to the cutscene
  family, including numeric types, strings, booleans, vector and transform
  structs, colors, rects, bounds, curves, gradients, layer masks, enums, and
  typed `UnityEngine.Object` references.
- The graph window overlay and node inspectors use the same shared GraphCore
  drawer logic, so direct values and blackboard-backed values behave
  consistently across UI Toolkit and IMGUI surfaces.
- Read and write values through `CutsceneRun.Blackboard`,
  `CutsceneExecutionContext.Blackboard`, or the convenience helpers
  `TryGetBlackboardValue`, `SetBlackboardValue`,
  `GetOrCreateBlackboardValue`, and `RemoveBlackboardValue`.
- Every node that is actually executing receives the same graph blackboard
  through `CutsceneExecutionContext`, so `OnEnter`, `Tick`, and `OnExit` can
  all use `context.Blackboard` without resolving separate graph state.
- Existing pre-GraphCore graphs remain valid; Cutscenes normalizes or migrates
  legacy authored wrappers as graphs are loaded, reconstructed, or serialized.

Built-in blackboard authoring nodes now ship in the base module and use the
shared GraphCore `GraphValueSource` model:

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
- Optionally gate node discovery with availability flags such as
  `[CutsceneNodeMenu("Folder/Node Name", requiresConversationsModule: true)]`
  or `requiresDialogueSystem: true` when the node depends on one optional
  module or third-party package.
- Keep the node type in a runtime assembly that is loaded by Unity.
- If the project uses asmdefs, reference `IndieGabo.HandyTools.Cutscenes`.
- Add a direct reference to `IndieGabo.HandyTools.GraphCore` when the node
  source file directly declares GraphCore types such as `GraphValueSource`,
  `GraphBlackboardValue`, or `GraphBlackboardVariableReference`.
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

If the project node only inherits `CutsceneNodeBase`, that single reference is
enough. If the project node directly uses GraphCore symbols in serialized
fields or helper types, add `IndieGabo.HandyTools.GraphCore` explicitly.

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
2. If the feature is graph-family-neutral and reusable across graph-backed
   modules, implement it in GraphCore instead of directly in Cutscenes.
3. When the feature is cutscene-specific and package-owned, put the runtime
   node in the owning category folder under
   `Runtime/Scripts/Cutscenes/Nodes/Actions`, `Flow`, `Signals`, or `Meta`.
4. Keep scene-owned authoring data on `CutsceneDirector` and keep run state in
   `CutsceneRun`, `CutsceneExecutionContext`, or node runtime state storage.
   Do not push transient runtime state back into serialized node definitions.
5. Rely on the default inspector and type discovery first. A new built-in node
   does not require editor registration when serialized fields and
   `CutsceneNodeMenu` metadata are enough.
6. Touch editor code only when the feature needs one new validation rule,
   custom drag-and-drop behavior, custom graph presentation, or a dedicated
   authoring affordance.
7. Keep GraphCore editor changes graph-family-neutral. Cutscene window
   composition, presentation, validation, and module-specific UX still belong
   in `IndieGabo.HandyTools.Cutscenes.Editor`.
8. If the node introduces new mandatory outputs, scene references, or binding
   rules, extend `CutsceneGraphValidator` so authoring failures are visible
   before play mode.
9. If the node introduces one new category or needs different visual language,
   update `CutsceneNodePresentationRegistry` so the graph and creation search
   stay visually coherent.
10. Keep optional Dialogue System behavior isolated. Serialized dialogue node
    data may live in the base cutscenes runtime, but typed Pixel Crushers code
    must remain in `IndieGabo.HandyTools.Cutscenes.DialogueSystem` and
    `IndieGabo.HandyTools.Cutscenes.DialogueSystem.Editor` behind the
    synchronized define.
11. Add or update EditMode smoke coverage under `Assets/Tests/CutscenesEditMode`
    when the feature changes execution flow, validation, or graph serialization.
12. Update the module guide and sample README files when the new feature
    changes the recommended authoring workflow or becomes part of the public
    built-in node catalog.

Ownership map for common expansion points:

- GraphCore runtime and editor: shared graph containers, family registries,
  reusable blackboard or value-source infrastructure, and reusable authoring
  shells.
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

## Optional Conversations Integration

- `CutsceneConversationReferenceNode` lives in the base cutscenes runtime
  assembly and serializes one `ConversationReference`.
- The node appears in the graph as `Conversations/Start Conversation`.
- Runtime playback is owned directly by the cutscene node through one authored
  playback controller, so the graph does not need one intermediary
  `ConversationTrigger` component.
- Conversation progression remains manual: the node waits for presenter-driven
  or input-driven advance, skip, or cancel requests and does not auto-advance
  the active conversation on its own.
- The node resolves successfully when the conversation session reaches
  `Completed` or `Canceled`, and fails explicitly when Conversations cannot
  start playback.
- Node creation stays hidden from the graph window when the `Conversations`
  module is inactive, while existing serialized nodes remain intact and warn
  through validation.
- Enable `Conversations` in `Handy Tools/Modules` before authoring or running
  cutscenes that depend on this node.

## Current Limitations

- `CutsceneParallelNode` fans out into all connected branches and waits for all
  spawned branches to complete before resolving the owning fork, but it does
  not yet expose configurable join policies or partial-failure recovery.
- The current graph window focuses on one director at a time and does not yet
  provide richer authoring helpers such as dedicated conversation pickers.

## Samples

- `Cutscenes Base Sample`
- `Cutscenes Conversations Example`
- `Cutscenes Dialogue System Sample`

Open `Samples/Cutscenes Base Sample/Scenes/CutscenesBaseSample.unity` and
press Play to run the non-dialogue sample. The base sample starts from one
scene-authored `CutsceneTrigger`, includes one native writer node that writes
the initial payload, and uses blackboard-bound consumers later in the same
flow.

Open `Samples/Cutscenes Dialogue System Sample/Scenes/CutscenesDialogueSystemSample.unity`
and press Play in a project where Dialogue System is installed to run the
optional integration sample.

Open `Samples/Cutscenes Conversations Example/Scenes/CutscenesConversationsExample.unity`
and press Play in a project where both `Cutscenes` and `Conversations` are
active to run the cross-module sample. The sample starts one authored
cutscene automatically and enters `Conversations/Start Conversation` twice.
Advance, skip, or cancel each conversation manually through the presenter or
the authored input actions to verify the cutscene resumes only after the
conversation reaches one terminal state. The sample table registers three
actors and intentionally leaves portrait sprites unassigned for project-
specific art.
