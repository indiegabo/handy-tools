# Scenes Guide

This document covers the shipped `HandyScene` slice of the HandyTools
`Scenes` support layer.

The slice is support-first rather than bootstrap-first. It does not register
one `ModuleDefinition`, does not participate in module activation, and does
not depend on one global runtime service. Its job is narrower and more
scene-centric: keep metadata attached to a regular Unity scene asset, expose
that metadata directly from the selected `SceneAsset` inspector, and resolve
the same typed data from loaded scenes, unloaded editor scenes, and unloaded
player-build scenes.

## Slice Identity

- Slice kind: Utility and support slice.
- Module definition: none.
- Activation model: Always available.
- Runtime assembly: `IndieGabo.HandyTools.Scenes`.
- Editor assembly: `IndieGabo.HandyTools.Scenes.Editor`.
- Sample assembly: `IndieGabo.HandyTools.Scenes.Samples`.
- Edit-mode test assembly: `IndieGabo.HandyTools.Scenes.EditMode.Tests`.
- Current schema version: `1`.
- Primary runtime entry point: `HandySceneReference`.
- Primary editor entry point: `HandySceneEditor`.
- Primary authoring surface: the selected `SceneAsset` inspector.

## Why This Slice Exists

`HandyScene` exists to solve one specific authoring and runtime problem:
attach structured metadata to one scene without creating one parallel
companion asset per scene.

The slice deliberately chooses these constraints:

- one `HandyScene` is still one regular `.unity` scene asset;
- metadata lives inside the scene YAML through one hidden carrier object;
- the scene importer `userData` stores only a lightweight marker;
- consumer projects define their own section types by deriving from
  `SceneExtender`;
- runtime code reads metadata through typed generic APIs instead of parsing
  raw serialized blobs.

This gives the package a few useful properties at the same time:

- scene-local object references survive while the scene is loaded;
- project asset references survive even when the scene is unloaded;
- no catalog asset has to be curated manually per scene;
- inspector authoring stays anchored to the scene asset itself;
- section types remain extensible by game code and sample code.

## Hard Product Rules

These are the stable rules of the current shipped slice.

- `HandyScene` stays a regular `.unity` scene asset.
- Every persistent `.unity` scene can activate any discovered section that is
  visible to its current authoring context.
- The selected `SceneAsset` inspector is the supported authoring surface.
- Metadata is stored on a hidden `HandySceneMetadataCarrier` serialized inside
  the scene YAML.
- Section activation is explicit and per section.
- A scene participates in HandyScene only while it owns at least one active
  section.
- Scene importer `userData` is marker-only and currently uses the
  `HandyTools.Scenes::` prefix.
- The internal importer marker is implementation detail. Users activate
  sections, not scenes.
- Runtime access goes through `HandySceneReference` and
  `HandySceneRuntimeReader`.
- `HandySceneReference.GetSection<T>()` is the strict API and throws when the
  requested section is not active.
- Loaded scenes resolve from the live carrier.
- Unloaded scenes in the editor resolve from one on-demand in-memory snapshot.
- Unloaded scenes in player builds resolve from one temporary build-only
  runtime catalog.
- No per-scene companion asset is part of the shipped design.
- The supported workflow no longer uses one dedicated create, mark, or unmark
  scene action in the inspector.
- The core package does not own level-manager semantics.
- Level-like metadata is valid in samples or game code, not as a core package
  rule.

## Quickstart In Five Minutes

Use this path when the goal is to prove the slice works before reading the
rest of the guide.

1. Define one concrete `SceneExtender` type with one stable
   `HandySceneSectionAttribute` id.
2. Create or select one regular `.unity` scene asset.
3. Select the scene asset in the Project window.
4. Click `Activate <Section Display Name>` for the section you want to persist.
5. Author the section values in the scene inspector.
6. Click `Apply Changes` when you edit section fields.
7. Reference the scene through one serialized `HandySceneReference`.
8. Call `TryGetSection<TSection>()` for optional reads or `GetSection<TSection>()`
   for required reads.
9. Validate with the sample or the CLI runner.

Minimal custom section example:

```csharp
using System;
using IndieGabo.HandyTools.Scenes;
using UnityEngine;

namespace Game.Scenes
{
    [Serializable]
    [HandySceneSection(
        "game.travel-profile",
        DisplayName = "Travel Profile",
        Order = 10)]
    public sealed class TravelProfile : SceneExtender
    {
        [SerializeField]
        private string _entrySpawnId = "spawn.main_gate";

        [SerializeField]
        private bool _allowFastTravel = true;

        public string EntrySpawnId => _entrySpawnId;

        public bool AllowFastTravel => _allowFastTravel;

        public void Configure(string entrySpawnId, bool allowFastTravel)
        {
            _entrySpawnId = entrySpawnId;
            _allowFastTravel = allowFastTravel;
        }
    }
}
```

