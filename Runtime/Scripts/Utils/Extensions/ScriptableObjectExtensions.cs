using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Utils.Extensions
{
    /// <summary>
    /// Adds editor-only mutation helpers for ScriptableObject assets.
    /// </summary>
    public static class ScriptableObjectExtensions
    {
#if UNITY_EDITOR
        /// <summary>
        /// Sets one field value, marks the asset dirty, and saves the asset
        /// when the value changed.
        /// </summary>
        /// <typeparam name="T">Field value type.</typeparam>
        /// <param name="so">Target ScriptableObject asset.</param>
        /// <param name="field">Field reference to mutate.</param>
        /// <param name="value">New field value.</param>
        public static void SetAndSave<T>(this ScriptableObject so, ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Sets one field value and marks the asset dirty when the value
        /// changed.
        /// </summary>
        /// <typeparam name="T">Field value type.</typeparam>
        /// <param name="so">Target ScriptableObject asset.</param>
        /// <param name="field">Field reference to mutate.</param>
        /// <param name="value">New field value.</param>
        public static void SetAndDirty<T>(this ScriptableObject so, ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            EditorUtility.SetDirty(so);
        }
#endif
    }
}
