using IndieGabo.HandyTools.Utils.InspectorFields;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Utils.InspectorFields
{
    /// <summary>
    /// Draws <see cref="TagField"/> values with Unity's built-in tag picker.
    /// </summary>
    [CustomPropertyDrawer(typeof(TagField))]
    public sealed class TagFieldPropertyDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty tagProperty = property.FindPropertyRelative("_tag");

            EditorGUI.BeginProperty(position, label, property);

            string currentTag = string.IsNullOrWhiteSpace(tagProperty.stringValue)
                ? "Untagged"
                : tagProperty.stringValue;

            EditorGUI.BeginChangeCheck();
            string selectedTag = EditorGUI.TagField(position, label, currentTag);
            if (EditorGUI.EndChangeCheck())
            {
                tagProperty.stringValue = selectedTag;
            }

            EditorGUI.EndProperty();
        }
    }
}