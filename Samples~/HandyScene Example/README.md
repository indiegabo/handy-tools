# HandyScene Example

This example provides one minimal pre-authored scene asset with two active
HandyScene metadata sections and two project-facing `SceneExtender` examples.

## Included content

- One ready-to-click scene asset at:
  `Assets/HandyTools/Samples/HandyScene Example/Scenes/HandySceneExample.unity`
- One `LevelMapping` metadata section sample.
- One `SceneTravelProfile` metadata section sample.

## Inspector smoke flow

1. Select `Scenes/HandySceneExample.unity` in the Project window.
2. Confirm the inspector renders the active `Level Mapping` section.
3. Confirm the inspector renders the active `Travel Profile` section.
4. Confirm each section exposes exactly two serialized fields.
5. Optionally click `Deactivate` on one section and confirm the matching
   `Activate <Section>` button appears.
6. Reactivate that section and confirm its block returns.
7. Optionally edit a value and click `Apply Changes` to write the updated
   carrier back to the scene file.

## Current scope

This sample is intentionally small.
Its job is to prove the section-by-section scene-asset inspector workflow and
one minimal runtime lookup path for consumer-defined scene metadata sections.

## Runtime contract

- Loaded HandyScenes read directly from the hidden in-scene carrier and keep
  Unity scene-object references alive.
- In the editor, unloaded HandyScenes read from one on-demand snapshot built
  from the scene asset only when needed.
- Player builds stage one temporary runtime catalog during the build so
  unloaded HandyScenes still resolve metadata at runtime without keeping one
  persistent catalog asset in the project.
- Snapshot data preserves project assets and serialized values,
  but they deliberately clear scene-object references because those objects do
  not exist when the scene is not loaded.

## Play mode smoke flow

1. Select `Scenes/HandySceneExample.unity` in the Project window.
2. Confirm the `HandyScene Sample Root` object contains the sample logger.
3. Confirm the logger references `Scenes/HandySceneExample` through its
   `Handy Scene` `HandySceneReference` field.
4. If you deactivated one of the sample sections during the inspector smoke
   flow, reactivate it before entering play mode.
5. If you changed metadata in the inspector, click `Apply Changes` before
   entering play mode.
6. Enter play mode and confirm the Console prints one message for each
   serialized field declared by the sample `SceneExtender` payloads.
