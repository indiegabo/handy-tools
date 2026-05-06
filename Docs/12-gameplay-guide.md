# Gameplay Guide

This guide explains how to use the Gameplay module as a runtime lifecycle
service and how its gameplay time persistence works.

## The Short Version

If a human only remembers four things, they should remember these:

- `GameplayService` is the global runtime entry point for gameplay state.
- Gameplay moves between `Off`, `On`, and `Paused`.
- Interrupting gameplay is indefinite after the freeze transition completes.
- Gameplay time persistence is configurable as `Local User Data` or
  `Save System`.

## Mental Model

The Gameplay module owns one global service that answers one question:

- is gameplay running,
- interrupted,
- or fully off?

It also owns the policy for tracking elapsed gameplay time while gameplay is
running.

The module is intentionally small:

- `GameplayService` owns the lifecycle state and time-scale transitions.
- `GameplayTimeRegisterer` records elapsed gameplay time when gameplay pauses
  or stops.
- `GameplayConfig` decides where that recorded time is persisted.

## State Model

The service exposes three stable states:

- `Off`
- `On`
- `Paused`

It also exposes whether a transition is currently in progress.

The intended transitions are:

1. `StartGameplay()` moves from `Off` to `On`.
2. `PauseGameplay(interruptionOwner)` moves from `On` to `Paused`.
3. `ResumeGameplay(interruptionOwner)` moves from `Paused` to `On`.
4. `StopGameplay()` moves from `On` or `Paused` to `Off`.

Calls that do not respect that flow are rejected.

That means `StartGameplay()` is not a generic "resume anything" shortcut.
If gameplay is interrupted, the expected return path is `ResumeGameplay()`.

## Interruptions Are Indefinite

An interruption is not a timed pause that automatically restores gameplay.

When `PauseGameplay()` finishes its freeze transition, gameplay stays
interrupted until something explicitly asks for `ResumeGameplay()`.

That explicit request can come from:

- the player,
- a pause menu,
- a cutscene controller,
- a dialogue flow,
- or any other system that owns the interruption.

The interruption owner is exclusive:

- the same owner that called `PauseGameplay(interruptionOwner)` must call
  `ResumeGameplay(interruptionOwner)`,
- a different owner cannot steal the resume path,
- and another system may still call `StopGameplay()` because stop has higher
  priority than interruption ownership.

There is no built-in countdown-based auto-return.

## Example 1: Start, Interrupt, Resume, Stop

```csharp
using IndieGabo.HandyTools.GameplayModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
{
    public sealed class GameplayFlowExample : MonoBehaviour
    {
        private GameplayService _gameplayService;

        private void Awake()
        {
            _gameplayService = ServiceLocator.GetRequired<GameplayService>();
        }

        public async Awaitable StartRun()
        {
            await _gameplayService.StartGameplay(0.25f);
        }

        public async Awaitable OpenPauseMenu()
        {
            await _gameplayService.PauseGameplay(this, 0.15f);
        }

        public async Awaitable ClosePauseMenu()
        {
            await _gameplayService.ResumeGameplay(this, 0.15f);
        }

        public async Awaitable FinishRun()
        {
            await _gameplayService.StopGameplay(0.2f);
        }
    }
}
```

## Transition Safety

`GameplayService` now rejects overlapping lifecycle operations while a
transition is running.

That prevents ambiguous flows such as:

- pausing and stopping at the same time,
- resuming during an unfinished pause transition,
- starting gameplay again while an earlier start is still unfreezing time.

If a caller needs queued or cancellable transitions, that should be modeled in
the owning system, not smuggled into overlapping direct calls.

## Gameplay Events

`GameplayService` publishes `GameplayStatusChangeEvent` whenever a transition
completes.

The event now carries four pieces of information:

- `PreviousStatus`: the state before the transition finished.
- `Status`: the new stable state after the transition finished.
- `Origin`: whether the change came from `Start`, `Pause`, `Resume`, or `Stop`.
- `SessionContext`: the gameplay session that owns the transition.

`SessionContext` is useful when several systems need to correlate events that
belong to the same gameplay run.

- `SessionId` stays stable across `Start -> Pause -> Resume -> Stop` inside one
  run.
- `SessionSequence` increments when a new gameplay run starts after a previous
  stop.
- `TransitionIndex` increments for each completed transition inside that run.

