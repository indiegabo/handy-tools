using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Owns Simple Blackboard availability checks and value access for one
    /// FSM brain.
    /// </summary>
    public sealed class FSMBrainBlackboardDomain
    {
        #region Fields

        private readonly FSMBrain _brain;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes one blackboard domain for the provided brain.
        /// </summary>
        /// <param name="brain">The owning brain.</param>
        public FSMBrainBlackboardDomain(FSMBrain brain)
        {
            _brain = brain;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the optional Simple Blackboard integration is enabled
        /// and available for the owning brain.
        /// </summary>
        public bool IsEnabled => _brain.UseSimpleBlackboard;

        /// <summary>
        /// Gets whether the owning brain currently exposes a valid blackboard
        /// instance.
        /// </summary>
        public bool HasBlackboard => TryGetBlackboard(out _);

        /// <summary>
        /// Gets the configured blackboard container component.
        /// </summary>
        public Component Container => _brain.ConfiguredBlackboardContainer;

        /// <summary>
        /// Gets the raw blackboard object exposed by the configured container.
        /// </summary>
        public object Value => TryGetBlackboard(out object blackboard)
            ? blackboard
            : null;

        #endregion

        #region Public API

        /// <summary>
        /// Tries to read a typed value from the configured blackboard.
        /// </summary>
        /// <typeparam name="T">The value type to read.</typeparam>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <param name="value">The resolved value if found.</param>
        /// <returns>True if the value exists and matches the requested type.</returns>
        public bool TryGetValue<T>(string propertyName, out T value)
        {
            if (!TryGetBlackboard(out object blackboard))
            {
                value = default;
                return false;
            }

            return FSMBrain.SimpleBlackboardBridge.TryGetValue(
                blackboard,
                propertyName,
                out value);
        }

        /// <summary>
        /// Writes a typed value into the configured blackboard.
        /// </summary>
        /// <typeparam name="T">The value type to write.</typeparam>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>True if the value was written successfully.</returns>
        public bool SetValue<T>(string propertyName, T value)
        {
            if (!TryGetBlackboard(out object blackboard))
            {
                return false;
            }

            return FSMBrain.SimpleBlackboardBridge.SetValue(
                blackboard,
                propertyName,
                value);
        }

        /// <summary>
        /// Tries to read an untyped value from the configured blackboard.
        /// </summary>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <param name="value">The resolved value if found.</param>
        /// <returns>True if the value exists.</returns>
        public bool TryGetObjectValue(string propertyName, out object value)
        {
            if (!TryGetBlackboard(out object blackboard))
            {
                value = null;
                return false;
            }

            return FSMBrain.SimpleBlackboardBridge.TryGetObjectValue(
                blackboard,
                propertyName,
                out value);
        }

        /// <summary>
        /// Gets whether the configured blackboard contains a property.
        /// </summary>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <returns>True if the property exists.</returns>
        public bool ContainsValue(string propertyName)
        {
            if (!TryGetBlackboard(out object blackboard))
            {
                return false;
            }

            return FSMBrain.SimpleBlackboardBridge.ContainsValue(
                blackboard,
                propertyName);
        }

        #endregion

        #region Internal Helpers

        private bool TryGetBlackboard(out object blackboard)
        {
            blackboard = null;

            return IsEnabled
                && FSMBrain.SimpleBlackboardBridge.TryGetBlackboard(
                    Container,
                    out blackboard);
        }

        #endregion
    }
}