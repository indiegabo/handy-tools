using System;
using IndieGabo.HandyTools.Utils.Identifying;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.IdentifyingModule
{
    [CustomPropertyDrawer(typeof(Identifier))]
    /// <summary>
    /// Draws Identifier values as read-only GUID strings in the inspector.
    /// </summary>
    public class IdentifierDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the identifier as a formatted GUID string.
        /// </summary>
        /// <param name="position">Drawing area in the inspector.</param>
        /// <param name="property">Serialized Identifier property.</param>
        /// <param name="label">Inspector label for the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var value0 = property.FindPropertyRelative(Identifier.VALUE0_FIELDNAME);
            var value1 = property.FindPropertyRelative(Identifier.VALUE1_FIELDNAME);
            var value2 = property.FindPropertyRelative(Identifier.VALUE2_FIELDNAME);
            var value3 = property.FindPropertyRelative(Identifier.VALUE3_FIELDNAME);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            string guidString = $"{(uint)value0.intValue:X8}{(uint)value1.intValue:X8}{(uint)value2.intValue:X8}{(uint)value3.intValue:X8}";
            Guid guid = new(guidString);
            EditorGUI.LabelField(position, guid.ToString());
            EditorGUI.EndProperty();
        }
    }
}