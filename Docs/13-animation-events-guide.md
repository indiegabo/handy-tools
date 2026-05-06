# Animation Events Guide

This guide covers the HandyTools Animation Events slice.

## Overview

Animation Events is an auto-activated optional module that adds two state-driven
authoring flows for Animator controllers:

- Local events via `AnimationEventStateBehaviour` and
  `AnimationEventReceiver`.
- Typed HandyBus events via `AnimationEventBusStateBehaviour` and
  `[AnimatorBusEvent(...)]` classes.

Both flows are authored on `StateMachineBehaviour` instances attached directly
to Animator states. HandyTools intentionally uses one behaviour instance per
event so each event has its own inspector workflow and trigger-time binding to
the Animation window.

## Local Event Flow

Use this flow when the response should stay on the Animator owner or when Unity
Event wiring is the most direct solution.

### Runtime Pieces

- `Runtime/Scripts/AnimationEvents/AnimationEventStateBehaviour.cs`
- `Runtime/Scripts/AnimationEvents/AnimationStateEventTrigger.cs`
- `Runtime/Scripts/AnimationEvents/AnimationEventReceiver.cs`
- `Runtime/Scripts/AnimationEvents/AnimationEventResponseBinding.cs`

### Authoring Steps

1. Add `AnimationEventReceiver` to the GameObject that owns the `Animator`.
2. Configure one or more string event bindings in the receiver.
3. Add `AnimationEventStateBehaviour` to the desired Animator state.
4. Set the event name and trigger time.
5. Keep the Animation window bound to the same Animator clip and use
   `Use Needle Time` when you want to copy the current needle time back into
   the trigger.

### Notes

- If the state behaviour fires an event name that is not configured in the
  receiver, nothing happens.
- The receiver caches bindings by event name at runtime.
- The trigger fires once per animation loop when the normalized time crosses
  the configured threshold.

## Typed HandyBus Flow

Use this flow when the response should be decoupled from the Animator owner and
handled by any subscriber in the project.

### Runtime Pieces

- `Runtime/Scripts/AnimationEvents/AnimationEventBusStateBehaviour.cs`
- `Runtime/Scripts/AnimationEvents/AnimationEventBusStateTrigger.cs`
- `Runtime/Scripts/AnimationEvents/AnimatorBusEventAttribute.cs`
- `Runtime/Scripts/AnimationEvents/AnimatorBusEventBase.cs`
- `Runtime/Scripts/AnimationEvents/AnimatorBusEventRegistry.cs`

### Event Declaration

Declare one serializable event class that derives from
`AnimatorBusEventBase` and annotate it with `AnimatorBusEventAttribute`.

```csharp
using System;
using IndieGabo.HandyTools.AnimationEventsModule;
using UnityEngine;

namespace Game.AnimationEvents
{
    [Serializable]
    [AnimatorBusEvent(
        "Characters.Buffy.OnAttack",
        DisplayName = "Buffy Attack",
        Description = "Raised when Buffy reaches the attack frame."
    )]
    public sealed class BuffyAttackEvent : AnimatorBusEventBase
    {
        [SerializeField] private int _comboIndex;

        public int ComboIndex => _comboIndex;
    }
}
```

### Authoring Steps

1. Add `AnimationEventBusStateBehaviour` to the desired Animator state.
2. Pick the event from the hierarchical event menu.
3. Author the payload inline in the same inspector.
4. Keep the Animation window bound to the same Animator clip and use
   `Use Needle Time` when you want to copy the current needle time back into
   the trigger.
5. Subscribe through `HandyBus<T>.Subscribe(...)` anywhere in the project.

### Notes

- The event picker stores both the logical event path and the resolved type
  name.
- Selecting an event creates a matching payload instance automatically. If it
  goes missing or mismatches the selected type, re-select the event to rebuild
  it.
- At dispatch time, the authored payload is shallow-cloned and enriched with
  runtime context such as the Animator, controller, state info, layer index,
  normalized time, and event path.
- Duplicate `AnimatorBusEvent` paths are rejected by the runtime registry.

## Dispatch Semantics

- Both local and typed triggers fire once per animation loop when normalized
  time crosses the configured threshold.
- Looping states rearm automatically on the next loop.
- Re-entering the state resets the trigger for the new activation.

## Animation Window Workflow

Both state behaviours expose a compact Animation Window integration section in
the inspector.

### Requirements

- Select a GameObject with an `Animator` in the editor, or keep the Animation
  window bound to the Animator owner while the state is selected.
- The resolved Animator must use an `AnimatorController` or an
  `AnimatorOverrideController` backed by an `AnimatorController`.
- The resolved Animator controller must contain the state that owns the edited
  behaviour.
- To sync with the Animation window needle, keep the Animation window open with
  a clip selected that matches the current state clip or one clip used by the
  current BlendTree.

### Controls

- `Trigger Time` now drives the Animation window needle directly whenever the
  current Animation window clip matches the inspected state.
- `Use Needle Time` copies the current Animation window needle time back into
  the serialized trigger time.

### Motion Support

- `AnimationClip` states sync directly against the matching clip shown in the
  Animation window.
- `BlendTree` states can sync when the Animation window is focused on one of the
  clips used by the BlendTree.
- Override controllers are respected where clip overrides are available.

## Validation Rules

- `AnimationEventStateBehaviour` warns when the event name is empty.
- `AnimationEventStateBehaviour` also warns when the resolved Animator target
  has an Animator but no `AnimationEventReceiver`.
- `AnimationEventBusStateBehaviour` warns when no typed event is selected.
- `AnimationEventBusStateBehaviour` also warns when the payload instance is
  missing or no longer matches the selected event type.

## AI Agent Notes

- Keep this slice as an auto-activated module unless it gains real project-level
  configuration.
- Preserve the one-behaviour-per-event model. The trigger-time and
  Animation-window workflow are tied to that authoring model.
- Prefer UI Toolkit for further editor work. Fall back only when Unity exposes
  no viable UI Toolkit surface.
