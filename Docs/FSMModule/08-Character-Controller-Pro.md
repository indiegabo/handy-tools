# Character Controller Pro

HandyFSM supports Character Controller Pro as an optional capability directly on `FSMBrain`.

There is no separate CCPro brain anymore. The integration now lives in the base `FSMBrain` and is activated by configuration when the CCPro package is installed.

The preserved CCPro runtime contract is intentionally narrow:

- `CharacterActor` remains the simulation, grounding, collision, root motion,
  and callback authority
- `CharacterBody` remains the body-shape and dimensional data source

The HandyFSM integration does not depend on CCPro Demo components or on the
built-in CCPro orchestration layer.

## What You Need

To use the integration:

1. the Character Controller Pro package must be installed in the project
2. `FSMBrain` must have the integration enabled in the inspector
3. `Character Actor` must be assigned on the actor branch
4. if the state needs environment multipliers, the branch must also include a
   component that implements `ICCProEnvironmentModifierSource`, such as
   `CCProEnvironmentSource`

## How to Enable It in the Inspector

With the package installed:

1. select the `FSMBrain`
2. assign `Animator` in the main brain fields
3. open `Third Party`
4. enable `Use Character Controller Pro?`
5. click `Setup CCPro FSM` if you want the brain branch to auto-add the
   HandyTools-owned support components
6. assign or confirm:
   - `Character Actor`
   - `Movement Reference`

`Setup CCPro FSM` is intentionally idempotent. It adds missing supporting
components such as `FSMPlayerInputSource`, `FSMStatsRegistry`, and
`CCProEnvironmentSource`, but it does not duplicate components that already
exist on the branch.

If your states use surface or volume multipliers, add `CCProEnvironmentSource`
or another `ICCProEnvironmentModifierSource` implementation somewhere on the
same actor branch. `FSMBrain` does not serialize this component directly.

If you want `InputMovementReference`, configure semantic movement input on the bound input source.

With `FSMPlayerInputSource`, assign `Movement Input Action` on that component.

That dedicated movement action is also republished into the regular `FSMBrain`
input cache, so it does not need to be duplicated in the source list of other
actions.

If `Movement Reference` is set to `External`, inject the runtime transform manually through the `FSMBrain` API:

```csharp
brain.CCPro.ExternalReference = cameraTransform;
```

## What Each Field Means

### `Animator`

Animator used by CCPro-aware states when they need to update parameters or IK.

### `Character Actor`

The main reference for locomotion, physical data, simulation callbacks, and actor properties.

### `Movement Reference`

The mode used to calculate the movement axis perceived by the state.

Current options:

- `World`
- `External`
- `Character`

### `External Reference`

Runtime transform used when the selected movement reference mode is external.

This value is no longer assigned in the `FSMBrain` inspector.
Set it manually from composition code through `FSMBrain.CCPro.ExternalReference`.

### `CCProEnvironmentSource` or Another `ICCProEnvironmentModifierSource`

Optional branch component that supplies surface and volume speed,
acceleration, deceleration, and gravity multipliers to CCPro-aware states.

This dependency is resolved lazily by `CCProState` and
`ScriptableCCProState`. If no source is found, those bases fall back to
neutral modifiers.

`CCProEnvironmentSource` can also consume a `CCProMaterialSettings` asset.
That asset is the owned material catalog for the branch: it stores default
surface and volume values, tagged overrides, and a semantic `ReactionKey`
per entry so gameplay, audio, VFX, or camera systems can react to materials
without coupling themselves to raw Unity tags.

When a material settings asset is assigned and auto-sync is enabled, the
source reads `CharacterActor.GroundObject`, `CurrentTrigger`, and `Triggers`
every fixed step and resolves the current surface and volume automatically.
Without an asset, the component still works as a manual runtime override
container.

## What `FSMBrain` Exposes When CCPro Is Active

On the brain, you gain access to:

- `UseCharacterControllerPro`
- `CCPro.Animator`
- `CCPro.Actor`
- `CCPro.InputMovementReference`
- `CCPro.MovementReferenceForward`
- `CCPro.MovementReferenceRight`
- `CCPro.ExternalReference`
- `CCPro.MovementReferenceMode`
- `CCPro.UseRootMotion`
- `CCPro.UpdateRootPosition`
- `CCPro.UpdateRootRotation`
- `CCPro.ResetIKWeights()`

## How the Loop Changes When CCPro Is Enabled

When the integration is active:

- the brain concentrates the relevant flow inside `FixedUpdate`
- it also listens to `CharacterActor` callbacks for pre-simulation, post-simulation, and IK
- states may implement extra hooks in addition to the normal FSM lifecycle hooks