Minimal reader example:

```csharp
using IndieGabo.HandyTools.Scenes;
using UnityEngine;

namespace Game.Scenes
{
    public sealed class TravelProfileReader : MonoBehaviour
    {
        [SerializeField]
        private HandySceneReference _scene;

        private void Start()
        {
            if (!_scene.TryGetSection(out TravelProfile profile))
            {
                Debug.LogWarning("The target HandyScene does not expose TravelProfile.");
                return;
            }

            Debug.Log(
                $"Spawn: {profile.EntrySpawnId} | " +
                $"Fast Travel: {profile.AllowFastTravel}");
        }
    }
}
```

## API Decision Table

Use this table when choosing which surface should own the next operation.

| Need                                           | Use                                                       | Why                                                                        |
| ---------------------------------------------- | --------------------------------------------------------- | -------------------------------------------------------------------------- |
| Persist one new metadata payload shape         | `SceneExtender` + `HandySceneSectionAttribute`            | Defines one consumer-owned section with one stable persisted id.           |
| Read one required section from gameplay code   | `HandySceneReference.GetSection<T>()`                     | Throws immediately when the section is not active on the referenced scene. |
| Read one optional section from gameplay code   | `HandySceneReference.TryGetSection<T>()`                  | Keeps consumer code typed and scene-centric without forcing exceptions.    |
| Read one section from one known loaded `Scene` | `HandySceneRuntimeReader.TryGetSection<T>(Scene, out T)`  | Reads directly from the live carrier.                                      |
| Read metadata from one unloaded scene path     | `HandySceneRuntimeReader.TryGetSection<T>(string, out T)` | Resolves through one editor snapshot or one build catalog entry.           |
| Load every section from one scene              | `HandySceneRuntimeReader.TryLoadSections(...)`            | Returns the whole resolved section list.                                   |
| Activate one section from editor tooling       | `HandySceneEditor.ActivateSection<T>()`                   | Persists just the requested section on the target scene.                   |
| Deactivate one section from editor tooling     | `HandySceneEditor.DeactivateSection<T>()`                 | Removes just the requested section and clears the carrier if it was last.  |
| Bulk-enable every discovered section           | `HandySceneEditor.MarkAsHandyScene(...)`                  | Compatibility helper that activates all currently discovered sections.     |
| Bulk-disable every discovered section          | `HandySceneEditor.UnmarkAsHandyScene(...)`                | Compatibility helper that deactivates every persisted section.             |
| Drive editor automation                        | `HandySceneEditor.OpenAuthoringSession(...)`              | Gives tools one consistent loaded-scene or preview-scene authoring target. |
| Validate batch edit-mode coverage              | `HandySceneCliEditModeTestRunner.Run`                     | Runs the shipped `Scenes` edit-mode suite through `-executeMethod`.        |

The supported user workflow is still section-first, not scene-first.
Legacy helpers such as `CreateHandyScene(...)` and `MarkAsHandyScene(...)`
exist for compatibility, but the intended path is regular scene creation plus
explicit section activation.

## Mental Model

The current slice is easiest to understand as four aligned pieces.

1. Every scene asset can expose one `Activate <Section>` button per discovered
   section descriptor.
2. Activating one section creates or reuses one hidden in-scene carrier slot
   and keeps the internal importer marker synchronized.
3. One editor snapshot path makes unloaded scene metadata queryable in the
   editor.
4. One build-only runtime catalog makes unloaded scene metadata queryable in
   player builds.

High-level data flow:

```text
SceneAsset (.unity)
  -> inspector activation buttons
    -> Activate Travel Profile
    -> Activate Level Mapping
  -> when one or more sections are active
    -> importer.userData marker: HandyTools.Scenes::
    -> hidden scene object: __HandySceneMetadata
      -> HandySceneMetadataCarrier
        -> _sectionIds[]
        -> _sections[] (SerializeReference SceneExtender payloads)

Runtime lookup
  -> loaded scene: live carrier
  -> unloaded editor scene: in-memory snapshot entry
  -> unloaded player scene: generated Resources catalog entry
```

## Resolution Modes

Not all contexts preserve the same reference fidelity.

