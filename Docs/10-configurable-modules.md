# Configurable Modules

This document covers the HandyTools modules that appear in the shared modules
window at `Handy Tools/Modules`. These modules are optional and
default to inactive unless a project explicitly activates them.

The `Modules` menu no longer exposes one submenu item per configurable module.
Open the shared window once and use the left sidebar to select the module you
want to configure.

## Summary Table

| Module        | Id               | Default | Load Order | Dedicated Panel |
| ------------- | ---------------- | ------- | ---------- | --------------- |
| Logging       | `logging`        | Off     | `-1000`    | Yes             |
| Input         | `input`          | Off     | `30`       | Yes             |
| Gameplay      | `gameplay`       | Off     | `40`       | Yes             |
| Save System   | `save-system`    | Off     | `100`      | Yes             |
| Globals       | `global-config`  | Off     | `130`      | Yes             |
| Steam         | `steam`          | Off     | `150`      | Yes             |
| ScreenShooter | `screen-shooter` | Off     | `160`      | Yes             |
| Debugging     | `debugging`      | Off     | `500`      | Yes             |

## Logging

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `-1000`
- Declared dependencies: none

### Responsibilities

Logging provides colored diagnostic logging for runtime and editor workflows.
It exists early in the boot sequence so other active modules can emit readable
logs during startup.

### Runtime Entry Points

- `Runtime/Scripts/Logging/LoggingModuleDefinition.cs`
- `Runtime/Scripts/Logging/LoggerBootstrapper.cs`
- `Runtime/Scripts/Logging/HandyLogger.cs`
- `Runtime/Scripts/Logging/HandyLoggerSetup.cs`

The logger setup asset is created through `HandyGlobalConfig` and resolves to
`Assets/Resources/Logging/HandyLoggerSetup.asset`.

### Editor Workflow

Use `Handy Tools/Modules` and select `Logging` in the sidebar to change module
activation and default colors.

### Notes for AI Agents

- Logging is optional. Do not make kernel or Utils code require it.
- `LoggerBootstrapper` is meaningful only in the editor or when `HANDY_DEBUG`
  style debug compilation conditions are enabled.

## Input

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `30`
- Declared dependencies: none

### Responsibilities

Input owns project-level player input bootstrapping, multiplayer input
configuration, rebinding helpers, and input feedback support types.

### Runtime Entry Points

- `Runtime/Scripts/Input/InputModuleDefinition.cs`
- `Runtime/Scripts/Input/InputModuleBootstrapper.cs`
- `Runtime/Scripts/Input/ProjectInputConfig.cs`
- `Runtime/Scripts/Input/PlayerManager.cs`
- `Runtime/Scripts/Input/Bindings/*`

`ProjectInputConfig.Bootstrap()` loads the existing
`Assets/Resources/HandyTools/ProjectInputConfig.asset`, validates the configured
`PlayerManager` prefab, avoids spawning duplicate managers, and marks the
instantiated runtime manager as `DontDestroyOnLoad`.

### Editor Workflow

Use `Handy Tools/Modules` and select `Input` to configure the player manager prefab and the
maximum player count. The panel also exposes a `Starter Setup` button that
creates `ProjectInputConfig` automatically when the project does not have it
yet, so manual configuration is always available. `Starter Setup` remains an
optional shortcut that imports the module-owned Input starter package into
`Assets/_Project/Input`, assigns the default `PlayerManager` prefab, and resets
the player count to `1`.
Input-owned support types such as rebinders and feedback containers also live
in the Input slice.

### Notes for AI Agents

- Input-owned helpers belong in Input, not in Utils.
- If you touch embedded input-related editor fields, use serialized-property
  flows rather than object-reference assumptions.

## Gameplay

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `40`
- Declared dependencies: none

### Responsibilities

Gameplay provides the global gameplay lifecycle service and gameplay session
time tracking. It owns the runtime object that coordinates gameplay state,
publishes lifecycle events, and persists tracked gameplay time through either
local user data or Save System-backed slot data.

### Runtime Entry Points

