# Debug, History, and Visualizer

If you want to understand what the machine is doing without cracking its skull open with an axe, this is the section you want.

## What the Package Offers Today

Inside the editor, HandyFSM offers:

- a button in the brain inspector to open the visualizer
- a menu entry for the visualization window
- a current-state view
- a history view of the last captured session

## How to Open the Visualizer

You have two paths:

1. in the `FSMBrain` inspector, click `Open State Visualizer`
2. use the menu `Window > HandyFSM > State Visualizer`

## What the `Current State` View Shows

This view builds a tree of the loaded state types and highlights:

- the current active state
- the parent types of the current state
- the path that leads to the currently active state

In practice, that helps answer questions like:

- which state is actually active right now?
- which inheritance chain does it come from?
- which state types were loaded into this machine?

## What the `History` View Shows

This view lists the last captured session for the selected machine.

Each record may include:

- previous state
- new state
- transition reason
- optional transition message

This is especially valuable when you use:

- `CompleteState(...)`
- `FailState(...)`
- `ThrowStateFailure(...)`

## How to Enable History Capture

On `FSMBrain`:

1. enable `Save History`
2. enter play mode
3. use the machine normally
4. open the visualizer and inspect the `History` tab

## When History Is Cleared

In the current editor flow, the last-session capture is cleared when play mode starts.

That means the displayed history corresponds to the current execution cycle, not to an infinite pile of old sessions.

## Does History Run in the Final Runtime Build?

The automatic tracker used by the history window is an editor tool.

In practice:

- the history window is editor-only
- the automatic capture of the last session used by it is also editor-only

The brain still exposes `ShouldCaptureHistory`, but the visual tracking mechanism described here belongs to the editor tooling layer.

## How Reasons Appear in History

Common reasons that may appear:

- `InitialEntry`
- `ExternalRequest`
- `ConditionTransition`
- `NaturalTransition`
- `ErrorTransition`

If an error transition carries a message, that message can appear in the record as well.

Example:

```csharp
FailState(null, "Navigation path became invalid.");
```

## `StateFailureException` and Diagnostics

When a state throws `StateFailureException`:

- the brain intercepts it
- logs an error
- tries to recover through a fallback path
- records `ErrorTransition` when applicable

That turns state failure into auditable machine data instead of letting it become just another forgotten stack trace.

## `StateRegistry`, `Session`, and `Record`

The package also includes lower-level registration types:

- `StateRegistry`
- `Session`
- `Record`

Today, for day-to-day usage, the practical recommendation is to trust the visualizer and the editor's automatic history tracker first.

But those types exist if you want to build your own tooling, persist sessions, or create custom analysis flows.

## Quick Reading of the Debug Components

### `MachineStateVisualizerWindow`

The main editor window.

### `StateVisualizer`

Builds the current-state and history views.

### `MachineStateHistoryTracker`

An editor-only tracker that watches active `FSMBrain` instances in play mode and stores the most recent recorded session.

## Common Problems and How to Read the Symptoms

### "Nothing appears in the History tab"

Checklist:

- is `Save History` enabled?
- did you enter play mode after enabling it?
- did the machine actually perform transitions?
- is the window pointing to the correct brain?

### "Nothing appears in Current State"

Checklist:

- is the correct brain selected?
- were the states actually loaded?
- is the machine initialized?

### "The current state is correct, but the transition reason is not helpful"

In that case the problem usually lives in your authoring. Start using better messages in `FailState(...)` and clearer modeling for natural and external transitions.

## Recommended Debug Strategy

When an FSM starts acting strangely, follow this order:

1. confirm that the machine turned on
2. inspect which state became active
3. inspect whether that state's transitions were recorded as expected
4. inspect the reason for the latest change
5. if an error happened, look for the `StateFailureException` message

That is far cheaper than spraying `Debug.Log` everywhere like propaganda flyers on strike day.
