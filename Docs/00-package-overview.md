# Package Overview

HandyTools is a Unity package organized around a mandatory kernel and a set of
optional modules. The kernel provides startup orchestration, event dispatch,
service registration, and module discovery. Optional modules own their own
runtime dependencies and, when implemented, their shared module panels,
standalone authoring tools, and configuration assets.

The markdown files in `Assets/HandyTools/Docs` are the canonical package
documentation. HandyTools no longer ships a separate generated documentation
site, so this folder is the source of truth for package behavior and workflows.

This document explains the package shape at a high level. For lifecycle and
boot details, continue with [Kernel and Boot Flow](01-kernel-and-boot-flow.md).

## Intended Audience

This documentation is written for both human developers and AI agents.

- Human readers should use it to understand package setup, configuration, and
  integration boundaries.
- AI agents should use it to identify code ownership, activation rules,
  dependency boundaries, and safe extension points before editing the package.

## Package Model

HandyTools is split into four broad layers:

1. Kernel infrastructure: startup orchestration, module metadata, event bus,
   and service locator.
2. Configurable modules: optional modules that usually ship shared module
   panels or dedicated authoring tools.
3. Auto-activated modules: optional modules that default to active but do not
   require editor configuration panels.
4. Utility and support code: static helpers and module-owned support slices
   that should not bootstrap themselves.

## Mandatory Infrastructure

These slices are mandatory and are not treated as user-togglable modules:

- HandyBus under `Runtime/Scripts/EventBus`
- ServiceLocator under `Runtime/Scripts/ServiceLocator`
- Kernel module contracts under `Runtime/Scripts/Core/Modules`
- Runtime bootstrap coordinator under `Runtime/Scripts/Core`

The kernel prepares and boots optional modules only after this infrastructure is
ready.

## Breaking Namespace Convention

All module-owned namespaces now end with `Module`.

- Use `IndieGabo.HandyTools.HandyBusModule` for HandyBus APIs.
- Use `IndieGabo.HandyTools.HandyServiceLocatorModule` for service locator APIs.
- Use feature namespaces such as `IndieGabo.HandyTools.GameplayModule`,
  `IndieGabo.HandyTools.PoolingModule`, and
  `IndieGabo.HandyTools.AnimationEventsModule` in new runtime code.

Folder paths and assembly names may still omit the suffix when they describe
package layout rather than C# namespaces. When in doubt, follow the owning
slice's current `rootNamespace` and runtime examples.

## Module Catalog

| Slice            | Kind                  | Default State    | Editor Surface                | Main Runtime Entry                                                                 |
| ---------------- | --------------------- | ---------------- | ----------------------------- | ---------------------------------------------------------------------------------- |
| Logging          | Configurable module   | Off              | Shared modules window         | `LoggingModuleDefinition`, `LoggerBootstrapper`                                    |
| Input            | Configurable module   | Off              | Shared modules window         | `InputModuleDefinition`, `ProjectInputConfig.Bootstrap()`                          |
| Gameplay         | Configurable module   | Off              | Shared modules window         | `GameplayModuleDefinition`, `GameplayServiceBootstrapper`, `GameplayConfig`        |
| Cutscenes        | Configurable module   | Off              | Shared modules window         | `CutscenesModuleDefinition`, `CutsceneDirector`, `ICutsceneService`                |
| Conversations    | Configurable module   | Off              | Modules window + graph window | `ConversationsModuleDefinition`, `ConversationTable`, `ConversationGraphFamily`    |
| Scenes           | Utility/support slice | Always available | SceneAsset inspector          | `HandySceneReference`, `SceneExtender`, `HandySceneRuntimeReader`                  |
| Command Pattern  | Auto-activated module | On when unset    | Monitor window                | `CommandPatternModuleDefinition`, `ICommandService`, `CommandPatternMonitorWindow` |
| FSM              | Auto-activated module | On when unset    | No panel                      | `FSMModuleDefinition`, `FSMBrain`, `State`, `ScriptableState`                      |
| Save System      | Configurable module   | Off              | Shared modules window         | `SaveSystemModuleDefinition`, `SaveSystemBootstrapper`                             |
| Globals          | Configurable module   | Off              | Shared modules window         | `GlobalConfigModuleDefinition`, `Globals.LoadFromGlobals()`                        |
| Debugging        | Configurable module   | Off              | Shared modules window         | `DebuggingModuleDefinition`, `DebugPanelBootstrapper`                              |
| Steam            | Configurable module   | Off              | Shared modules window         | `SteamModuleDefinition`, `SteamModuleBootstrapper`                                 |
| ScreenShooter    | Configurable module   | Off              | Shared modules window         | `ScreenShooterModuleDefinition`, `ScreenShooterModuleBootstrapper`                 |
| Animation Events | Auto-activated module | On when unset    | No panel                      | `AnimationEventsModuleDefinition`, `AnimationEventsModuleBootstrapper`             |
| Web              | Auto-activated module | On when unset    | No panel                      | `WebModuleDefinition`                                                              |
| Pooling          | Auto-activated module | On when unset    | No panel                      | `PoolingModuleDefinition`                                                          |
| Identifying      | Auto-activated module | On when unset    | No panel                      | `IdentifyingModuleDefinition`                                                      |
| Rendering        | Auto-activated module | On when unset    | No panel                      | `RenderingModuleDefinition`                                                        |
| GraphCore        | Utility/support slice | Always available | Reusable graph shell          | `GraphDefinition`, `GraphBlackboard`, `GraphFamilyRegistry`, `GraphValueSource`    |
| Utils            | Utility/support slice | Always available | No panel                      | Static helpers only                                                                |

