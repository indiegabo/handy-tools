using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Synchronizes animator parameters from matching Simple Blackboard values.
    /// </summary>
    [AddComponentMenu("HandyTools/FSM/Animator Syncer")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AnimatorSyncer : MonoBehaviour
    {
        #region Inspector

        /// <summary>
        /// Animator that receives the synchronized parameter values.
        /// </summary>
        [SerializeField]
        private Animator _animator;

        /// <summary>
        /// Simple Blackboard container used as the canonical source of animator values.
        /// </summary>
        [SerializeField]
        private Component _blackboardContainer;

        /// <summary>
        /// Rebuilds bindings automatically when the configuration changes in the editor.
        /// </summary>
        [SerializeField]
        private bool _rebuildBindingsAutomatically = true;

        /// <summary>
        /// Generated bindings that map animator parameters to blackboard keys.
        /// </summary>
        [SerializeField]
        private List<AnimatorSyncBinding> _bindings = new();

        #endregion

        #region State

        private readonly Dictionary<string, Type> _propertyMetadata =
            new(StringComparer.Ordinal);

        #endregion

        #region Unity Messages

        /// <summary>
        /// Resolves obvious local references when the component is first added.
        /// </summary>
        private void Reset()
        {
            TryResolveMissingReferences();
            TryAutoRebuildBindings();
        }

        /// <summary>
        /// Ensures editor-time bindings stay aligned when the component becomes active.
        /// </summary>
        private void OnEnable()
        {
            TryResolveMissingReferences();
            TryAutoRebuildBindings();
        }

        /// <summary>
        /// Keeps the generated bindings aligned with inspector changes.
        /// </summary>
        private void OnValidate()
        {
            TryResolveMissingReferences();
            TryAutoRebuildBindings();
        }

        /// <summary>
        /// Pushes the current blackboard values into the configured animator during play mode.
        /// </summary>
        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SyncAnimator();
        }

        #endregion

        #region Inspector Actions

        /// <summary>
        /// Rebuilds the parameter bindings from the current animator and blackboard configuration.
        /// </summary>
        [ContextMenu("Rebuild Bindings")]
        private void RebuildBindings()
        {
            BuildBindings();
        }

        #endregion

        #region Binding Generation

        /// <summary>
        /// Rebuilds bindings automatically when the component is configured to do so.
        /// </summary>
        private void TryAutoRebuildBindings()
        {
            if (!_rebuildBindingsAutomatically)
            {
                return;
            }

            BuildBindings();
        }

        /// <summary>
        /// Generates one binding row per animator parameter using exact name matches.
        /// </summary>
        private void BuildBindings()
        {
            _bindings.Clear();

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            if (!TryGetResolvedBlackboard(out object blackboard, !Application.isPlaying)
                || !FSMBrain.SimpleBlackboardBridge.TryGetPropertyMetadata(
                    blackboard,
                    _propertyMetadata))
            {
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;

            foreach (AnimatorControllerParameter parameter in parameters)
            {
                AnimatorSyncBinding binding = new()
                {
                    _parameterName = parameter.name,
                    _parameterHash = parameter.nameHash,
                    _blackboardKey = parameter.name,
                    _parameterType = parameter.type,
                    _status = "Missing blackboard key.",
                    _sourceTypeName = "None"
                };

                if (!_propertyMetadata.TryGetValue(parameter.name, out Type valueType))
                {
                    _bindings.Add(binding);
                    continue;
                }

                binding._sourceTypeName = GetDisplayTypeName(valueType);

                if (!TryResolveSourceType(
                        parameter.type,
                        valueType,
                        out AnimatorSyncSourceType sourceType,
                        out string status))
                {
                    binding._status = status;
                    _bindings.Add(binding);
                    continue;
                }

                binding._sourceType = sourceType;
                binding._isMapped = true;
                binding._status = status;
                _bindings.Add(binding);
            }
        }

        /// <summary>
        /// Tries to auto-fill obvious local references when they are missing.
        /// </summary>
        private void TryResolveMissingReferences()
        {
            _animator ??= GetComponent<Animator>();

            if (_blackboardContainer != null || !FSMBrain.SimpleBlackboardBridge.IsAvailable)
            {
                return;
            }

            Type containerType = FSMBrain.SimpleBlackboardBridge.ContainerType;

            if (containerType == null)
            {
                return;
            }

            _blackboardContainer = GetComponent(containerType) as Component;
        }

        /// <summary>
        /// Resolves the active blackboard, recreating it in edit mode when required.
        /// </summary>
        /// <param name="blackboard">The resolved runtime blackboard instance.</param>
        /// <param name="refreshInEditor">Recreates the blackboard before reading it in edit mode.</param>
        /// <returns>True when the configured container exposed a runtime blackboard.</returns>
        private bool TryGetResolvedBlackboard(out object blackboard, bool refreshInEditor)
        {
            blackboard = null;

            if (_blackboardContainer == null || !FSMBrain.SimpleBlackboardBridge.IsAvailable)
            {
                return false;
            }

            if (refreshInEditor)
            {
                FSMBrain.SimpleBlackboardBridge.RecreateBlackboard(_blackboardContainer);
            }

            return FSMBrain.SimpleBlackboardBridge.TryGetBlackboard(
                _blackboardContainer,
                out blackboard);
        }

        /// <summary>
        /// Resolves the supported source type for a given animator parameter.
        /// </summary>
        /// <param name="parameterType">Animator parameter type.</param>
        /// <param name="valueType">Blackboard value type.</param>
        /// <param name="sourceType">Resolved source type used during runtime sync.</param>
        /// <param name="status">Human-readable binding status.</param>
        /// <returns>True when the types can be synchronized automatically.</returns>
        private static bool TryResolveSourceType(
            AnimatorControllerParameterType parameterType,
            Type valueType,
            out AnimatorSyncSourceType sourceType,
            out string status)
        {
            sourceType = AnimatorSyncSourceType.None;
            status = "Type mismatch.";

            switch (parameterType)
            {
                case AnimatorControllerParameterType.Float:
                    if (valueType == typeof(float))
                    {
                        sourceType = AnimatorSyncSourceType.Float;
                        status = "Mapped.";
                        return true;
                    }

                    if (valueType == typeof(int))
                    {
                        sourceType = AnimatorSyncSourceType.Int;
                        status = "Mapped using int-to-float conversion.";
                        return true;
                    }

                    if (valueType == typeof(double))
                    {
                        sourceType = AnimatorSyncSourceType.Double;
                        status = "Mapped using double-to-float conversion.";
                        return true;
                    }

                    status = "Expected float, int, or double.";
                    return false;

                case AnimatorControllerParameterType.Int:
                    if (valueType == typeof(int))
                    {
                        sourceType = AnimatorSyncSourceType.Int;
                        status = "Mapped.";
                        return true;
                    }

                    status = "Expected int.";
                    return false;

                case AnimatorControllerParameterType.Bool:
                    if (valueType == typeof(bool))
                    {
                        sourceType = AnimatorSyncSourceType.Bool;
                        status = "Mapped.";
                        return true;
                    }

                    status = "Expected bool.";
                    return false;

                case AnimatorControllerParameterType.Trigger:
                    if (valueType == typeof(bool))
                    {
                        sourceType = AnimatorSyncSourceType.Bool;
                        status = "Mapped as a rising-edge trigger.";
                        return true;
                    }

                    status = "Expected bool for trigger sync.";
                    return false;

                default:
                    status = "Unsupported animator parameter type.";
                    return false;
            }
        }

        /// <summary>
        /// Formats value types for the generated bindings table.
        /// </summary>
        /// <param name="valueType">The reflected blackboard value type.</param>
        /// <returns>A readable display name for the reflected type.</returns>
        private static string GetDisplayTypeName(Type valueType)
        {
            if (valueType == null)
            {
                return "None";
            }

            if (valueType == typeof(float))
            {
                return "float";
            }

            if (valueType == typeof(int))
            {
                return "int";
            }

            if (valueType == typeof(bool))
            {
                return "bool";
            }

            if (valueType == typeof(double))
            {
                return "double";
            }

            return valueType.Name;
        }

        #endregion

        #region Runtime Synchronization

        /// <summary>
        /// Applies the generated bindings to the animator during play mode.
        /// </summary>
        private void SyncAnimator()
        {
            if (_animator == null)
            {
                return;
            }

            if (_bindings.Count == 0)
            {
                BuildBindings();
            }

            if (_bindings.Count == 0
                || !TryGetResolvedBlackboard(out object blackboard, false))
            {
                return;
            }

            foreach (AnimatorSyncBinding binding in _bindings)
            {
                if (!binding._isMapped)
                {
                    continue;
                }

                ApplyBinding(binding, blackboard);
            }
        }

        /// <summary>
        /// Applies a single binding using the smallest compatible typed read.
        /// </summary>
        /// <param name="binding">Binding metadata for the target animator parameter.</param>
        /// <param name="blackboard">Resolved runtime blackboard instance.</param>
        private void ApplyBinding(AnimatorSyncBinding binding, object blackboard)
        {
            switch (binding._parameterType)
            {
                case AnimatorControllerParameterType.Float:
                    ApplyFloatBinding(binding, blackboard);
                    return;

                case AnimatorControllerParameterType.Int:
                    ApplyIntBinding(binding, blackboard);
                    return;

                case AnimatorControllerParameterType.Bool:
                    ApplyBoolBinding(binding, blackboard);
                    return;

                case AnimatorControllerParameterType.Trigger:
                    ApplyTriggerBinding(binding, blackboard);
                    return;
            }
        }

        /// <summary>
        /// Applies a float parameter binding.
        /// </summary>
        /// <param name="binding">Binding metadata for the target animator parameter.</param>
        /// <param name="blackboard">Resolved runtime blackboard instance.</param>
        private void ApplyFloatBinding(AnimatorSyncBinding binding, object blackboard)
        {
            switch (binding._sourceType)
            {
                case AnimatorSyncSourceType.Float:
                    if (FSMBrain.SimpleBlackboardBridge.TryGetValue(
                            blackboard,
                            binding._blackboardKey,
                            out float floatValue))
                    {
                        _animator.SetFloat(binding._parameterHash, floatValue);
                    }

                    return;

                case AnimatorSyncSourceType.Int:
                    if (FSMBrain.SimpleBlackboardBridge.TryGetValue(
                            blackboard,
                            binding._blackboardKey,
                            out int intValue))
                    {
                        _animator.SetFloat(binding._parameterHash, intValue);
                    }

                    return;

                case AnimatorSyncSourceType.Double:
                    if (FSMBrain.SimpleBlackboardBridge.TryGetValue(
                            blackboard,
                            binding._blackboardKey,
                            out double doubleValue))
                    {
                        _animator.SetFloat(binding._parameterHash, (float)doubleValue);
                    }

                    return;
            }
        }

        /// <summary>
        /// Applies an int parameter binding.
        /// </summary>
        /// <param name="binding">Binding metadata for the target animator parameter.</param>
        /// <param name="blackboard">Resolved runtime blackboard instance.</param>
        private void ApplyIntBinding(AnimatorSyncBinding binding, object blackboard)
        {
            if (binding._sourceType != AnimatorSyncSourceType.Int)
            {
                return;
            }

            if (FSMBrain.SimpleBlackboardBridge.TryGetValue(
                    blackboard,
                    binding._blackboardKey,
                    out int intValue))
            {
                _animator.SetInteger(binding._parameterHash, intValue);
            }
        }

        /// <summary>
        /// Applies a bool parameter binding.
        /// </summary>
        /// <param name="binding">Binding metadata for the target animator parameter.</param>
        /// <param name="blackboard">Resolved runtime blackboard instance.</param>
        private void ApplyBoolBinding(AnimatorSyncBinding binding, object blackboard)
        {
            if (binding._sourceType != AnimatorSyncSourceType.Bool)
            {
                return;
            }

            if (FSMBrain.SimpleBlackboardBridge.TryGetValue(
                    blackboard,
                    binding._blackboardKey,
                    out bool boolValue))
            {
                _animator.SetBool(binding._parameterHash, boolValue);
            }
        }

        /// <summary>
        /// Applies a trigger parameter binding using a rising-edge bool source.
        /// </summary>
        /// <param name="binding">Binding metadata for the target animator parameter.</param>
        /// <param name="blackboard">Resolved runtime blackboard instance.</param>
        private void ApplyTriggerBinding(AnimatorSyncBinding binding, object blackboard)
        {
            if (binding._sourceType != AnimatorSyncSourceType.Bool)
            {
                return;
            }

            if (!FSMBrain.SimpleBlackboardBridge.TryGetValue(
                    blackboard,
                    binding._blackboardKey,
                    out bool triggerValue))
            {
                return;
            }

            if (triggerValue && !binding._previousTriggerValue)
            {
                _animator.SetTrigger(binding._parameterHash);
            }

            binding._previousTriggerValue = triggerValue;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Serializable metadata describing a single animator-to-blackboard binding.
        /// </summary>
        [Serializable]
        private sealed class AnimatorSyncBinding
        {
            /// <summary>
            /// Animator parameter name used to locate the binding target.
            /// </summary>
            [SerializeField]
            internal string _parameterName;

            /// <summary>
            /// Blackboard key used as the value source.
            /// </summary>
            [SerializeField]
            internal string _blackboardKey;

            /// <summary>
            /// Animator parameter type resolved from the runtime controller.
            /// </summary>
            [SerializeField]
            internal AnimatorControllerParameterType _parameterType;

            /// <summary>
            /// Blackboard value type used to drive the synchronization.
            /// </summary>
            [SerializeField]
            internal string _sourceTypeName;

            /// <summary>
            /// Human-readable binding status shown in the inspector.
            /// </summary>
            [SerializeField]
            internal string _status;

            /// <summary>
            /// Cached animator parameter hash used during runtime updates.
            /// </summary>
            [SerializeField]
            [HideInInspector]
            internal int _parameterHash;

            /// <summary>
            /// Typed blackboard source kind used by runtime reads.
            /// </summary>
            [SerializeField]
            [HideInInspector]
            internal AnimatorSyncSourceType _sourceType;

            /// <summary>
            /// Indicates whether the binding can be synchronized at runtime.
            /// </summary>
            [SerializeField]
            [HideInInspector]
            internal bool _isMapped;

            /// <summary>
            /// Previous bool value used to emit trigger pulses only on rising edges.
            /// </summary>
            [SerializeField]
            [HideInInspector]
            internal bool _previousTriggerValue;
        }

        /// <summary>
        /// Supported source kinds used by runtime blackboard reads.
        /// </summary>
        private enum AnimatorSyncSourceType
        {
            None,
            Bool,
            Int,
            Float,
            Double
        }

        #endregion
    }
}