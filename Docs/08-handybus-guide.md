# HandyBus Guide

HandyBus is HandyTools' typed static event bus. It is designed for cheap event
dispatch, predictable subscription lifetime, and safe runtime reset across
editor play mode boundaries.

## Migration Notes

This guide reflects the current breaking namespace and API contract.

- `using IndieGabo.HandyTools.HandyBus;` became
  `using IndieGabo.HandyTools.HandyBusModule;`
- `EventBus<T>` became `HandyBus<T>`
- `EventBusUtil` became `HandyBusUtil`
- `EventSubscription<T>` and `IEvent` did not change names

The source files still live under `Runtime/Scripts/EventBus`, but the public
namespace is now `IndieGabo.HandyTools.HandyBusModule`.

## Mental Model

- `HandyBus<T>` owns listeners for one event type.
- The subscription token returned by `Subscribe` is the listener identity.
- The bus does not use string keys, GUIDs, or locator-style identifiers for
  subscriptions.
- Registering or deregistering during a raise is safe.

## Recommended Subscription Pattern

Own the subscription through an `EventSubscription<T>` field and dispose it
when the owner is disabled or destroyed.

```csharp
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine;

public sealed class GameplayStatusListener : MonoBehaviour
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
        // React to the event.
    }
}
```

This is the preferred API for new code. The older `Register` and `Deregister`
methods still exist for manual binding ownership, but token-based ownership is
easier to read and harder to misuse.

## Subscription Shapes

Payload-aware callback:

```csharp
EventSubscription<SlotEvent> subscription = HandyBus<SlotEvent>
    .Subscribe(OnSlotEvent);
```

Payload-agnostic callback:

```csharp
EventSubscription<SlotEvent> subscription = HandyBus<SlotEvent>
    .Subscribe(OnAnySlotEvent);
```

Combined callback:

```csharp
EventSubscription<SlotEvent> subscription = HandyBus<SlotEvent>
    .Subscribe(OnSlotEvent, OnAnySlotEvent);
```

Manual binding when one owner needs to add and remove callbacks dynamically:

```csharp
EventBinding<SlotEvent> binding = new(OnSlotEvent);
binding.Add(OnAnySlotEvent);

EventSubscription<SlotEvent> subscription = HandyBus<SlotEvent>
    .Subscribe(binding);
```

## Dispatch Semantics

HandyBus now guarantees mutation-safe dispatch.

- A listener can subscribe during `Raise` without corrupting the current
  iteration.
- A listener can deregister during `Raise` without throwing collection
  modification exceptions.
- A listener removed before its turn in the same dispatch will be skipped.
- New listeners added during dispatch are not visible until the outermost
  nested dispatch completes.

These rules make nested gameplay flows and self-unsubscribing listeners safe.

## Event Authoring Rules

Any event type must implement `IEvent`.

Use a `struct` when the event is small, value-like, and raised frequently.

```csharp
using IndieGabo.HandyTools.HandyBusModule;

public struct GameplayStatusChangeEvent : IEvent
{
    public GameplayService.Status Status { get; set; }
}
```

Use a `class` when the event payload is larger, naturally reference-based, or
rare enough that allocation cost is not the primary concern.

```csharp
using System;
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine.InputSystem;

public class PlayerJoinedEvent : IEvent
{
    public int playerIndex;
    public Guid persistentGuid;
    public PlayerInput playerInput;
}
```

## Raise Pattern

Publishing remains intentionally direct.

```csharp
HandyBus<GameplayStatusChangeEvent>.Raise(
    new GameplayStatusChangeEvent
    {
        Status = GameplayService.Status.On
    }
);
```

Prefer a single event type per domain transition. Do not overload one event
with unrelated meanings that force every listener to branch on incidental data.

## Runtime Reset Model

`HandyBusUtil` no longer depends on scanning `Assembly-CSharp` to discover event
types. Each closed bus registers itself lazily the first time it is used.

That means:

- buses defined in asmdef-based modules are cleared correctly,
- editor play mode exit can clear subscriptions reliably,
- and runtime reset no longer depends on reflection over hard-coded assembly
  names.

## When Not To Use HandyBus

Do not use HandyBus when a direct method call or explicit service dependency is
clearer and cheaper.

- Use the service locator for stable runtime services.
- Use direct references for one-to-one object relationships.
- Use HandyBus when multiple independent listeners should react to the same
  domain transition without hard coupling.