- `Runtime/Scripts/Gameplay/GameplayModuleDefinition.cs`
- `Runtime/Scripts/Gameplay/GameplayModuleBootstrapper.cs`
- `Runtime/Scripts/Gameplay/GameplayServiceBootstrapper.cs`
- `Runtime/Scripts/Gameplay/GameplayService.cs`
- `Runtime/Scripts/Gameplay/GameplayEvent.cs`
- `Runtime/Scripts/Gameplay/GameplayConfig.cs`
- `Runtime/Scripts/Gameplay/GameplayLocalUserData.cs`
- `Runtime/Scripts/Gameplay/GameplayTimeRegisterer.cs`
- `Runtime/Scripts/Gameplay/GameplayTimeScaler.cs`

The gameplay service bootstrapper creates the runtime service object and
registers it in the global service locator. `GameplayConfig` resolves to
`Assets/Resources/Gameplay/GameplayConfig.asset` and controls the gameplay time
persistence strategy.

### Editor Workflow

Use `Handy Tools/Modules` and select `Gameplay` to manage activation and choose the gameplay
time persistence strategy.

- `Local User Data` stores accumulated gameplay time in machine-local user data.
- `Save System` writes gameplay time into the currently loaded slot.

The `Save System` option is only selectable when the Save System module is
active. See [Gameplay Guide](12-gameplay-guide.md) for usage patterns and state
semantics.

### Notes for AI Agents

- Gameplay runtime code should resolve through the service surface rather than
  ad hoc singleton patterns.
- Keep gameplay-specific time handling inside the Gameplay slice instead of
  reintroducing a generic Time Management module.
- Interrupting gameplay is indefinite after the pause transition completes.
  Returning to gameplay is an explicit `ResumeGameplay()` decision, not an
  automatic timeout.
- Pause ownership is exclusive. The owner that called
  `PauseGameplay(interruptionOwner)` must be the one that calls
  `ResumeGameplay(interruptionOwner)`, while `StopGameplay()` stays available
  as the higher-priority shutdown path.
- `GameplayStatusChangeEvent` is now rich enough to carry previous status,
  transition origin, and session context. Prefer consuming that payload over
  rebuilding transition meaning from the current status alone.

## Save System

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `100`
- Declared dependencies: none

### Responsibilities

Save System owns slot management, persistence settings, Easy Save integration,
and optional AES-backed local obfuscation for save data.

### Runtime Entry Points

- `Runtime/Scripts/SaveSystem/SaveSystemModuleDefinition.cs`
- `Runtime/Scripts/SaveSystem/SaveSystemBootstrapper.cs`
- `Runtime/Scripts/SaveSystem/SaveSystemConfig.cs`
- `Runtime/Scripts/SaveSystem/SlotManager.cs`
- `Runtime/Scripts/SaveSystem/LoadedSlotService.cs`

The config asset resolves to
`Assets/Resources/SaveSystem/SaveSystemConfig.asset`. Runtime bootstrap creates
the `SaveSystem` GameObject, registers `SlotManager` and `LoadedSlotService` in
the global service locator, and can pre-create indexed slots.

### Editor Workflow

Use `Handy Tools/Modules` and select `Save System` to control auto boot, slot strategy,
indexed slot limits, and encryption settings.

### Notes for AI Agents

- Save encryption here is local obfuscation, not strong client-side security.
- Reuse `Utils/Crypto` instead of creating a new crypto ownership boundary.

## Globals

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `130`
- Declared dependencies: none

### Responsibilities

Globals provides editable global JSON data backed by a value tree. It is the
package-wide path-based configuration and lookup surface.

### Runtime Entry Points

- `Runtime/Scripts/GlobalConfig/GlobalConfigModuleDefinition.cs`
- `Runtime/Scripts/GlobalConfig/GlobalConfigModuleBootstrapper.cs`
- `Runtime/Scripts/GlobalConfig/Globals.cs`
- `Runtime/Scripts/GlobalConfig/JsonTree/*`

The runtime module loads JSON from `Assets/Resources/globals`. JsonTree types
are internal support code for this module and now live under the
`IndieGabo.HandyTools.GlobalConfig.JsonTree` namespace.

### Editor Workflow

Use `Handy Tools/Modules` and select `Globals` to edit the JSON tree, reload from disk, and
save back to the `globals` resource file. The panel now exposes a `Starter
Setup` button that creates `Assets/Resources/globals.json` explicitly when the
project does not provide it yet, instead of silently creating the file while
opening the editor surface.

