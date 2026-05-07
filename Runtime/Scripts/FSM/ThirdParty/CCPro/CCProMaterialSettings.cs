using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Stores one tagged surface entry used to resolve locomotion and reaction
    /// data from a grounded collider.
    /// </summary>
    [Serializable]
    public sealed class CCProSurfaceMaterialDefinition
    {
        #region Inspector

        [SerializeField]
        private string _tagName;

        [SerializeField]
        private string _reactionKey;

        [SerializeField]
        private Color _debugColor = Color.white;

        [SerializeField]
        private CCProSurfaceModifierSettings _surfaceModifiers =
            new(1f, 1f, 1f);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the tag used to match colliders against this definition.
        /// </summary>
        public string TagName => _tagName ?? string.Empty;

        /// <summary>
        /// Gets the semantic key consumed by gameplay reaction systems.
        /// </summary>
        public string ReactionKey => string.IsNullOrWhiteSpace(_reactionKey)
            ? TagName
            : _reactionKey;

        /// <summary>
        /// Gets the debug color associated with this definition.
        /// </summary>
        public Color DebugColor => _debugColor;

        /// <summary>
        /// Gets the runtime movement modifiers authored by this definition.
        /// </summary>
        public CCProSurfaceModifiers Modifiers =>
            _surfaceModifiers.ToRuntimeModifiers();

        #endregion

        #region Queries

        /// <summary>
        /// Checks whether this definition matches the supplied game object tag.
        /// </summary>
        /// <param name="target">Game object evaluated against this definition.</param>
        /// <returns>
        /// <see langword="true" /> when the object tag matches this definition;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool Matches(GameObject target)
        {
            return target != null
                && !string.IsNullOrWhiteSpace(TagName)
                && string.Equals(target.tag, TagName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Converts this authored definition into a runtime snapshot.
        /// </summary>
        /// <param name="fallbackReactionKey">
        /// Semantic key used when the authored reaction key is empty.
        /// </param>
        /// <returns>The converted runtime surface material info.</returns>
        public CCProSurfaceMaterialInfo ToRuntimeInfo(
            string fallbackReactionKey)
        {
            return new CCProSurfaceMaterialInfo(
                TagName,
                string.IsNullOrWhiteSpace(ReactionKey)
                    ? fallbackReactionKey
                    : ReactionKey,
                DebugColor,
                Modifiers);
        }

        #endregion
    }

    /// <summary>
    /// Stores one tagged volume entry used to resolve locomotion and reaction
    /// data from overlapping triggers.
    /// </summary>
    [Serializable]
    public sealed class CCProVolumeMaterialDefinition
    {
        #region Inspector

        [SerializeField]
        private string _tagName;

        [SerializeField]
        private string _reactionKey;

        [SerializeField]
        private CCProVolumeModifierSettings _volumeModifiers =
            new(1f, 1f, 1f, 1f, 1f);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the tag used to match colliders against this definition.
        /// </summary>
        public string TagName => _tagName ?? string.Empty;

        /// <summary>
        /// Gets the semantic key consumed by gameplay reaction systems.
        /// </summary>
        public string ReactionKey => string.IsNullOrWhiteSpace(_reactionKey)
            ? TagName
            : _reactionKey;

        /// <summary>
        /// Gets the runtime movement and gravity modifiers authored by this
        /// definition.
        /// </summary>
        public CCProVolumeModifiers Modifiers =>
            _volumeModifiers.ToRuntimeModifiers();

        #endregion

        #region Queries

        /// <summary>
        /// Checks whether this definition matches the supplied game object tag.
        /// </summary>
        /// <param name="target">Game object evaluated against this definition.</param>
        /// <returns>
        /// <see langword="true" /> when the object tag matches this definition;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool Matches(GameObject target)
        {
            return target != null
                && !string.IsNullOrWhiteSpace(TagName)
                && string.Equals(target.tag, TagName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Converts this authored definition into a runtime snapshot.
        /// </summary>
        /// <param name="fallbackReactionKey">
        /// Semantic key used when the authored reaction key is empty.
        /// </param>
        /// <returns>The converted runtime volume material info.</returns>
        public CCProVolumeMaterialInfo ToRuntimeInfo(string fallbackReactionKey)
        {
            return new CCProVolumeMaterialInfo(
                TagName,
                string.IsNullOrWhiteSpace(ReactionKey)
                    ? fallbackReactionKey
                    : ReactionKey,
                Modifiers);
        }

        #endregion
    }

    /// <summary>
    /// Acts as the authored catalog of default and tagged environment materials
    /// used by HandyFSM locomotion and any higher-level reaction systems.
    /// </summary>
    [CreateAssetMenu(
        menuName = "HandyTools/FSM/CCPro/Material Settings",
        fileName = "CCPro Material Settings")]
    public sealed class CCProMaterialSettings : ScriptableObject
    {
        #region Constants

        private const string DefaultSurfaceReactionKey = "default-surface";
        private const string DefaultVolumeReactionKey = "default-volume";

        #endregion

        #region Inspector

        [SerializeField]
        private CCProSurfaceMaterialDefinition _defaultSurface = new();

        [SerializeField]
        private CCProVolumeMaterialDefinition _defaultVolume = new();

        [SerializeField]
        private CCProSurfaceMaterialDefinition[] _surfaces = Array.Empty<
            CCProSurfaceMaterialDefinition>();

        [SerializeField]
        private CCProVolumeMaterialDefinition[] _volumes = Array.Empty<
            CCProVolumeMaterialDefinition>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default surface used when no tagged surface matches.
        /// </summary>
        public CCProSurfaceMaterialInfo DefaultSurface =>
            _defaultSurface.ToRuntimeInfo(DefaultSurfaceReactionKey);

        /// <summary>
        /// Gets the default volume used when no tagged volume matches.
        /// </summary>
        public CCProVolumeMaterialInfo DefaultVolume =>
            _defaultVolume.ToRuntimeInfo(DefaultVolumeReactionKey);

        #endregion

        #region Surface Queries

        /// <summary>
        /// Tries to resolve a tagged surface definition from the supplied game
        /// object.
        /// </summary>
        /// <param name="target">Ground object evaluated against the catalog.</param>
        /// <param name="surfaceInfo">Resolved runtime info when successful.</param>
        /// <returns>
        /// <see langword="true" /> when a tagged surface was found;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool TryGetSurface(
            GameObject target,
            out CCProSurfaceMaterialInfo surfaceInfo)
        {
            for (int i = 0; i < _surfaces.Length; i++)
            {
                var surface = _surfaces[i];

                if (surface != null && surface.Matches(target))
                {
                    surfaceInfo = surface.ToRuntimeInfo(surface.TagName);
                    return true;
                }
            }

            surfaceInfo = DefaultSurface;
            return false;
        }

        /// <summary>
        /// Resolves a surface using the tagged entries and falling back to the
        /// default surface when no match exists.
        /// </summary>
        /// <param name="target">Ground object evaluated against the catalog.</param>
        /// <returns>The resolved runtime surface info.</returns>
        public CCProSurfaceMaterialInfo ResolveSurface(GameObject target)
        {
            return TryGetSurface(target, out CCProSurfaceMaterialInfo surfaceInfo)
                ? surfaceInfo
                : DefaultSurface;
        }

        #endregion

        #region Volume Queries

        /// <summary>
        /// Tries to resolve a tagged volume definition from the supplied game
        /// object.
        /// </summary>
        /// <param name="target">Trigger object evaluated against the catalog.</param>
        /// <param name="volumeInfo">Resolved runtime info when successful.</param>
        /// <returns>
        /// <see langword="true" /> when a tagged volume was found; otherwise,
        /// <see langword="false" />.
        /// </returns>
        public bool TryGetVolume(
            GameObject target,
            out CCProVolumeMaterialInfo volumeInfo)
        {
            for (int i = 0; i < _volumes.Length; i++)
            {
                var volume = _volumes[i];

                if (volume != null && volume.Matches(target))
                {
                    volumeInfo = volume.ToRuntimeInfo(volume.TagName);
                    return true;
                }
            }

            volumeInfo = DefaultVolume;
            return false;
        }

        /// <summary>
        /// Resolves a volume using the tagged entries and falling back to the
        /// default volume when no match exists.
        /// </summary>
        /// <param name="target">Trigger object evaluated against the catalog.</param>
        /// <returns>The resolved runtime volume info.</returns>
        public CCProVolumeMaterialInfo ResolveVolume(GameObject target)
        {
            return TryGetVolume(target, out CCProVolumeMaterialInfo volumeInfo)
                ? volumeInfo
                : DefaultVolume;
        }

        #endregion
    }
}