# Conversations Module

The `Conversations` module is the current asset-hosted GraphCore-backed
conversation-authoring and authored or exported playback slice in HandyTools. It owns
`ConversationTable` assets, one dedicated graph window, one shared
modules-window configuration panel, deterministic JSON export, build-time
staging into runtime artifacts, one authored-table playback controller
(`ConversationTrigger`), one exported-data playback controller
(`ConversationRunner`), one shared `ConversationSession`, one presenter-prefab
composition root (`ConversationPresenterRoot`), one built-in `UI Toolkit`
presenter, one built-in `Canvas` presenter, and one lightweight runtime
loading surface that proves the same exported artifacts can be read in a built
player.

The current runtime milestone is still intentionally bounded, but it is no
longer line-only. It covers `Entry`, `Spoken Line`, and `Narration Line`
playback plus one first asynchronous action-node slice: `Wait`,
`Emit HandyBus Event`, `Wait For Event`, and authored-runtime `Play Timeline`.
Advance requests still move line nodes forward or complete when no next node
exists, async utility nodes are ticked until they finish, skip or cancel
requests end the current session, table-level default presenter selection,
conversation-level presenter overrides, per-line speaker or listener slot
authoring with `Auto` fallback to speaker-left and listener-right, one
persistent presenter cache rooted under one `DontDestroyOnLoad` container, one
single-active-conversation rule that rejects overlapping activation requests
with an exception, `ConversationReference` authoring for scene-facing
controllers, and one editor-facing table `Display Name` used by pickers and
overlays. Exported runtime stays scene-reference-free, so `Play Timeline`
remains authored-runtime only.

## Current Package State

- Runtime code lives under `Runtime/Scripts/Conversations`.
- Editor code lives under `Editor/Scripts/Conversations`.
- Shared graph-neutral runtime and editor infrastructure still lives in
  `Runtime/Scripts/GraphCore` and `Editor/Scripts/GraphCore`.
- `ConversationsModuleConfigurationPanel` is available in
  `Handy Tools/Modules` for runtime loading strategy, cache capacity, and
  optional StreamingAssets root override.
- `ConversationTableInspector` opens the dedicated authoring window directly
  from any `ConversationTable` asset.
- `ConversationGraphWindow` owns the Conversations-specific authoring layout,
  conversation selection, table-level `Presentation` and `Input` tabs,
  conversation-local presentation override overlay, conversant authoring,
  node inspector, graph canvas, blackboard overlay, and validation tab.
- `ConversationTable` now owns one persisted `Display Name` override used by
  `ConversationReference` pickers, graph-window overlays, and other
  editor-facing labels. Blank values fall back to the asset name.
- New conversations now seed one entry node automatically, and the graph view
  protects the last remaining entry node from accidental deletion.
- The current authored node catalog includes `Entry`, `Spoken Line`,
  `Narration Line`, `Wait`, `Emit HandyBus Event`, `Wait For Event`, and
  authored-runtime `Play Timeline`.
- `ConversationLineNode` currently backs the authored `Spoken Line` node and
  now stores speaker or listener bindings plus optional presenter-slot
  overrides. `ConversationNarrationLineNode` backs `Narration Line`.
- The `ConversationReference` drawer now provides one searchable picker that
  groups authored conversations by table display name and title path.
- The runtime slice registers one cache, one catalog provider, and one loader
  through the shared service locator when the module is active.
- `ConversationTrigger` can play directly from authored
  `ConversationReference` selections backed by `ConversationTable` assets and
  activate the effective presenter prefab from one persistent runtime cache.
- Runtime loading supports `StreamingAssetsOnly`, `AddressablesOnly`, and both
  hybrid fallback strategies.
- Player builds stage only `ConversationTable` assets referenced by enabled
  build scenes, then restore pre-existing project StreamingAssets content after
  the build finishes.
- The shipped `Conversations Example` sample includes one `UI Toolkit`
  presenter prefab, one `Canvas` presenter prefab, one table default
  presenter, and one per-conversation override that prove the presenter system
  stays renderer-agnostic.
