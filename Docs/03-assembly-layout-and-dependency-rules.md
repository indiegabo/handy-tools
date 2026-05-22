# Assembly Layout and Dependency Rules

HandyTools uses asmdef boundaries as an architectural contract, not just as a
compile-time optimization. This document explains the dependency rules that keep
the kernel isolated and optional packages owned by the correct slices.

## Non-Negotiable Rules

These rules define the package shape:

- `IndieGabo.HandyTools.Runtime` must not reference optional external packages.
- `IndieGabo.HandyTools.Editor` must reference only
  `IndieGabo.HandyTools.Runtime`.
- Optional runtime features must own their package dependencies in
  module-specific asmdefs.
- Optional editor features must own their package dependencies in matching
  module editor asmdefs.
- Module-owned namespaces must end with `Module`, and subnamespaces should stay
  rooted from that module namespace.
- The main Utils asmdef must remain free of module-owned or optional-package
  dependencies.
- Shared support asmdefs such as GraphCore may be referenced by multiple
  modules, but they must stay consumer-neutral and must not absorb
  module-specific semantics.

## Runtime Assembly Strategy

The root runtime asmdef provides kernel-level types and common code that can be
compiled without optional packages. Feature slices that need package-specific
references or isolated ownership compile into their own runtime asmdefs.

Examples:

- `IndieGabo.HandyTools.GraphCore` owns graph definitions, family registries,
  blackboard and value-source wrappers, and shared execution or validation
  contracts used by graph-backed modules.
- `IndieGabo.HandyTools.SaveSystem` owns Easy Save references.
- `IndieGabo.HandyTools.Input` owns Input System references.
- `IndieGabo.HandyTools.FSM` owns the state machine runtime, while
  `IndieGabo.HandyTools.FSM.CCPro` isolates Character Controller Pro-specific
  state bases behind a dependency-backed child asmdef.
- `IndieGabo.HandyTools.Cutscenes` owns scene-authored cutscene orchestration,
  runtime execution, and cutscene semantics on top of GraphCore, while
  `IndieGabo.HandyTools.Cutscenes.DialogueSystem` isolates typed Dialogue
  System references behind the `HANDY_DIALOGUE_SYSTEM_PRESENT` define.
- `IndieGabo.HandyTools.Conversations` owns asset-authored conversation graphs
  on top of GraphCore.
- `IndieGabo.HandyTools.CommandPattern` owns command orchestration,
  scheduling, undo and redo history, and runtime diagnostics without polluting
  the root runtime asmdef.
- `IndieGabo.HandyTools.Rendering` owns URP-specific references.
- `IndieGabo.HandyTools.Web` owns web request helpers without polluting the
  root runtime asmdef.

Those asmdef names are assembly identities, not necessarily the public C#
namespace roots. The current runtime namespaces are `SaveSystemModule`,
`HandyInputSystemModule`, `RenderingModule`, `WebModule`, and other `*Module`
variants.

## Editor Assembly Strategy

The root editor asmdef provides shared editor helpers and menu path constants.
Feature-specific editor integrations compile into module editor asmdefs.

Examples:

- `IndieGabo.HandyTools.GraphCore.Editor`
- `IndieGabo.HandyTools.Cutscenes.Editor`
- `IndieGabo.HandyTools.Cutscenes.DialogueSystem.Editor`
- `IndieGabo.HandyTools.Conversations.Editor`
- `IndieGabo.HandyTools.CommandPattern.Editor`
- `IndieGabo.HandyTools.FSM.Editor`
- `IndieGabo.HandyTools.Input.Editor`
- `IndieGabo.HandyTools.SaveSystem.Editor`
- `IndieGabo.HandyTools.Debugging.Editor`
- `IndieGabo.HandyTools.Modules.Editor` for the shared modules window that
  depends on multiple module editor panels

The Command Pattern slice follows the same split: runtime orchestration and
journal APIs compile into `IndieGabo.HandyTools.CommandPattern`, while the
play-mode monitor window and its UI Toolkit support code compile into
`IndieGabo.HandyTools.CommandPattern.Editor`.