GraphCore was extracted from the original Cutscenes graph stack and now owns
the reusable graph model, family registry, blackboard and value-source
infrastructure, shared validation contracts, canvas shell, and shared editor
drawers consumed by graph-backed modules.

Cutscenes remains the richest scene-authored proving vertical on top of that
layer, while Conversations now demonstrates the lighter asset-host path with
the same family-scoped infrastructure. That slice now also covers authored
`ConversationReference` selection, one table `Display Name` used by editor
pickers, and a first action-node family (`Wait`, `Emit HandyBus Event`,
`Wait For Event`, and authored-runtime `Play Timeline`).

Scenes is another always-available support slice rather than one activated
module. It provides scene-authored metadata through one hidden in-scene
carrier, section-by-section activation directly from the scene asset
inspector, one marker-only importer payload, editor snapshot fallback for
unloaded scenes, and one build-only runtime catalog for player fallback reads.

## Documentation Reading Map

- Read [Kernel and Boot Flow](01-kernel-and-boot-flow.md) to understand the
  startup order and module discovery rules.
- Read [Service Locator Guide](07-service-locator-guide.md) to understand the
  default-versus-identified registration model and the intended lookup APIs.
- Read [HandyBus Guide](08-handybus-guide.md) to understand event
  subscription tokens, dispatch semantics, and event authoring rules.
- Read [Pooling Guide](09-pooling-guide.md) to understand pool definitions,
  independent runtimes, and identifier-based pool lookup.
- Read [Command Pattern Guide](16-command-pattern-guide.md) to understand the
  command runtime service, queue policies, undo and redo flow, scheduling, the
  play-mode monitor window, and the new five-minute quickstart for authoring
  and dispatching commands without reverse engineering the sample or tests.
- Read [Scenes Guide](18-scenes-guide.md) to understand HandyScene carriers,
  section-by-section scene-asset inspector authoring, typed runtime lookup,
  unloaded-scene
  snapshots, build-only runtime catalogs, and the shipped CLI validation path.
- Read [Gameplay Guide](13-gameplay-guide.md) to understand lifecycle state,
  indefinite interruptions, and gameplay time persistence strategy.
- Read [FSM Guide](15-fsm-guide.md) to understand the integrated state machine
  module, its editor surfaces, and its optional integrations.
- Read [Planned Modules](../../../Docs/planned-modules.md) to review candidate
  and planned module investigations before turning an idea into a new slice.
- Read [Editor Setup and Configuration](02-editor-setup-and-configuration.md)
  to understand menus, setup automation, and asset locations.
- Read [Assembly Layout and Dependency Rules](03-assembly-layout-and-dependency-rules.md)
  before adding references or moving code between asmdefs.
- Read [Module Authoring Guide](06-module-authoring-guide.md) before creating
  new slices or extracting shared support layers such as GraphCore.
- Read [Utils and Reclassifications](04-utils-and-reclassifications.md) before
  deciding whether a new feature belongs in a module or in support code.
- Read [Configurable Modules](10-configurable-modules.md) and
  [Auto-Activated Modules](11-auto-activated-modules.md) when touching a
  specific feature slice.
- Read [Cutscenes Module](12-cutscenes-module.md) for the richest current
  GraphCore-backed runtime, editor workflow, optional Dialogue System
  integration, and sample coverage.
- Read [Conversations Module](19-conversations-module.md) for the current
  asset-hosted graph workflow, runtime export and loading path, build staging,
  built-player proof sample, and current scope limits.
- Read [ConversationTable Window And Presenter Prefabs](20-conversation-table-window-and-presenter-prefabs.md)
  when working inside the dedicated Conversations window or building custom
  presenter prefabs.
- Read [Animation Events Guide](14-animation-events-guide.md) when working on
  state-authored event dispatch, typed animator payloads, or Animation Window
  inspector tooling.

## Source Tree Orientation

The most important top-level package paths are:

- `Assets/HandyTools/Runtime`: runtime code and runtime asmdefs.
- `Assets/HandyTools/Editor`: editor code, editor asmdefs, setup tools, and
  module panels.
- `Assets/HandyTools/Runtime/Scripts/GraphCore`: shared graph runtime
  primitives consumed by graph-backed modules.
- `Assets/HandyTools/Editor/Scripts/GraphCore`: shared graph editor shells,
  drawers, and registries consumed by graph-backed modules.
- `Assets/HandyTools/Runtime/Scripts/Scenes`: HandyScene runtime contracts,
  carriers, references, readers, and generated catalog shapes.
- `Assets/HandyTools/Editor/Scripts/Scenes`: HandyScene editor authoring,
  inspector integration, build catalog staging, and CLI validation.
- `Assets/HandyTools/Docs`: package-local documentation intended to stay close
  to the code.
- `Assets/Resources`: project-level generated or edited assets used by module
  configs and module activation.

## Guidance for AI Agents

- Treat the package as ownership-first. Identify the owning module or support
  slice before changing code.
- Do not add optional package references to the root runtime or root editor
  asmdefs.
- If a feature needs bootstrap logic, explicit activation, or dependencies, it
  probably belongs to a module. If it is a static helper with no setup, it
  probably belongs to a utility or module-owned support slice.
- `Scenes` currently belongs in that support-slice category. Do not invent one
  `ModuleDefinition` for HandyScene unless the architecture deliberately
  changes.
- For graph-backed work, decide first whether the change belongs in GraphCore
  or in one consumer family such as Cutscenes or Conversations.
- If a change alters module activation, boot order, asset paths, menus, or
  ownership rules, update the matching documentation file in the same change.

Continue with [Kernel and Boot Flow](01-kernel-and-boot-flow.md).