- Edit-mode tests cover the builder, exporter, loader factory, streaming loader,
  runtime session behavior, build reference discovery, extension boundaries,
  and first editor regressions.

## Ownership And GraphCore Split

Conversations no longer owns one isolated graph substrate.

- `ConversationGraph` derives from `GraphDefinition`.
- `ConversationNodeBase` derives from `GraphNodeBase`.
- Conversation line text uses the shared GraphCore `GraphValueSource` model.
- The graph blackboard overlay, value-source drawers, variable references,
  family registry, and node canvas shell all come from GraphCore.
- Conversations still owns the asset host, graph family id, conversation table,
  conversant registry, node catalog, validation semantics,
  export semantics, build discovery rules, and runtime payload DTOs.

Use this split as the package rule: move graph-family-neutral behavior into
GraphCore, keep asset-specific conversation behavior inside Conversations.

## Runtime Surface

- `ConversationsModuleDefinition`
- `ConversationsModuleBootstrapper`
- `ConversationTable`
- `ConversationDefinition`
- `ConversationReference`
- `ConversationGraph`
- `ConversationGraphFamily`
- `ConversationActorDefinition`
- `ConversationNodeBase`
- `ConversationEntryNode`
- `ConversationLineNode`
- `ConversationNarrationLineNode`
- `ConversationActionNodeBase`
- `ConversationWaitNode`
- `ConversationEmitHandyBusEventNode`
- `ConversationWaitForEventNode`
- `ConversationPlayTimelineNode`
- `ConversationRuntimeSettings`
- `ConversationLoadingStrategy`
- `ConversationLoaderFactory`
- `ConversationAuthoredRuntimeBuilder`
- `ConversationSession`
- `ConversationTrigger`
- `ConversationAuthoredPlaybackController`
- `ConversationRunner`
- `IConversationPlaybackController`
- `ConversationPresenterRoot`
- `ConversationPresenterComponent`
- `ConversationDefaultPresenter`
- `ConversationCanvasPresenter`
- `IConversationCatalogProvider`
- `IConversationLoader`
- `ConversationLoadResult`
- `ConversationCacheLRU`
- `ConversationRuntimeCatalog`
- `ConversationData`
- `ConversationNodeData`
- `ConversationStringValueData`
- `ConversationTimeMode`
- `ConversationExternalEventRaisedEvent`
- `ConversationActorId`
- `ConversationActorIdBlackboardValue`

## Runtime Reference Cards

### `ConversationTable`

- Asset-facing authoring root for one indexed set of authored conversations.
- Owns the conversation list, shared conversant registry, and module-level
  input defaults.
- Use it when authoring tools, exports, or tests need a stable asset host for
  one or more conversations.
- Do not move runtime playback state or scene-owned transient execution data
  into it; this asset is the source of truth for authored content only.

### `ConversationDefinition`

- Per-conversation authored record stored inside `ConversationTable`.
- Owns the stable conversation id, title path, and graph.
- Guarantees a valid authored graph shell through `EnsureAuthoringIds()` and
  keeps at least one entry-compatible node available.
- Use it when the tooling needs to reason about one authored conversation,
  not when a built player needs the runtime export.

### `ConversationReference`

- Serializable authored pointer to one conversation inside one
  `ConversationTable`.
- Stores one table reference, one stable conversation id, and one cached
  conversation title for resilient editor labels and legacy migration.
- Use it in scene-facing authoring surfaces such as `ConversationTrigger`,
  `ConversationAuthoredPlaybackController`, and cutscene integration nodes.
- Do not author new runtime APIs around raw table-plus-title pairs when this
  type already exists.

### `ConversationActorDefinition`

- Shared speaker registry entry reused by multiple conversations in one table.
- Stores stable actor id, slug-like key, display name, portrait, theme color,
  and notes.
- Use it when one speaker should stay consistent across multiple authored
  conversations.
