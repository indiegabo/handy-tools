using System;
using Lightbug.CharacterControllerPro.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Stores the authored default movement modifiers used by one environment source.
    /// </summary>
    [Serializable]
    public struct CCProSurfaceModifierSettings
    {
        #region Inspector

        [Min(0.01f)]
        [SerializeField]
        private float _accelerationMultiplier;

        [Min(0.01f)]
        [SerializeField]
        private float _decelerationMultiplier;

        [Min(0.01f)]
        [SerializeField]
        private float _speedMultiplier;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one authored surface modifier block.
        /// </summary>
        /// <param name="accelerationMultiplier">
        /// Multiplier applied to grounded acceleration.
        /// </param>
        /// <param name="decelerationMultiplier">
        /// Multiplier applied to grounded deceleration.
        /// </param>
        /// <param name="speedMultiplier">
        /// Multiplier applied to speed.
        /// </param>
        public CCProSurfaceModifierSettings(
            float accelerationMultiplier,
            float decelerationMultiplier,
            float speedMultiplier)
        {
            _accelerationMultiplier = Mathf.Max(0.01f, accelerationMultiplier);
            _decelerationMultiplier = Mathf.Max(0.01f, decelerationMultiplier);
            _speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Converts the authored values into the runtime modifier format.
        /// </summary>
        /// <returns>The converted runtime surface modifiers.</returns>
        public CCProSurfaceModifiers ToRuntimeModifiers()
        {
            return new CCProSurfaceModifiers(
                _accelerationMultiplier,
                _decelerationMultiplier,
                _speedMultiplier);
        }

        #endregion
    }

    /// <summary>
    /// Stores the authored default volume modifiers used by one environment source.
    /// </summary>
    [Serializable]
    public struct CCProVolumeModifierSettings
    {
        #region Inspector

        [Min(0.01f)]
        [SerializeField]
        private float _accelerationMultiplier;

        [Min(0.01f)]
        [SerializeField]
        private float _decelerationMultiplier;

        [Min(0.01f)]
        [SerializeField]
        private float _speedMultiplier;

        [Range(0.05f, 50f)]
        [SerializeField]
        private float _gravityAscendingMultiplier;

        [Range(0.05f, 50f)]
        [SerializeField]
        private float _gravityDescendingMultiplier;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one authored volume modifier block.
        /// </summary>
        /// <param name="accelerationMultiplier">
        /// Multiplier applied to acceleration.
        /// </param>
        /// <param name="decelerationMultiplier">
        /// Multiplier applied to deceleration.
        /// </param>
        /// <param name="speedMultiplier">
        /// Multiplier applied to speed.
        /// </param>
        /// <param name="gravityAscendingMultiplier">
        /// Multiplier applied while the actor is ascending.
        /// </param>
        /// <param name="gravityDescendingMultiplier">
        /// Multiplier applied while the actor is descending.
        /// </param>
        public CCProVolumeModifierSettings(
            float accelerationMultiplier,
            float decelerationMultiplier,
            float speedMultiplier,
            float gravityAscendingMultiplier,
            float gravityDescendingMultiplier)
        {
            _accelerationMultiplier = Mathf.Max(0.01f, accelerationMultiplier);
            _decelerationMultiplier = Mathf.Max(0.01f, decelerationMultiplier);
            _speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            _gravityAscendingMultiplier = Mathf.Clamp(
                gravityAscendingMultiplier,
                0.05f,
                50f);
            _gravityDescendingMultiplier = Mathf.Clamp(
                gravityDescendingMultiplier,
                0.05f,
                50f);
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Converts the authored values into the runtime modifier format.
        /// </summary>
        /// <returns>The converted runtime volume modifiers.</returns>
        public CCProVolumeModifiers ToRuntimeModifiers()
        {
            return new CCProVolumeModifiers(
                _accelerationMultiplier,
                _decelerationMultiplier,
                _speedMultiplier,
                _gravityAscendingMultiplier,
                _gravityDescendingMultiplier);
        }

        #endregion
    }

    /// <summary>
    /// Provides a HandyTools-owned runtime source for movement and gravity modifiers.
    /// </summary>
    [AddComponentMenu("HandyTools/FSM/CCPro/Environment Source")]
    [DisallowMultipleComponent]
    public sealed class CCProEnvironmentSource : MonoBehaviour,
        ICCProEnvironmentModifierSource
    {
        #region Inspector

        [SerializeField]
        private CCProMaterialSettings _materialSettings;

        [SerializeField]
        private bool _synchronizeWithCharacterActor = true;

        [SerializeField]
        private bool _resetToDefaultsOnEnable = true;

        [SerializeField]
        private CCProSurfaceModifierSettings _defaultSurface =
            new(1f, 1f, 1f);

        [SerializeField]
        private CCProVolumeModifierSettings _defaultVolume =
            new(1f, 1f, 1f, 1f, 1f);

        #endregion

        #region Fields

        private CharacterActor _characterActor;

        private CCProSurfaceMaterialInfo _currentSurfaceInfo =
            CCProSurfaceMaterialInfo.Default;

        private CCProVolumeMaterialInfo _currentVolumeInfo =
            CCProVolumeMaterialInfo.Default;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current surface modifiers.
        /// </summary>
        public CCProSurfaceModifiers CurrentSurface => _currentSurfaceInfo.Modifiers;

        /// <summary>
        /// Gets the current volume modifiers.
        /// </summary>
        public CCProVolumeModifiers CurrentVolume => _currentVolumeInfo.Modifiers;

        /// <summary>
        /// Gets the current resolved surface material information.
        /// </summary>
        public CCProSurfaceMaterialInfo CurrentSurfaceInfo => _currentSurfaceInfo;

        /// <summary>
        /// Gets the current resolved volume material information.
        /// </summary>
        public CCProVolumeMaterialInfo CurrentVolumeInfo => _currentVolumeInfo;

        /// <summary>
        /// Gets the authored default surface modifiers.
        /// </summary>
        public CCProSurfaceModifiers DefaultSurface =>
            DefaultSurfaceInfo.Modifiers;

        /// <summary>
        /// Gets the authored default volume modifiers.
        /// </summary>
        public CCProVolumeModifiers DefaultVolume =>
            DefaultVolumeInfo.Modifiers;

        /// <summary>
        /// Gets the authored default surface material information.
        /// </summary>
        public CCProSurfaceMaterialInfo DefaultSurfaceInfo => _materialSettings != null
            ? _materialSettings.DefaultSurface
            : new CCProSurfaceMaterialInfo(
                string.Empty,
                CCProSurfaceMaterialInfo.Default.ReactionKey,
                Color.white,
                _defaultSurface.ToRuntimeModifiers());

        /// <summary>
        /// Gets the authored default volume material information.
        /// </summary>
        public CCProVolumeMaterialInfo DefaultVolumeInfo => _materialSettings != null
            ? _materialSettings.DefaultVolume
            : new CCProVolumeMaterialInfo(
                string.Empty,
                CCProVolumeMaterialInfo.Default.ReactionKey,
                _defaultVolume.ToRuntimeModifiers());

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Resolves the character actor in the current branch.
        /// </summary>
        private void Awake()
        {
            ResolveCharacterActor();
        }

        /// <summary>
        /// Restores the authored defaults whenever the component becomes active.
        /// </summary>
        private void OnEnable()
        {
            if (_resetToDefaultsOnEnable)
            {
                ResetToDefaults();
            }
        }

        /// <summary>
        /// Synchronizes the current environment from CharacterActor contacts when
        /// automatic material resolution is enabled.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_synchronizeWithCharacterActor || _materialSettings == null)
            {
                return;
            }

            ResolveCharacterActor();

            if (_characterActor == null)
            {
                return;
            }

            RefreshCurrentSurface();
            RefreshCurrentVolume();
        }

        #endregion

        #region Runtime API

        /// <summary>
        /// Replaces the current surface modifiers.
        /// </summary>
        /// <param name="surfaceModifiers">The new current surface modifiers.</param>
        public void SetCurrentSurface(CCProSurfaceModifiers surfaceModifiers)
        {
            SetCurrentSurface(new CCProSurfaceMaterialInfo(
                _currentSurfaceInfo.TagName,
                _currentSurfaceInfo.ReactionKey,
                _currentSurfaceInfo.DebugColor,
                surfaceModifiers));
        }

        /// <summary>
        /// Replaces the current surface material information.
        /// </summary>
        /// <param name="surfaceInfo">The new current surface material info.</param>
        public void SetCurrentSurface(CCProSurfaceMaterialInfo surfaceInfo)
        {
            _currentSurfaceInfo = surfaceInfo;
        }

        /// <summary>
        /// Replaces the current volume modifiers.
        /// </summary>
        /// <param name="volumeModifiers">The new current volume modifiers.</param>
        public void SetCurrentVolume(CCProVolumeModifiers volumeModifiers)
        {
            SetCurrentVolume(new CCProVolumeMaterialInfo(
                _currentVolumeInfo.TagName,
                _currentVolumeInfo.ReactionKey,
                volumeModifiers));
        }

        /// <summary>
        /// Replaces the current volume material information.
        /// </summary>
        /// <param name="volumeInfo">The new current volume material info.</param>
        public void SetCurrentVolume(CCProVolumeMaterialInfo volumeInfo)
        {
            _currentVolumeInfo = volumeInfo;
        }

        /// <summary>
        /// Replaces both the current surface and volume modifiers.
        /// </summary>
        /// <param name="surfaceModifiers">The new current surface modifiers.</param>
        /// <param name="volumeModifiers">The new current volume modifiers.</param>
        public void SetCurrentEnvironment(
            CCProSurfaceModifiers surfaceModifiers,
            CCProVolumeModifiers volumeModifiers)
        {
            SetCurrentSurface(surfaceModifiers);
            SetCurrentVolume(volumeModifiers);
        }

        /// <summary>
        /// Replaces both the current surface and volume material information.
        /// </summary>
        /// <param name="surfaceInfo">The new current surface material info.</param>
        /// <param name="volumeInfo">The new current volume material info.</param>
        public void SetCurrentEnvironment(
            CCProSurfaceMaterialInfo surfaceInfo,
            CCProVolumeMaterialInfo volumeInfo)
        {
            _currentSurfaceInfo = surfaceInfo;
            _currentVolumeInfo = volumeInfo;
        }

        /// <summary>
        /// Restores the current surface modifiers to the authored defaults.
        /// </summary>
        public void ResetSurfaceToDefault()
        {
            _currentSurfaceInfo = DefaultSurfaceInfo;
        }

        /// <summary>
        /// Restores the current volume modifiers to the authored defaults.
        /// </summary>
        public void ResetVolumeToDefault()
        {
            _currentVolumeInfo = DefaultVolumeInfo;
        }

        /// <summary>
        /// Restores both current modifier sets to the authored defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            _currentSurfaceInfo = DefaultSurfaceInfo;
            _currentVolumeInfo = DefaultVolumeInfo;
        }

        #endregion

        #region Internal Queries

        /// <summary>
        /// Resolves the CharacterActor reference from the current branch.
        /// </summary>
        private void ResolveCharacterActor()
        {
            if (_characterActor == null)
            {
                _characterActor = GetComponentInParent<CharacterActor>();
            }
        }

        /// <summary>
        /// Reads the grounded collider and resolves the current surface material.
        /// </summary>
        private void RefreshCurrentSurface()
        {
            if (!_characterActor.IsGrounded)
            {
                ResetSurfaceToDefault();
                return;
            }

            GameObject groundObject = _characterActor.GroundObject;

            if (groundObject == null)
            {
                ResetSurfaceToDefault();
                return;
            }

            SetCurrentSurface(_materialSettings.ResolveSurface(groundObject));
        }

        /// <summary>
        /// Reads overlapping triggers and resolves the current volume material.
        /// </summary>
        private void RefreshCurrentVolume()
        {
            GameObject triggerObject = _characterActor.CurrentTrigger.gameObject;

            if (triggerObject == null)
            {
                ResetVolumeToDefault();
                return;
            }

            if (_materialSettings.TryGetVolume(
                triggerObject,
                out CCProVolumeMaterialInfo volumeInfo))
            {
                SetCurrentVolume(volumeInfo);
                return;
            }

            int triggerCount = _characterActor.Triggers.Count;

            for (int i = triggerCount - 1; i >= 0; i--)
            {
                if (_materialSettings.TryGetVolume(
                    _characterActor.Triggers[i].gameObject,
                    out volumeInfo))
                {
                    SetCurrentVolume(volumeInfo);
                    return;
                }
            }

            ResetVolumeToDefault();
        }

        #endregion
    }
}