using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Exposes the current environment movement and gravity multipliers used by
    /// CCPro-aware HandyFSM states.
    /// </summary>
    public interface ICCProEnvironmentModifierSource
    {
        /// <summary>
        /// Gets the current surface movement multipliers.
        /// </summary>
        CCProSurfaceModifiers CurrentSurface { get; }

        /// <summary>
        /// Gets the current volume movement and gravity multipliers.
        /// </summary>
        CCProVolumeModifiers CurrentVolume { get; }

        /// <summary>
        /// Gets the current resolved surface material information.
        /// </summary>
        CCProSurfaceMaterialInfo CurrentSurfaceInfo { get; }

        /// <summary>
        /// Gets the current resolved volume material information.
        /// </summary>
        CCProVolumeMaterialInfo CurrentVolumeInfo { get; }
    }

    /// <summary>
    /// Stores movement multipliers contributed by the current surface.
    /// </summary>
    [Serializable]
    public readonly struct CCProSurfaceModifiers : IEquatable<CCProSurfaceModifiers>
    {
        #region Constants

        /// <summary>
        /// Neutral surface modifiers that leave movement unchanged.
        /// </summary>
        public static readonly CCProSurfaceModifiers Neutral = new(1f, 1f, 1f);

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one surface modifier snapshot.
        /// </summary>
        /// <param name="accelerationMultiplier">
        /// Multiplier applied to grounded acceleration.
        /// </param>
        /// <param name="decelerationMultiplier">
        /// Multiplier applied to grounded deceleration.
        /// </param>
        /// <param name="speedMultiplier">
        /// Multiplier applied to authored speed limits.
        /// </param>
        public CCProSurfaceModifiers(
            float accelerationMultiplier,
            float decelerationMultiplier,
            float speedMultiplier)
        {
            AccelerationMultiplier = Mathf.Max(0.01f, accelerationMultiplier);
            DecelerationMultiplier = Mathf.Max(0.01f, decelerationMultiplier);
            SpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the multiplier applied to acceleration.
        /// </summary>
        public float AccelerationMultiplier { get; }

        /// <summary>
        /// Gets the multiplier applied to deceleration.
        /// </summary>
        public float DecelerationMultiplier { get; }

        /// <summary>
        /// Gets the multiplier applied to speed.
        /// </summary>
        public float SpeedMultiplier { get; }

        #endregion

        #region Equality

        /// <inheritdoc />
        public bool Equals(CCProSurfaceModifiers other)
        {
            return AccelerationMultiplier.Equals(other.AccelerationMultiplier)
                && DecelerationMultiplier.Equals(other.DecelerationMultiplier)
                && SpeedMultiplier.Equals(other.SpeedMultiplier);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CCProSurfaceModifiers other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(
                AccelerationMultiplier,
                DecelerationMultiplier,
                SpeedMultiplier);
        }

        public static bool operator ==(
            CCProSurfaceModifiers left,
            CCProSurfaceModifiers right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CCProSurfaceModifiers left,
            CCProSurfaceModifiers right)
        {
            return !left.Equals(right);
        }

        #endregion
    }

    /// <summary>
    /// Stores movement and gravity multipliers contributed by the current volume.
    /// </summary>
    [Serializable]
    public readonly struct CCProVolumeModifiers : IEquatable<CCProVolumeModifiers>
    {
        #region Constants

        /// <summary>
        /// Neutral volume modifiers that leave movement and gravity unchanged.
        /// </summary>
        public static readonly CCProVolumeModifiers Neutral =
            new(1f, 1f, 1f, 1f, 1f);

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one volume modifier snapshot.
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
        public CCProVolumeModifiers(
            float accelerationMultiplier,
            float decelerationMultiplier,
            float speedMultiplier,
            float gravityAscendingMultiplier,
            float gravityDescendingMultiplier)
        {
            AccelerationMultiplier = Mathf.Max(0.01f, accelerationMultiplier);
            DecelerationMultiplier = Mathf.Max(0.01f, decelerationMultiplier);
            SpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            GravityAscendingMultiplier = Mathf.Clamp(
                gravityAscendingMultiplier,
                0.05f,
                50f);
            GravityDescendingMultiplier = Mathf.Clamp(
                gravityDescendingMultiplier,
                0.05f,
                50f);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the multiplier applied to acceleration.
        /// </summary>
        public float AccelerationMultiplier { get; }

        /// <summary>
        /// Gets the multiplier applied to deceleration.
        /// </summary>
        public float DecelerationMultiplier { get; }

        /// <summary>
        /// Gets the multiplier applied to speed.
        /// </summary>
        public float SpeedMultiplier { get; }

        /// <summary>
        /// Gets the multiplier applied while the actor is ascending.
        /// </summary>
        public float GravityAscendingMultiplier { get; }

        /// <summary>
        /// Gets the multiplier applied while the actor is descending.
        /// </summary>
        public float GravityDescendingMultiplier { get; }

        #endregion

        #region Equality

        /// <inheritdoc />
        public bool Equals(CCProVolumeModifiers other)
        {
            return AccelerationMultiplier.Equals(other.AccelerationMultiplier)
                && DecelerationMultiplier.Equals(other.DecelerationMultiplier)
                && SpeedMultiplier.Equals(other.SpeedMultiplier)
                && GravityAscendingMultiplier.Equals(
                    other.GravityAscendingMultiplier)
                && GravityDescendingMultiplier.Equals(
                    other.GravityDescendingMultiplier);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CCProVolumeModifiers other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(
                AccelerationMultiplier,
                DecelerationMultiplier,
                SpeedMultiplier,
                GravityAscendingMultiplier,
                GravityDescendingMultiplier);
        }

        public static bool operator ==(
            CCProVolumeModifiers left,
            CCProVolumeModifiers right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CCProVolumeModifiers left,
            CCProVolumeModifiers right)
        {
            return !left.Equals(right);
        }

        #endregion
    }

    /// <summary>
    /// Describes one resolved surface material together with the movement data
    /// used by locomotion states and the semantic key used by reaction systems.
    /// </summary>
    [Serializable]
    public readonly struct CCProSurfaceMaterialInfo :
        IEquatable<CCProSurfaceMaterialInfo>
    {
        #region Constants

        /// <summary>
        /// Default unnamed surface material information.
        /// </summary>
        public static readonly CCProSurfaceMaterialInfo Default = new(
            string.Empty,
            "default-surface",
            Color.white,
            CCProSurfaceModifiers.Neutral);

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one resolved surface material snapshot.
        /// </summary>
        /// <param name="tagName">Unity tag that resolved this surface.</param>
        /// <param name="reactionKey">
        /// Semantic key that gameplay, VFX, or audio systems can react to.
        /// </param>
        /// <param name="debugColor">Inspector or gizmo color for the surface.</param>
        /// <param name="modifiers">Runtime movement modifiers.</param>
        public CCProSurfaceMaterialInfo(
            string tagName,
            string reactionKey,
            Color debugColor,
            CCProSurfaceModifiers modifiers)
        {
            TagName = tagName ?? string.Empty;
            ReactionKey = string.IsNullOrWhiteSpace(reactionKey)
                ? string.IsNullOrWhiteSpace(TagName)
                    ? Default.ReactionKey
                    : TagName
                : reactionKey;
            DebugColor = debugColor;
            Modifiers = modifiers;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Unity tag that resolved this surface.
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// Gets the semantic key used by reaction systems.
        /// </summary>
        public string ReactionKey { get; }

        /// <summary>
        /// Gets the debug color associated with the surface.
        /// </summary>
        public Color DebugColor { get; }

        /// <summary>
        /// Gets the resolved movement modifiers.
        /// </summary>
        public CCProSurfaceModifiers Modifiers { get; }

        #endregion

        #region Equality

        /// <inheritdoc />
        public bool Equals(CCProSurfaceMaterialInfo other)
        {
            return string.Equals(TagName, other.TagName, StringComparison.Ordinal)
                && string.Equals(
                    ReactionKey,
                    other.ReactionKey,
                    StringComparison.Ordinal)
                && DebugColor.Equals(other.DebugColor)
                && Modifiers.Equals(other.Modifiers);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CCProSurfaceMaterialInfo other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(
                TagName,
                ReactionKey,
                DebugColor,
                Modifiers);
        }

        public static bool operator ==(
            CCProSurfaceMaterialInfo left,
            CCProSurfaceMaterialInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CCProSurfaceMaterialInfo left,
            CCProSurfaceMaterialInfo right)
        {
            return !left.Equals(right);
        }

        #endregion
    }

    /// <summary>
    /// Describes one resolved volume material together with the movement data
    /// used by locomotion states and the semantic key used by reaction systems.
    /// </summary>
    [Serializable]
    public readonly struct CCProVolumeMaterialInfo :
        IEquatable<CCProVolumeMaterialInfo>
    {
        #region Constants

        /// <summary>
        /// Default unnamed volume material information.
        /// </summary>
        public static readonly CCProVolumeMaterialInfo Default = new(
            string.Empty,
            "default-volume",
            CCProVolumeModifiers.Neutral);

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one resolved volume material snapshot.
        /// </summary>
        /// <param name="tagName">Unity tag that resolved this volume.</param>
        /// <param name="reactionKey">
        /// Semantic key that gameplay, VFX, or audio systems can react to.
        /// </param>
        /// <param name="modifiers">Runtime movement and gravity modifiers.</param>
        public CCProVolumeMaterialInfo(
            string tagName,
            string reactionKey,
            CCProVolumeModifiers modifiers)
        {
            TagName = tagName ?? string.Empty;
            ReactionKey = string.IsNullOrWhiteSpace(reactionKey)
                ? string.IsNullOrWhiteSpace(TagName)
                    ? Default.ReactionKey
                    : TagName
                : reactionKey;
            Modifiers = modifiers;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Unity tag that resolved this volume.
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// Gets the semantic key used by reaction systems.
        /// </summary>
        public string ReactionKey { get; }

        /// <summary>
        /// Gets the resolved movement and gravity modifiers.
        /// </summary>
        public CCProVolumeModifiers Modifiers { get; }

        #endregion

        #region Equality

        /// <inheritdoc />
        public bool Equals(CCProVolumeMaterialInfo other)
        {
            return string.Equals(TagName, other.TagName, StringComparison.Ordinal)
                && string.Equals(
                    ReactionKey,
                    other.ReactionKey,
                    StringComparison.Ordinal)
                && Modifiers.Equals(other.Modifiers);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CCProVolumeMaterialInfo other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(TagName, ReactionKey, Modifiers);
        }

        public static bool operator ==(
            CCProVolumeMaterialInfo left,
            CCProVolumeMaterialInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CCProVolumeMaterialInfo left,
            CCProVolumeMaterialInfo right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}