| Context                           | Source of truth                            | Scene object references | Project asset references | Notes                                                                      |
| --------------------------------- | ------------------------------------------ | ----------------------- | ------------------------ | -------------------------------------------------------------------------- |
| Loaded scene in editor or runtime | Live `HandySceneMetadataCarrier`           | Preserved               | Preserved                | This is the highest-fidelity path.                                         |
| Unloaded scene in editor          | In-memory snapshot entry                   | Cleared to `null`       | Preserved                | Snapshots sanitize scene-only references because the objects do not exist. |
| Unloaded scene in player build    | Generated `HandySceneRuntimeCatalog` entry | Cleared to `null`       | Preserved                | The build catalog is created only for the build and cleaned afterward.     |

This rule matters whenever a section stores scene objects such as
`GameObject`, `Component`, `Transform`, or custom wrappers around scene-local
identity.

## Runtime Surface

The runtime slice is intentionally small.

- `SceneExtender`
- `HandySceneSectionAttribute`
- `HandySceneMetadataCarrier`
- `HandySceneReference`
- `HandySceneRuntimeReader`
- `HandySceneEditorSnapshotUtility`
- `HandySceneRuntimeCatalog`
- `HandySceneRuntimeCatalogEntry`
- `HandySceneSchema`

### `SceneExtender`

- Base type for every consumer-defined metadata section.
- Serializable by Unity and stored through `SerializeReference` on the carrier.
- Extend this type when authoring one new scene metadata payload.
- Do not put slice orchestration logic here; keep it as data-first payload
  shape plus light helpers when needed.

### `HandySceneSectionAttribute`

- Provides one stable `sectionId` plus optional `DisplayName` and `Order`.
- `sectionId` is the persisted identity that should survive type renames or
  namespace moves.
- `DisplayName` controls the inspector label.
- `Order` controls the relative section ordering in the inspector.

Best practice:

- always declare the attribute even though fallback ids exist;
- choose one id that is stable, explicit, and not tied to one class name;
- keep ids lowercase and domain-shaped, such as
  `game.travel-profile` or `sample.level-mapping`.

### `HandySceneMetadataCarrier`

- Hidden `MonoBehaviour` serialized directly into the scene.
- Stores `_sectionIds` aligned with `_sections`.
- Stores `_schemaVersion` and `_isHandyScene`.
- `_isHandyScene` now reflects whether at least one section is currently
  active on the owning scene.
- This is the actual persisted metadata host.
- Consumer code normally should not touch it directly outside specialized
  editor or migration work.

### `HandySceneReference`

- Serializable reference type used by gameplay code, samples, and other data.
- Stores one editor-only `SceneAsset` object plus normalized path token and
  scene GUID.
- Exposes the high-level generic API:
  `HasSection<T>()`, `TryGetSection<T>()`, `GetSectionOrNull<T>()`, and
  `GetSection<T>()`.
- `GetSection<T>()` throws `InvalidOperationException` when the referenced
  scene does not currently expose that active section.
- This is the preferred consumer-facing runtime surface.

### `HandySceneRuntimeReader`

- Static resolver that decides which metadata source is valid now.
- Supports `HandySceneReference`, scene asset path, and loaded `Scene` entry
  points.
- Invalidates cached snapshots through `InvalidateCachedScene`.
- The reader owns the loaded-versus-unloaded decision, so most code should not
  duplicate that logic.

### `HandySceneEditorSnapshotUtility`

- Editor-only helper compiled only in the editor.
- Opens or reuses one scene, finds the carrier, clones section payloads, and
  sanitizes non-persistent Unity object references.
- Used both by unloaded-scene editor reads and by runtime catalog generation.

### `HandySceneRuntimeCatalog`

- Temporary `ScriptableObject` asset shape used only for builds.
- Loaded from `Resources` at runtime through the stable path
  `HandyTools/Scenes/HandySceneRuntimeCatalog`.
- Stores one list of `HandySceneRuntimeCatalogEntry` values.

### `HandySceneRuntimeCatalogEntry`

- Snapshot of one scene path, guid, name, schema, section ids, and section
  payloads.
- Mirrors the carrier shape, but in build-friendly generated form.

### `HandySceneSchema`

- Centralizes the current serialized schema version.
- The current shipped value is `1`.
- Use this constant when migrations or future persistence changes need one
  stable version boundary.

## Editor Surface

The editor slice owns section activation, authoring, inspector rendering, type discovery,
build staging, and batch validation.

- `HandySceneEditor`
- `HandySceneAuthoringSession`
- `HandySceneMetadataStore`
- `HandySceneSectionTypeCache`
- `HandySceneRuntimeCatalogBuilder`
- `HandySceneRuntimeCatalogBuildProcessor`
- `HandySceneAssetInspector`
- `HandySceneReferencePropertyDrawer`
- `HandySceneCliEditModeTestRunner`

