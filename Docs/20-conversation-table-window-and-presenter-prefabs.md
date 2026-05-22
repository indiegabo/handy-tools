# ConversationTable Window And Presenter Prefabs

This guide explains the practical authoring workflow around one
`ConversationTable` asset. Use it when the task is operating the dedicated
Conversations window, deciding where a setting belongs, or building one
presenter prefab that should work with the current runtime controllers.

## What Lives Where

- `ConversationTable`: asset root. Owns the indexed conversations, shared
  conversants, one optional editor-facing `Display Name`, table-level input
  defaults, and the table default presenter prefab.
- `ConversationDefinition`: one conversation stored inside the table. Owns the
  conversation path, graph, and optional presenter-prefab override.
- `ConversationReference`: serialized authored pointer to one conversation in
  one table. Scene-facing controllers should use this instead of raw table plus
  title fields.
- `ConversationLineNode`: one spoken line. Owns the text, speaker, optional
  listener, and the presenter-slot choice for each participant.
- `ConversationTrigger`: scene controller that plays directly from one authored
  `ConversationReference` and activates the effective presenter prefab from the
  runtime cache.
- `ConversationRunner`: scene controller that loads exported runtime data and
  drives the same presenter contract.

Keep this boundary clear while authoring. Table-level settings are shared by
every conversation in the asset. Conversation-level settings affect only one
conversation. Node-level settings affect only the selected line or graph node.

## Opening The Window

1. Create one table through
   `Create/HandyTools/Conversations/Conversation Table`.
2. Select the asset and click `Open Conversations Window`, or open
   `HandyTools/Conversations/Conversations Window`.
3. Bind the target table from the shared header when the selection did not carry
   over automatically.

The table inspector is the entry point. The dedicated window is the real
authoring surface.

## Window Workflow

### Shared Table Header

This header stays visible regardless of the selected tab.

- `Table` binds the active `ConversationTable`.
- `Display Name` stores one optional human-readable label on the table asset.
- When `Display Name` is empty, editor surfaces fall back to the asset name.
- `ConversationReference` pickers use the table display name as the primary
  label and group authored conversations as
  `<table-display-name>|<groups>|<conversation-name>`.

### `Conversations`

This tab is the day-to-day authoring workspace.

- Use the shared header to bind the table and edit its `Display Name`. Use the
  tab toolbar to pick the active conversation, create or delete conversations,
  add nodes, and frame the graph.
- Use the selected-conversation header to edit the conversation path. `/` and
  `|` both create hierarchy segments; the last segment becomes the visible
  conversation name.
- Use `Add Node` or right-click the graph canvas to create and connect
  `Spoken Line`, `Narration Line`, `Wait`, `Emit HandyBus Event`,
  `Wait For Event`, and `Play Timeline` nodes from `Entry`.
- Use the blackboard overlay for graph-local values referenced by GraphCore
  value sources.
- Use the presentation override overlay to assign one presenter prefab only
  for the selected conversation.
- Use the node inspector to edit the selected line or utility node.

`Spoken Line` currently organizes its inspector into three sections:

- `Identity`
- `Participants`
- `Values`

`Participants` is where you bind the shared `Speaker` and optional `Listener`
and choose a `Presenter Slot` for each.

Slot behavior:

- `Auto`: speaker resolves to the left slot and listener resolves to the right
  slot.
- `Left`: force this participant into the left presenter slot.
- `Right`: force this participant into the right presenter slot.

Use `Auto` by default. Only force one side when the line intentionally flips
the visual composition.

### `Presentation`

This tab configures the table default presenter prefab.

- Any conversation without its own override uses this prefab.
- The assigned prefab must contain `ConversationPresenterRoot`.
- The renderer is not fixed. The prefab can use `UI Toolkit`, `Canvas`, or
  another composition that binds through the shared presenter contract.

### `Input`

This tab configures the table-level actions that drive playback.

- `Advance Action`
- `Cancel Action`
- `Skip Action`

Leave fields empty when the table should fall back to the module defaults
configured in `Handy Tools/Modules`.

### `Conversants`

This tab manages the shared actor registry reused by every conversation in the
table.

Each conversant can currently store:

- key
- display name
- portrait
- theme color
- notes

Spoken-line speaker and listener bindings always resolve from this shared
registry. Portraits shown by presenters also come from these conversants.

### `Validation`

Use this tab before play mode, export, or build validation.

It surfaces table-wide and graph-level issues such as:

- missing speaker bindings on spoken lines
- empty spoken or narration text
- duplicate actor keys
- duplicate conversation ids or conflicting titles
- missing entry-node or graph structure problems

Treat it as the last pass before you decide the table is ready.

## Recommended Authoring Flow

1. Create or bind the target `ConversationTable`.
2. Add the shared conversants first.
3. Create one conversation and keep the seeded `Entry` node.
4. Add and connect `Spoken Line` or `Narration Line` nodes.
5. Fill text and participant bindings in the node inspector.
6. Leave `Presenter Slot` on `Auto` unless the line must intentionally flip
   the visual side.
7. Fix validation warnings and errors.
8. Assign the table default presenter in `Presentation`.
9. Add one conversation-specific presenter override only when a conversation
   truly needs a different UI.
10. Put `ConversationTrigger` in the scene and assign one
    `ConversationReference` that points at the authored conversation you want
    to play.

## Presenter Prefab Architecture

The effective presenter prefab resolves in this order:

1. `ConversationDefinition.PresenterOverridePrefab`
2. `ConversationTable.DefaultPresenterPrefab`

