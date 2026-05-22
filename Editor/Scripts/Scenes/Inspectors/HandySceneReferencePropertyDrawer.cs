using System;
using IndieGabo.HandyTools.Scenes;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Scenes.Inspectors
{
    /// <summary>
    /// Draws HandySceneReference values as SceneAsset object fields in the
    /// inspector while preserving the serialized path token used at runtime.
    /// </summary>
    [CustomPropertyDrawer(typeof(HandySceneReference))]
    public sealed class HandySceneReferencePropertyDrawer : PropertyDrawer
    {
        #region Public API

        /// <summary>
        /// Draws the serialized HandyScene reference and stores the normalized
        /// scene path payload.
        /// </summary>
        /// <param name="position">Drawing area in the inspector.</param>
        /// <param name="property">Serialized HandySceneReference property.</param>
        /// <param name="label">Inspector label for the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sceneAssetProperty =
                property.FindPropertyRelative("_sceneAsset");
            SerializedProperty sceneNameProperty =
                property.FindPropertyRelative("_sceneName");
            SerializedProperty sceneGuidProperty =
                property.FindPropertyRelative("_sceneGuid");

            position = EditorGUI.PrefixLabel(
                position,
                GUIUtility.GetControlID(FocusType.Passive),
                label);

            if (sceneAssetProperty != null
                && sceneNameProperty != null
                && sceneGuidProperty != null)
            {
                EditorGUI.BeginChangeCheck();

                UnityEngine.Object value = EditorGUI.ObjectField(
                    position,
                    sceneAssetProperty.objectReferenceValue,
                    typeof(SceneAsset),
                    false);

                if (EditorGUI.EndChangeCheck())
                {
                    sceneAssetProperty.objectReferenceValue = value;

                    if (value != null)
                    {
                        string sceneAssetPath = AssetDatabase.GetAssetPath(value);
                        sceneNameProperty.stringValue = BuildStoredSceneValue(sceneAssetPath);
                        sceneGuidProperty.stringValue =
                            AssetDatabase.AssetPathToGUID(sceneAssetPath);
                    }
                    else
                    {
                        sceneNameProperty.stringValue = string.Empty;
                        sceneGuidProperty.stringValue = string.Empty;
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        #endregion

        #region Helpers

        private static string BuildStoredSceneValue(string sceneAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return string.Empty;
            }

            string normalizedValue = sceneAssetPath.Replace('\\', '/');

            if (normalizedValue.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedValue = normalizedValue["Assets/".Length..];
            }

            if (normalizedValue.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                normalizedValue = normalizedValue[..^".unity".Length];
            }

            return normalizedValue;
        }

        #endregion
    }
}