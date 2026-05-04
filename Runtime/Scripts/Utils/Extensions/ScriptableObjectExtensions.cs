using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Utils.Extensions
{
    public static class ScriptableObjectExtensions
    {
#if UNITY_EDITOR
        public static void SetAndSave<T>(this ScriptableObject so, ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

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
