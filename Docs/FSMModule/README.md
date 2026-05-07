# HandyTools FSM Module

The former standalone HandyFSM package now lives inside HandyTools as the
auto-activated `FSM` module.

This folder remains the canonical detailed reference for the FSM feature set,
but package-level activation, editor setup, and optional integrations now flow
through HandyTools module infrastructure.

Read [Operating The Module](00-Operating-The-Module.md) first when you
need to review activation overrides, validate optional integrations, or import
the package samples.

## Current Scope

- core FSM authoring with `FSMBrain`, `State`, and `ScriptableState`
- state loading, transitions, triggers, and runtime flow
- delegated brain domains for machine control, state lookup, input caching,
  runtime stats, optional blackboard access, and optional CCPro services
- editor-side debugging with the visualizer and history tooling
- optional Simple Blackboard integration through the reflection bridge in `FSMBrain`
- optional Character Controller Pro integration, including typed CCPro state bases
- whole-build stats resolution through `FSMStatsRegistry` and `Brain.Stats`
- package-managed sample import through HandyTools package samples

## Recommended Reading Order

1. [Operating The Module](00-Operating-The-Module.md)
2. [Getting Started](01-Getting-Started.md)
3. [Core Concepts](02-Core-Concepts.md)
4. [FSMBrain and Machine Flow](03-FSMBrain-and-Machine-Flow.md)
5. [Creating States](04-Creating-States.md)
6. [Transitions and Flow Control](05-Transitions-and-Flow-Control.md)
7. [Triggers](06-Triggers.md)
8. [Blackboard with Simple Blackboard](07-Blackboard-with-Simple-Blackboard.md)
9. [Character Controller Pro](08-Character-Controller-Pro.md)
10. [Debug, History, and Visualizer](09-Debug-History-and-Visualizer.md)
11. [Best Practices and FAQ](10-Best-Practices-and-FAQ.md)

## Brain Domain Reference

Read these when you need to write real gameplay code against the modern
`FSMBrain` surface instead of browsing source files.

1. [FSMBrain and Machine Flow](03-FSMBrain-and-Machine-Flow.md)
2. [Machine Domain](12-Machine-Domain.md)
3. [States Domain](13-States-Domain.md)
4. [Input Domain](14-Input-Domain.md)
5. [Stats Domain](15-Stats-Domain.md)
6. [Triggers](06-Triggers.md)
7. [Blackboard with Simple Blackboard](07-Blackboard-with-Simple-Blackboard.md)
8. [Character Controller Pro](08-Character-Controller-Pro.md)

## Main Module Surfaces

- `FSMBrain`: the composition root and runtime branch owner
- `FSMBrain.Machine`: lifecycle and transition control
- `FSMBrain.States`: state loading and lookup
- `FSMBrain.Input`: input cache, snapshots, and recent-button consumption
- `FSMBrain.Stats`: default stats resolution and runtime overrides
- `FSMBrain.Blackboard`: optional Simple Blackboard value access
- `FSMBrain.CCPro`: optional Character Controller Pro runtime services
- `State`: the base class for runtime states implemented as regular C# classes
- `ScriptableState`: the base class for asset-authored states
- `StateProvider`: the loader, lookup, and initialization layer for states
- `StateTransition`: a transition rule containing a condition, a target state, and a priority
- `TriggersProvider`: a lightweight named-event channel
- optional integrations: Simple Blackboard and Character Controller Pro
- editor tooling: custom inspector, state visualizer, and history tracking
- package samples: `FSM CCPro Starter Kit`

## Reading by Goal

If your goal is "I want to make it work today," read:

1. [00 - Operating The Module](00-Operating-The-Module.md)
2. [01 - Getting Started](01-Getting-Started.md)
3. [04 - Creating States](04-Creating-States.md)
4. [05 - Transitions and Flow Control](05-Transitions-and-Flow-Control.md)

If your goal is "I want to integrate it with other systems," read:

1. [00 - Operating The Module](00-Operating-The-Module.md)
2. [03 - FSMBrain and Machine Flow](03-FSMBrain-and-Machine-Flow.md)
3. [14 - Input Domain](14-Input-Domain.md)
4. [15 - Stats Domain](15-Stats-Domain.md)
5. [07 - Blackboard with Simple Blackboard](07-Blackboard-with-Simple-Blackboard.md)
6. [08 - Character Controller Pro](08-Character-Controller-Pro.md)

If your goal is "I want better debugging and inspection," read:

1. [03 - FSMBrain and Machine Flow](03-FSMBrain-and-Machine-Flow.md)
2. [09 - Debug, History, and Visualizer](09-Debug-History-and-Visualizer.md)
3. [10 - Best Practices and FAQ](10-Best-Practices-and-FAQ.md)

If your goal is "I need to teach another engineer or an AI agent how to use the
domain API correctly," read:

1. [03 - FSMBrain and Machine Flow](03-FSMBrain-and-Machine-Flow.md)
2. [12 - Machine Domain](12-Machine-Domain.md)
3. [13 - States Domain](13-States-Domain.md)
4. [14 - Input Domain](14-Input-Domain.md)
5. [15 - Stats Domain](15-Stats-Domain.md)
6. [06 - Triggers](06-Triggers.md)
7. [07 - Blackboard with Simple Blackboard](07-Blackboard-with-Simple-Blackboard.md)
8. [08 - Character Controller Pro](08-Character-Controller-Pro.md)