### `HandySceneEditor`

- Public editor API for section activation, section deactivation,
  compatibility bulk activation, and session open.
- The supported workflow no longer ships one dedicated `Create HandyScene`
  menu item.
- Invalidates runtime-reader caches when active-section state changes.
- This is the primary entry for custom editor tooling.

### `HandySceneAuthoringSession`

- Editor authoring abstraction that binds either to the loaded scene or to one
  preview scene.
- Keeps one `SerializedObject` for the carrier.
- Keeps only the currently active section payloads in the carrier.
- Activates and deactivates individual sections on demand.
- Saves back either to the live loaded scene or to the persistent asset
  through one isolated preview-to-persistent copy path.
- Removes the carrier entirely when the last section is deactivated.

### `HandySceneMetadataStore`

- Reads and writes the importer marker through `AssetImporter.userData`.
- Uses the `HandyTools.Scenes::` prefix.
- The marker is synchronized only while the scene has at least one active
  section.
- This store is intentionally marker-only. It does not own the actual section
  payloads.

### `HandySceneSectionTypeCache`

- Discovers `SceneExtender` types through `TypeCache`.
- Excludes abstract and generic definition types.
- Builds descriptors from `HandySceneSectionAttribute` when present.
- Excludes `.Tests` assemblies for regular scenes.
- Includes `.Tests` assemblies only for scenes under `Assets/Tests`.
- Sorts by `Order`, then `DisplayName`, then type name.

### `HandySceneRuntimeCatalogBuilder`

- Builds one in-memory snapshot for validation.
- Creates one temporary asset at the build staging path:
  `Assets/__HandyToolsGenerated/ScenesBuild/Resources/HandyTools/Scenes/HandySceneRuntimeCatalog.asset`.
- Exposes the editor menu `HandyTools/Scenes/Validate Runtime Snapshots`.

### `HandySceneRuntimeCatalogBuildProcessor`

- Hooks the player build pipeline.
- Creates the runtime catalog before the build.
- Cleans the generated asset after the build.
- Throws one `BuildFailedException` if catalog preparation fails.

### `HandySceneAssetInspector`

- Hooks `finishedDefaultHeaderGUI` for persistent `SceneAsset` inspectors.
- Draws one `Activate <Section>` button for every discovered inactive section.
- Draws one boxed payload block for every currently active section.
- Exposes one `Deactivate` button inside each active section block.
- Draws resolved section fields by regular `SerializedProperty` traversal.

### `HandySceneReferencePropertyDrawer`

- Draws `HandySceneReference` as one `SceneAsset` object field.
- Preserves the normalized stored path token and scene GUID.
- Keeps consumer inspectors scene-friendly while runtime still reads one
  stable serialized path.

### `HandySceneCliEditModeTestRunner`

- Dedicated Unity CLI entry point for the `Scenes` edit-mode suite.
- Uses `TestRunnerApi` synchronously.
- Logs a suite summary and any leaf test failures.
- Exits with:
  - `0` when the suite passes,
  - `2` when the suite reports test failures,
  - `1` when the run infrastructure itself fails to produce a result.

## Activating Sections On Any Scene

Every persistent `.unity` scene participates in the same user-facing workflow.
There is no dedicated inspector action to mark the scene itself, and the
supported workflow no longer ships one dedicated `Create HandyScene` menu.

### Inspector Flow

1. Create or select one regular `.unity` scene asset.
2. Select the scene asset in the Project window.
3. The inspector shows one `Activate <Section Display Name>` button for every
   discovered inactive section.
4. Click the button for the section you want.
5. The activation is persisted immediately, the carrier is created if needed,
   and the inspector redraws with the active section block.
6. Edit the fields inside that section block.
7. Click `Apply Changes` to persist field edits.
8. Click `Deactivate` inside a section block when that payload should no
   longer exist on the scene.

If the scene has no active sections, the carrier and the internal importer
marker are removed automatically.

### Programmatic Activation Flow

