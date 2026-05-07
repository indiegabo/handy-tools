# Service Locator Guide

HandyTools uses one static global service locator bootstrapped by the kernel.
The locator is intentionally strict so that default lookups remain predictable
and additional instances are always explicit.

## Mental Model

- Each service type may have one default unnamed registration.
- Additional instances of the same type must use a `ServiceIdentifier`.
- Unnamed lookups never guess between multiple instances.
- Module-owned key helpers should define identifiers so consumers do not spread
  raw strings through the codebase.

## Registration Modes

Use default registration when a type has exactly one canonical runtime
instance.

```csharp
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.GameplayModule;
using UnityEngine;

GameObject go = new("Gameplay Service");
GameplayService gameplayService = go.AddComponent<GameplayService>();

ServiceLocator.Register(gameplayService);

GameplayService resolved = ServiceLocator.GetRequired<GameplayService>();
bool found = ServiceLocator.TryGet(out GameplayService optionalGameplayService);
```

If a second unnamed registration of the same type is attempted, the locator
throws. That is intentional. Additional instances must be identified.

```csharp
using IndieGabo.HandyTools.HandyServiceLocatorModule;

PlayerInput playerOne = /* existing instance */;
PlayerInput playerTwo = /* existing instance */;

ServiceLocator.Register(
    PlayerInputServiceKeys.ForPlayerIndex(playerOne.playerIndex),
    playerOne
);

ServiceLocator.Register(
    PlayerInputServiceKeys.ForPlayerIndex(playerTwo.playerIndex),
    playerTwo
);
```

## Lookup APIs

Use the unnamed APIs only for the default registration.

```csharp
GameplayService gameplayService = ServiceLocator.GetRequired<GameplayService>();
```

Use identified APIs when more than one instance of a type may exist.

```csharp
using IndieGabo.HandyTools.HandyInputSystemModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine.InputSystem;

PlayerInput playerTwo = ServiceLocator.GetRequired<PlayerInput>(
    PlayerInputServiceKeys.ForPlayerIndex(2)
);

bool found = ServiceLocator.TryGet(
    PlayerInputServiceKeys.ForInputUserId(17),
    out PlayerInput userBoundPlayer
);
```

Use `GetAll<T>(List<T>)` when a type may have both one default registration and
multiple identified registrations.

```csharp
List<PlayerInput> players = new();
int count = ServiceLocator.GetAll(players);
```

Do not rely on the returned order from `GetAll`.

## ServiceIdentifier Usage

`ServiceIdentifier` supports string and GUID-backed identifiers. Prefer module
helpers and cached identifiers over ad-hoc string literals.

```csharp
using System;
using IndieGabo.HandyTools.HandyServiceLocatorModule;

public static class GameplayServiceKeys
{
    public static readonly ServiceIdentifier MainSession
        = ServiceIdentifier.Create("Gameplay/MainSession");

    public static ServiceIdentifier ForMatch(Guid matchGuid)
    {
        return ServiceIdentifier.Create(matchGuid);
    }
}
```

## Input Module Keys

The input module centralizes its identifiers in
`Runtime/Scripts/Input/PlayerInputServiceKeys.cs`.

- `PlayerInputServiceKeys.ForPlayerIndex(int playerIndex)`
- `PlayerInputServiceKeys.ForPlayerId(string playerId)`
- `PlayerInputServiceKeys.ForInputUserId(uint inputUserId)`
- `PlayerInputServiceKeys.ForPersistentGuid(Guid persistentGuid)`

The single-player `PlayerInput` is owned by `PlayerManager`. Consumers that
need it should resolve the `PlayerManager` and request the input from that
component instead of using a public single-player service identifier.

`PlayerManager` automatically registers multiplayer `PlayerInput` instances by
player index, `InputUser.id`, and a persistent runtime GUID.

```csharp
using System;
using IndieGabo.HandyTools.HandyInputSystemModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine.InputSystem;

bool foundByIndex = ServiceLocator.TryGet(
    PlayerInputServiceKeys.ForPlayerIndex(2),
    out PlayerInput playerTwo
);

bool foundByUser = ServiceLocator.TryGet(
    PlayerInputServiceKeys.ForInputUserId(17),
    out PlayerInput userPlayer
);

Guid persistentGuid = /* value captured from an event or manager query */;

PlayerInput samePlayer = ServiceLocator.GetRequired<PlayerInput>(
    PlayerInputServiceKeys.ForPersistentGuid(persistentGuid)
);
```

## Event Flow Example

`PlayerJoinedEvent` and `PlayerLeftEvent` expose the persistent GUID assigned to
the player registration so listeners can reconnect to the same `PlayerInput`
through the locator.

```csharp
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.HandyInputSystemModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine.InputSystem;

private EventBinding<PlayerJoinedEvent> _playerJoinedBinding;

private void OnEnable()
{
    _playerJoinedBinding = new EventBinding<PlayerJoinedEvent>(OnPlayerJoined);
    HandyBus<PlayerJoinedEvent>.Register(_playerJoinedBinding);
}

private void OnDisable()
{
    HandyBus<PlayerJoinedEvent>.Deregister(_playerJoinedBinding);
}

private void OnPlayerJoined(PlayerJoinedEvent @event)
{
    if (!ServiceLocator.TryGet<PlayerInput>(
        PlayerInputServiceKeys.ForPersistentGuid(@event.persistentGuid),
        out PlayerInput playerInput
    ))
    {
        return;
    }

    // Use playerInput.
}
```

## Deregistration Rules

Remove the default registration by instance reference.

```csharp
ServiceLocator.Deregister(gameplayService);
```

Remove identified registrations by the same identifier used to add them.

```csharp
ServiceLocator.Deregister<PlayerInput>(
    PlayerInputServiceKeys.ForPlayerIndex(2)
);
```

If a module owns service identifiers, that module should also own the helper
that creates them.