```csharp
using IndieGabo.HandyTools.GameplayModule;
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
{
    public sealed class GameplayEventExample : MonoBehaviour
    {
        private EventSubscription<GameplayStatusChangeEvent> _subscription;

        private void OnEnable()
        {
            _subscription = HandyBus<GameplayStatusChangeEvent>
                .Subscribe(OnGameplayStatusChanged);
        }

        private void OnDisable()
        {
            _subscription.Dispose();
        }

        private void OnGameplayStatusChanged(GameplayStatusChangeEvent @event)
        {
            Debug.Log(
                $"Gameplay {@event.Origin}: {@event.PreviousStatus} -> {@event.Status} " +
                $"(session {@event.SessionContext.SessionSequence}, transition {@event.SessionContext.TransitionIndex})"
            );
        }
    }
}
```

## Gameplay Time Persistence

The module records gameplay time only while gameplay is running.

When gameplay moves from `On` to `Paused` or `Off`, the module calculates the
elapsed gameplay time for that active interval and persists it according to the
selected strategy.

The strategy is configured in `GameplayConfig`, which resolves to:

- `Assets/Resources/Gameplay/GameplayConfig.asset`

### Strategy 1: Local User Data

`Local User Data` stores the accumulated gameplay time in local machine data
through `GameplayLocalUserData`.

Use this when:

- the project does not use Save System,
- gameplay time should be user-local and not slot-based,
- or the module should work even when no save slot is loaded.

```csharp
using IndieGabo.HandyTools.GameplayModule;

float totalGameplayTime = GameplayLocalUserData.TotalGameplayTime;
```

### Strategy 2: Save System

`Save System` writes gameplay time into the currently loaded slot through
`LoadedSlotService`.

Use this when:

- the project already uses the Save System module,
- gameplay time is part of slot progression,
- and the active session should persist per save slot rather than globally per
  machine.

Important rules:

- the `Save System` option is only selectable in the Gameplay panel when the
  Save System module is active,
- the runtime only uses the Save System path when that module is active,
- and slot-backed persistence only happens when a slot is actually loaded.

If the strategy is configured as `Save System` but the module is not active at
runtime, Gameplay falls back to `Local User Data`.

## Example 2: Pause Menu Ownership

This is the intended ownership pattern for interruptions.

```csharp
using IndieGabo.HandyTools.GameplayModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;

namespace IndieGabo.HandyTools.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        private GameplayService _gameplayService;

        private void Awake()
        {
            _gameplayService = ServiceLocator.GetRequired<GameplayService>();
        }

        public async Awaitable ShowPauseMenu()
        {
            await _gameplayService.PauseGameplay(this, 0.15f);
            gameObject.SetActive(true);
        }

        public async Awaitable HidePauseMenu()
        {
            gameObject.SetActive(false);
            await _gameplayService.ResumeGameplay(this, 0.15f);
        }
    }
}
```

The interruption stays active until `HidePauseMenu()` explicitly resumes the
service.

If some other system decides the run must end, it can still call
`StopGameplay()` without owning the pause.

## Service Access

Gameplay owns one global service instance bootstrapped by the module.

Use the service locator to resolve it:

```csharp
using IndieGabo.HandyTools.GameplayModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;

GameplayService gameplayService = ServiceLocator.GetRequired<GameplayService>();
```

The bootstrapper is idempotent. If the gameplay service already exists, the
module will not create a duplicate runtime object.

## Editor Workflow

Use `Handy Tools/Modules` and select `Gameplay` to configure the module.

The panel currently exposes:

- gameplay time persistence strategy,
- availability feedback for Save System-backed persistence,
- and reminders about indefinite interruptions.

If Save System is disabled, the panel keeps `Save System` visible in the
dropdown but unavailable for selection.

## Common Mistakes

### Mistake 1: Using Start As Resume

`StartGameplay()` is for entering gameplay from `Off`.

If gameplay is interrupted, use `ResumeGameplay()`.

### Mistake 2: Assuming Pause Will Auto-Expire

It will not. Once interrupted, gameplay stays interrupted until a caller
explicitly resumes it.

### Mistake 3: Assuming Save System Persistence Works Without A Loaded Slot

Even when the Save System strategy is selected, gameplay time only lands in a
slot when a slot is actually loaded.

### Mistake 4: Calling Lifecycle Methods Concurrently

The service now blocks overlapping transitions on purpose. If multiple systems
need to negotiate lifecycle ownership, solve that in orchestration code rather
than racing direct service calls.

### Mistake 5: Resuming With A Different Owner

If `PauseGameplay()` was requested by one owner, only that same owner can call
`ResumeGameplay()`.
