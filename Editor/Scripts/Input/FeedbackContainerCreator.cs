using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndieGabo.HandyTools.HandyInputSystem.Feedbacks;
using IndieGabo.HandyTools.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
namespace IndieGabo.HandyTools.Editor
{
    public static class ProjectCreator
    {
        [MenuItem("Assets/HandyTools/Create Input Feedback Container", true, priority = 80)] // Enable validation
        private static bool ValidateCreationMenuItem()
        {
            // Check if any assets are selected
            if (Selection.assetGUIDs.Length == 0)
                return false; // No assets selected, hide the menu item

            if (Selection.assetGUIDs.Length > 1)
                return false; // More than one asset selected, hide the menu item

            string guid = Selection.assetGUIDs[0];
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            InputActionAsset projectFile = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            return projectFile != null;
        }

        [MenuItem("Assets/HandyTools/Create Input Feedback Container", false, priority = 80)]
        private static void RequestProjectCreation()
        {
            string guid = Selection.assetGUIDs[0];
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            InputActionAsset actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

            if (actionAsset == null)
            {
                HandyLogger.Error($"{nameof(ProjectCreator)}", $"Could not load project file at {assetPath}");
                return;
            }

            string fileNameToRemove = assetPath.Split("/").Last();
            string fileNameWithoutExtension = fileNameToRemove.Split(".").First();
            string directoryPath = assetPath.Replace(fileNameToRemove, string.Empty);
            string containerAssetPath = Path.Combine(directoryPath, fileNameWithoutExtension + "_FeedbackContainer.asset");

            FeedbackContainer container = ScriptableObject.CreateInstance<FeedbackContainer>();

            AssetDatabase.CreateAsset(container, containerAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();

            container.Initialize(actionAsset);

            Selection.activeObject = container;
        }
    }
}