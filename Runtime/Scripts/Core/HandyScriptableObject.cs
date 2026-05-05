using UnityEngine;
using System;
using System.Reflection;

#if UNITY_EDITOR            
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.HandyInputSystem.Feedbacks
{
    /// <summary>
    /// Base ScriptableObject that exposes reflection-based field assignment for
    /// editor-persisted configuration assets.
    /// </summary>
    public class HandyScriptableObject : ScriptableObject
    {
        /// <summary>
        /// Assigns a field by name and marks the asset dirty in the editor.
        /// </summary>
        /// <param name="fieldName">Field name to update.</param>
        /// <param name="value">Value assigned to the field.</param>
        protected virtual void SetFieldValue(string fieldName, object value)
        {
            FieldInfo field = this.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            ) ?? throw new ArgumentException($"Field {fieldName} not found for {this.GetType().Name}");

            if (value != null && !field.FieldType.IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException($"Value \"{value}\" is not assignable to field {field.Name} in {this.GetType().Name}");
            }

            field.SetValue(this, value);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }
}