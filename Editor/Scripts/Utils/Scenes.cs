using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HandyTools.Editor.Utils
{
    public static class Scenes
    {
        public static SceneAsset CreateSceneAsset(Scene scene, string scenePath, NewSceneSetup sceneSetup = NewSceneSetup.EmptyScene)
        {
            // Creates the scene, saves it and unloads it from hierarchy
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.UnloadSceneAsync(scene);

            // Gets the scene asset from project folder
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

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