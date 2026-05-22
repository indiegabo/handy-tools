# Command Pattern Example

This sample scene demonstrates the first shipped HandyTools `Command Pattern`
workflow in a project-facing format.

If you want to author your own commands after exploring the sample, read
`../../Docs/16-command-pattern-guide.md` and start with the `Quickstart In Five
Minutes` section.

## Included Scene

- `Scenes/CommandPatternExample.unity`

## What It Shows

- immediate movement commands;
- next-frame, scaled-delay, and unscaled-delay scheduling;
- undo and redo in one explicit history scope;
- persistent trail visualization for executed grid steps; and
- one vertical in-game request list rendered through IMGUI.

## Scene Controls

- `Up`, `Down`, `Left`, `Right` submit immediate commands.
- `Next Frame Up`, `Scaled Right`, and `Unscaled Left` submit scheduled work.
- `Undo` and `Redo` exercise command history.
- `Reset` restores the actor and clears the request list.

The runtime monitor window remains available separately at
`HandyTools/Command Pattern/Monitor` during play mode.

The sample controller and command implementation are the fastest package-local
references when validating how `CommandRequest`, scheduling, and scope-based
undo and redo are meant to be consumed.
