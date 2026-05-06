# Kernel and Boot Flow

The HandyTools kernel is the mandatory infrastructure that exists before any
optional feature slice is allowed to boot. It is responsible for preparing
required infrastructure, discovering optional modules, filtering them by
activation and dependency state, and booting them in a stable order.

## Kernel Files

The kernel is primarily defined by these files:

- `Runtime/Scripts/Core/RuntimeBootstrapCoordinator.cs`
- `Runtime/Scripts/Core/Modules/HandyModuleRuntimeLoader.cs`
- `Runtime/Scripts/Core/Modules/HandyModuleDescriptor.cs`
- `Runtime/Scripts/Core/Modules/HandyModuleSettings.cs`
- `Runtime/Scripts/Core/Modules/IHandyModuleBootstrapper.cs`
- `Runtime/Scripts/EventBus/Utils/EventBusUtil.cs` (`HandyBusUtil`)
- `Runtime/Scripts/ServiceLocator/Core/ServiceLocator.cs`

## Startup Phases

The runtime boot sequence is intentionally split across Unity startup phases.

| Unity phase             | HandyTools action                                            | Why it exists                                                                |
| ----------------------- | ------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| `SubsystemRegistration` | Reset kernel and loader state                                | Prevents stale static state between play sessions                            |
| `BeforeSplashScreen`    | Prepare mandatory infrastructure and discover active modules | Makes module availability deterministic before scene work starts             |
| `BeforeSceneLoad`       | Bootstrap the active optional modules in load-order order    | Ensures services and runtime objects exist before gameplay scenes initialize |

## Mandatory Infrastructure

Before optional modules are considered, the loader invokes two mandatory static
bootstrap calls:

- `IndieGabo.HandyTools.HandyBusModule.HandyBusUtil.Initialize`
- `IndieGabo.HandyTools.HandyServiceLocatorModule.ServiceLocator.BootstrapGlobal`

These are invoked by name through `HandyModuleRuntimeLoader`, not through the
optional module contract. That makes HandyBus and ServiceLocator mandatory
kernel infrastructure rather than user-managed modules.

Module-owned namespaces now follow the `*Module` convention even when the
physical folder path keeps an older slice name such as `EventBus`.

## HandyBus Contract

HandyBus is a typed static event bus.

- Each closed `HandyBus<T>` registers itself lazily with `HandyBusUtil` on
  first use.
- `HandyBusUtil` clears all previously used buses on runtime reset and editor
  play mode exit boundaries.
- Subscription lifetime should be owned through the returned
  `EventSubscription<T>` token rather than raw string keys or domain IDs.

Read [HandyBus Guide](08-handybus-guide.md) for the developer-facing API,
usage examples, and dispatch rules.

## Service Locator Contract

The service locator is a single global runtime registry.

- Each service type can have one default unnamed registration.
- Additional instances of the same service type must be registered with a
  `ServiceIdentifier`.
- `TryGet<T>()` and `GetRequired<T>()` resolve only the default unnamed
  registration.
- `TryGet<T>(identifier)` and `GetRequired<T>(identifier)` resolve one
  identified registration.

Read [Service Locator Guide](07-service-locator-guide.md) for the full
developer-facing API contract and examples.

## Pooling Contract

Pooling is definition-driven but runtime-instance-based.

- `HandyPool<T>` is a definition asset, not the globally shared mutable pool
  owner.
- `HandyPoolRuntime<T>` owns one independent set of active subpools for one
  scene owner or runtime context.
- Identified active subpools register with `PoolRegistry` and resolve by
  `PoolIdentifier`.
- Dismissing a runtime clears pooled inactive subjects and destroys the tracked
  active subjects created by that runtime.

Read [Pooling Guide](09-pooling-guide.md) for the developer-facing API,
ownership rules, and identifier guidance.

## Optional Module Discovery

Optional modules implement `IHandyModuleBootstrapper`.

The loader searches every loaded assembly, finds every non-abstract concrete
type that implements the contract, instantiates it, then filters the resulting
bootstrappers.

Filtering happens in this order:

1. Check `HandyModuleSettings.Instance.IsModuleActive(descriptor)`.
2. Check that all `HandyModuleDependencyStatus` entries are satisfied.
3. Sort the remaining bootstrappers by `Descriptor.LoadOrder`.
4. Call `Bootstrap()` on each active bootstrapper during `BeforeSceneLoad`.

## Module Activation Rules

Activation is controlled by `HandyModuleDescriptor` and `HandyModuleSettings`.

- Required modules always resolve as active.
- Optional modules resolve from stored project state when a state exists.
- Optional modules fall back to `Descriptor.IsActiveByDefault` when no explicit
  project state has been stored yet.

This is how auto-activated modules such as Web, Pooling, Identifying, and
Rendering remain optional in the architecture while still defaulting to active.

## Module Settings Asset

Project-level activation is stored in:

- `Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset`

`HandyModuleSettings` auto-creates this asset in the editor when it does not
already exist. That allows new projects to discover modules without a manual
asset creation step.

## Load Order Philosophy

Load order should communicate dependency direction and startup intent.

- Logging loads very early because diagnostic visibility is useful before other
  modules start creating runtime objects.
- Input and Gameplay load before modules that commonly depend on input or
  gameplay state.
- GlobalConfig, Save System, Steam, and ScreenShooter load later because they
  either consume external assets, create runtime services, or are less central
  to initial scene startup.
- Debugging loads very late because it is diagnostic tooling rather than core
  game runtime infrastructure.

## What the Kernel Does Not Do

The kernel does not own feature-specific configuration. It only knows:

- which modules exist,
- whether they are active,
- whether their dependencies are satisfied,
- and in which order they must boot.

Feature-specific configuration belongs to the owning module or support slice.

## Guidance for AI Agents

- When a behavior looks like startup orchestration, first inspect the kernel
  files listed in this document before touching a module.
- Do not convert mandatory infrastructure into optional modules.
- Do not add direct calls to module bootstrappers inside
  `RuntimeBootstrapCoordinator`; boot should remain loader-driven.
- If you add a new module, make sure it exposes a valid descriptor, implements
  `IHandyModuleBootstrapper`, and compiles into an assembly that will be loaded
  into the current AppDomain.
- If you change load order, explain why in the matching module documentation.
- If you change service registration semantics, update the service locator
  guide in the same change.
- If you change event subscription semantics, update the HandyBus guide in the
  same change.

Continue with [Service Locator Guide](07-service-locator-guide.md).
