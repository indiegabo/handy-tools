# Module Authoring Guide

This document explains how to extend HandyTools with new feature slices and
shared support layers while preserving the package architecture.

## Choose the Correct Artifact Type First

Before creating files, decide what you are adding.

| Feature shape                                               | Recommended implementation                 |
| ----------------------------------------------------------- | ------------------------------------------ |
| Needs explicit activation, load order, or dependency gating | Configurable module                        |
| Needs activation semantics but no dedicated editor surface  | Auto-activated optional module             |
| Needs one reusable substrate shared by multiple modules     | Shared support asmdef                      |
| Needs neither boot nor configuration                        | Support code in the owning module or Utils |

## Creating a Configurable Module

Use this pattern when a feature needs runtime activation and one user-facing
authoring surface.

1. Create a runtime asmdef for the feature slice.
2. Add a `ModuleDefinition` with a stable id, display name, description,
   activation mode, and load order.
3. Add a runtime bootstrapper that implements `IHandyModuleBootstrapper`.
4. Add any runtime config assets or `HandyGlobalConfig` assets the module needs.
5. Create a matching editor asmdef.
6. Create a `HandyModuleConfigurationPanelBase` implementation when the module
   needs shared-window configuration, or create a dedicated authoring tool when
   the workflow is asset- or host-centric.
7. Register the panel in `Editor/Scripts/Modules/HandyToolsModulesWindow.cs`
   only when the module participates in the shared modules window.
8. Update the documentation menu and module reference docs.

## Creating an Auto-Activated Optional Module

Use this pattern when a feature should remain optional but should default to
active and does not need a shared module panel.

1. Create a runtime asmdef for the feature slice.
2. Add a `ModuleDefinition` with `HandyModuleActivationMode.Optional` and
   `isActiveByDefault: true`.
3. Add a bootstrapper if the slice still needs the module contract, even when
   the bootstrap body is intentionally empty.
4. Keep the feature out of the shared modules window unless it gains real
   editor configuration.
5. Standalone diagnostic or monitor windows are still acceptable when they do
   not become configuration surfaces.
6. Document how projects can override activation through
   `HandyModuleSettings.asset`.

## Creating Support Code Instead of a Module

If the feature does not need boot, dependency gating, or user-facing activation,
do not force it into the module system.

Preferred homes:

- owner module support folder for module-specific helpers,
- `Utils` for package-wide static helpers that stay free of optional package
  ownership,
- or an internal support folder such as `GlobalConfig/JsonTree`.

## Extracting a Shared Support Slice

Use this pattern when one proving module uncovers runtime or editor mechanics
that another module also needs.

1. Identify the consumer-neutral primitives first.
2. Move shared runtime containers, registries, or contracts into a dedicated
   support asmdef instead of cloning them into the next module.
3. Move shared editor shells and drawers into a matching editor support asmdef.
4. Keep host objects, graph family ids, node catalogs, validation semantics,
   and module-specific presentation inside each consumer module.
5. Register family-specific wrappers and catalogs through shared registries
   rather than hard-coding consumer knowledge into the support slice.
6. Keep optional overlays and helper surfaces opt-in at the host-window level.

GraphCore is the canonical package example. It was extracted from the original
Cutscenes graph stack once the package needed the same graph model, blackboard,
value-source system, and editor shell in more than one place. Cutscenes stayed
the richer scene-authored proving vertical, while Conversations now reuses the
same substrate for a lighter asset-hosted graph workflow.

When a migration has to move multiple asmdefs at once, plan one coherent slice,
write the shared layer and the proving consumer in one wave, then run focused
validation and cleanup. Do not mix unrelated refactors into that boundary move.

## Recording Unapproved Candidates

Do not turn a feature idea into a new module only because it sounds useful.

- Record module ideas in `../../../Docs/planned-modules.md` using the existing
  candidate-module and research-notes format.
- Treat that document as a research backlog, not as approval.
- Promote a candidate only after a focused study shows that the feature needs
  package-level ownership, has recurring real workloads, and is stronger than
  keeping the code inside an existing module or inside `Utils`.

## Editor Integration Rules

- Reuse `HandyToolsModulesWindow` for configurable modules.
- Reuse `HandyModuleConfigurationPanelBase` and the dependency gate element.
- Do not create a new standalone configuration window when a shared panel is
  enough.
- A dedicated authoring window is acceptable when the module has little or no
  module-wide configuration and the real workflow is asset- or host-driven.
  Conversations is the current example.
- If an optional third-party integration needs dependency-neutral runtime
  contracts or serialized data, keep those types in the base module asmdef and
  isolate only the typed bridge code in a define-constrained child asmdef.
- Mirror that split on the editor side when authoring helpers also depend on
  the optional package.

## Documentation Checklist

Whenever you add a new slice, update all relevant documents:

- `README.md` documentation menu
- `00-package-overview.md`
- either `10-configurable-modules.md` or `11-auto-activated-modules.md`
- `03-assembly-layout-and-dependency-rules.md` if asmdef boundaries changed
- `04-utils-and-reclassifications.md` if ownership changed
- every affected consumer guide when a shared support slice changes its public
  workflow or ownership rules

## Guidance for AI Agents

- Prefer the smallest architecture change that satisfies the feature.
- Keep `HandyModuleDescriptor.Id` stable once published.
- Preserve deterministic load order semantics. Do not assign random ordering
  values without explaining the relationship to neighboring modules.
- If a new module needs optional package references, isolate them inside the
  new module asmdef instead of widening an existing shared asmdef.
- If a second consumer would force copy-paste of the same runtime or editor
  substrate, stop and evaluate whether the feature really belongs in a shared
  support asmdef first.

Continue with [Configurable Modules](10-configurable-modules.md).