## CCPro-Specific Base Types

Use:

- `CCProState` for class-based states
- `ScriptableCCProState` for asset-based states

These bases already expose typed shortcuts:

- `Animator`
- `CharacterActor`
- `EnvironmentModifierSource`
- `CurrentSurfaceModifiers`
- `CurrentVolumeModifiers`
- `InputMovementReference`
- `MovementReferenceForward`
- `MovementReferenceRight`

`CurrentSurfaceModifiers` and `CurrentVolumeModifiers` are safe even when no
environment source is present because they default to neutral multipliers.

If another branch system needs richer material context than plain multipliers,
read `CCProEnvironmentSource.CurrentSurfaceInfo` and
`CCProEnvironmentSource.CurrentVolumeInfo`. Those runtime snapshots expose the
resolved tag and `ReactionKey` alongside the modifier data.

## Extra Hooks Recognized by the CCPro Bases

In addition to `OnInit`, `OnEnter`, `OnExit`, `OnTick`, `OnFixedTick`, and `OnLateTick`, the CCPro bases recognize:

- `OnPreCharacterSimulation(float dt)`
- `OnPostCharacterSimulation(float dt)`
- `OnPreFixedTick()`
- `OnPostFixedTick()`
- `OnTickIK(int layerIndex)`

Those hooks are excellent when you want to integrate the FSM with the CCPro simulation cycle without resorting to hacks.

## Example of `ScriptableCCProState`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using IndieGabo.HandyTools.FSMModule.CCPro;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    [CreateAssetMenu(
        fileName = "CCProLocomotionState",
        menuName = "HandyTools/FSM/Examples/CCPro Locomotion State")]
    public sealed class CCProLocomotionState : ScriptableCCProState
    {
        [SerializeField]
        private InputActionReference _moveAction;

        private void OnEnter()
        {
            UseRootMotion(false);
        }

        private void OnFixedTick()
        {
            Vector2 moveInput = Brain.Input.TryGetVector2(_moveAction, out Vector2 value)
                ? value
                : Vector2.zero;

            Vector3 moveReference = MovementReferenceRight * moveInput.x;
            CharacterActor.PlanarVelocity = moveReference * 4f;
        }

        private void UseRootMotion(bool value)
        {
            CCPro.UseRootMotion = value;
        }
    }
}
```

## Example with `CCProState`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using IndieGabo.HandyTools.FSMModule.CCPro;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public abstract class PlayerCCProState : CCProState
    {
    }

    public sealed class PlayerCCProIdleState : PlayerCCProState
    {
        [SerializeField]
        private InputActionReference _moveAction;

        private void OnFixedTick()
        {
            Vector2 moveInput = Brain.Input.TryGetVector2(_moveAction, out Vector2 value)
                ? value
                : Vector2.zero;

            Vector3 moveReference = MovementReferenceRight * moveInput.x
                + MovementReferenceForward * moveInput.y;

            CharacterActor.PlanarVelocity = moveReference;
        }
    }
}
```

## Validation Performed by the CCPro Bases

`CCProState` and `ScriptableCCProState` automatically validate whether:

- a valid `FSMBrain` exists
- the CCPro usage flag is enabled
- `CharacterActor` was assigned

If something is wrong, they raise `StateFailureException` and the brain enters its recovery path.

## About Movement Reference

`InputMovementReference` represents player input already projected into the selected reference space.
It is built from semantic movement input reported by the bound `FSMInputSource`.

When the source is `FSMPlayerInputSource`, that semantic input comes from the source-local `Movement Input Action` field.

The most important helper vectors are:

- `MovementReferenceForward`
- `MovementReferenceRight`

Use those when the state needs to decide movement relative to the camera, the world, or the character itself.

## Practical Usage Advice

If the state depends heavily on CCPro features, use the CCPro-specific base classes from the beginning. Do not scatter manual casts and component assumptions across every class. That only creates organizational ruin.

For new gameplay states, prefer `InputActionReference` fields plus the
`Brain.Input` domain (`TryGetButton`, `TryGetVector2`, `TryGetSnapshot`)
instead of introducing parallel input abstractions.

## Common Errors

### "The CCPro section does not appear"

The CCPro package was probably not resolved in the current project.

### "The section appears, but the state fails during initialization"

Checklist:

- is `Use Character Controller Pro?` enabled?
- was `Character Actor` assigned?
- did you actually derive from `CCProState` or `ScriptableCCProState`?

### "My transitions stopped happening where I expected them to"

When CCPro is active, the relevant flow is tied to `FixedUpdate` and simulation callbacks. Organize the machine like a physics-driven FSM, not like a purely `Update`-driven FSM.
