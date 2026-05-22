# Cutscenes Conversations Example

This sample demonstrates one scene-authored cutscene that pauses twice for the
new `Conversations/Start Conversation` node.

Open `Scenes/CutscenesConversationsExample.unity` and press Play.

The scene includes:

- One authored cutscene graph that starts automatically at runtime.
- One authored `ConversationTable` with two registered conversations and three
  shared actors.
- One sample `UI Toolkit` presenter prefab scoped to this sample folder.
- One sample installer that creates a simple stage and starts the cutscene.

The cutscene flow exercises:

- `Entry`
- `Log`
- `Wait`
- `Conversations/Start Conversation` for `Act 1/Briefing`
- `Log`
- `Wait`
- `Conversations/Start Conversation` for `Act 1/Checkpoint`
- `Log`
- `Finish`

The cutscene pauses on each `Conversations/Start Conversation` node until the
operator advances, skips, or cancels the active conversation through the bound
presenter or authored input actions. This keeps the sample aligned with the
intended manual-control flow.

The shared actors intentionally ship without portrait sprites so project teams
can replace them with their own assets after importing the sample.