```csharp
using IndieGabo.HandyTools.Editor.Scenes;
using IndieGabo.HandyTools.Editor.Scenes.Authoring;
using IndieGabo.HandyTools.Scenes;
using UnityEditor;

namespace Game.EditorTools
{
    public static class SceneActivationExample
    {
        public static void ConfigureHubScene()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                "Assets/Game/Scenes/Hub.unity");

            HandySceneReference sceneReference =
                HandySceneReference.FromAsset(sceneAsset);

            if (sceneReference == null || !sceneReference.IsAssigned)
            {
                UnityEngine.Debug.LogError("Could not resolve the target scene.");
                return;
            }

            if (!HandySceneEditor.ActivateSection<HubMetadata>(sceneReference))
            {
                UnityEngine.Debug.LogError("Could not activate HubMetadata.");
                return;
            }

            using HandySceneAuthoringSession session =
                HandySceneEditor.OpenAuthoringSession(sceneReference);

            HubMetadata metadata = session.GetSectionOrNull<HubMetadata>();
            if (metadata == null)
            {
                UnityEngine.Debug.LogError("HubMetadata is not available.");
                return;
            }

            metadata.Configure("hub.central", true);
            session.MarkDirty();
            session.Save();
        }
    }

    [System.Serializable]
    [HandySceneSection("game.hub-metadata", DisplayName = "Hub Metadata")]
    public sealed class HubMetadata : SceneExtender
    {
        [UnityEngine.SerializeField]
        private string _hubId;

        [UnityEngine.SerializeField]
        private bool _allowRespawn;

        public void Configure(string hubId, bool allowRespawn)
        {
            _hubId = hubId;
            _allowRespawn = allowRespawn;
        }
    }
}
```

Compatibility note:

- `HandySceneEditor.MarkAsHandyScene(...)` still exists and now acts as one
  bulk-activation helper for all discovered sections.
- `HandySceneEditor.UnmarkAsHandyScene(...)` now acts as one bulk-deactivation
  helper that removes all active sections.
- These helpers are compatibility affordances, not the primary user-facing
  inspector workflow.

## Inspector Workflow

Once one scene has at least one active section, the selected scene asset
inspector becomes the main authoring surface.

The current inspector flow shows:

- one `Activate <Section>` button for each inactive discovered section;
- one boxed block per active section;
- one `Deactivate` button inside each active section block;
- one action row with `Apply Changes` and `Revert` for field edits.

### Editing Modes

The authoring session automatically selects one of two modes.

| Mode          | When it happens                                | What the inspector edits                               |
| ------------- | ---------------------------------------------- | ------------------------------------------------------ |
| Loaded Scene  | The target scene is already open in the editor | The live scene carrier in the loaded scene instance    |
| Preview Scene | The target scene is not loaded                 | One isolated preview scene that is copied back on save |

### Button Semantics

| Button               | Loaded Scene                                                                                 | Preview Scene                                                                                                  |
| -------------------- | -------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `Activate <Section>` | Adds the requested section to the live carrier, saves the scene, and synchronizes the marker | Adds the requested section in preview, copies it into the persistent scene, saves, and synchronizes the marker |
| `Deactivate`         | Removes the requested section, saves immediately, and clears the carrier if it was the last  | Removes the requested section from preview, persists the removal, and clears the carrier if it was the last    |
| `Apply Changes`      | Saves field edits on the live scene carrier                                                  | Copies preview field edits into the persistent scene and saves                                                 |
| `Revert`             | Disabled                                                                                     | Closes the preview session and reopens from disk on the next draw                                              |

Activation and deactivation save immediately.
`Apply Changes` is only needed after editing serialized fields inside already
active sections.

### Important Authoring Rule

Section activation is explicit.

That means:

- every scene can activate any discovered concrete `SceneExtender` visible to
  the current authoring context;
- inactive sections are absent from the carrier and from runtime resolution;
- per-scene section selection is opt-in and happens one section at a time;
- test-only sections are filtered away from regular project scenes.

The internal importer marker still exists, but it is derived from active
section presence rather than one separate scene-level toggle.

## Defining Section Types

The intended extension point is simple: projects create their own concrete
`SceneExtender` types.

Recommended authoring rules:

- mark the type `[Serializable]`;
- derive from `SceneExtender`;
- declare one stable `HandySceneSectionAttribute` id;
- keep fields Unity-serializable;
- expose read-only properties or helper methods for consumers;
- avoid turning section types into orchestration services.

Example with both asset and scene-local references:

```csharp
using System;
using IndieGabo.HandyTools.Scenes;
using UnityEngine;

namespace Game.Scenes
{
    [Serializable]
    [HandySceneSection(
        "game.encounter-profile",
        DisplayName = "Encounter Profile",
        Order = 30)]
    public sealed class EncounterProfile : SceneExtender
    {
        [SerializeField]
        private string _encounterId = "encounter.bandits.bridge";

        [SerializeField]
        private AudioClip _musicCue;

        [SerializeField]
        private Transform _fallbackSpawnPoint;

        public string EncounterId => _encounterId;

        public AudioClip MusicCue => _musicCue;

        public Transform FallbackSpawnPoint => _fallbackSpawnPoint;
    }
}
```

