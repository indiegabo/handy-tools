# Auto-Activated Modules

This document covers the optional HandyTools modules that default to active when
no explicit override exists in `HandyModuleSettings`. These modules do not own
dedicated editor panels.

## How Activation Works

These modules are still optional. They simply use
`HandyModuleDescriptor.IsActiveByDefault = true`, which means they resolve as
active until a project stores an explicit state override.

Because they do not appear in the shared modules window, projects that need to
override their activation should do so through
`Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset`.

## Summary Table

| Module           | Id                 | Default       | Load Order | Bootstrap Body   |
| ---------------- | ------------------ | ------------- | ---------- | ---------------- |
| Web              | `web`              | On when unset | `170`      | Empty            |
| FSM              | `fsm`              | On when unset | `180`      | Empty            |
| Pooling          | `pooling`          | On when unset | `180`      | Empty            |
| Animation Events | `animation-events` | On when unset | `190`      | Registry refresh |
| Identifying      | `identifying`      | On when unset | `190`      | Empty            |
| Rendering        | `rendering`        | On when unset | `195`      | Empty            |

## Web

### Activation Profile

- Activation mode: Optional
- Active by default: Yes
- Load order: `170`
- Declared dependencies: none

### Responsibilities

Web provides static request and response helpers for package-level HTTP-style
integrations.

### Runtime Entry Points

- `Runtime/Scripts/Web/WebModuleDefinition.cs`
- `Runtime/Scripts/Web/WebModuleBootstrapper.cs`
- `Runtime/Scripts/Web/WebRequest.cs`
- `Runtime/Scripts/Web/WebResponse.cs`
- `Runtime/Scripts/Web/WebRequestPerformer.cs`

The bootstrapper body is intentionally empty because the slice exposes helper
types rather than a runtime service object.

### Notes for AI Agents

- Keep Web as an auto-activated helper slice unless it gains real user-facing
  configuration or runtime boot responsibilities.
- The old Strapi-specific wrappers were removed. Do not reintroduce dead API
  surfaces without real consumers.

## FSM

### Activation Profile

- Activation mode: Optional
- Active by default: Yes
- Load order: `180`
- Declared dependencies: none

### Responsibilities

FSM provides finite state machine brains, runtime and ScriptableObject-authored
states, transition evaluation, named triggers, play-mode state history, and a
dedicated state visualizer window.

### Runtime Entry Points

- `Runtime/Scripts/FSM/FSMModuleDefinition.cs`
- `Runtime/Scripts/FSM/FSMModuleBootstrapper.cs`
- `Runtime/Scripts/FSM/Core/FSMBrain.cs`
- `Runtime/Scripts/FSM/Core/State.cs`
- `Runtime/Scripts/FSM/Core/ScriptableState.cs`
- `Runtime/Scripts/FSM/Core/Triggers/*`
- `Runtime/Scripts/FSM/ThirdParty/CCPro/*`

The bootstrapper body is intentionally empty because the slice exposes runtime
types and editor workflows rather than spawning a global boot-time service.
Projects that need to disable FSM can store an explicit `fsm` override in
`HandyModuleSettings.asset`; when disabled, `FSMBrain` stays serialized but does
not initialize at runtime until the override is removed or re-enabled.

`FSMBrain` uses its custom inspector automatically. The `Third Party` section on
the brain reports Simple Blackboard and Character Controller Pro availability
directly, and the visualizer window remains available at
`Handy Tools/FSM/State Visualizer`.

### Notes for AI Agents

- The former standalone HandyFSM package now lives under `Runtime/Scripts/FSM`
  and `Editor/Scripts/FSM`.
- Optional Simple Blackboard and Character Controller Pro support resolve by
  reflection in `FSMBrain`, while the typed CCPro state bases compile only when
  `HANDY_CHARACTER_CONTROLLER_PRO_PRESENT` is synchronized by the editor setup
  tooling.
- Keep additional optional integrations out of `IndieGabo.HandyTools.FSM`.
  Package-specific code should live in a dedicated child asmdef such as
  `IndieGabo.HandyTools.FSM.CCPro`.

## Pooling

### Activation Profile

- Activation mode: Optional
- Active by default: Yes
- Load order: `180`
- Declared dependencies: none

### Responsibilities

Pooling provides definition-driven object pooling, independent runtime owners,
and optional identifier-based pool registry lookup.

### Runtime Entry Points