### Notes for AI Agents

- Treat JsonTree as GlobalConfig-owned support code, not as a standalone module.
- Preserve the `Resources/globals` contract when changing load or save logic.

## Debugging

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `500`
- Declared dependencies: none

### Responsibilities

Debugging provides the runtime debug panel, debug settings, and pluggable panel
sections discovered through attributes.

### Runtime Entry Points

- `Runtime/Scripts/Debugging/DebuggingModuleDefinition.cs`
- `Runtime/Scripts/Debugging/DebuggingModuleBootstrapper.cs`
- `Runtime/Scripts/Debugging/DebugPanelBootstrapper.cs`
- `Runtime/Scripts/Debugging/DebugPanel.cs`
- `Runtime/Scripts/Debugging/DebugPanelRegistry.cs`

The panel config asset resolves to `Assets/Resources/Debugging/DebugPanel.asset`.
The module loads late because it is diagnostic tooling rather than core game
runtime infrastructure.

### Editor Workflow

Use `Handy Tools/Modules` and select `Debugging` to toggle the panel, control cursor and pause
behavior, and edit the embedded toggle `InputAction`.

### Notes for AI Agents

- Embedded `InputAction` editing must use serialized-property workflows.
- Debugging code may be inactive in non-editor or non-debug targets depending on
  compile conditions and the runtime bootstrap path.

## Steam

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `150`
- Declared dependencies: desktop standalone Steamworks support

### Responsibilities

Steam bootstraps the persistent Steamworks.NET manager for supported desktop
targets and exposes editor visibility into the configured app id and project
root `steam_appid.txt` file. At runtime the manager uses `SteamAPI.InitEx` so
initialization failures can report a concrete Steamworks reason instead of a
generic failure message.

### Runtime Entry Points

- `Runtime/Scripts/Steam/SteamModuleDefinition.cs`
- `Runtime/Scripts/Steam/SteamModuleBootstrapper.cs`
- `Runtime/Scripts/Steam/HandySteamManager.cs`

The module uses dependency status to block activation on unsupported targets.
If Steamworks cannot initialize, the transient manager destroys itself for that
run instead of remaining half-initialized.

### Editor Workflow

Use `Handy Tools/Modules` and select `Steam` to inspect the platform dependency state, the
hardcoded app id, and whether `steam_appid.txt` exists in the project root.
The panel also exposes a `Starter Setup` button that writes the file when the
project still does not provide it. That file helps local launches outside the
Steam client, but it does not replace the requirement for the Steam client to
be running under the same OS user context as Unity.

### Notes for AI Agents

- Do not assume Steam is available on mobile, WebGL, or unsupported desktop
  configurations.
- Respect the dependency gate and keep platform messaging accurate.
- Do not treat `steam_appid.txt` as sufficient proof that Steam will
  initialize successfully.

## ScreenShooter

### Activation Profile

- Activation mode: Optional
- Active by default: No
- Load order: `160`
- Declared dependencies: none

### Responsibilities

ScreenShooter owns the runtime screenshot capturer, its trigger input action,
and the output directory resolution logic.

### Runtime Entry Points

- `Runtime/Scripts/ScreenShooter/ScreenShooterModuleDefinition.cs`
- `Runtime/Scripts/ScreenShooter/ScreenShooterModuleBootstrapper.cs`
- `Runtime/Scripts/ScreenShooter/ScreenShooter.cs`
- `Runtime/Scripts/ScreenShooter/ScreenShooterConfig.cs`

The config asset resolves to
`Assets/Resources/ScreenShooter/ScreenShooterConfig.asset`. The default trigger
is `Left Ctrl + F12`, and relative output directories resolve from the project
root.

### Editor Workflow

Use `Handy Tools/Modules` and select `ScreenShooter` to edit the embedded trigger action and
the output directory.

### Notes for AI Agents

- Preserve relative-path resolution semantics when editing output path logic.
- Treat the embedded `InputAction` as asset data, not as a standalone asset
  reference.

Continue with [Auto-Activated Modules](11-auto-activated-modules.md).