- Do not duplicate speaker presentation data inside each conversation unless a
  later architecture intentionally adds conversation-local overrides.

### `ConversationRuntimeSettings`

- Project-level runtime settings asset stored in
  `Assets/Resources/HandyTools/Conversations/ConversationRuntimeSettings.asset`.
- Controls runtime loading strategy, default cache capacity, and an optional
  StreamingAssets root override.
- Use it to choose whether built payloads should come from StreamingAssets,
  Addressables, or one hybrid fallback chain.
- Do not hard-code runtime backend assumptions in consumers when the module
  already exposes this settings asset.

### `IConversationCatalogProvider`

- Lightweight runtime boundary that resolves `catalog.json` and payload paths
  or address keys.
- Streaming and Addressables implementations both exist, and hybrid strategies
  are expressed through one fallback provider wrapper.
- Depend on this interface when the task is catalog lookup, path resolution,
  or invalidation.
- Do not treat it as a playback API; it only resolves exported metadata.

### `IConversationLoader`

- Runtime boundary that loads one exported `ConversationData` payload by stable
  conversation id.
- Handles cache reuse, in-flight request deduplication, and backend-specific
  read mechanics.
- Use it when game code or proof tooling needs the built runtime data for one
  conversation.
- Do not mistake it for a presenter or execution service; it loads data, it
  does not yet own dialogue progression.

## What The Runtime Actually Does Today

When the optional module is active, `ConversationsModuleBootstrapper` registers
three default runtime services:

- one `IConversationCache`
- one `IConversationCatalogProvider`
- one `IConversationLoader`

Those services back the exported-runtime path, but the module now ships two
playback entry points on top of them:

- `ConversationTrigger`: reads authored `ConversationTable` data directly in
  the scene.
- `ConversationRunner`: loads exported `ConversationData` through the active
  runtime backend.

Both playback entry points drive the same `ConversationSession` model and
expose the same `IConversationPlaybackController` contract to presenter
prefabs.

That runtime slice currently supports:

- loading the shared runtime catalog
- resolving one conversation payload by stable id
- direct authored playback from one `ConversationReference` without requiring
  one export round-trip
- linear `Entry -> Spoken Line/Narration Line` session start and advance
- `Wait` execution in scaled or unscaled time
- named external event emission through `ConversationExternalEventRaisedEvent`
- `Wait For Event` suspension through HandyBus until one matching named event
  arrives
- authored `Play Timeline` execution through one scene `PlayableDirector`
- automatic session completion when the current line has no `Next` route
- explicit skip and cancel requests that end the active session
- table-level continue, cancel, and skip input bindings with module fallbacks
- table default presenter prefab selection
- per-conversation presenter prefab override resolution
- presenter prefab activation through `ConversationTrigger` from one
  `DontDestroyOnLoad` cache root
- presenter release back into the runtime cache after `Completed`,
  `Canceled`, or `Faulted` states
- one global single-active-conversation rule shared by `ConversationTrigger`
  and `ConversationRunner`
- binding any prefab rooted with `ConversationPresenterRoot` to authored or
  exported playback
- built-in `ConversationDefaultPresenter` (`UI Toolkit`) and
  `ConversationCanvasPresenter` (`uGUI`)
- portrait resolution from shared conversants
- left or right participant presentation driven by
  `ConversationParticipantSlot`
- alternate localization lookup keyed by authored `Text Id`
- loading from StreamingAssets JSON files
- loading from Addressables text assets when the package backend is available
- hybrid fallback between both backends
- retaining idle payloads in the module cache
- returning diagnostics through `ConversationLoadResult`

That runtime slice does not yet ship:

- authored `Choice`, `Branch`, or `Set Blackboard` execution
- scene-agnostic exported equivalents for `Play Timeline`
- richer subtitle formatting, expressions, voice, or animation orchestration
- deeper gameplay, cutscene, or quest integration beyond the current linear
  slice

The built-player proof sample is intentionally explicit about this boundary: it
proves the current linear presentation slice, not a finished branching
dialogue runtime.

