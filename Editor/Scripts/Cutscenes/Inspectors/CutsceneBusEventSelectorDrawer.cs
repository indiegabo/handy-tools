using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Events;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    /// <summary>
    /// Draws the dual-mode cutscene event selector used by cutscene nodes.
    /// </summary>
    [CustomPropertyDrawer(typeof(CutsceneBusEventSelector))]
    public sealed class CutsceneBusEventSelectorDrawer : PropertyDrawer
    {
        #region Constants

        private const string SelectionModeFieldName = "_selectionMode";
        private const string EventNameFieldName = "_eventName";
        private const string EventReferenceFieldName = "_eventReference";
        private const string EventPathFieldName = "_eventPath";
        private const string EventTypeNameFieldName = "_eventTypeName";

        #endregion

        #region Overrides

        /// <summary>
        /// Draws the selector in IMGUI-based inspectors.
        /// </summary>
        /// <param name="position">Draw area for the property.</param>
        /// <param name="property">Serialized selector property.</param>
        /// <param name="label">Root property label.</param>
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            Rect contentRect = EditorGUI.IndentedRect(position);
            contentRect.height = lineHeight;

            SerializedProperty selectionModeProperty = property.FindPropertyRelative(
                SelectionModeFieldName);
            SerializedProperty eventNameProperty = property.FindPropertyRelative(
                EventNameFieldName);
            SerializedProperty eventReferenceProperty = property.FindPropertyRelative(
                EventReferenceFieldName);

            EditorGUI.PropertyField(
                contentRect,
                selectionModeProperty,
                new GUIContent("Event Source"));

            contentRect.y += lineHeight + verticalSpacing;

            if ((CutsceneBusEventSelector.EventSelectionMode)
                    selectionModeProperty.enumValueIndex
                == CutsceneBusEventSelector.EventSelectionMode.CustomName)
            {
                EditorGUI.PropertyField(
                    contentRect,
                    eventNameProperty,
                    new GUIContent("Event Name"));
            }
            else
            {
                DrawRegisteredEventField(contentRect, eventReferenceProperty);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets the IMGUI height required by the selector.
        /// </summary>
        /// <param name="property">Serialized selector property.</param>
        /// <param name="label">Root property label.</param>
        /// <returns>Height required to draw the selector.</returns>
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight * 2f)
                + EditorGUIUtility.standardVerticalSpacing;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Draws the popup used to select one registered cutscene event.
        /// </summary>
        /// <param name="position">Draw area for the popup.</param>
        /// <param name="eventReferenceProperty">Serialized event reference.</param>
        private static void DrawRegisteredEventField(
            Rect position,
            SerializedProperty eventReferenceProperty)
        {
            IReadOnlyList<CutsceneBusEventMetadata> metadata =
                CutsceneBusEventRegistry.GetEventMetadata();

            SerializedProperty eventPathProperty = eventReferenceProperty
                .FindPropertyRelative(EventPathFieldName);
            SerializedProperty eventTypeNameProperty = eventReferenceProperty
                .FindPropertyRelative(EventTypeNameFieldName);

            List<string> displayedOptions = new() { "None" };
            int selectedIndex = 0;

            for (int index = 0; index < metadata.Count; index++)
            {
                displayedOptions.Add(GetOptionLabel(metadata[index]));
                if (string.Equals(
                        metadata[index].Path,
                        eventPathProperty.stringValue,
                        System.StringComparison.Ordinal))
                {
                    selectedIndex = index + 1;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(
                position,
                "Registered Event",
                selectedIndex,
                displayedOptions.ToArray());

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            if (selectedIndex <= 0)
            {
                eventPathProperty.stringValue = string.Empty;
                eventTypeNameProperty.stringValue = string.Empty;
                return;
            }

            CutsceneBusEventMetadata selection = metadata[selectedIndex - 1];
            eventPathProperty.stringValue = selection.Path;
            eventTypeNameProperty.stringValue =
                selection.EventType.AssemblyQualifiedName ?? string.Empty;
        }

        /// <summary>
        /// Builds the label shown for one popup option.
        /// </summary>
        /// <param name="metadata">Metadata entry to format.</param>
        /// <returns>One readable popup label.</returns>
        private static string GetOptionLabel(CutsceneBusEventMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.DisplayName)
                || string.Equals(
                    metadata.DisplayName,
                    metadata.Path,
                    System.StringComparison.Ordinal))
            {
                return metadata.Path;
            }

            return $"{metadata.Path} ({metadata.DisplayName})";
        }

        #endregion
    }
}