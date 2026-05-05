# Editor Setup and Configuration

HandyTools exposes a small set of editor entry points for setup, module
configuration, and scripting define management. This document explains how
those entry points fit together and where configuration data is stored.

## Main Editor Menus

| Menu path                                    | Purpose                                                                |
| -------------------------------------------- | ---------------------------------------------------------------------- |
| `HandyTools/Complete Setup`                  | Runs the one-time project setup flow                                   |
| `HandyTools/Modules/Configuration`           | Opens the shared modules configuration window                          |
| `HandyTools/Modules/<Module Name>`           | Opens the shared modules window preselected on one configurable module |
| `HandyTools/Configuration/Scripting Defines` | Opens the managed scripting define window                              |

## One-Time Setup Flow

The setup flow is implemented in `Editor/Scripts/Setup/Setuper.cs`.

Important behaviors:

- The setup class is marked with `InitializeOnLoad`, so it participates in the
  editor startup lifecycle.
- It uses a project-root anchor file to avoid rerunning the initial setup on
  every load.
- It removes unavailable scripting defines before applying setup defaults.
- It runs the Input module starter setup so the default project-side input
  assets are available under `Assets/_Project/Input` when the project wants
  the package-provided starter stack.
- It runs the Steam module starter setup so `steam_appid.txt` exists in the
  project root.

## Shared Modules Window

Configurable modules are surfaced through a single editor window:

- `Editor/Scripts/Modules/HandyToolsModulesWindow.cs`

That window registers each configurable panel explicitly and routes the module
submenu entries back into the same shared host. The current configurable set is:

- Input
- Gameplay
- Save System
- Debugging
- Logging
- Globals
- Steam
- ScreenShooter

Each configurable module can also expose a `Starter Setup` action inside its
panel. Starter setups create the project-side assets or files that a module
needs before its runtime bootstrap becomes useful.

Auto-activated modules do not appear in this window because they do not own a
dedicated configuration surface.

## Configuration Data Locations

The package uses a combination of `Resources` assets and project files.

| Owner             | Data location                                                   | Notes                                              |
| ----------------- | --------------------------------------------------------------- | -------------------------------------------------- |
| Module activation | `Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset` | Stores explicit activation overrides               |
| Input             | `Assets/Resources/HandyTools/ProjectInputConfig.asset`          | Player manager prefab and player count             |
| Save System       | `Assets/Resources/SaveSystem/SaveSystemConfig.asset`            | Slot strategy, auto boot, encryption options       |
| Logging           | `Assets/Resources/Logging/HandyLoggerSetup.asset`               | Runtime/editor log colors                          |
| Debugging         | `Assets/Resources/Debugging/DebugPanel.asset`                   | Debug panel enable state and embedded input action |
| ScreenShooter     | `Assets/Resources/ScreenShooter/ScreenShooterConfig.asset`      | Embedded input action and output path              |
| Globals           | `Assets/Resources/globals`                                      | JSON file edited through the Globals panel         |
| Steam             | `steam_appid.txt` at project root                               | Editor panel surfaces status and hardcoded app id  |

## Starter Setup Surface

- Input starter setup is optional. The Input panel can create
  `Assets/Resources/HandyTools/ProjectInputConfig.asset` on demand for manual
  configuration, while Starter Setup imports the module-owned default Player
  Manager stack under `Assets/_Project/Input`, assigns the imported prefab, and
  resets the player count to `1`.
- Globals starter setup creates `Assets/Resources/globals.json` when the
  project does not provide it yet.
- Steam starter setup writes `steam_appid.txt` to the project root when the
  file is missing.
- Modules that do not yet own project-side starter assets show the Starter
  Setup section as unavailable.

## Embedded InputAction Editing

Some module configs store `InputAction` values directly inside the asset rather
than referencing a separate object. That affects editor implementation.

Current embedded-action configs include:

- `DebugPanelConfig`
- `ScreenShooterConfig`

When editing those values in editor code, use `SerializedObject` and
`PropertyField`-style flows rather than trying to treat them like standalone
object references.

## Manual Control for Auto-Activated Modules

Web, Pooling, Identifying, and Rendering do not have dedicated module panels.
They still remain optional modules, so their explicit activation override can be
stored in `HandyModuleSettings.asset` if a project needs to disable one of them.

## Guidance for AI Agents

- If a module owns configuration, document both the editor panel entry point and
  the underlying asset path.
- If a module has no panel, state clearly how its activation is controlled.
- Do not add new standalone editor windows when a new module can live inside
  `HandyToolsModulesWindow`.
- Treat `Resources` paths as public package contracts. Moving them is a
  breaking change.

Continue with [Assembly Layout and Dependency Rules](03-assembly-layout-and-dependency-rules.md).
