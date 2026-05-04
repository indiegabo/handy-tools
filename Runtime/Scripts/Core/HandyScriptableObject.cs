using UnityEngine;
using System;
using System.Reflection;

#if UNITY_EDITOR            
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.HandyInputSystem.Feedbacks
{
    public class HandyScriptableObject : ScriptableObject
    {
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