`ConversationTrigger` uses that effective prefab, resolves one cached runtime
instance for it, binds it to itself through `ConversationPresenterRoot`, and
disables it back into the cache when the session completes, cancels, or
faults.

The current runtime no longer destroys presenter instances after each
conversation. It keeps one cached instance per prefab under one persistent
`DontDestroyOnLoad` cache root, rebinds that instance when the same prefab is
requested again, and disables it when the session completes, cancels, or
faults.

`ConversationRunner` implements the same
`IConversationPlaybackController` contract for exported-runtime playback. A
presenter prefab that works with `ConversationTrigger` should also work with
`ConversationRunner` when it depends only on the shared controller contract.

The runtime also enforces one business rule globally: only one conversation
may be active at a time. If any controller requests activation while another
conversation is still active, the runtime throws an exception instead of
starting a second presenter or session.

### Required Root Component

Every presenter prefab root must contain `ConversationPresenterRoot`.

- It propagates the active playback controller to every
  `ConversationPresenterComponent` found under the prefab hierarchy.
- Without it, the prefab can instantiate but it will not bind cleanly to the
  Conversations runtime.

In normal authored prefabs, leave the explicit controller override empty. The
runtime controllers bind themselves programmatically.

### Built-In Presenter Components

- `ConversationDefaultPresenter`: built-in `UI Toolkit` presenter.
- `ConversationCanvasPresenter`: built-in `uGUI` presenter.

These components already understand the shared playback contract, current line
text, actor portraits, left or right participant slots, and the table input
action labels.

## Creating A `UI Toolkit` Presenter Prefab

1. Create one prefab root GameObject.
2. Add `ConversationPresenterRoot`.
3. Add `UIDocument`.
4. Add `ConversationDefaultPresenter`.
5. Assign one `PanelSettings` asset to the `UIDocument`.
6. Save the prefab.
7. Assign it in the table `Presentation` tab or in the conversation override
   overlay.

Notes:

- The built-in `UI Toolkit` presenter builds its visual tree at runtime, so
  the prefab does not need one handcrafted panel hierarchy to start working.
- Use
  `Assets/HandyTools/Samples/Conversations Example/Prefabs/Conversation Example UI Toolkit Presenter.prefab`
  as the reference implementation.

## Creating A `Canvas` Presenter Prefab

1. Create one prefab root GameObject.
2. Add `Canvas`, `CanvasScaler`, and `GraphicRaycaster`.
3. Add `ConversationPresenterRoot`.
4. Add `ConversationCanvasPresenter`.
5. Create the UI hierarchy and wire the serialized fields.
6. Save the prefab.
7. Assign it in the table `Presentation` tab or in the conversation override
   overlay.

The built-in `Canvas` presenter expects explicit references for these fields:

- title text
- speaker panel
- speaker portrait image
- speaker name text
- listener panel
- listener portrait image
- listener name text
- line text
- status text
- action hints text

Notes:

- A pretty hierarchy alone is not enough. The serialized references must be
  wired.
- Use
  `Assets/HandyTools/Samples/Conversations Example/Prefabs/Conversation Example Canvas Presenter.prefab`
  as the reference implementation.

## Practical Rules For Presenter Prefabs

- Do not parent the presenter under the trigger manually. The current
  authored-runtime flow keeps presenters under one persistent
  `DontDestroyOnLoad` cache root and reuses them automatically.
- Do not move conversation execution state into the prefab. The prefab should
  stay as presentation only.
- Do not duplicate actor portraits, names, or default slot assumptions inside
  the prefab. Those values already resolve from the table and active node.
- Use the table default presenter for the common case. Reserve
  conversation-level overrides for genuinely different layouts.
- Do not design gameplay around multiple simultaneous conversations. The
  runtime rejects overlapping activations by design.
- Do not expect exported JSON playback to carry scene-bound `Play Timeline`
  nodes. That node is authored-runtime only because it depends on one scene
  `PlayableDirector` reference.

## Troubleshooting

- No presenter appears:
  assign the table default or conversation override, confirm the prefab root
  has `ConversationPresenterRoot`, and verify the scene object uses
  `ConversationTrigger` with one valid `ConversationReference` or
  `ConversationRunner` with one valid exported conversation id.
- Presenter appears but stays blank:
  validate the graph, confirm the selected conversation can reach one line, and
  make sure the line text is not empty.
- Wrong participant side:
  inspect the `Spoken Line` node and set `Presenter Slot` back to `Auto`,
  `Left`, or `Right` as intended.
- Portrait is missing:
  confirm the portrait is authored on the shared conversant, not only implied
  by the node.
- Conversation ends but the UI still remains:
  authored-table playback through `ConversationTrigger` should disable the
  active presenter and return it to the runtime cache automatically. If you
  are using another custom controller, mirror that same lifecycle.
- Export fails on `Play Timeline`:
  this is expected. Keep `Play Timeline` in authored-runtime flows driven
  directly from `ConversationTable` data, or replace it with one exportable
  scene-agnostic gameplay signal for built runtime payloads.

## Sample Paths

- Table asset:
  `Assets/HandyTools/Samples/Conversations Example/ConversationTable_Example.asset`
- Sample scene:
  `Assets/HandyTools/Samples/Conversations Example/Scenes/ConversationsExample.unity`
- `UI Toolkit` presenter prefab:
  `Assets/HandyTools/Samples/Conversations Example/Prefabs/Conversation Example UI Toolkit Presenter.prefab`
- `Canvas` presenter prefab:
  `Assets/HandyTools/Samples/Conversations Example/Prefabs/Conversation Example Canvas Presenter.prefab`

Start from those assets when the goal is speed. They already prove the current
table default plus conversation override workflow end to end.
