using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HandyTools.Editor.Utils
{
    /// <summary>
    /// Provides scene asset creation helpers for editor tooling.
    /// </summary>
    public static class Scenes
    {
        /// <summary>
        /// Saves an existing scene to disk and returns the resulting SceneAsset.
        /// </summary>
        /// <param name="scene">Scene to save.</param>
        /// <param name="scenePath">Asset path where the scene will be saved.</param>
        /// <returns>The saved SceneAsset.</returns>
        public static SceneAsset CreateSceneAsset(Scene scene, string scenePath)
        {
            // Creates the scene, saves it and unloads it from hierarchy
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.UnloadSceneAsync(scene);

            // Gets the scene asset from project folder
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        /// <summary>
        /// Creates a new scene, saves it to disk, and returns the SceneAsset.
        /// </summary>
        /// <param name="scenePath">Asset path where the scene will be saved.</param>
        /// <param name="sceneSetup">Template used to create the new scene.</param>
        /// <returns>The saved SceneAsset.</returns>
        public static SceneAsset CreateSceneAsset(string scenePath, NewSceneSetup sceneSetup = NewSceneSetup.EmptyScene)
        {
            // Creates the scene, saves it and unloads it from hierarchy
            Scene scene = EditorSceneManager.NewScene(sceneSetup, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.UnloadSceneAsync(scene);

            // Gets the scene asset from project folder
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }
    }
}