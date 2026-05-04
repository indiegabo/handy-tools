# Utils and Reclassifications

HandyTools no longer treats every shared helper as a standalone module
candidate. This document explains what remains in Utils, what was moved into
module-owned slices, and which support systems now live inside feature folders.

## What Belongs in Utils Now

The main Utils assembly is reserved for helpers that meet all of these goals:

- no runtime bootstrap is required,
- no dedicated editor panel is required,
- no optional package dependency should leak into the main Utils asmdef,
- and the code is either kernel-safe or generic support code.

Typical examples include extension methods, validation helpers, threading and
retry helpers, clipboard and IO helpers, and small shared data types.

## Important Reclassifications

### Crypto moved into Utils

`HandyAESEncryption` and `StringEncoder` now live under `Utils/Crypto` in the
`IndieGabo.HandyTools.Utils.Crypto` namespace. They are pure static helpers and
do not justify a separate module or a dedicated asmdef.

### Time helpers moved into Utils

The former Time Management slice was reduced to utility helpers such as
`TimeScalerAsync` and `TimeScalerRoutines`. These remain shared helpers rather
than module candidates.

### JsonTree moved into GlobalConfig

`JsonTree` is now owned by `GlobalConfig/JsonTree` because Globals is the only
remaining consumer. It is implementation support for Globals, not a separate
feature surface.

### Identifying was split by responsibility

The scene GUID system remains an auto-activated module under
`Runtime/Scripts/Identifying`, while `Identifier`, `HashedKey`, and
`IdentifiableScriptableObject` live under `Utils/Identifying` as generic
support code.

### Rendering was extracted from Utils

URP-specific light helpers were moved into `Runtime/Scripts/Rendering` so the
main Utils asmdef no longer needs URP references.

## Removed or Deprecated Code

These removals matter because they describe current ownership policy:

- `VolumeExtensions` was removed because it no longer exposed a meaningful API.
- The old Strapi-specific wrappers under Web were removed because they had no
  remaining consumers and no functional request implementation.

## Ownership Decision Rule

When deciding whether code belongs in Utils or somewhere else, use this order:

1. If the feature needs boot, activation, dependency gating, or a configuration
   panel, it should be a module.
2. If the feature is only meaningful inside one module, keep it in that module
   as support code.
3. If the feature is package-wide, static, and free of optional package
   ownership, it can remain in Utils.

## Guidance for AI Agents

- Do not grow Utils into a second miscellaneous package root.
- If a helper depends on Input System, URP, or another feature-specific package,
  keep it in the owning module or a dedicated support slice.
- If a feature has no panel and creates no bootstrap-time runtime objects,
  prefer utility or auto-activated ownership over a new configurable module.
- Document reclassifications when moving code between Utils and module-owned
  folders, because those moves change architectural intent.

Continue with [AI Agent Playbook](05-ai-agent-playbook.md).