## Editor Surface

- `ConversationsModuleConfigurationPanel`
- `ConversationTableInspector`
- `ConversationGraphWindow`
- `ConversationGraphView`
- `ConversationGraphInspectorView`
- `ConversationGraphBlackboardView`
- `ConversationGraphPresentationOverrideView`
- `ConversationConversantsView`
- `ConversationValidationView`
- `ConversationNodeCreationRegistry`
- `ConversationTableValidator`
- `ConversationRuntimeCatalogBuilder`
- `ConversationExporter`
- `ConversationBuildReferenceDiscovery`
- `ConversationBuildExportProcessor`

### Shared Modules Window

Open `Handy Tools/Modules` and select `Conversations` to configure the runtime
loading backend.

The current panel exposes:

- runtime loading strategy
- cache capacity
- optional StreamingAssets root override
- alternate localization overlay root folder name
- optional locale override for alternate localization lookup

The panel is intentionally narrow. Conversations authoring still happens in the
dedicated graph window rather than directly inside the shared modules panel.

### Conversation Table Inspector

Select any `ConversationTable` asset to reach the primary `Open Conversations
Window` button. The inspector also exposes a foldout with the serialized table
for low-level inspection, but the intended workflow is the dedicated window.

### Conversation Graph Window

Open `HandyTools/Conversations/Conversations Window` or use the inspector
button.

The shared table header exposes table binding plus one persisted `Display Name`
override. That display name becomes the primary table label shown by
`ConversationReference` pickers and window overlays; blank values fall back to
the asset name.

The window currently contains five top-level tabs:

- `Conversations`: select, create, delete, rename, and edit one conversation
  graph plus its conversation-local blackboard and presenter override.
- `Presentation`: configure the table default presenter prefab used when the
  selected conversation does not override it.
- `Input`: configure the table-level advance, cancel, and skip actions.
- `Conversants`: maintain the shared speaker registry reused across the table.
- `Validation`: inspect table-wide errors, warnings, and info issues with one
  severity indicator on the tab itself.

The Conversations tab currently exposes:

- table binding and table `Display Name` editing in the shared header
- conversation selector button
- create and delete conversation actions
- selected-conversation path editing
- `Add Node` plus right-click graph creation
- `Show Whole Graph`
- shared GraphCore canvas
- conversation-local blackboard overlay
- conversation-local presenter override overlay
- node inspector bound to the current selection, with boxed `Identity`,
  `Participants`, and `Values` sections for line nodes and generic
  Conversations-authored fields for utility action nodes

The `Presentation` tab currently exposes one table default presenter prefab
field. Individual conversations can override that default from the
Conversations tab overlay.

The `Input` tab currently exposes one table-wide continue, cancel, and skip
binding set. Empty table fields fall back to the module-level defaults.

The Conversants tab currently exposes one table-wide speaker list and detail
editor for:

- stable actor id
- key
- display name
- portrait
- theme color
- notes
- usage references back into authored conversations and nodes

The Validation tab runs table validation through a debounced refresh flow and
surfaces navigable issues grouped by severity.

Read [ConversationTable Window And Presenter Prefabs](20-conversation-table-window-and-presenter-prefabs.md)
for the operational walkthrough of the window and the prefab-authoring path.

## Current Authored Model

The current authoring slice revolves around one `ConversationTable` asset.

One table currently owns:

- one list of `ConversationDefinition` entries
- one shared conversant registry (`ConversationActorDefinition`)
- one authored display-name override used by editor labels and
  `ConversationReference` pickers
- one table-level continue, cancel, and skip input-default set
- one default presenter prefab
- one graph per conversation

Each conversation currently owns:

- one stable conversation id
- one slash-delimited title path used for menus and hierarchy
- one graph with at least one entry node
- one graph-local blackboard
- one optional presenter-prefab override

### Current Authored Node Catalog

The authored node catalog now includes both line nodes and one first
Conversations-specific action-node family.