Reference behavior for that example:

- `MusicCue` survives loaded and unloaded reads because it is one persistent
  asset reference.
- `FallbackSpawnPoint` survives only while the scene is loaded.
- unloaded reads return `null` for `FallbackSpawnPoint` by design.

### Duplicate Section Ids

If two section types publish the same `sectionId`, the cache logs one warning
and ignores the later type.

Treat duplicate ids as one correctness bug, not one supported override model.

### Attribute Fallbacks

If the attribute is omitted entirely, the section cache falls back to:

- section id: `type.FullName` or `type.Name`;
- display name: `ObjectNames.NicifyVariableName(type.Name)`;
- order: `0`.

This fallback exists only so the system remains resilient.
Projects should still declare the attribute explicitly.

## Section Discovery And Test Scoping

`HandySceneSectionTypeCache` resolves section descriptors with a few important
rules.

- concrete derived types only;
- abstract types are ignored;
- generic type definitions are ignored;
- regular scenes exclude assemblies whose name contains `.Tests`;
- scenes under `Assets/Tests` include those test assemblies.

This gives the slice one clean split:

- production scenes only see production section types;
- test scenes can author special validation-only sections;
- test-only sections are filtered out of normal authoring and build snapshot
  generation.

That filtering is deliberate protection against leaking test-managed reference
types into player data.

## Supported Payload Shapes

The inspector draws section fields through regular `SerializedProperty`
traversal, so it inherits Unity serialization and the project's existing
property drawers.

The shipped validation proves support for these categories:

- primitive values and strings;
- enums and structs handled by Unity serialization;
- `UnityEngine.Object` asset references;
- scene object references when the scene is loaded;
- project payload types that already ship custom property drawers;
- `SceneField` payloads;
- `GuidReference` payloads.

What to expect:

- if the existing type already serializes correctly in Unity and already has a
  working property drawer, it should usually render correctly inside one
  section;
- unloaded-scene reads still sanitize scene-only object references to `null`;
- persistent asset references remain intact through snapshots and build
  catalogs.

## Runtime Reading Patterns

### Preferred Consumer API: `HandySceneReference`

This type now exposes both strict and non-strict read paths.

Use the strict getter when the section is required.

```csharp
TravelProfile profile = _sceneReference.GetSection<TravelProfile>();
Debug.Log(profile.EntrySpawnId);
```

If `TravelProfile` is not active on the referenced scene, that call throws
`InvalidOperationException`.

Use the non-throwing APIs when section presence is optional.

```csharp
if (_sceneReference.HasSection<TravelProfile>())
{
    TravelProfile profile = _sceneReference.GetSectionOrNull<TravelProfile>();
    Debug.Log(profile.EntrySpawnId);
}
```

### Explicit Generic Try-Get

```csharp
if (!_sceneReference.TryGetSection(out TravelProfile profile))
{
    Debug.LogWarning("TravelProfile is missing.");
    return;
}

Debug.Log($"Spawn id: {profile.EntrySpawnId}");
```

### Read From One Scene Path

Use this path in systems that know one project-relative scene asset path but do
not own one serialized `HandySceneReference`.

```csharp
using IndieGabo.HandyTools.Scenes;

if (HandySceneRuntimeReader.TryGetSection(
        "Assets/Game/Scenes/Hub.unity",
        out TravelProfile profile))
{
    Debug.Log(profile.EntrySpawnId);
}
```

### Read From One Loaded `Scene`

Use this path when the caller already owns one loaded additive scene handle.

```csharp
using IndieGabo.HandyTools.Scenes;
using UnityEngine.SceneManagement;

Scene loadedScene = SceneManager.GetSceneByPath("Assets/Game/Scenes/Hub.unity");

if (HandySceneRuntimeReader.TryGetSection(loadedScene, out TravelProfile profile))
{
    Debug.Log(profile.EntrySpawnId);
}
```

### Load Every Section

Use this only when the caller truly needs the whole section list.

```csharp
using IndieGabo.HandyTools.Scenes;

if (HandySceneRuntimeReader.TryLoadSections(_sceneReference, out var sections))
{
    foreach (SceneExtender section in sections)
    {
        Debug.Log(section.GetType().Name);
    }
}
```

## Reference Survival Rules In Practice

These examples reflect the current shipped behavior.

### Example: Loaded Scene

If one section stores:

- one `GameObject` scene anchor,
- one `ScriptableObject` asset reference,
- one string,
- one integer,

