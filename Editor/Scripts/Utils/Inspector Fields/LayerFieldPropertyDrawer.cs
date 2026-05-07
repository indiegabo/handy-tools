using IndieGabo.HandyTools.Utils.InspectorFields;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Utils.InspectorFields
{
    /// <summary>
    /// Draws <see cref="LayerField"/> values with Unity's built-in layer picker.
    /// </summary>
    [CustomPropertyDrawer(typeof(LayerField))]
    public sealed class LayerFieldPropertyDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty indexProperty = property.FindPropertyRelative("_index");

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            int selectedLayer = EditorGUI.LayerField(
                position,
                label,
                indexProperty.intValue
            );
            if (EditorGUI.EndChangeCheck())
            {
                indexProperty.intValue = selectedLayer;
            }

            EditorGUI.EndProperty();
        }
    }
}