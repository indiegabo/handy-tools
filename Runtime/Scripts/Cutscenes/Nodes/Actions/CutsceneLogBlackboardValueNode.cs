using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.LoggerModule;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    /// <summary>
    /// Reads one value from the graph blackboard and writes it to the logger.
    /// </summary>
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Blackboard/Log Blackboard Value", "Log Blackboard Value")]
    public sealed class CutsceneLogBlackboardValueNode : CutsceneNodeBase
    {
        [SerializeField]
        private CutsceneBlackboardVariableReference _variable = new();

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _warningSource =
            CutsceneValueSource.CreateDirect(false);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _errorSource =
            CutsceneValueSource.CreateDirect(false);

        /// <summary>
        /// Applies the authoring configuration used by tests and samples.
        /// </summary>
        /// <param name="key">Blackboard key to read.</param>
        /// <param name="valueType">Expected value type.</param>
        /// <param name="warning">Whether to log as warning.</param>
        /// <param name="error">Whether to log as error.</param>
        public void Configure(
            string key,
            CutsceneBlackboardValueType valueType,
            bool warning = false,
            bool error = false)
        {
            EnsureConfiguration();
            _variable.BindLegacy(key, ResolveLegacyValueType(valueType));
            _warningSource.SetDirectValue(warning);
            _errorSource.SetDirectValue(error);
        }

        /// <summary>
        /// Returns the blackboard key and expected type displayed in the graph.
        /// </summary>
        /// <returns>The compact node summary.</returns>
        public override string GetSummary()
        {
            EnsureConfiguration();

            string key = string.IsNullOrWhiteSpace(_variable.EntryKey)
                ? "Unassigned"
                : _variable.EntryKey;
            Type valueType = _variable.ValueType;
            string valueTypeName = valueType?.Name ?? "Unknown";

            return $"{key} ({valueTypeName})";
        }

        /// <summary>
        /// Reads the configured blackboard value and logs it.
        /// </summary>
        /// <param name="context">Runtime execution context.</param>
        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureConfiguration();

            if (!context.TryGetRuntimeBlackboardEntry(_variable, out GraphBlackboardEntry entry)
                || entry?.Value == null)
            {
                string variableKey = string.IsNullOrWhiteSpace(_variable.EntryKey)
                    ? "Unassigned"
                    : _variable.EntryKey;
                context.TryComplete(CutsceneNodeResult.Failure(
                    $"Log Blackboard Value node could not resolve variable '{variableKey}'."));
                return;
            }

            if (!_warningSource.TryGetValue(context, out bool warning)
                || !_errorSource.TryGetValue(context, out bool error))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Log Blackboard Value node requires valid severity sources."));
                return;
            }

            string formattedValue = FormatValue(
                entry.Value.GetBoxedValue(),
                entry.Value.GetExpectedValueType());

            if (error)
            {
                HandyLogger.Error(nameof(CutsceneLogBlackboardValueNode), formattedValue);
            }
            else if (warning)
            {
                HandyLogger.Warning(nameof(CutsceneLogBlackboardValueNode), formattedValue);
            }
            else
            {
                HandyLogger.Message(nameof(CutsceneLogBlackboardValueNode), formattedValue);
            }

            context.TryComplete(CutsceneNodeResult.Success());
        }

        private void EnsureConfiguration()
        {
            _variable ??= new CutsceneBlackboardVariableReference();
            _warningSource ??= CutsceneValueSource.CreateDirect(false);
            _errorSource ??= CutsceneValueSource.CreateDirect(false);

            _warningSource.SetExpectedValueType(typeof(bool));
            _errorSource.SetExpectedValueType(typeof(bool));
        }

        private static string FormatValue(object value, Type valueType)
        {
            if (value == null)
            {
                return valueType == typeof(string) ? string.Empty : "Null";
            }

            return value switch
            {
                float floatValue => floatValue.ToString("0.###"),
                double doubleValue => doubleValue.ToString("0.###"),
                Object unityObject => unityObject == null ? "Null" : unityObject.name,
                _ => value.ToString() ?? string.Empty,
            };
        }

        private static Type ResolveLegacyValueType(CutsceneBlackboardValueType valueType)
        {
            return valueType switch
            {
                CutsceneBlackboardValueType.Int => typeof(int),
                CutsceneBlackboardValueType.Float => typeof(float),
                CutsceneBlackboardValueType.String => typeof(string),
                CutsceneBlackboardValueType.Bool => typeof(bool),
                CutsceneBlackboardValueType.Object => typeof(Object),
                _ => typeof(string),
            };
        }
    }

    /// <summary>
    /// Writes one or more configured values to the graph blackboard.
    /// </summary>
    [Serializable]
    [CutsceneNodeMenu("Actions/Blackboard/Set Blackboard Values", "Set Blackboard Values")]
    public sealed class CutsceneSetBlackboardValuesNode : CutsceneNodeBase
    {
        /// <summary>
        /// Represents one typed value assignment applied by the node.
        /// </summary>
        [Serializable]
        public sealed class BlackboardValueAssignment
        {
            [SerializeField]
            private CutsceneBlackboardVariableReference _targetVariable = new();

            [SerializeField]
            private CutsceneValueSource _valueSource =
                CutsceneValueSource.CreateDirect(string.Empty);

            /// <summary>
            /// Gets the destination blackboard key.
            /// </summary>
            public string Key => _targetVariable?.EntryKey ?? string.Empty;

            /// <summary>
            /// Gets the referenced target variable.
            /// </summary>
            public CutsceneBlackboardVariableReference TargetVariable => _targetVariable;

            /// <summary>
            /// Gets the value source applied to the target variable.
            /// </summary>
            public CutsceneValueSource ValueSource => _valueSource;

            /// <summary>
            /// Gets the configured value type.
            /// </summary>
            public CutsceneBlackboardValueType ValueType => ResolveValueType(
                _targetVariable?.ValueType ?? _valueSource?.ExpectedValueType);

            /// <summary>
            /// Creates one int assignment descriptor.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Int value to store.</param>
            /// <returns>The configured assignment.</returns>
            public static BlackboardValueAssignment CreateInt(string key, int value)
            {
                BlackboardValueAssignment assignment = new();
                assignment.Configure(key, value);
                return assignment;
            }

            /// <summary>
            /// Creates one float assignment descriptor.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Float value to store.</param>
            /// <returns>The configured assignment.</returns>
            public static BlackboardValueAssignment CreateFloat(string key, float value)
            {
                BlackboardValueAssignment assignment = new();
                assignment.Configure(key, value);
                return assignment;
            }

            /// <summary>
            /// Creates one string assignment descriptor.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">String value to store.</param>
            /// <returns>The configured assignment.</returns>
            public static BlackboardValueAssignment CreateString(
                string key,
                string value)
            {
                BlackboardValueAssignment assignment = new();
                assignment.Configure(key, value);
                return assignment;
            }

            /// <summary>
            /// Creates one bool assignment descriptor.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Bool value to store.</param>
            /// <returns>The configured assignment.</returns>
            public static BlackboardValueAssignment CreateBool(string key, bool value)
            {
                BlackboardValueAssignment assignment = new();
                assignment.Configure(key, value);
                return assignment;
            }

            /// <summary>
            /// Creates one Unity object assignment descriptor.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Object reference to store.</param>
            /// <returns>The configured assignment.</returns>
            public static BlackboardValueAssignment CreateObject(
                string key,
                UnityEngine.Object value)
            {
                BlackboardValueAssignment assignment = new();
                assignment.Configure(key, value);
                return assignment;
            }

            /// <summary>
            /// Configures one int assignment.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Int value to store.</param>
            public void Configure(string key, int value)
            {
                EnsureConfiguration();
                _targetVariable.BindLegacy(key, typeof(int));
                _valueSource.SetDirectValue(value);
            }

            /// <summary>
            /// Configures one float assignment.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Float value to store.</param>
            public void Configure(string key, float value)
            {
                EnsureConfiguration();
                _targetVariable.BindLegacy(key, typeof(float));
                _valueSource.SetDirectValue(value);
            }

            /// <summary>
            /// Configures one string assignment.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">String value to store.</param>
            public void Configure(string key, string value)
            {
                EnsureConfiguration();
                _targetVariable.BindLegacy(key, typeof(string));
                _valueSource.SetDirectValue(value ?? string.Empty);
            }

            /// <summary>
            /// Configures one bool assignment.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Bool value to store.</param>
            public void Configure(string key, bool value)
            {
                EnsureConfiguration();
                _targetVariable.BindLegacy(key, typeof(bool));
                _valueSource.SetDirectValue(value);
            }

            /// <summary>
            /// Configures one Unity object assignment.
            /// </summary>
            /// <param name="key">Destination blackboard key.</param>
            /// <param name="value">Object reference to store.</param>
            public void Configure(string key, UnityEngine.Object value)
            {
                EnsureConfiguration();
                _targetVariable.BindLegacy(key, typeof(Object));
                _valueSource.SetDirectValue(value);
            }

            /// <summary>
            /// Returns the compact assignment summary shown in the graph body.
            /// </summary>
            /// <returns>A readable key and value description.</returns>
            public string GetSummary()
            {
                EnsureConfiguration();
                return $"{Key} = {_valueSource.GetSummary()}";
            }

            /// <summary>
            /// Applies the configured value to the provided execution context.
            /// </summary>
            /// <param name="context">Active runtime execution context.</param>
            /// <param name="failureReason">Failure reason when the assignment is invalid.</param>
            /// <returns>True when the assignment was written successfully.</returns>
            public bool TryApply(
                CutsceneExecutionContext context,
                out string failureReason)
            {
                EnsureConfiguration();
                failureReason = string.Empty;

                if (string.IsNullOrWhiteSpace(Key))
                {
                    failureReason = "Blackboard assignment requires one target variable.";
                    return false;
                }

                if (_targetVariable.TryResolveEntry(
                        context.RuntimeBlackboard,
                        out GraphBlackboardEntry entry)
                    && entry?.Value != null)
                {
                    Type targetType = entry.Value.GetExpectedValueType();

                    if (!_valueSource.TryGetValue(
                            context.RuntimeBlackboard,
                            targetType,
                            out object resolvedValue))
                    {
                        failureReason =
                            $"Blackboard assignment '{Key}' could not resolve its value source as {targetType?.Name ?? "Unknown"}.";
                        return false;
                    }

                    if (!entry.TrySetBoxedValue(resolvedValue))
                    {
                        failureReason =
                            $"Blackboard assignment '{Key}' could not write to the target variable.";
                        return false;
                    }

                    return true;
                }

                Type fallbackValueType = _targetVariable.ValueType
                    ?? _valueSource.ExpectedValueType;

                if (fallbackValueType == null)
                {
                    failureReason =
                        $"Blackboard assignment '{Key}' could not determine one value type for legacy creation.";
                    return false;
                }

                if (!_valueSource.TryGetValue(
                    context.RuntimeBlackboard,
                        fallbackValueType,
                        out object fallbackValue))
                {
                    failureReason =
                        $"Blackboard assignment '{Key}' could not resolve its value source as {fallbackValueType.Name}.";
                    return false;
                }

                if (!context.TrySetBlackboardValue(Key, fallbackValue, fallbackValueType))
                {
                    failureReason =
                        $"Blackboard assignment '{Key}' could not create or update the target variable.";
                    return false;
                }

                _targetVariable.BindLegacy(Key, fallbackValueType);
                return true;
            }

            private void EnsureConfiguration()
            {
                _targetVariable ??= new CutsceneBlackboardVariableReference();
                _valueSource ??= CutsceneValueSource.CreateDirect(string.Empty);
            }

            private static CutsceneBlackboardValueType ResolveValueType(Type valueType)
            {
                if (valueType == typeof(int))
                {
                    return CutsceneBlackboardValueType.Int;
                }

                if (valueType == typeof(float))
                {
                    return CutsceneBlackboardValueType.Float;
                }

                if (valueType == typeof(bool))
                {
                    return CutsceneBlackboardValueType.Bool;
                }

                if (typeof(Object).IsAssignableFrom(valueType))
                {
                    return CutsceneBlackboardValueType.Object;
                }

                return CutsceneBlackboardValueType.String;
            }
        }

        [SerializeField]
        private List<BlackboardValueAssignment> _assignments = new()
        {
            BlackboardValueAssignment.CreateString("value", string.Empty),
        };

        /// <summary>
        /// Gets the configured assignments for tests and validation.
        /// </summary>
        public IReadOnlyList<BlackboardValueAssignment> Assignments => _assignments;

        /// <summary>
        /// Replaces the current assignment list.
        /// </summary>
        /// <param name="assignments">Assignments serialized by the node.</param>
        public void SetAssignments(params BlackboardValueAssignment[] assignments)
        {
            _assignments = assignments == null
                ? new List<BlackboardValueAssignment>()
                : assignments.Where(assignment => assignment != null).ToList();
        }

        /// <summary>
        /// Returns a concise summary of the current assignment payload.
        /// </summary>
        /// <returns>The summary displayed in the graph.</returns>
        public override string GetSummary()
        {
            if (_assignments == null || _assignments.Count == 0)
            {
                return "No assignments";
            }

            if (_assignments.Count == 1 && _assignments[0] != null)
            {
                return _assignments[0].GetSummary();
            }

            return $"{_assignments.Count} assignments";
        }

        /// <summary>
        /// Writes all configured assignments to the graph blackboard.
        /// </summary>
        /// <param name="context">Runtime execution context.</param>
        public override void OnEnter(CutsceneExecutionContext context)
        {
            if (_assignments == null || _assignments.Count == 0)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set Blackboard Values node requires at least one assignment."));
                return;
            }

            int appliedAssignments = 0;

            for (int index = 0; index < _assignments.Count; index++)
            {
                BlackboardValueAssignment assignment = _assignments[index];

                if (assignment == null)
                {
                    continue;
                }

                if (!assignment.TryApply(context, out string failureReason))
                {
                    context.TryComplete(CutsceneNodeResult.Failure(failureReason));
                    return;
                }

                appliedAssignments++;
            }

            if (appliedAssignments == 0)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set Blackboard Values node requires at least one valid assignment."));
                return;
            }

            context.TryComplete(CutsceneNodeResult.Success());
        }

        private static Type ResolveLegacyValueType(CutsceneBlackboardValueType valueType)
        {
            return valueType switch
            {
                CutsceneBlackboardValueType.Int => typeof(int),
                CutsceneBlackboardValueType.Float => typeof(float),
                CutsceneBlackboardValueType.String => typeof(string),
                CutsceneBlackboardValueType.Bool => typeof(bool),
                CutsceneBlackboardValueType.Object => typeof(Object),
                _ => typeof(string),
            };
        }
    }
}