then a loaded-scene read keeps all four values available.

### Example: Unloaded Editor Scene

The same section resolved from one unloaded editor scene keeps:

- the `ScriptableObject` asset reference,
- the string,
- the integer,

but clears:

- the `GameObject` scene anchor.

### Example: Unloaded Player Build Scene

The player build behaves like the unloaded editor path.

This is the reason sample and game code should avoid depending on scene-local
references unless the scene is actually loaded.

## Build And Player Workflow

### What Happens During A Build

Before the build:

1. `HandySceneRuntimeCatalogBuildProcessor` calls
   `HandySceneRuntimeCatalogBuilder.PrepareBuildCatalogAsset()`.
2. The builder finds all scenes that currently own at least one active
   section.
3. It snapshots each scene into one `HandySceneRuntimeCatalogEntry`.
4. It writes one temporary asset into the generated folder under `Resources`.

After the build:

1. the build processor calls `CleanupBuildCatalogAsset()`;
2. the temporary generated asset is removed again.

### Generated Build Path

The temporary asset path is:

`Assets/__HandyToolsGenerated/ScenesBuild/Resources/HandyTools/Scenes/HandySceneRuntimeCatalog.asset`

This is implementation detail, not one asset that should be curated manually.

### Manual Validation Menu

The editor menu `HandyTools/Scenes/Validate Runtime Snapshots` builds one
transient in-memory snapshot and logs how many entries were validated.

Use this menu when the goal is to sanity-check unloaded-scene coverage without
building a player.

## Sample Tour

The shipped sample currently lives at:

`Assets/HandyTools/Samples/HandyScene Example`

The sample scene ships with both sample sections already active so the runtime
logger can prove the full flow without extra setup.

Included content:

- `Scenes/HandySceneExample.unity`
- `Scripts/LevelMapping.cs`
- `Scripts/SceneTravelProfile.cs`
- `Scripts/HandySceneMetadataConsoleLogger.cs`
- `README.md`

### Sample Section Defaults

`LevelMapping` ships with:

- level code `Level-01`;
- recommended power `5`;
- one sample scene-object reference field.

`SceneTravelProfile` ships with:

- entry spawn id `spawn.main_gate`;
- fast travel enabled by default.

### Sample Logger Behavior

`HandySceneMetadataConsoleLogger` does three things.

- resolves the selected `HandySceneReference` in `Start()`;
- logs each serialized field to the Console;
- mirrors the same values into one runtime overlay canvas.

This sample is intentionally small.
Its purpose is to prove the core flow, not to define one gameplay framework.

### Expected Console Shape

The exact object-reference text depends on the authored scene, but the output
shape should look like this:

```text
HandyScene 'HandySceneExample' | LevelMapping.LevelCode = Level-01
HandyScene 'HandySceneExample' | LevelMapping.RecommendedPower = 5
HandyScene 'HandySceneExample' | SceneTravelProfile.EntrySpawnId = spawn.main_gate
HandyScene 'HandySceneExample' | SceneTravelProfile.AllowFastTravel = True
```

## Validation Surface

The current slice is covered by editor diagnostics, one package sample, one
runtime overlay sample reader, one dedicated CLI runner, and one focused
edit-mode suite.

### Current Edit-Mode Coverage

The shipped `Scenes` edit-mode suite verifies these public behaviors.

1. explicit section activation persists only the requested section payloads;
2. loaded-scene reads preserve live scene references while unloaded editor
   reads sanitize them;
3. preview-scene authoring saves correctly and deactivating the last section
   removes the carrier;
4. strict section reads throw when the requested section is not active;
5. test-only section discovery is scoped to scenes under `Assets/Tests`;
6. `SceneField` and `GuidReference` payloads roundtrip through section data;
7. build catalog creation exists only during build preparation and is removed
   during cleanup.

### CLI Runner Command

The dedicated runner entry point is:

`IndieGabo.HandyTools.Editor.Scenes.Testing.HandySceneCliEditModeTestRunner.Run`

Typical batch invocation:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe" `
  -batchmode `
  -quit `
  -nographics `
  -projectPath "C:\Path\To\Project" `
  -executeMethod "IndieGabo.HandyTools.Editor.Scenes.Testing.HandySceneCliEditModeTestRunner.Run"
