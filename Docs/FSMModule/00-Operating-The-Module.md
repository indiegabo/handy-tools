# Operating The Module

This document explains how the FSM module is activated, how its optional
integrations become available, and how to import its package samples.

## Activation

The FSM module is an auto-activated optional module. It does not appear in the
shared HandyTools modules window.

Use this flow:

1. Open `Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset`.
2. Add or edit the `fsm` entry only when the project needs an explicit
   activation override.
3. Leave the module unset to keep the default active behavior, or store an
   inactive override only when every `FSMBrain` in the project must stay
   dormant.

The module defaults to active so former HandyFSM workflows continue to run after
the integration into HandyTools.

If the module is disabled, existing `FSMBrain` components and state assets stay
serialized in scenes and assets, but `FSMBrain` stops initializing at runtime
until the module is activated again.

## Runtime And Editor Surfaces

When the module is active, the main supported surfaces are:

- `FSMBrain` for machine orchestration
- `State` and `ScriptableState` for runtime and asset-authored states
- `TriggersProvider` for lightweight named trigger dispatch
- the dedicated `FSMBrain` custom inspector
- `Handy Tools/FSM/State Visualizer` for recorded state-path inspection

## Optional Integrations

### Simple Blackboard

Simple Blackboard support is resolved dynamically inside `FSMBrain`. No extra
module activation step is required.

When the runtime types are present in the project:

- the `Third Party` section exposes the Simple Blackboard toggle and fields
- the Blackboard fields become available on `FSMBrain`
- blackboard helper APIs become valid at runtime
- the integration documentation in
  [07 - Blackboard with Simple Blackboard](07-Blackboard-with-Simple-Blackboard.md)
  applies directly

### Character Controller Pro

Character Controller Pro support remains optional.

When `Lightbug.CharacterControllerPro.Core.CharacterActor` and
`Lightbug.CharacterControllerPro.Core.CharacterBody` are present in the
project:

- HandyTools editor setup synchronizes
  `HANDY_CHARACTER_CONTROLLER_PRO_PRESENT`
- `IndieGabo.HandyTools.FSM.CCPro` becomes compilable
- `FSMBrain` keeps `Animator` in the base brain fields and exposes the CCPro
  toggle-owned fields in its `Third Party` section
- typed bases such as `CCProState` and `ScriptableCCProState` become available
- optional environment modifiers come from branch components such as
  `CCProEnvironmentSource`, not from a dedicated brain field

If CCPro is not installed, the core FSM module still works. Only the CCPro
child assembly and CCPro-specific workflows stay unavailable.

## Samples

The FSM module currently ships the `FSM CCPro Starter Kit` sample.

This sample is distributed as a HandyTools package sample and is intended for
projects that also install Character Controller Pro.

To import it:

1. Open Unity Package Manager.
2. Select `Handy Tools`.
3. Open the `Samples` tab.
4. Import `FSM CCPro Starter Kit`.

The sample contains a CCPro-driven scene, state assets, stats assets, and input
bindings that demonstrate how the integrated FSM module and the optional CCPro
support work together.

If the sample is imported before CCPro is available, the sample assembly stays
gated by `HANDY_CHARACTER_CONTROLLER_PRO_PRESENT` and the CCPro-specific sample
scripts do not compile until the dependency is installed.

Inside the development repository, the sample content lives under
`Samples/FSM CCPro Starter Kit` so Unity can keep the assets visible and
editable during package development, and `package.json` keeps the sample path
under `Samples/`.

This matches the Unity package authoring model: the source package uses a
`Samples` folder, and the exported package payload renames that folder to
`Samples~` so it stays hidden inside the installed package cache. Our release
workflow mirrors that export behavior before publishing to the public package
repository.

## Ownership Map

The FSM module currently owns these package paths:

- `Runtime/Scripts/FSM`
- `Editor/Scripts/FSM`
- `Editor/Resources/UI Toolkit/FSM`
- `Docs/FSMModule`
- `Samples/FSM CCPro Starter Kit` in the development repository
- `package.json` sample path `Samples/FSM CCPro Starter Kit`
- `Samples~/FSM CCPro Starter Kit` in the published package payload

Treat those paths as the canonical surface when extending or documenting the
module.
