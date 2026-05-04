# AI Agent Playbook

This document is written for agents that need to modify HandyTools without
breaking ownership boundaries, startup order, or module activation rules.

## Core Working Model

Treat HandyTools as a kernel plus owned slices. Do not start from folder names
alone. Start from the behavior you need to change, then identify:

1. the owning asmdef,
2. the owning module or support slice,
3. the boot and activation path,
4. the editor surface and config asset path,
5. and the smallest validation scope that can prove the change.

## Required Pre-Edit Checklist

Before editing a feature slice, verify these facts:

- Which asmdef compiles the touched file?
- Is the slice mandatory kernel infrastructure, a configurable module, an
  auto-activated module, or support code?
- Does the slice own a `ModuleDefinition`, a bootstrapper, and a panel?
- Does the slice use `Resources`, a project-root file, or generated assets?
- Does the slice depend on optional package references or scripting defines?

## Safe Editing Heuristics

- Preserve the root asmdef rules. Optional packages must not creep back into
  `IndieGabo.HandyTools.Runtime` or `IndieGabo.HandyTools.Editor`.
- If a problem is local to one module, fix it inside that module before looking
  for broader architectural changes.
- If a helper lives in Utils but clearly depends on one module, move it to the
  owning slice instead of broadening Utils.
- If a module has no configuration panel and no startup object creation, assume
  it should stay auto-activated or utility-owned unless the code proves
  otherwise.

## Common Traps

- Auto-activated modules still use `HandyModuleSettings`. They are not hardcoded
  mandatory infrastructure.
- Embedded `InputAction` fields must be edited through serialized-property
  workflows, not as standalone object references.
- Steam dependency state is target-dependent. Do not assume it is available on
  unsupported platforms.
- Resources paths are part of the public package contract. Silent path changes
  break discovery.
- Reflection-based module discovery can fail silently if a bootstrapper compiles
  into the wrong assembly or never loads into the current AppDomain.

## Validation Habits

- Prefer validating the touched asmdef, module, or file group instead of the
  whole package.
- After editing moved files, re-read the resulting file directly. Auto-corrected
  patches can leave malformed namespaces or headers even when diagnostics lag.
- When changing documentation or menus, validate the links or menu path strings
  directly instead of assuming they stayed aligned.

## Documentation Rule

If your change affects any of the following, update the corresponding document
in `Assets/HandyTools/Docs` in the same change:

- module activation behavior,
- load order,
- config asset paths,
- asmdef ownership,
- editor menu paths,
- or feature reclassification.

## When in Doubt

If ownership is ambiguous, compare three options in this order:

1. owning module support code,
2. auto-activated optional module,
3. shared utility code.

Avoid creating a new configurable module unless the feature clearly needs a
user-managed activation state and a dedicated configuration surface.

Continue with [Module Authoring Guide](06-module-authoring-guide.md).