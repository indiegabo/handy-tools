using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.HandyInputSystemModule.Bindings;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.Editor.InputModule
{
    /// <summary>
    /// Draws <see cref="InputActionMapField"/> values with an action asset slot
    /// and a popup for the available maps.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputActionMapField))]
    public sealed class InputActionMapFieldPropertyDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            _ = property;
            _ = label;

            return (EditorGUIUtility.singleLineHeight * 2f)
                + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <inheritdoc />
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty assetProperty = property.FindPropertyRelative(
                "_inputActionAsset"
            );
            SerializedProperty mapIdProperty = property.FindPropertyRelative("_mapId");

            Rect assetRect = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );
            Rect mapRect = new(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight
                    + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                EditorGUIUtility.singleLineHeight
            );

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(assetRect, assetProperty, label);

            InputActionAsset actionAsset = assetProperty.objectReferenceValue
                as InputActionAsset;

            if (actionAsset == null || actionAsset.actionMaps.Count == 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(mapRect, "Map", string.Empty);
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndProperty();
                return;
            }

            List<string> optionNames = new(actionAsset.actionMaps.Count);
            List<string> optionIds = new(actionAsset.actionMaps.Count);

            int selectedIndex = 0;
            string selectedMapId = mapIdProperty.stringValue;

            for (int index = 0; index < actionAsset.actionMaps.Count; index++)
            {
                InputActionMap map = actionAsset.actionMaps[index];
                optionNames.Add(map.name);
                optionIds.Add(map.id.ToString());

                if (string.Equals(
                    optionIds[index],
                    selectedMapId,
                    StringComparison.Ordinal
                ))
                {
                    selectedIndex = index;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(
                mapRect,
                "Map",
                selectedIndex,
                optionNames.ToArray()
            );
            if (EditorGUI.EndChangeCheck())
            {
                mapIdProperty.stringValue = optionIds[newIndex];
            }
            else if (string.IsNullOrWhiteSpace(mapIdProperty.stringValue))
            {
                mapIdProperty.stringValue = optionIds[selectedIndex];
            }

            EditorGUI.EndProperty();
        }
    }
}