- `Entry`: required authored start point for one conversation graph.
- `Spoken Line`: text node spoken by one specific conversant, with one
  optional listener, one optional `Next` connection, and one presenter-slot
  selection for each participant.
- `Narration Line`: text node that presents sequential text without speaker or
  listener composition, with one optional `Next` connection.
- `Wait`: delays progression for one authored duration using scaled or
  unscaled time.
- `Emit HandyBus Event`: raises one named
  `ConversationExternalEventRaisedEvent` and then continues through `Next`.
- `Wait For Event`: suspends the session until one matching named event is
  observed through HandyBus.
- `Play Timeline`: plays one scene `PlayableDirector` in authored runtime and
  completes when the director stops.

The current model does not require one explicit terminal node because spoken,
narration, and action nodes can all terminate by omitting their optional
`Next` connection.

The runtime DTO layer still reserves enum slots for `Choice`, `Branch`, and
`SetBlackboard`, but the current builder does not author or export those node
families yet.

## Five-Minute Quickstart

Use this path when the goal is to prove the module is wired correctly without
reverse engineering the tests.

1. Enable `Conversations` in `Handy Tools/Modules` when the module is inactive.
2. Create one `ConversationTable` through
   `Create/HandyTools/Conversations/Conversation Table`.
3. Open the table inspector and click `Open Conversations Window`.
4. Bind the table in the window toolbar when the selection did not carry over.
5. Create one conversation if the table is empty.
6. Confirm the seeded `Entry` node already exists.
7. Click `Add Node` and create one `Spoken Line` or `Narration Line` node.
8. Connect `Entry ->` the created text node.
9. Edit the node text directly in the inspector.
10. Check the `Validation` tab and confirm the table has no blocking issues.
11. Open `Presentation` and assign one presenter prefab when you want the same
    table to be immediately playable in-scene.

Expected result:

- the selected table owns one conversation with one valid entry node
- the authored text node can be `Spoken Line` or `Narration Line`
- the validation tab stays clean for the minimal graph
- the authored graph is exportable into one runtime payload

Read [ConversationTable Window And Presenter Prefabs](20-conversation-table-window-and-presenter-prefabs.md)
for the dedicated authoring-window walkthrough and presenter-prefab recipe.

## Export And Build Flow

Conversations currently exports authored tables into deterministic JSON
artifacts.

The export shape is:

- one `catalog.json`
- one `conversations/<conversation-hex-id>.json` payload per conversation

The current export builder supports `Entry`, `Spoken Line`, `Narration Line`,
`Wait`, `Emit HandyBus Event`, and `Wait For Event`. `Play Timeline` is
intentionally rejected during export because deterministic JSON payloads cannot
serialize scene-bound `PlayableDirector` references.

`ConversationExporter` can write directly to any requested output root and uses
`Application.streamingAssetsPath/HandyTools/Conversations` as the default root.

### Build-Time Staging

Player builds do not export every table in the project automatically.

The current build path is:

1. `ConversationBuildExportProcessor` runs before the player build.
2. If the module is inactive, build staging is skipped.
3. If the module is active, `ConversationBuildReferenceDiscovery` scans enabled
   build scenes.
4. The scan walks serialized scene dependencies and resolves every referenced
   `ConversationTable` asset.
5. The discovered tables are exported into
   `Assets/StreamingAssets/HandyTools/Conversations` so the player build can
   consume them as StreamingAssets content.
6. If the project already owns authored Conversations StreamingAssets content,
   the build pipeline backs it up before staging and restores it after the
   build finishes.
7. If the active runtime loading strategy uses Addressables, the build slice
   also stages generated JSON text assets for the Addressables backend.
8. After the build, temporary staged artifacts are cleaned up and backed-up
   authored content is restored.

This means the build output only contains conversation payloads that are
actually reachable through the enabled build scenes.

## Runtime Loading Strategies

`ConversationLoadingStrategy` currently supports four modes:

