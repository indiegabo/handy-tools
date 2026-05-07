# Intended Features

This document records feature directions that are intentionally not
implemented yet, but already fit the current HandyTools architecture.

## Material Reaction Dispatch for CCPro Environment Materials

Status: Intended feature.

Primary module of interest: `FSM CCPro` (`IndieGabo.HandyTools.FSM.CCPro`).

Why this module is the right home:

- `CurrentSurfaceInfo` and `CurrentVolumeInfo` already belong to the CCPro
  environment integration surface.
- `ReactionKey` is authored on `CCProMaterialSettings`, which is already a
  CCPro-focused asset.
- The feature only makes sense when the optional Character Controller Pro
  integration is present.

Why this should not live in the base `FSM` module:

- the base FSM runtime should remain agnostic about CCPro-specific surface and
  volume semantics.
- the feature depends on CCPro environment material resolution, not on the
  general machine lifecycle.

Why this is only a secondary concern for the `Gameplay` module:

- `Gameplay` may consume the reactions, but it should not own the source of
  truth for CCPro material detection.
- any project-level response policy should sit above the CCPro resolver,
  not replace it.

Intended scope:

1. Add a reaction component that consumes `CurrentSurfaceInfo` and
   `CurrentVolumeInfo`.
2. Resolve environment changes by `ReactionKey`.
3. Dispatch or play concrete feedback payloads such as particles, audio,
   camera shake requests, and response curves.
4. Support separate surface and volume reaction flows when that distinction is
   useful.

Suggested runtime shape:

- `CCProMaterialReactionDispatcher` lives in
  `IndieGabo.HandyTools.FSM.CCPro`.
- It reads an `ICCProEnvironmentModifierSource` from the actor branch.
- It compares current and previous resolved material info.
- It dispatches reactions when the resolved `ReactionKey` changes or when the
  active surface or volume enters or exits.

Suggested asset expansion:

- extend `CCProMaterialSettings` with optional payload blocks per default,
  surface, and volume entry.
- payloads may include particle references, audio references, camera response
  data, and authored curves.
- payload references should stay optional so locomotion-only projects keep the
  current lightweight setup.

Dependency boundary:

- the core CCPro runtime slice should not hard-code project-specific audio,
  camera, or VFX implementations.
- when a payload depends on another HandyTools module or an external package,
  bridge it through adapters, events, or optional sub-assets.

Validation target when this is implemented:

- entering a tagged surface changes locomotion modifiers and triggers the
  matching reaction once per transition.
- entering and leaving a tagged volume updates the active volume reaction
  without desynchronizing locomotion state.
- projects with no configured reaction payloads still behave exactly like the
  current locomotion-only setup.