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
- The main Utils asmdef must remain free of module-owned or optional-package
  dependencies.

## Runtime Assembly Strategy

The root runtime asmdef provides kernel-level types and common code that can be
compiled without optional packages. Feature slices that need package-specific
references or isolated ownership compile into their own runtime asmdefs.

Examples:

- `IndieGabo.HandyTools.SaveSystem` owns Easy Save references.
- `IndieGabo.HandyTools.Input` owns Input System references.
- `IndieGabo.HandyTools.Rendering` owns URP-specific references.
- `IndieGabo.HandyTools.Web` owns web request helpers without polluting the
  root runtime asmdef.

## Editor Assembly Strategy

The root editor asmdef provides shared editor helpers and menu path constants.
Feature-specific editor integrations compile into module editor asmdefs.

Examples:

- `IndieGabo.HandyTools.Input.Editor`
- `IndieGabo.HandyTools.SaveSystem.Editor`
- `IndieGabo.HandyTools.Debugging.Editor`
- `IndieGabo.HandyTools.Modules.Editor` for the shared modules window that
  depends on multiple module editor panels

## Choosing the Correct Home for Code

Use this rule of thumb before adding a new file:

| Code shape | Recommended home |
| --- | --- |
| Mandatory startup infrastructure | Kernel under `Runtime/Scripts/Core`, `EventBus`, or `ServiceLocator` |
| Feature with explicit activation, load order, or runtime bootstrap | Module-specific runtime asmdef |
| Feature with package-specific editor UI | Matching module editor asmdef |
| Pure helper with no bootstrap and no optional package ownership | `Utils` or owner module support folder |
| Helper tied to one module but not to module startup | Module-owned support code, not global Utils |

## Dependency Ownership Rules

When adding a package reference, ask these questions in order:

1. Does the feature belong to a specific module?
2. Does the feature need startup activation or dependency gating?
3. Does the package dependency apply to the whole package or only to one slice?

If the answer is "one slice", do not add the reference to the root asmdef.

## Current Ownership Examples

- `Utils/Crypto` owns static AES and string encoding helpers, so Save System
  can consume them without creating a separate Crypto module.
- `GlobalConfig/JsonTree` is internal support code for Globals rather than a
  standalone module.
- `Rendering/Extensions/Light2DExtensions` lives outside Utils so URP
  references do not leak into the main utility asmdef.
- Input-owned helpers such as `UI.cs` and `InputActionMapField.cs` were moved
  out of Utils because they rely on the Input slice.

## Guidance for AI Agents

- Never solve a local compile problem by adding an optional package reference to
  the root runtime asmdef.
- Prefer moving code to the owning module over broadening a shared asmdef.
- Keep namespace and ownership aligned with the slice that compiles the file.
- When introducing a new cross-module dependency, document why it cannot be
  modeled as support code or a dependency status entry instead.

Continue with [Utils and Reclassifications](04-utils-and-reclassifications.md).