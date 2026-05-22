using IndieGabo.HandyTools.CutscenesModule.Triggers;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    /// <summary>
    /// Replays automatic cutscene trigger entry points when the Unity editor
    /// enters play mode without reloading the active scene.
    /// </summary>
    [InitializeOnLoad]
    internal static class CutsceneEnterPlayModeLifecycleBridge
    {
        static CutsceneEnterPlayModeLifecycleBridge()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Replays automatic trigger entry points for persisted active scene
        /// objects.
        /// </summary>
        internal static void ReplayPersistentSceneTriggers()
        {
            foreach (CutsceneTrigger trigger in Object.FindObjectsByType<CutsceneTrigger>())
            {
                if (trigger == null || !trigger.isActiveAndEnabled)
                {
                    continue;
                }

                trigger.HandlePlaySessionStartWithoutSceneReload();
            }
        }

        /// <summary>
        /// Responds to editor play-mode changes and replays trigger entry
        /// points only when Unity keeps the current scene objects alive.
        /// </summary>
        /// <param name="state">The new editor play-mode state.</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode
                || !ShouldReplayPersistentSceneTriggers())
            {
                return;
            }

            EditorApplication.delayCall -= ReplayTriggersOnNextEditorTick;
            EditorApplication.delayCall += ReplayTriggersOnNextEditorTick;
        }

        /// <summary>
        /// Replays trigger entry points after the editor finishes entering play
        /// mode and the runtime bootstrappers have had one chance to register
        /// services.
        /// </summary>
        private static void ReplayTriggersOnNextEditorTick()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            ReplayPersistentSceneTriggers();
        }

        /// <summary>
        /// Determines whether the current editor play-mode configuration keeps
        /// active scene objects alive between play sessions.
        /// </summary>
        /// <returns>
        /// True when scene reload is disabled for enter play mode.
        /// </returns>
        private static bool ShouldReplayPersistentSceneTriggers()
        {
            if (!EditorSettings.enterPlayModeOptionsEnabled)
            {
                return false;
            }

            return (EditorSettings.enterPlayModeOptions
                & EnterPlayModeOptions.DisableSceneReload) != 0;
        }
    }
}