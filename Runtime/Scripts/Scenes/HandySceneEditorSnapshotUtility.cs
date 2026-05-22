#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;
using Scene = UnityEngine.SceneManagement.Scene;

namespace IndieGabo.HandyTools.Scenes
{
    /// <summary>
    /// Builds editor-only in-memory HandyScene snapshots so unloaded scenes can
    /// be queried in the editor without persisting a runtime catalog asset in
    /// the project.
    /// </summary>
    public static class HandySceneEditorSnapshotUtility
    {
        #region Types

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            #region Public API

            /// <summary>
            /// Shared reference comparer instance.
            /// </summary>
            public static readonly ReferenceEqualityComparer Instance = new();

            /// <summary>
            /// Gets whether two objects are the same reference.
            /// </summary>
            /// <param name="left">Left object instance.</param>
            /// <param name="right">Right object instance.</param>
            /// <returns>True when both values share the same reference.</returns>
            bool IEqualityComparer<object>.Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            /// <summary>
            /// Gets the runtime hash code for one object reference.
            /// </summary>
            /// <param name="obj">Object reference to hash.</param>
            /// <returns>The reference-based hash code.</returns>
            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }

            #endregion
        }

        #endregion

        #region Public API

        /// <summary>
        /// Creates one in-memory runtime snapshot entry for one HandyScene.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="entry">Generated snapshot entry.</param>
        /// <returns>True when the scene could be opened and snapshotted.</returns>
        public static bool TryCreateCatalogEntry(
            string sceneAssetPath,
            out HandySceneRuntimeCatalogEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return false;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = default;
            bool shouldCloseScene = false;

            try
            {
                if (!TryResolveScene(
                        sceneAssetPath,
                        out scene,
                        out shouldCloseScene))
                {
                    return false;
                }

                HandySceneMetadataCarrier carrier = FindCarrierInScene(scene);
                if (carrier == null || !carrier.IsHandyScene)
                {
                    return false;
                }

                entry = new HandySceneRuntimeCatalogEntry();
                entry.Reset(
                    sceneAssetPath,
                    AssetDatabase.AssetPathToGUID(sceneAssetPath),
                    System.IO.Path.GetFileNameWithoutExtension(sceneAssetPath),
                    HandySceneSchema.CurrentVersion);

                for (int index = 0; index < carrier.SectionCount; index++)
                {
                    SceneExtender section = carrier.GetSection(index);
                    if (!ShouldIncludeSectionInSnapshot(sceneAssetPath, section))
                    {
                        continue;
                    }

                    entry.AddSection(
                        carrier.GetSectionId(index),
                        CreateSectionSnapshot(section));
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not create the HandyScene editor snapshot for " +
                    $"'{sceneAssetPath}'. {exception.Message}");
                entry = null;
                return false;
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    _ = SceneManager.SetActiveScene(previousActiveScene);
                }

                if (shouldCloseScene && scene.IsValid() && scene.isLoaded)
                {
                    _ = EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        /// <summary>
        /// Creates one in-memory runtime catalog snapshot for the provided
        /// HandyScene asset paths.
        /// </summary>
        /// <param name="sceneAssetPaths">Project-relative scene asset paths.</param>
        /// <returns>The generated in-memory catalog snapshot.</returns>
        public static HandySceneRuntimeCatalog CreateCatalogSnapshot(
            IEnumerable<string> sceneAssetPaths)
        {
            HandySceneRuntimeCatalog catalog = ScriptableObject.CreateInstance<
                HandySceneRuntimeCatalog>();

            catalog.ResetEntries(HandySceneSchema.CurrentVersion);

            if (sceneAssetPaths == null)
            {
                return catalog;
            }

            foreach (string sceneAssetPath in sceneAssetPaths)
            {
                if (TryCreateCatalogEntry(sceneAssetPath, out HandySceneRuntimeCatalogEntry entry)
                    && entry != null)
                {
                    catalog.AddEntry(entry);
                }
            }

            catalog.SortEntries();
            return catalog;
        }

        #endregion

        #region Helpers

        private static bool TryResolveScene(
            string sceneAssetPath,
            out Scene scene,
            out bool shouldCloseScene)
        {
            scene = SceneManager.GetSceneByPath(sceneAssetPath);
            if (scene.IsValid() && scene.isLoaded)
            {
                shouldCloseScene = false;
                return true;
            }

            shouldCloseScene = true;
            scene = EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Additive);
            return scene.IsValid() && scene.isLoaded;
        }

        private static HandySceneMetadataCarrier FindCarrierInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                HandySceneMetadataCarrier carrier = roots[rootIndex]
                    .GetComponentInChildren<HandySceneMetadataCarrier>(true);

                if (carrier != null)
                {
                    return carrier;
                }
            }

            return null;
        }

        private static SceneExtender CreateSectionSnapshot(SceneExtender source)
        {
            if (source == null)
            {
                return null;
            }

            SceneExtender snapshot = CreateSectionInstance(source.GetType());
            if (snapshot == null)
            {
                return null;
            }

            EditorUtility.CopySerializedManagedFieldsOnly(source, snapshot);

            HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
            _ = SanitizeObjectGraph(snapshot, visited);
            return snapshot;
        }

        private static bool ShouldIncludeSectionInSnapshot(
            string sceneAssetPath,
            SceneExtender section)
        {
            if (section == null)
            {
                return true;
            }

            if (IsTestScenePath(sceneAssetPath))
            {
                return true;
            }

            string assemblyName = section.GetType().Assembly.GetName().Name
                ?? string.Empty;

            return !assemblyName.Contains(".Tests", StringComparison.Ordinal);
        }

        private static bool IsTestScenePath(string sceneAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return false;
            }

            string normalizedPath = sceneAssetPath.Replace('\\', '/');
            return normalizedPath.StartsWith(
                "Assets/Tests/",
                StringComparison.OrdinalIgnoreCase);
        }

        private static SceneExtender CreateSectionInstance(Type sectionType)
        {
            try
            {
                return Activator.CreateInstance(sectionType, true) as SceneExtender;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not instantiate the HandyScene snapshot section " +
                    $"'{sectionType?.FullName}'. {exception.Message}");
                return null;
            }
        }

        private static object SanitizeObjectGraph(
            object value,
            HashSet<object> visited)
        {
            if (value == null)
            {
                return null;
            }

            Type valueType = value.GetType();

            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
            {
                UnityEngine.Object unityObject = value as UnityEngine.Object;
                if (unityObject == null)
                {
                    return null;
                }

                return EditorUtility.IsPersistent(unityObject)
                    ? unityObject
                    : null;
            }

            if (valueType == typeof(string)
                || valueType.IsPrimitive
                || valueType.IsEnum
                || valueType == typeof(decimal))
            {
                return value;
            }

            if (value is Array array)
            {
                for (int index = 0; index < array.Length; index++)
                {
                    array.SetValue(
                        SanitizeObjectGraph(array.GetValue(index), visited),
                        index);
                }

                return array;
            }

            if (value is IList list)
            {
                for (int index = 0; index < list.Count; index++)
                {
                    list[index] = SanitizeObjectGraph(list[index], visited);
                }

                return list;
            }

            if (!valueType.IsValueType && !visited.Add(value))
            {
                return value;
            }

            FieldInfo[] fields = GetSerializableFields(valueType);
            for (int index = 0; index < fields.Length; index++)
            {
                FieldInfo field = fields[index];
                object fieldValue = field.GetValue(value);
                object sanitizedValue = SanitizeObjectGraph(fieldValue, visited);

                if (!ReferenceEquals(fieldValue, sanitizedValue)
                    || field.FieldType.IsValueType)
                {
                    field.SetValue(value, sanitizedValue);
                }
            }

            return value;
        }

        private static FieldInfo[] GetSerializableFields(Type type)
        {
            List<FieldInfo> fields = new();
            Type currentType = type;

            while (currentType != null && currentType != typeof(object))
            {
                FieldInfo[] declaredFields = currentType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                for (int index = 0; index < declaredFields.Length; index++)
                {
                    FieldInfo field = declaredFields[index];
                    if (IsUnitySerializedField(field))
                    {
                        fields.Add(field);
                    }
                }

                currentType = currentType.BaseType;
            }

            return fields.ToArray();
        }

        private static bool IsUnitySerializedField(FieldInfo field)
        {
            if (field == null || field.IsStatic || field.IsNotSerialized)
            {
                return false;
            }

            if (field.IsPublic)
            {
                return true;
            }

            return Attribute.IsDefined(field, typeof(SerializeField))
                || Attribute.IsDefined(field, typeof(SerializeReference));
        }

        #endregion
    }
}
#endif