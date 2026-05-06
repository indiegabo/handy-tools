using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndieGabo.HandyTools.HandyInputSystemModule.Feedbacks;
using IndieGabo.HandyTools.LoggerModule;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// Creates feedback container assets from selected InputActionAsset files.
    /// </summary>
    public static class FeedbackContainerCreator
    {
        /// <summary>
        /// Validates whether the feedback container creation menu item should be enabled.
        /// </summary>
        /// <returns>True when the current selection can create a container.</returns>
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

        /// <summary>
        /// Creates a feedback container asset from the currently selected input asset.
        /// </summary>
        [MenuItem("Assets/HandyTools/Create Input Feedback Container", false, priority = 80)]
        private static void RequestProjectCreation()
        {
            string guid = Selection.assetGUIDs[0];
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            InputActionAsset actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

            if (actionAsset == null)
            {
                HandyLogger.Error($"{nameof(FeedbackContainerCreator)}", $"Could not load project file at {assetPath}");
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