- `StreamingAssetsOnly`
- `AddressablesOnly`
- `AddressablesWithStreamingFallback`
- `StreamingWithAddressablesFallback`

Current behavior notes:

- `StreamingAssetsOnly` is the simplest current proof path and matches the
  built-player sample HUD.
- `AddressablesOnly` requires the Unity Addressables editor and runtime backend
  to be available.
- Hybrid strategies build one primary provider plus one fallback provider and
  retry the secondary backend only when the primary load fails.
- The optional StreamingAssets root override is interpreted as either one
  absolute path or one path relative to `Application.streamingAssetsPath`.
- Alternate localization overlays are optional and are looked up under
  `Application.streamingAssetsPath/<overlay-root>/<locale>/conversations`.
- Runtime text resolution now uses authored `Text Id` values and falls back to
  the exported base text when the active locale does not provide one override.

## Validation

`ConversationTableValidator` currently covers both table-level content issues
and graph-level issues.

The validator currently checks:

- empty or duplicate actor ids
- duplicate actor keys
- duplicate conversation ids or conflicting conversation titles
- missing graphs or missing entry nodes
- GraphCore structural issues emitted from the authored graph itself
- spoken line nodes with missing or invalid speaker or listener bindings
- spoken or narration line nodes with empty authored text

The Validation tab groups the results by severity and supports navigation back
to the affected context from the issue surface.

## Tests And CLI Validation

The current edit-mode suite covers these areas:

- `ConversationRuntimeCatalogBuilderTests`
- `ConversationExporterTests`
- `ConversationLoaderFactoryTests`
- `ConversationStreamingLoaderTests`
- `ConversationSessionTests`
- `ConversationBuildReferenceDiscoveryTests`
- `ConversationExternalExtensionTests`
- `ConversationEditorRegressionTests`

The package also ships one Unity CLI entry point:

```text
IndieGabo.HandyTools.Editor.ConversationsModule.Testing.ConversationCliEditModeTestRunner.Run
```

Typical batch invocation shape:

```text
Unity.exe -batchmode -projectPath <project-path> -executeMethod IndieGabo.HandyTools.Editor.ConversationsModule.Testing.ConversationCliEditModeTestRunner.Run -quit
```

The CLI runner executes the Conversations edit-mode assembly synchronously,
logs any leaf failures explicitly, and exits with a non-zero code when the
suite fails.

## Sample Coverage

The shipped sample lives under `Samples/Conversations Example`.

It currently includes:

- one committed `ConversationTable_Example.asset`
- one committed `ConversationsExample.unity` scene
- one committed `Conversation Example UI Toolkit Presenter.prefab`
- one committed `Conversation Example Canvas Presenter.prefab`
- one table-level default presenter and one per-conversation override already
  wired in the sample data
- one scene marker and authored-runtime trigger that anchor the table for
  build reference discovery and direct playback
- one built-player proof path that exercises the exported catalog and payload
  through the selected runtime backend

Read [Conversations Example](../Samples/Conversations%20Example/README.md) for
the sample-specific walkthrough.

## Current Scope And Known Limits

What exists now:

- asset-authored conversation tables
- authored `Display Name` overrides for editor-facing table labels
- `ConversationReference`-based scene authoring and picker workflows
- shared conversant registry
- dedicated graph authoring window with `Conversations`, `Presentation`,
  `Input`, `Conversants`, and `Validation` tabs
- GraphCore-backed blackboard and value sources
- entry, spoken line, narration line, and first utility action-node authoring
- per-line participant slot authoring
- table default and per-conversation presenter prefab selection
- authored runtime playback through `ConversationTrigger`
- authored runtime utility-node execution through `ConversationSession`
- exported runtime playback through `ConversationRunner` and the runtime loader
  stack
- export of wait and named-event nodes with one explicit authored-runtime-only
  `Play Timeline` limit
- presenter prefab composition rooted in `ConversationPresenterRoot`
- built-in `UI Toolkit` and `Canvas` presenters
- deterministic export to runtime JSON DTOs
- StreamingAssets and Addressables catalog loading
- build-scene-based export discovery and player staging
- built-player runtime proof sample
- edit-mode test coverage for the current slice

