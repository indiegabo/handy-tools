using System;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Identifies the origin used to resolve one serialized cutscene value.
    /// </summary>
    public enum CutsceneValueSourceMode
    {
        /// <summary>
        /// Reads the value from the locally serialized payload.
        /// </summary>
        Direct,

        /// <summary>
        /// Reads the value from one blackboard variable reference.
        /// </summary>
        Blackboard,
    }

    /// <summary>
    /// Declares the runtime type one <see cref="CutsceneValueSource"/> field should represent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class CutsceneValueSourceTypeAttribute : PropertyAttribute
    {
        /// <summary>
        /// Initializes one type constraint for a value source field.
        /// </summary>
        /// <param name="valueType">Runtime type the field should resolve.</param>
        public CutsceneValueSourceTypeAttribute(Type valueType)
        {
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        }

        /// <summary>
        /// Gets the runtime type the field should resolve.
        /// </summary>
        public Type ValueType { get; }
    }

    /// <summary>
    /// Stores one stable reference to a blackboard entry.
    /// </summary>
    [Serializable]
    public sealed class CutsceneBlackboardVariableReference
    {
        [SerializeField, HideInInspector]
        private SerializableGuid _entryId;

        [SerializeField, HideInInspector]
        private string _entryKey = string.Empty;

        [SerializeField, HideInInspector]
        private string _valueTypeName = string.Empty;

        /// <summary>
        /// Gets the stable blackboard entry identifier.
        /// </summary>
        public SerializableGuid EntryId => _entryId;

        /// <summary>
        /// Gets the cached authored key used for labels and migration fallback.
        /// </summary>
        public string EntryKey => _entryKey;

        /// <summary>
        /// Gets whether the reference currently points to one serialized variable.
        /// </summary>
        public bool IsAssigned => _entryId != SerializableGuid.Empty
            || !string.IsNullOrWhiteSpace(_entryKey);

        /// <summary>
        /// Gets the cached runtime value type represented by the referenced variable.
        /// </summary>
        public Type ValueType => ResolveSerializedType(_valueTypeName);

        /// <summary>
        /// Clears the stored variable reference.
        /// </summary>
        public void Clear()
        {
            _entryId = SerializableGuid.Empty;
            _entryKey = string.Empty;
            _valueTypeName = string.Empty;
        }

        /// <summary>
        /// Assigns the reference from one authored blackboard entry.
        /// </summary>
        /// <param name="entry">Entry to reference.</param>
        public void Bind(CutsceneGraphBlackboardEntry entry)
        {
            if (entry == null)
            {
                Clear();
                return;
            }

            entry.EnsureId();
            _entryId = entry.Id;
            _entryKey = entry.Key ?? string.Empty;
            _valueTypeName = entry.Value?.GetExpectedValueType()?.AssemblyQualifiedName
                ?? string.Empty;
        }

        /// <summary>
        /// Stores one legacy fallback binding that can be resolved by key until the stable entry id is known.
        /// </summary>
        /// <param name="entryKey">Legacy blackboard key.</param>
        /// <param name="valueType">Expected runtime value type.</param>
        public void BindLegacy(string entryKey, Type valueType)
        {
            _entryId = SerializableGuid.Empty;
            _entryKey = entryKey ?? string.Empty;
            _valueTypeName = valueType?.AssemblyQualifiedName ?? string.Empty;
        }

        /// <summary>
        /// Attempts to resolve the referenced entry from one blackboard instance.
        /// </summary>
        /// <param name="blackboard">Blackboard that owns the variable.</param>
        /// <param name="entry">Resolved entry when available.</param>
        /// <returns>True when the reference could be resolved.</returns>
        public bool TryResolveEntry(
            CutsceneGraphBlackboard blackboard,
            out CutsceneGraphBlackboardEntry entry)
        {
            entry = null;

            if (blackboard == null)
            {
                return false;
            }

            if (_entryId != SerializableGuid.Empty
                && blackboard.TryGetEntry(_entryId, out entry))
            {
                Bind(entry);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(_entryKey)
                && blackboard.TryGetEntry(_entryKey, out entry))
            {
                Bind(entry);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve the referenced payload as one typed value.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="blackboard">Blackboard that owns the variable.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
        public bool TryGetValue<T>(
            CutsceneGraphBlackboard blackboard,
            out T value)
        {
            value = default;

            return TryResolveEntry(blackboard, out CutsceneGraphBlackboardEntry entry)
                && entry.TryGetValue(out value);
        }

        /// <summary>
        /// Attempts to resolve the referenced payload as one runtime type.
        /// </summary>
        /// <param name="blackboard">Blackboard that owns the variable.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
        public bool TryGetValue(
            CutsceneGraphBlackboard blackboard,
            Type requestedType,
            out object value)
        {
            value = null;

            return TryResolveEntry(blackboard, out CutsceneGraphBlackboardEntry entry)
                && entry.Value != null
                && entry.Value.TryGetValue(requestedType, out value);
        }

        private static Type ResolveSerializedType(string serializedTypeName)
        {
            return string.IsNullOrWhiteSpace(serializedTypeName)
                ? null
                : Type.GetType(serializedTypeName);
        }
    }

    /// <summary>
    /// Stores one value that can be authored directly or resolved from the graph blackboard.
    /// </summary>
    [Serializable]
    public sealed class CutsceneValueSource
    {
        [SerializeField]
        private CutsceneValueSourceMode _mode;

        [SerializeField]
        private CutsceneBlackboardVariableReference _blackboardVariable = new();

        [SerializeReference]
        private CutsceneGraphBlackboardValue _directValue;

        [SerializeField, HideInInspector]
        private string _expectedValueTypeName = string.Empty;

        /// <summary>
        /// Gets or sets the currently selected value origin.
        /// </summary>
        public CutsceneValueSourceMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>
        /// Gets the referenced blackboard variable metadata.
        /// </summary>
        public CutsceneBlackboardVariableReference BlackboardVariable
        {
            get
            {
                _blackboardVariable ??= new CutsceneBlackboardVariableReference();
                return _blackboardVariable;
            }
        }

        /// <summary>
        /// Gets the locally serialized direct payload wrapper.
        /// </summary>
        public CutsceneGraphBlackboardValue DirectValue => _directValue;

        /// <summary>
        /// Gets the runtime type currently expected by this source.
        /// </summary>
        public Type ExpectedValueType
        {
            get
            {
                Type serializedType = ResolveSerializedType(_expectedValueTypeName);

                if (serializedType != null)
                {
                    return serializedType;
                }

                if (_directValue != null)
                {
                    return _directValue.GetExpectedValueType();
                }

                return BlackboardVariable.ValueType;
            }
        }

        /// <summary>
        /// Creates one value source initialized with one direct value.
        /// </summary>
        /// <typeparam name="T">Runtime type represented by the value.</typeparam>
        /// <param name="value">Direct value payload.</param>
        /// <returns>The created value source.</returns>
        public static CutsceneValueSource CreateDirect<T>(T value)
        {
            CutsceneValueSource source = new();
            source.SetDirectValue(value);
            return source;
        }

        /// <summary>
        /// Creates one value source initialized from one blackboard variable.
        /// </summary>
        /// <param name="entry">Referenced blackboard entry.</param>
        /// <returns>The created value source.</returns>
        public static CutsceneValueSource CreateBlackboard(
            CutsceneGraphBlackboardEntry entry)
        {
            CutsceneValueSource source = new();
            source.BindBlackboardVariable(entry);
            return source;
        }

        /// <summary>
        /// Stores the runtime type this source should represent.
        /// </summary>
        /// <param name="valueType">Runtime type expected by the caller.</param>
        public void SetExpectedValueType(Type valueType)
        {
            if (valueType == null)
            {
                return;
            }

            _expectedValueTypeName = valueType.AssemblyQualifiedName ?? string.Empty;
            EnsureDirectValueWrapper(valueType);
        }

        /// <summary>
        /// Switches the source to direct mode and stores one typed payload.
        /// </summary>
        /// <typeparam name="T">Runtime type represented by the payload.</typeparam>
        /// <param name="value">Direct value payload.</param>
        public void SetDirectValue<T>(T value)
        {
            Type valueType = value == null ? typeof(T) : value.GetType();
            SetExpectedValueType(valueType);

            if (_directValue == null)
            {
                return;
            }

            _directValue.TrySetBoxedValue(value);
            _mode = CutsceneValueSourceMode.Direct;
        }

        /// <summary>
        /// Switches the source to blackboard mode and binds one variable reference.
        /// </summary>
        /// <param name="entry">Referenced blackboard entry.</param>
        public void BindBlackboardVariable(CutsceneGraphBlackboardEntry entry)
        {
            BlackboardVariable.Bind(entry);

            Type valueType = entry?.Value?.GetExpectedValueType();

            if (valueType != null)
            {
                SetExpectedValueType(valueType);
            }

            _mode = CutsceneValueSourceMode.Blackboard;
        }

        /// <summary>
        /// Switches the source to blackboard mode using one legacy key binding until a stable id is resolved.
        /// </summary>
        /// <param name="entryKey">Legacy blackboard key.</param>
        /// <param name="valueType">Expected runtime value type.</param>
        public void BindLegacyBlackboardVariable(string entryKey, Type valueType)
        {
            BlackboardVariable.BindLegacy(entryKey, valueType);

            if (valueType != null)
            {
                SetExpectedValueType(valueType);
            }

            _mode = CutsceneValueSourceMode.Blackboard;
        }

        /// <summary>
        /// Attempts to resolve one typed value from the current source mode.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="context">Execution context that owns the graph blackboard.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue<T>(
            CutsceneExecutionContext context,
            out T value)
        {
            value = default;

            return TryGetValue(context?.Blackboard, typeof(T), out object boxedValue)
                && TryUnbox(boxedValue, out value);
        }

        /// <summary>
        /// Attempts to resolve one runtime type from the current source mode.
        /// </summary>
        /// <param name="blackboard">Blackboard used for blackboard-backed lookups.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue(
            CutsceneGraphBlackboard blackboard,
            Type requestedType,
            out object value)
        {
            value = null;

            if (requestedType == null)
            {
                return false;
            }

            Type expectedType = ResolveResolutionType(requestedType);

            if (_mode == CutsceneValueSourceMode.Blackboard)
            {
                return BlackboardVariable.TryGetValue(blackboard, requestedType, out value);
            }

            EnsureDirectValueWrapper(expectedType);

            return _directValue != null
                && _directValue.TryGetValue(requestedType, out value);
        }

        /// <summary>
        /// Resolves one typed value or returns a fallback when resolution fails.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="context">Execution context that owns the graph blackboard.</param>
        /// <param name="fallbackValue">Fallback returned when resolution fails.</param>
        /// <returns>The resolved or fallback value.</returns>
        public T GetValueOrDefault<T>(
            CutsceneExecutionContext context,
            T fallbackValue = default)
        {
            return TryGetValue(context, out T value)
                ? value
                : fallbackValue;
        }

        /// <summary>
        /// Returns one compact human-readable summary of the configured value source.
        /// </summary>
        /// <returns>A readable label for graph summaries and editor chrome.</returns>
        public string GetSummary()
        {
            if (_mode == CutsceneValueSourceMode.Blackboard)
            {
                return string.IsNullOrWhiteSpace(BlackboardVariable.EntryKey)
                    ? "Blackboard"
                    : $"@{BlackboardVariable.EntryKey}";
            }

            object boxedValue = _directValue?.GetBoxedValue();

            return boxedValue switch
            {
                null => "Null",
                string stringValue => string.IsNullOrWhiteSpace(stringValue)
                    ? "Empty"
                    : stringValue,
                UnityEngine.Object unityObject => unityObject == null
                    ? "Null"
                    : unityObject.name,
                _ => boxedValue.ToString(),
            };
        }

        private void EnsureDirectValueWrapper(Type valueType)
        {
            if (valueType == null)
            {
                return;
            }

            if (CanRepresentExpectedValueType(_directValue, valueType))
            {
                return;
            }

            if (CutsceneBlackboardValueRegistry.TryCreateValue(
                    valueType,
                    out CutsceneGraphBlackboardValue directValue))
            {
                _directValue = directValue;
            }
        }

        private Type ResolveResolutionType(Type requestedType)
        {
            Type expectedType = ExpectedValueType;

            if (expectedType == null
                || requestedType == typeof(object)
                || expectedType.IsAssignableFrom(requestedType))
            {
                return requestedType;
            }

            if (requestedType.IsAssignableFrom(expectedType))
            {
                return expectedType;
            }

            return requestedType;
        }

        private static Type ResolveSerializedType(string serializedTypeName)
        {
            return string.IsNullOrWhiteSpace(serializedTypeName)
                ? null
                : Type.GetType(serializedTypeName);
        }

        private static bool TryUnbox<T>(object boxedValue, out T value)
        {
            if (boxedValue == null)
            {
                value = default;
                return !typeof(T).IsValueType
                    || Nullable.GetUnderlyingType(typeof(T)) != null;
            }

            if (boxedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        private static bool CanRepresentExpectedValueType(
            CutsceneGraphBlackboardValue value,
            Type expectedType)
        {
            if (value == null || expectedType == null)
            {
                return false;
            }

            if (value is CutsceneGraphBlackboardUnityObjectValue objectValue)
            {
                return objectValue.GetExpectedValueType() == expectedType;
            }

            return value.CanStoreValueType(expectedType);
        }
    }
}