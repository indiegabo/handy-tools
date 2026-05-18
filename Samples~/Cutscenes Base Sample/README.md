# Cutscenes Base Sample

This sample demonstrates the base non-dialogue cutscene workflow, including a
runtime flow that writes and consumes graph blackboard values through built-in
nodes and starts through one scene-authored trigger.

Open `Scenes/CutscenesBaseSample.unity` and press Play.

`CutsceneTrigger` on the root sample object starts the graph automatically in
`Start`, so the scene demonstrates both the director-owned graph and the
standard trigger path without custom gameplay glue.

The scene-authored graph exercises:

- `Entry`
- `Set Blackboard Values`
- `Log Blackboard Value`
- `Set GameObject Active` using blackboard-bound sources
- `Wait` using scaled time
- `Wait` using unscaled time
- `Set GameObject Active`
- `Finish`

Open the graph window and inspect the `Blackboard` foldout to author graph
defaults directly. The sample runs out of the box because the first native
writer node seeds the same keys at runtime before the blackboard consumer
nodes read them, and the second wait demonstrates that the flow can continue
with unscaled time after the global time scale is paused.
