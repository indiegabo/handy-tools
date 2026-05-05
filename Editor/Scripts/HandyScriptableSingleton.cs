using System;
using System.Reflection;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// Base ScriptableSingleton that exposes reflection-based field assignment
    /// with automatic persistence.
    /// </summary>
    /// <typeparam name="T">Concrete singleton type.</typeparam>
    public class HandyScriptableSingleton<T> : ScriptableSingleton<T> where T : ScriptableSingleton<T>, new()
    {
        /// <summary>
        /// Assigns a field by name and persists the singleton asset.
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
            Save(true);
        }
    }
}