# Package Overview

HandyTools is a Unity package organized around a mandatory kernel and a set of
optional modules. The kernel provides startup orchestration, event dispatch,
service registration, and module discovery. Optional modules own their own
runtime dependencies, editor panels, and configuration assets.

This document explains the package shape at a high level. For lifecycle and
boot details, continue with [Kernel and Boot Flow](01-kernel-and-boot-flow.md).

## Intended Audience

This documentation is written for both human developers and AI agents.

- Human readers should use it to understand package setup, configuration, and
  integration boundaries.
- AI agents should use it to identify code ownership, activation rules,
  dependency boundaries, and safe extension points before editing the package.

## Package Model

HandyTools is split into four broad layers:

1. Kernel infrastructure: startup orchestration, module metadata, event bus,
   and service locator.
2. Configurable modules: optional modules with dedicated editor panels.
3. Auto-activated modules: optional modules that default to active but do not
   require editor configuration panels.
4. Utility and support code: static helpers and module-owned support slices
   that should not bootstrap themselves.

## Mandatory Infrastructure

These slices are mandatory and are not treated as user-togglable modules:

- HandyBus under `Runtime/Scripts/EventBus`
- ServiceLocator under `Runtime/Scripts/ServiceLocator`
- Kernel module contracts under `Runtime/Scripts/Core/Modules`
- Runtime bootstrap coordinator under `Runtime/Scripts/Core`

The kernel prepares and boots optional modules only after this infrastructure is
ready.

## Module Catalog

| Slice         | Kind                  | Default State    | Editor Surface        | Main Runtime Entry                                                 |
| ------------- | --------------------- | ---------------- | --------------------- | ------------------------------------------------------------------ |
| Logging       | Configurable module   | Off              | Shared modules window | `LoggingModuleDefinition`, `LoggerBootstrapper`                    |
| Input         | Configurable module   | Off              | Shared modules window | `InputModuleDefinition`, `ProjectInputConfig.Bootstrap()`          |
| Gameplay      | Configurable module   | Off              | Shared modules window | `GameplayModuleDefinition`, `GameplayServiceBootstrapper`          |
| Save System   | Configurable module   | Off              | Shared modules window | `SaveSystemModuleDefinition`, `SaveSystemBootstrapper`             |
| Globals       | Configurable module   | Off              | Shared modules window | `GlobalConfigModuleDefinition`, `Globals.LoadFromGlobals()`        |
| Debugging     | Configurable module   | Off              | Shared modules window | `DebuggingModuleDefinition`, `DebugPanelBootstrapper`              |
| Steam         | Configurable module   | Off              | Shared modules window | `SteamModuleDefinition`, `SteamModuleBootstrapper`                 |
| ScreenShooter | Configurable module   | Off              | Shared modules window | `ScreenShooterModuleDefinition`, `ScreenShooterModuleBootstrapper` |
| Web           | Auto-activated module | On when unset    | No panel              | `WebModuleDefinition`                                              |
| Pooling       | Auto-activated module | On when unset    | No panel              | `PoolingModuleDefinition`                                          |
| Identifying   | Auto-activated module | On when unset    | No panel              | `IdentifyingModuleDefinition`                                      |
| Rendering     | Auto-activated module | On when unset    | No panel              | `RenderingModuleDefinition`                                        |
| Utils         | Utility/support slice | Always available | No panel              | Static helpers only                                                |

## Documentation Reading Map

- Read [Kernel and Boot Flow](01-kernel-and-boot-flow.md) to understand the
  startup order and module discovery rules.
- Read [Editor Setup and Configuration](02-editor-setup-and-configuration.md)
  to understand menus, setup automation, and asset locations.
- Read [Assembly Layout and Dependency Rules](03-assembly-layout-and-dependency-rules.md)
  before adding references or moving code between asmdefs.
- Read [Utils and Reclassifications](04-utils-and-reclassifications.md) before
  deciding whether a new feature belongs in a module or in support code.
- Read [Configurable Modules](10-configurable-modules.md) and
  [Auto-Activated Modules](11-auto-activated-modules.md) when touching a
  specific feature slice.

## Source Tree Orientation

The most important top-level package paths are:

- `Assets/HandyTools/Runtime`: runtime code and runtime asmdefs.
- `Assets/HandyTools/Editor`: editor code, editor asmdefs, setup tools, and
  module panels.
- `Assets/HandyTools/Docs`: package-local documentation intended to stay close
  to the code.
- `Assets/Resources`: project-level generated or edited assets used by module
  configs and module activation.

## Guidance for AI Agents

- Treat the package as ownership-first. Identify the owning module or support
  slice before changing code.
- Do not add optional package references to the root runtime or root editor
  asmdefs.
- If a feature needs bootstrap logic, explicit activation, or dependencies, it
  probably belongs to a module. If it is a static helper with no setup, it
  probably belongs to a utility or module-owned support slice.
- If a change alters module activation, boot order, asset paths, menus, or
  ownership rules, update the matching documentation file in the same change.

Continue with [Kernel and Boot Flow](01-kernel-and-boot-flow.md).
