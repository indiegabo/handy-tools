# FSM Guide

This guide explains how the former HandyFSM package now fits inside HandyTools
as the auto-activated `FSM` module.

## What Changed

- FSM now lives under `Assets/HandyTools/Runtime/Scripts/FSM` and
  `Assets/HandyTools/Editor/Scripts/FSM`.
- The module defaults to active so existing FSMBrains continue to work after
  the integration.
- The module no longer appears in `Handy Tools/Modules`; explicit activation
  overrides go through `Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset`.
- Disabling the module keeps scripts and serialized references in place, but
  `FSMBrain` does not initialize at runtime until the module is reactivated.

## Delegated Brain Surface

New code should prefer the delegated domains exposed by `FSMBrain`.

- `brain.Machine` owns lifecycle and transition requests.
- `brain.States` owns state loading and lookup.
- `brain.Input` owns cached input values and recent-button consumption.
- `brain.Blackboard` owns optional Simple Blackboard access.
- `brain.CCPro` owns optional Character Controller Pro runtime services.
- `brain.Triggers` remains the trigger provider surface.

Example:

```csharp
brain.Machine.RequestStateChange<JumpState>();
brain.States.Get<JumpState>();
brain.Input.TryGetVector2(moveAction, out Vector2 movement);
brain.Blackboard.TryGetValue("Speed", out float speed);
brain.CCPro.ResetIKWeights();
```

The legacy root-level methods still exist as compatibility forwarders during
the migration, but they are now deprecated and should not be used by new code.

## Optional Integrations

- Simple Blackboard support stays reflection-based inside `FSMBrain` and
  becomes available when the runtime types are present.
- Character Controller Pro support stays optional. The editor setup now
  synchronizes `HANDY_CHARACTER_CONTROLLER_PRO_PRESENT` automatically when
  `CharacterActor` and `CharacterBody` are installed, and the typed CCPro
  state bases compile inside `IndieGabo.HandyTools.FSM.CCPro`.

## Editor Surfaces

- The FSM state visualizer is available at `Handy Tools/FSM/State Visualizer`.
- `FSMBrain` continues to use its dedicated custom inspector, including the
  `Third Party` section that reports Simple Blackboard and Character Controller
  Pro availability directly on the brain.
- The `FSM CCPro Starter Kit` is distributed through the package samples tab.

## Reading Order

1. Read [Auto-Activated Modules](11-auto-activated-modules.md#fsm) for the package-level activation rules.
2. Read [FSM Module Index](FSMModule/README.md) for the detailed workflow documentation.
3. Read [Operating The Module](FSMModule/00-Operating-The-Module.md) for activation, integrations, and sample import flow.
4. Read [Character Controller Pro Integration](FSMModule/08-Character-Controller-Pro.md) when working on CCPro-backed states.
