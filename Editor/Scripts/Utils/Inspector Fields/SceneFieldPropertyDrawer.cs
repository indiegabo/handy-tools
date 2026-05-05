using System;
using UnityEngine;
using UnityEditor;
using IndieGabo.HandyTools.Utils.InspectorFields;

namespace IndieGabo.HandyTools.Editor.Utils.InspectorFields
{
    [CustomPropertyDrawer(typeof(SceneField))]
    /// <summary>
    /// Draws SceneField values as SceneAsset object fields in the inspector.
    /// </summary>
    public class SceneFieldPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the serialized SceneField and stores the scene path payload.
        /// </summary>
        /// <param name="position">Drawing area in the inspector.</param>
        /// <param name="property">Serialized SceneField property.</param>
        /// <param name="label">Inspector label for the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            var sceneAsset = property.FindPropertyRelative("_sceneAsset");
            var sceneName = property.FindPropertyRelative("_sceneName");
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            if (sceneAsset != null)
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    sceneAsset.objectReferenceValue = value;
                    if (sceneAsset.objectReferenceValue != null)
                    {
                        var scenePath = AssetDatabase.GetAssetPath(sceneAsset.objectReferenceValue);
                        var assetsIndex = scenePath.IndexOf("Assets", StringComparison.Ordinal) + 7;
                        var extensionIndex = scenePath.LastIndexOf(".unity", StringComparison.Ordinal);
                        scenePath = scenePath.Substring(assetsIndex, extensionIndex - assetsIndex);
                        sceneName.stringValue = scenePath;
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }
}