```

Operational guidance:

- close the interactive Unity editor before running batch validation;
- keep unrelated asset import errors fixed, because batch import happens
  before the suite executes;
- treat exit code `0` as success;
- treat exit code `2` as suite failure;
- treat exit code `1` as infrastructure or pre-suite failure.

## Troubleshooting

### The HandyScene controls do not appear in the scene asset inspector

Check these in order:

1. the selected asset is one persistent `.unity` scene asset;
2. the editor assembly compiled successfully;
3. there is at least one discovered `SceneExtender` type visible to the
   current scene path;
4. there is no broader Unity import failure preventing the editor from
   completing domain reload.

### An `Activate <Section>` button is missing from one normal project scene

Possible causes:

- the type is abstract or generic;
- the assembly name contains `.Tests` and the scene is not under `Assets/Tests`;
- the type does not derive from `SceneExtender`;
- the editor has not reloaded since the new type was added.

### `HandySceneReference.GetSection<T>()` throws

That means the requested section is not currently active on the referenced
scene.

Use the scene asset inspector to activate that section, or switch the caller
to `TryGetSection<T>()` when the section is optional.

### Runtime returns `null` for one scene object reference

That is expected when the scene is unloaded.

Only the live loaded-scene path preserves scene-local objects.
Use persistent asset references or explicit scene loading if the data must stay
available while the scene remains unloaded.

### The sample logger warns that one section could not be resolved

Usually this means the inspector changes were not saved.

Select the scene asset, click `Apply Changes`, and run again.

### Batch validation exits before the suite starts

Check these in order:

1. the interactive editor is closed;
2. there are no unrelated asset import failures in the project;
3. the `Scenes` editor assembly compiled cleanly;
4. the batch log reaches the `Running HandyScene edit-mode tests` line.

### A build cannot resolve unloaded-scene metadata

Check these in order:

1. the scene currently owns at least one active section;
2. the build includes the editor assembly that owns the build processor;
3. the build completed the pre-build catalog generation step;
4. the requested section contains only data that can survive snapshotting when
   the scene is unloaded.

## FAQ

### Is `Scenes` one HandyTools module?

No.

It is one always-available support slice with runtime and editor asmdefs.
It does not own one `ModuleDefinition` or one runtime bootstrapper.

### Why use both the importer marker and the hidden carrier?

They solve different problems.

- the importer marker cheaply answers whether the scene currently owns at
  least one active section and should participate in the workflow;
- the hidden carrier stores the actual section payloads inside the scene YAML.

### Why not one companion `ScriptableObject` asset per scene?

Because the shipped design deliberately avoids per-scene asset pollution and
needs to preserve scene-local references while the scene is loaded.

### Can one section store level-specific or game-specific data?

Yes, in project code or sample code.

What the package does not do is impose level-manager semantics on the core
slice itself.

### Can I rename one section class later?

Yes, if the stable `sectionId` remains the same.

That id is the persistence identity that should survive refactors.

### Can I use existing custom property drawers inside sections?

Yes.

The inspector traverses child `SerializedProperty` values, so existing drawer
behavior continues to apply.

### Does every scene get every discovered section?

No.

Every scene can activate any discovered section that is valid for its current
authoring context, but only the active sections are persisted and resolved at
runtime.

### Do I still need to mark the scene as a HandyScene?

No, not in the supported user-facing workflow.

Create or select one regular scene and activate the sections you want.
The internal marker is synchronized automatically.

## Guidance For AI Agents

Use these rules when editing the slice.

- Treat `Scenes` as one support slice, not one module.
- Do not add one `ModuleDefinition`, bootstrapper, or modules-window panel
  unless the architectural scope explicitly changes.
- Preserve the no-companion-asset rule.
- Preserve explicit per-section activation on any persistent scene asset.
- Do not reintroduce one dedicated create, mark, or unmark scene UX in the
  inspector.
- Do not auto-synchronize all discovered sections into every scene.
- Keep the selected `SceneAsset` inspector as the main authoring surface.
- Keep the importer marker lightweight, derived from active-section presence,
  and the carrier authoritative.
- Prefer `HandySceneReference` in consumer code instead of duplicating reader
  routing logic.
- Use `GetSection<T>()` only when the section is required and absence should
  throw. Use `TryGetSection<T>()` or `HasSection<T>()` when absence is valid.
- Preserve the loaded-versus-unloaded reference rules.
- Keep test-only section types scoped to `Assets/Tests` scenes.
- Keep build-only catalog assets temporary and cleaned afterward.
- Keep level-specific semantics out of the core slice.
- Update this guide whenever persistence, authoring, section discovery,
  snapshotting, build behavior, or validation entry points change.

## Current Shipped State

The current HandyScene slice is implemented around explicit per-section
activation, documented, sample-backed, and validated through the shipped
`Scenes` edit-mode CLI runner.
