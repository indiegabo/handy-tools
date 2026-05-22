# Conversations Example

This example provides a committed Conversations authoring sample that can be
opened directly from the project without any generation step.

## Included content

- One authored conversation table asset at:
  `Assets/HandyTools/Samples/Conversations Example/ConversationTable_Example.asset`
- One authored sample scene at:
  `Assets/HandyTools/Samples/Conversations Example/Scenes/ConversationsExample.unity`
- One authored `UI Toolkit` presenter prefab at:
  `Assets/HandyTools/Samples/Conversations Example/Prefabs/Conversation Example UI Toolkit Presenter.prefab`
- One authored `Canvas` presenter prefab at:
  `Assets/HandyTools/Samples/Conversations Example/Prefabs/Conversation Example Canvas Presenter.prefab`
- One in-scene `ConversationTrigger` that stores one authored
  `ConversationReference` and plays the selected conversation directly in play
  mode.

## Authoring smoke flow

1. Open `Assets/HandyTools/Samples/Conversations Example/Scenes/ConversationsExample.unity`.
2. Select `ConversationTable_Example.asset` or the `Conversation Example` scene object.
3. Open `HandyTools/Conversations/Conversations Window`.
4. Bind `ConversationTable_Example.asset` if the window did not restore it automatically.
5. Open `Presentation` and confirm the table default presenter points to the
   `UI Toolkit` sample prefab.
6. Select the first conversation and confirm the presentation override points
   to the `Canvas` sample prefab.
7. Select one authored `Spoken Line` or create a `Narration Line`.

## Runtime Play Mode Flow

1. Open `Assets/HandyTools/Samples/Conversations Example/Scenes/ConversationsExample.unity`.
2. Press Play.
3. Confirm the `Conversations Example Presenter` panel appears in the top-left corner.
4. Confirm the first authored conversation starts automatically and presents the current spoken or narration line.
5. Use `Advance`, `Skip`, `Cancel`, and `Restart` to exercise the runtime session.
6. Confirm repeated restarts reuse the same presenter instance instead of creating one new scene object every time.
7. Inspect the trigger authoring after exiting play mode and confirm it still
   points at the same `ConversationReference` selection.

## Runtime In A Built Player

1. Build the project with `Assets/HandyTools/Samples/Conversations Example/Scenes/ConversationsExample.unity` enabled.
2. Launch the built player.
3. Confirm the same presenter panel appears and runs directly from the
   authored `ConversationReference` embedded in the scene.

## Current scope

This example now covers both authoring validation for the shared GraphCore and
basic runtime playback of the authored table through one in-scene trigger plus
both built-in presenter strategies.
The runtime now also demonstrates the persistent presenter-cache behavior: one
conversation stays active at a time, and the presenter is disabled and reused
through the `DontDestroyOnLoad` cache instead of being destroyed after every
session.
It remains intentionally lightweight and is aimed at proving the current
Conversations authoring and linear runtime flow end to end.

Read `Assets/HandyTools/Docs/20-conversation-table-window-and-presenter-prefabs.md`
for the dedicated window and presenter walkthrough.