GraphCore.Editor is the reusable shell for graph canvases, blackboard
overlays, shared value-source drawers, drag-session helpers, and family-scoped
node catalog registries. Consumer editor asmdefs still own host-window
composition, inspector layout, validation semantics, and module-specific
presentation.

Cutscenes now follows a three-layer split directly: graph-neutral authoring
primitives compile into `IndieGabo.HandyTools.GraphCore.Editor`, cutscene
window composition and validation compile into
`IndieGabo.HandyTools.Cutscenes.Editor`, and Dialogue System-specific
authoring helpers compile into
`IndieGabo.HandyTools.Cutscenes.DialogueSystem.Editor` behind the synchronized
`HANDY_DIALOGUE_SYSTEM_PRESENT` define.

Conversations follows the lighter version of the same pattern: shared graph
canvas and blackboard behavior live in `IndieGabo.HandyTools.GraphCore.Editor`,
while the asset-hosted Conversations window and node views stay in
`IndieGabo.HandyTools.Conversations.Editor`.

## Choosing the Correct Home for Code

Use this rule of thumb before adding a new file:

| Code shape                                                         | Recommended home                                                     |
| ------------------------------------------------------------------ | -------------------------------------------------------------------- |
| Mandatory startup infrastructure                                   | Kernel under `Runtime/Scripts/Core`, `EventBus`, or `ServiceLocator` |
| Feature with explicit activation, load order, or runtime bootstrap | Module-specific runtime asmdef                                       |
| Feature with package-specific editor UI                            | Matching module editor asmdef                                        |
| Shared runtime or editor substrate reused by multiple modules      | Dedicated support asmdef such as GraphCore                           |
| Pure helper with no bootstrap and no optional package ownership    | `Utils` or owner module support folder                               |
| Helper tied to one module but not to module startup                | Module-owned support code, not global Utils                          |

## Dependency Ownership Rules

When adding a package reference, ask these questions in order:

1. Does the feature belong to a specific module?
2. Does the feature need startup activation or dependency gating?
3. Does the package dependency apply to the whole package or only to one slice?

If the answer is "one slice", do not add the reference to the root asmdef.

Transitive asmdef references are not enough when a source file directly names
symbols from another assembly. If code directly declares `GraphNodeBase`,
`GraphValueSource`, `GraphBlackboardValue`, `GraphCanvasView`, or other
GraphCore types, add an explicit reference to
`IndieGabo.HandyTools.GraphCore` or `IndieGabo.HandyTools.GraphCore.Editor`
instead of relying on a consumer asmdef such as Cutscenes or Conversations.

## Current Ownership Examples

- `Utils/Crypto` owns static AES and string encoding helpers, so Save System
  can consume them without creating a separate Crypto module.
- `GlobalConfig/JsonTree` is internal support code for Globals rather than a
  standalone module.
- `GraphCore` owns graph-neutral runtime containers, registries, shared value
  wrappers, and reusable editor shells; Cutscenes and Conversations own their
  host objects, family ids, node catalogs, validation, and presentation.
- `Rendering/Extensions/Light2DExtensions` lives outside Utils so URP
  references do not leak into the main utility asmdef.
- Input-owned helpers such as `UI.cs` and `InputActionMapField.cs` were moved
  out of Utils because they rely on the Input slice.

## Guidance for AI Agents

- Never solve a local compile problem by adding an optional package reference to
  the root runtime asmdef.
- Prefer moving code to the owning module over broadening a shared asmdef.
- When a graph change is reusable across multiple families, prefer GraphCore;
  when it changes host semantics, node catalogs, or module validation, keep it
  in the consumer module.
- Keep namespace and ownership aligned with the slice that compiles the file.
- When introducing a new cross-module dependency, document why it cannot be
  modeled as support code or a dependency status entry instead.

Continue with [Utils and Reclassifications](04-utils-and-reclassifications.md).