- `Runtime/Scripts/Pooling/PoolingModuleDefinition.cs`
- `Runtime/Scripts/Pooling/PoolingModuleBootstrapper.cs`
- `Runtime/Scripts/Pooling/PoolIdentifier.cs`
- `Runtime/Scripts/Pooling/PoolRegistry.cs`
- `Runtime/Scripts/Pooling/HandyPool.cs`
- `Runtime/Scripts/Pooling/HandyPoolRuntime.cs`
- `Runtime/Scripts/Pooling/IHandyPool.cs`
- `Runtime/Scripts/Pooling/IPoolSubject.cs`
- `Runtime/Scripts/Pooling/HandyPoolInitializer.cs`

The bootstrapper resets `PoolRegistry` for a clean runtime session. Actual pool
creation is still driven by scene initializers and direct consumers rather than
by a global spawned singleton.

### Notes for AI Agents

- Do not add fake bootstrap-time runtime objects just to justify the module.
- Preserve the split between asset definitions and runtime owners.
- Preserve the hardened pool lifecycle: clear pools on dismiss, destroy tracked
  created subjects, and avoid unnecessary prewarm allocations.
- Use `PoolIdentifier` and `PoolRegistry` only when the caller truly benefits
  from identifier-based lookup instead of prefab references.

## Animation Events

### Activation Profile

- Activation mode: Optional
- Active by default: Yes
- Load order: `190`
- Declared dependencies: none

### Responsibilities

Animation Events provides state-authored animation event dispatch for two use
cases: local string callbacks routed to `AnimationEventReceiver`, and typed
HandyBus dispatch routed through attributed `AnimatorBusEventBase` payloads.

### Runtime Entry Points

- `Runtime/Scripts/AnimationEvents/AnimationEventsModuleDefinition.cs`
- `Runtime/Scripts/AnimationEvents/AnimationEventsModuleBootstrapper.cs`
- `Runtime/Scripts/AnimationEvents/AnimationEventStateBehaviour.cs`
- `Runtime/Scripts/AnimationEvents/AnimationEventBusStateBehaviour.cs`
- `Runtime/Scripts/AnimationEvents/AnimationEventReceiver.cs`
- `Runtime/Scripts/AnimationEvents/AnimatorBusEventRegistry.cs`

The bootstrapper refreshes the attributed event registry so typed animation
events can resolve without scene-local setup.

### Notes for AI Agents

- Preserve the one-behaviour-per-event authoring model. The trigger-time and
  Animation Window workflow depend on that granularity.
- Keep editor tooling in UI Toolkit unless Unity exposes no viable UI Toolkit
  surface for a required Animator-host integration.
- If new event metadata is added, update both the runtime registry and the
  typed-event authoring documentation.

## Identifying

### Activation Profile

- Activation mode: Optional
- Active by default: Yes
- Load order: `190`
- Declared dependencies: none

### Responsibilities

Identifying provides the scene-object GUID system used for GUID-backed
references.

### Runtime Entry Points

- `Runtime/Scripts/Identifying/IdentifyingModuleDefinition.cs`
- `Runtime/Scripts/Identifying/IdentifyingModuleBootstrapper.cs`
- `Runtime/Scripts/Identifying/SceneGuids/GuidComponent.cs`
- `Runtime/Scripts/Identifying/SceneGuids/GuidManager.cs`
- `Runtime/Scripts/Identifying/SceneGuids/GuidReference.cs`

The bootstrapper is empty because the module exposes types and workflows rather
than creating a global runtime manager object at startup.

### Notes for AI Agents

- The GUID runtime now lives under `Identifying/SceneGuids` and the runtime
  namespace `IndieGabo.HandyTools.Identifying.SceneGuids`.
- This rename is intentionally breaking for source compatibility. Update any
  consumer `using` directives that referenced the old namespace.
- Keep the split between scene GUID runtime code and `Utils/Identifying`
  support code intact.
- Editor drawers under `Identifying.Editor` serve both the GUID system and the
  utility identifier types.

## Rendering

### Activation Profile

- Activation mode: Optional
- Active by default: Yes
- Load order: `195`
- Declared dependencies: none

### Responsibilities

Rendering owns rendering-specific helpers such as URP 2D light transitions.

### Runtime Entry Points

- `Runtime/Scripts/Rendering/RenderingModuleDefinition.cs`
- `Runtime/Scripts/Rendering/RenderingModuleBootstrapper.cs`
- `Runtime/Scripts/Rendering/Extensions/Light2DExtensions.cs`

The bootstrapper is empty because the slice currently exposes extension helpers
only.

### Notes for AI Agents

- Keep URP references inside the Rendering asmdef so the main Utils asmdef stays
  package-clean.
- If the slice ever gains real configuration, document the change and move it
  into the configurable modules surface.
