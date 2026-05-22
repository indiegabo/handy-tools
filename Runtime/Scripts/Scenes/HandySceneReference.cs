using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Scenes
{
    /// <summary>
    /// Stores one serialized reference to a HandyScene asset and exposes the
    /// generic runtime API used to resolve its metadata sections.
    /// </summary>
    [Serializable]
    public sealed class HandySceneReference
    {
        #region Fields

        [SerializeField]
        private UnityEngine.Object _sceneAsset;

        [SerializeField]
        private string _sceneName = string.Empty;

        [SerializeField]
        private string _sceneGuid = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the editor-only SceneAsset reference when the project is open
        /// in the editor.
        /// </summary>
        public UnityEngine.Object SceneAsset => _sceneAsset;

        /// <summary>
        /// Gets the Unity asset GUID of the referenced scene when available.
        /// </summary>
        public string SceneGuid => _sceneGuid ?? string.Empty;

        /// <summary>
        /// Gets the project-relative `.unity` asset path represented by this
        /// reference.
        /// </summary>
        public string SceneAssetPath
        {
            get
            {
                string storedSceneValue = NormalizeStoredSceneValue(_sceneName);
                return string.IsNullOrWhiteSpace(storedSceneValue)
                    ? string.Empty
                    : $"Assets/{storedSceneValue}.unity";
            }
        }

        /// <summary>
        /// Gets the short scene name without path segments or extension.
        /// </summary>
        public string SceneName
        {
            get
            {
                string storedSceneValue = NormalizeStoredSceneValue(_sceneName);
                if (string.IsNullOrWhiteSpace(storedSceneValue))
                {
                    return string.Empty;
                }

                int separatorIndex = storedSceneValue.LastIndexOf('/');
                return separatorIndex >= 0
                    ? storedSceneValue[(separatorIndex + 1)..]
                    : storedSceneValue;
            }
        }

        /// <summary>
        /// Gets whether this reference currently points to one scene asset.
        /// </summary>
        public bool IsAssigned => !string.IsNullOrWhiteSpace(SceneAssetPath);

        #endregion

        #region Public API

        /// <summary>
        /// Gets whether the referenced HandyScene resolves one section of the
        /// requested type.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <returns>True when the section could be resolved.</returns>
        public bool HasSection<TSection>()
            where TSection : SceneExtender
        {
            return TryGetSection<TSection>(out _);
        }

        /// <summary>
        /// Resolves one metadata section of the requested type from this
        /// HandyScene reference.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="section">Resolved section instance.</param>
        /// <returns>True when the requested section could be resolved.</returns>
        public bool TryGetSection<TSection>(out TSection section)
            where TSection : SceneExtender
        {
            return HandySceneRuntimeReader.TryGetSection(this, out section);
        }

        /// <summary>
        /// Resolves one metadata section of the requested type or throws when
        /// the referenced scene does not expose an active section of that
        /// type.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <returns>The resolved section instance.</returns>
        public TSection GetSection<TSection>()
            where TSection : SceneExtender
        {
            if (TryGetSection(out TSection section))
            {
                return section;
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(SceneAssetPath)
                    ? $"The HandyScene reference does not point to one scene, so " +
                      $"section '{typeof(TSection).Name}' cannot be resolved."
                    : $"HandyScene '{SceneAssetPath}' does not have one active " +
                      $"'{typeof(TSection).Name}' section.");
        }

        /// <summary>
        /// Resolves one metadata section of the requested type or returns null
        /// when the reference does not expose that section.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <returns>The resolved section instance when available.</returns>
        public TSection GetSectionOrNull<TSection>()
            where TSection : SceneExtender
        {
            _ = TryGetSection(out TSection section);
            return section;
        }

        /// <summary>
        /// Returns the referenced project-relative scene asset path.
        /// </summary>
        /// <returns>The referenced scene asset path.</returns>
        public override string ToString()
        {
            return SceneAssetPath;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Creates one HandyScene reference from one SceneAsset.
        /// </summary>
        /// <param name="sceneAsset">Persistent SceneAsset to reference.</param>
        /// <returns>The initialized HandyScene reference.</returns>
        public static HandySceneReference FromAsset(SceneAsset sceneAsset)
        {
            HandySceneReference sceneReference = new();

            if (sceneAsset == null)
            {
                return sceneReference;
            }

            sceneReference.SetSceneData(
                sceneAsset,
                AssetDatabase.GetAssetPath(sceneAsset));
            return sceneReference;
        }

        /// <summary>
        /// Creates one HandyScene reference from one project-relative scene
        /// asset path.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <returns>The initialized HandyScene reference.</returns>
        public static HandySceneReference FromSceneAssetPath(string sceneAssetPath)
        {
            HandySceneReference sceneReference = new();

            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return sceneReference;
            }

            sceneReference.SetSceneData(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneAssetPath),
                sceneAssetPath);
            return sceneReference;
        }

        internal void SetSceneData(UnityEngine.Object sceneAsset, string sceneAssetPath)
        {
            _sceneAsset = sceneAsset;
            _sceneName = NormalizeStoredSceneValue(sceneAssetPath);
            _sceneGuid = string.IsNullOrWhiteSpace(sceneAssetPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(sceneAssetPath);
        }
#endif

        #endregion

        #region Helpers

        private static string NormalizeStoredSceneValue(string sceneValue)
        {
            if (string.IsNullOrWhiteSpace(sceneValue))
            {
                return string.Empty;
            }

            string normalizedValue = sceneValue.Replace('\\', '/').Trim();

            if (normalizedValue.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedValue = normalizedValue["Assets/".Length..];
            }

            if (normalizedValue.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                normalizedValue = normalizedValue[..^".unity".Length];
            }

            return normalizedValue;
        }

        #endregion
    }
}