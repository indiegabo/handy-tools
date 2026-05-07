using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR            
using UnityEditor;
#endif

namespace IndieGabo.HandyTools
{
    /// <summary>
    /// Defines the Resources path used to load a global HandyTools config.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GlobalConfigAttribute : Attribute
    {
        /// <summary>
        /// Creates the attribute with a Resources-relative config path.
        /// </summary>
        /// <param name="resourcePath">Resources-relative folder or asset path.</param>
        public GlobalConfigAttribute(string resourcePath)
        {
            ResourcePath = resourcePath ?? string.Empty;
        }

        /// <summary>
        /// Gets the configured Resources-relative folder or asset path.
        /// </summary>
        public string ResourcePath { get; }
    }

    /// <summary>
    /// Unity-only global config base that loads configuration assets from Resources.
    /// It loads config assets from Resources and creates an in-memory fallback
    /// when no asset exists.
    /// </summary>
    /// <typeparam name="T">Concrete config type.</typeparam>
    public class HandyGlobalConfig<T> : ScriptableObject where T : HandyGlobalConfig<T>
    {
        #region State

        private static T _instance;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the loaded config asset or creates an in-memory fallback.
        /// </summary>
        public static T Instance => _instance != null ? _instance : _instance = LoadInstance();

        /// <summary>
        /// Reloads the current config instance from Resources.
        /// </summary>
        public static void ReloadInstance()
        {
            _instance = LoadInstance();
        }

        #endregion

        #region Field Access

        /// <summary>
        /// Updates a serialized field and marks the asset dirty in the editor.
        /// </summary>
        /// <param name="fieldName">Name of the field to update.</param>
        /// <param name="value">Value assigned to the field.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the field cannot be found or the value type is invalid.
        /// </exception>
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

        #endregion

        #region Loading

        private static T LoadInstance()
        {
            Type configType = typeof(T);
            string configuredPath = GetConfiguredResourcePath(configType);

            T asset = LoadFromResourcePath(configuredPath);
            if (asset != null)
            {
                return asset;
            }

            if (!string.IsNullOrEmpty(configuredPath))
            {
                asset = LoadFromResourcePath($"{configuredPath}/{configType.Name}");
                if (asset != null)
                {
                    return asset;
                }
            }

#if UNITY_EDITOR
            asset = CreateEditorAsset(configType, configuredPath);
            if (asset != null)
            {
                return asset;
            }
#endif

            return CreateInstance<T>();
        }

        private static T LoadFromResourcePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return Resources.Load<T>(NormalizeResourcePath(path));
        }

        private static string GetConfiguredResourcePath(Type configType)
        {
            var attribute = configType.GetCustomAttribute<GlobalConfigAttribute>();
            return attribute?.ResourcePath ?? configType.Name;
        }

        private static string NormalizeResourcePath(string path)
        {
            string normalizedPath = path.Replace('\\', '/').Trim('/');

            if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                int resourcesIndex = normalizedPath.IndexOf(
                    "/Resources/",
                    StringComparison.OrdinalIgnoreCase
                );

                if (resourcesIndex >= 0)
                {
                    normalizedPath = normalizedPath[(resourcesIndex + "/Resources/".Length)..];
                }
            }

            if (normalizedPath.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath["Resources/".Length..];
            }

            if (normalizedPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = normalizedPath[..^".asset".Length];
            }

            return normalizedPath;
        }

#if UNITY_EDITOR
        private static T CreateEditorAsset(Type configType, string configuredPath)
        {
            string assetPath = BuildEditorAssetPath(configType, configuredPath);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolderForAssetPath(assetPath);
            asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static string BuildEditorAssetPath(Type configType, string configuredPath)
        {
            string normalizedConfiguredPath = (configuredPath ?? string.Empty)
                .Replace('\\', '/')
                .Trim();

            bool isExplicitAssetPath = normalizedConfiguredPath.EndsWith(
                ".asset",
                StringComparison.OrdinalIgnoreCase
            );

            bool treatConfiguredPathAsFolder =
                !isExplicitAssetPath &&
                (normalizedConfiguredPath.StartsWith(
                    "Resources/",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                normalizedConfiguredPath.StartsWith(
                    "Assets/Resources/",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                normalizedConfiguredPath.EndsWith("/", StringComparison.Ordinal));

            string resourcePath = treatConfiguredPathAsFolder
                ? NormalizeResourcePath($"{normalizedConfiguredPath}/{configType.Name}")
                : NormalizeResourcePath(normalizedConfiguredPath);

            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                resourcePath = configType.Name;
            }

            return $"Assets/Resources/{resourcePath}.asset";
        }

        private static void EnsureFolderForAssetPath(string assetPath)
        {
            string[] segments = assetPath.Split('/');
            string currentPath = segments[0];

            for (int index = 1; index < segments.Length - 1; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }
#endif

        #endregion
    }
}