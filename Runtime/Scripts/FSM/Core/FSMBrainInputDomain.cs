using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Owns runtime input caching, recent-button consumption, and source
    /// binding for one FSM brain.
    /// </summary>
    public sealed class FSMBrainInputDomain
    {
        #region Fields

        private readonly FSMBrain _brain;
        private readonly FSMBrainCCProDomain _characterControllerProDomain;
        private readonly Dictionary<Guid, InputRuntimeState> _inputStates = new();

        private bool _isInputSourceBound;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes one input domain for the provided brain.
        /// </summary>
        /// <param name="brain">The owning brain.</param>
        /// <param name="characterControllerProDomain">
        /// The optional CCPro domain used to receive semantic movement input.
        /// </param>
        public FSMBrainInputDomain(
            FSMBrain brain,
            FSMBrainCCProDomain characterControllerProDomain)
        {
            _brain = brain;
            _characterControllerProDomain = characterControllerProDomain;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the currently configured input source.
        /// </summary>
        public FSMInputSource Source => _brain.ConfiguredInputSource;

        #endregion

        #region Query

        /// <summary>
        /// Gets whether one cached input exists for the provided action
        /// reference.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <returns>True when the action currently has a cached value.</returns>
        public bool Has(InputActionReference actionReference)
        {
            return TryResolveActionId(actionReference, out Guid actionId)
                && Has(actionId);
        }

        /// <summary>
        /// Gets whether one cached input exists for the provided action id.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <returns>True when the action currently has a cached value.</returns>
        public bool Has(Guid actionId)
        {
            return actionId != Guid.Empty && _inputStates.ContainsKey(actionId);
        }

        /// <summary>
        /// Tries to read a cached button value.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="value">Resolved button value when available.</param>
        /// <returns>True when a button value was cached for that action.</returns>
        public bool TryGetButton(InputActionReference actionReference, out bool value)
        {
            value = default;

            return TryResolveActionId(actionReference, out Guid actionId)
                && TryGetButton(actionId, out value);
        }

        /// <summary>
        /// Tries to read a cached button value.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="value">Resolved button value when available.</param>
        /// <returns>True when a button value was cached for that action.</returns>
        public bool TryGetButton(Guid actionId, out bool value)
        {
            value = default;

            if (!TryGetInputState(actionId, out InputRuntimeState state)
                || state.ValueKind != FSMInputValueKind.Button)
            {
                return false;
            }

            value = state.ButtonValue;
            return true;
        }

        /// <summary>
        /// Tries to read a cached float value.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="value">Resolved float value when available.</param>
        /// <returns>True when a float value was cached for that action.</returns>
        public bool TryGetFloat(InputActionReference actionReference, out float value)
        {
            value = default;

            return TryResolveActionId(actionReference, out Guid actionId)
                && TryGetFloat(actionId, out value);
        }

        /// <summary>
        /// Tries to read a cached float value.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="value">Resolved float value when available.</param>
        /// <returns>True when a float value was cached for that action.</returns>
        public bool TryGetFloat(Guid actionId, out float value)
        {
            value = default;

            if (!TryGetInputState(actionId, out InputRuntimeState state)
                || state.ValueKind != FSMInputValueKind.Float)
            {
                return false;
            }

            value = state.FloatValue;
            return true;
        }

        /// <summary>
        /// Tries to read a cached vector value.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="value">Resolved vector value when available.</param>
        /// <returns>True when a vector value was cached for that action.</returns>
        public bool TryGetVector2(InputActionReference actionReference, out Vector2 value)
        {
            value = default;

            return TryResolveActionId(actionReference, out Guid actionId)
                && TryGetVector2(actionId, out value);
        }

        /// <summary>
        /// Tries to read a cached vector value.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="value">Resolved vector value when available.</param>
        /// <returns>True when a vector value was cached for that action.</returns>
        public bool TryGetVector2(Guid actionId, out Vector2 value)
        {
            value = default;

            if (!TryGetInputState(actionId, out InputRuntimeState state)
                || state.ValueKind != FSMInputValueKind.Vector2)
            {
                return false;
            }

            value = state.Vector2Value;
            return true;
        }

        /// <summary>
        /// Tries to read one immutable input snapshot from the cache.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="snapshot">Resolved input snapshot when available.</param>
        /// <returns>True when a cached value exists for that action.</returns>
        public bool TryGetSnapshot(
            InputActionReference actionReference,
            out FSMInputSnapshot snapshot)
        {
            snapshot = default;

            return TryResolveActionId(actionReference, out Guid actionId)
                && TryGetSnapshot(actionId, out snapshot);
        }

        /// <summary>
        /// Tries to read one immutable input snapshot from the cache.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="snapshot">Resolved input snapshot when available.</param>
        /// <returns>True when a cached value exists for that action.</returns>
        public bool TryGetSnapshot(Guid actionId, out FSMInputSnapshot snapshot)
        {
            snapshot = default;

            if (!TryGetInputState(actionId, out InputRuntimeState state))
            {
                return false;
            }

            snapshot = state.ToSnapshot();
            return true;
        }

        /// <summary>
        /// Gets whether one unconsumed button press transition happened
        /// recently enough to still be relevant for fixed-step state logic.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="maxElapsedTimeSeconds">Maximum accepted realtime age.</param>
        /// <returns>True when the button started recently and is still unconsumed.</returns>
        public bool HasRecentButtonStart(
            InputActionReference actionReference,
            float maxElapsedTimeSeconds)
        {
            return TryResolveActionId(actionReference, out Guid actionId)
                && HasRecentButtonStart(actionId, maxElapsedTimeSeconds);
        }

        /// <summary>
        /// Gets whether one unconsumed button press transition happened
        /// recently enough to still be relevant for fixed-step state logic.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="maxElapsedTimeSeconds">Maximum accepted realtime age.</param>
        /// <returns>True when the button started recently and is still unconsumed.</returns>
        public bool HasRecentButtonStart(Guid actionId, float maxElapsedTimeSeconds)
        {
            if (actionId == Guid.Empty || maxElapsedTimeSeconds < 0f)
            {
                return false;
            }

            if (!TryGetInputState(actionId, out InputRuntimeState state)
                || state.ValueKind != FSMInputValueKind.Button
                || double.IsPositiveInfinity(state.ButtonStartedTime)
                || state.ButtonStartedTime <= state.LastConsumedButtonStartedTime)
            {
                return false;
            }

            return Time.realtimeSinceStartupAsDouble - state.ButtonStartedTime
                <= maxElapsedTimeSeconds;
        }

        /// <summary>
        /// Marks one recent button press transition as consumed.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="maxElapsedTimeSeconds">Maximum accepted realtime age.</param>
        /// <returns>True when the recent press existed and was consumed.</returns>
        public bool TryConsumeRecentButtonStart(
            InputActionReference actionReference,
            float maxElapsedTimeSeconds)
        {
            return TryResolveActionId(actionReference, out Guid actionId)
                && TryConsumeRecentButtonStart(actionId, maxElapsedTimeSeconds);
        }

        /// <summary>
        /// Marks one recent button press transition as consumed.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="maxElapsedTimeSeconds">Maximum accepted realtime age.</param>
        /// <returns>True when the recent press existed and was consumed.</returns>
        public bool TryConsumeRecentButtonStart(
            Guid actionId,
            float maxElapsedTimeSeconds)
        {
            if (!HasRecentButtonStart(actionId, maxElapsedTimeSeconds)
                || !TryGetInputState(actionId, out InputRuntimeState state))
            {
                return false;
            }

            state.LastConsumedButtonStartedTime = state.ButtonStartedTime;
            return true;
        }

        /// <summary>
        /// Copies every currently cached input snapshot into the provided list.
        /// </summary>
        /// <param name="results">Target list that receives the current snapshots.</param>
        public void CopySnapshots(List<FSMInputSnapshot> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            foreach (KeyValuePair<Guid, InputRuntimeState> entry in _inputStates)
            {
                results.Add(entry.Value.ToSnapshot());
            }

            results.Sort(static (left, right) => string.Compare(
                left.EffectiveDisplayName,
                right.EffectiveDisplayName,
                StringComparison.Ordinal));
        }

        #endregion

        #region Mutation

        /// <summary>
        /// Stores one button value in the cache.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="value">Current button value.</param>
        /// <param name="displayName">Optional display name override.</param>
        /// <returns>True when the action id could be resolved successfully.</returns>
        public bool SetButtonValue(
            InputActionReference actionReference,
            bool value,
            string displayName = null)
        {
            return TryResolveActionId(actionReference, out Guid actionId)
                && SetButtonValue(
                    actionId,
                    ResolveDisplayName(actionReference, displayName),
                    value);
        }

        /// <summary>
        /// Stores one button value in the cache.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="displayName">Display name used by diagnostics.</param>
        /// <param name="value">Current button value.</param>
        /// <returns>True when the action id was valid.</returns>
        public bool SetButtonValue(Guid actionId, string displayName, bool value)
        {
            if (actionId == Guid.Empty)
            {
                return false;
            }

            double currentTime = Time.realtimeSinceStartupAsDouble;
            InputRuntimeState state = GetOrCreateInputState(
                actionId,
                displayName,
                FSMInputValueKind.Button);

            bool previousValue = state.ButtonValue;

            if (!previousValue && value)
            {
                state.ButtonStartedFrame = Time.frameCount;
                state.ButtonStartedTime = currentTime;
            }
            else if (previousValue && !value)
            {
                state.ButtonCanceledFrame = Time.frameCount;
                state.ButtonCanceledTime = currentTime;
            }

            state.ButtonValue = value;
            state.LastUpdatedTime = currentTime;
            return true;
        }

        /// <summary>
        /// Stores one float value in the cache.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="value">Current float value.</param>
        /// <param name="displayName">Optional display name override.</param>
        /// <returns>True when the action id could be resolved successfully.</returns>
        public bool SetFloatValue(
            InputActionReference actionReference,
            float value,
            string displayName = null)
        {
            return TryResolveActionId(actionReference, out Guid actionId)
                && SetFloatValue(
                    actionId,
                    ResolveDisplayName(actionReference, displayName),
                    value);
        }

        /// <summary>
        /// Stores one float value in the cache.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="displayName">Display name used by diagnostics.</param>
        /// <param name="value">Current float value.</param>
        /// <returns>True when the action id was valid.</returns>
        public bool SetFloatValue(Guid actionId, string displayName, float value)
        {
            if (actionId == Guid.Empty)
            {
                return false;
            }

            InputRuntimeState state = GetOrCreateInputState(
                actionId,
                displayName,
                FSMInputValueKind.Float);

            state.FloatValue = value;
            state.LastUpdatedTime = Time.realtimeSinceStartupAsDouble;
            return true;
        }

        /// <summary>
        /// Stores one vector value in the cache.
        /// </summary>
        /// <param name="actionReference">Action reference that identifies the input.</param>
        /// <param name="value">Current vector value.</param>
        /// <param name="displayName">Optional display name override.</param>
        /// <returns>True when the action id could be resolved successfully.</returns>
        public bool SetVector2Value(
            InputActionReference actionReference,
            Vector2 value,
            string displayName = null)
        {
            return TryResolveActionId(actionReference, out Guid actionId)
                && SetVector2Value(
                    actionId,
                    ResolveDisplayName(actionReference, displayName),
                    value);
        }

        /// <summary>
        /// Stores one vector value in the cache.
        /// </summary>
        /// <param name="actionId">Unique action identifier.</param>
        /// <param name="displayName">Display name used by diagnostics.</param>
        /// <param name="value">Current vector value.</param>
        /// <returns>True when the action id was valid.</returns>
        public bool SetVector2Value(Guid actionId, string displayName, Vector2 value)
        {
            if (actionId == Guid.Empty)
            {
                return false;
            }

            InputRuntimeState state = GetOrCreateInputState(
                actionId,
                displayName,
                FSMInputValueKind.Vector2);

            state.Vector2Value = value;
            state.LastUpdatedTime = Time.realtimeSinceStartupAsDouble;
            return true;
        }

        /// <summary>
        /// Clears every cached runtime input currently owned by this domain.
        /// </summary>
        public void ClearValues()
        {
            _inputStates.Clear();
            ClearReportedMovementInput();
        }

        #endregion

        #region Binding

        /// <summary>
        /// Binds the configured input source to the owning brain.
        /// </summary>
        internal void BindSource()
        {
            if (_isInputSourceBound)
            {
                return;
            }

            if (_brain.ConfiguredInputSource == null)
            {
                _brain.ConfiguredInputSource = _brain.GetComponent<FSMInputSource>();
            }

            if (_brain.ConfiguredInputSource == null)
            {
                return;
            }

            _brain.ConfiguredInputSource.BindToBrain(_brain);
            _isInputSourceBound = true;
        }

        /// <summary>
        /// Assigns one colocated input source when the brain still has no
        /// explicit source configured.
        /// </summary>
        /// <param name="inputSource">Input source discovered on this GameObject.</param>
        internal void TryAssignSource(FSMInputSource inputSource)
        {
            if (inputSource == null || inputSource.gameObject != _brain.gameObject)
            {
                return;
            }

            if (_brain.ConfiguredInputSource != null
                && _brain.ConfiguredInputSource != inputSource)
            {
                return;
            }

            _brain.ConfiguredInputSource = inputSource;
        }

        /// <summary>
        /// Unbinds the configured input source from the owning brain.
        /// </summary>
        internal void UnbindSource()
        {
            if (!_isInputSourceBound)
            {
                return;
            }

            _brain.ConfiguredInputSource?.UnbindFromBrain(_brain);
            _isInputSourceBound = false;
            ClearValues();
        }

        #endregion

        #region CCPro Feed

        /// <summary>
        /// Stores the semantic movement input reported by the bound input
        /// source for the optional CCPro movement-reference runtime.
        /// </summary>
        /// <param name="value">Current semantic movement input.</param>
        internal void SetReportedMovementInput(Vector2 value)
        {
            _characterControllerProDomain.SetReportedMovementInput(value);
        }

        /// <summary>
        /// Clears the semantic movement input reported by the bound input
        /// source for the optional CCPro movement-reference runtime.
        /// </summary>
        internal void ClearReportedMovementInput()
        {
            _characterControllerProDomain.ClearReportedMovementInput();
        }

        #endregion

        #region Internal Helpers

        private bool TryGetInputState(Guid actionId, out InputRuntimeState state)
        {
            state = null;

            return actionId != Guid.Empty
                && _inputStates.TryGetValue(actionId, out state);
        }

        private InputRuntimeState GetOrCreateInputState(
            Guid actionId,
            string displayName,
            FSMInputValueKind valueKind)
        {
            if (!_inputStates.TryGetValue(actionId, out InputRuntimeState state))
            {
                state = new InputRuntimeState(actionId);
                _inputStates.Add(actionId, state);
            }

            state.ValueKind = valueKind;

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                state.DisplayName = displayName;
            }

            return state;
        }

        private static bool TryResolveActionId(
            InputActionReference actionReference,
            out Guid actionId)
        {
            actionId = actionReference != null && actionReference.action != null
                ? actionReference.action.id
                : Guid.Empty;

            return actionId != Guid.Empty;
        }

        private static string ResolveDisplayName(
            InputActionReference actionReference,
            string displayName)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            return actionReference != null && actionReference.action != null
                ? actionReference.action.name
                : string.Empty;
        }

        #endregion
    }
}