## Current Delivered Utility Slice

Conversations is no longer just `Entry` plus text nodes.

The shipped action-node family currently includes:

- `Wait`
- `Emit HandyBus Event`
- `Wait For Event`
- `Play Timeline`

Current behavior notes:

- `Wait` stores one literal duration and one `ConversationTimeMode`
  (`Scaled` or `Unscaled`).
- `Emit HandyBus Event` raises one named
  `ConversationExternalEventRaisedEvent` and then continues through `Next`.
- `Wait For Event` suspends the active session until one matching named event
  is observed through HandyBus.
- `Play Timeline` starts one scene `PlayableDirector`, waits for it to stop,
  and then continues through `Next`.
- Action nodes share the same optional `Next` output semantics as the rest of
  the current Conversations flow.
- Authored runtime supports the full set above through
  `ConversationAuthoredRuntimeBuilder` and `ConversationSession`.
- Exported runtime currently supports `Wait`, `Emit HandyBus Event`, and
  `Wait For Event`.
- Export intentionally rejects `Play Timeline` because deterministic runtime
  payloads cannot serialize scene-bound `PlayableDirector` references.

### MVP Node Set

Current MVP target:

- `Entry`
- `Spoken Line`
- `Narration Line`
- utility nodes derived from or strongly aligned with the Cutscenes utility
  surface

`Choice` is explicitly planned, but it is not part of the first MVP.

### Explicit Non-Goal

Quest logic does not belong in the Conversations core module.

- Do not add quest state, quest storage, or quest-specific authored semantics
  directly to the Conversations node core.
- If the project later ships a quest system or a separate quest module,
  Conversations may gain one integration node that talks to that system.
- Keep that integration boundary explicit instead of turning Conversations into
  the owner of quest data.

What is intentionally not shipped yet:

- authored choice, branch, or blackboard-write node families
- richer presenter theming or presentation variants beyond the first built-in
  `UI Toolkit` and `Canvas` runtime surfaces
- play-mode graph execution tracing comparable to the richer Cutscenes slice

## Deferred Backlog After The MVP

These nodes and authoring features are currently registered for later
implementation, but are not part of the first MVP slice unless the plan changes
explicitly.

### Deferred Node Families

- `Choice`
- `Branch`
- `Set Blackboard`
- `Event` or `Command` integration nodes beyond the first utility subset
- `Jump`
- `Call Conversation`
- `Return`
- explicit terminal authoring nodes if the team later decides the graph should
  expose terminal intent visually instead of relying only on implicit terminal
  lines
- `Random`
- comment or note-only authoring nodes

### Deferred Editor Features

- richer node cards with more speaker or presentation metadata visible directly
  in the graph
- graph grouping, bookmarks, and larger-scale authoring affordances
- runtime preview or simulation directly from the graph window
- deeper play-mode tracing comparable to the Cutscenes graph window
- fuller presentation-facing inspector controls for portraits, expressions,
  voice, and subtitle formatting
- broader integration-node catalogs for gameplay, UI, audio, and external
  modules

### Deferred Runtime Features

- one complete choice-selection runtime
- one complete branch or blackboard-write runtime
- one complete portrait, subtitle, or voice presentation stack
- broader utility-node families beyond the current wait, event, and timeline
  slice
- direct ownership of quest or quest-log semantics in the Conversations core

Treat the current module as one complete authoring-and-loading slice, not as a
finished dialogue runtime.

## Guidance For AI Agents

- Keep Conversations asset-hosted. Do not move serialized authoring state into
  scene bootstrap components.
- Keep graph-family-neutral behavior in GraphCore.
- Keep conversation-specific nodes, wrappers, loaders, export rules, and
  validation semantics inside Conversations.
- If you change runtime loading behavior, build export paths, sample proof
  flow, module activation, or the authored node catalog, update this document
  and the package overview in the same change.
