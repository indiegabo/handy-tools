using System;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Identifies the origin used to resolve one serialized graph value.
    /// </summary>
    public enum GraphValueSourceMode
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
    /// Declares the runtime type one GraphValueSource field should represent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class GraphValueSourceTypeAttribute : PropertyAttribute
    {
        /// <summary>
        /// Initializes one type constraint for a value source field.
        /// </summary>
        /// <param name="valueType">Runtime type the field should resolve.</param>
        public GraphValueSourceTypeAttribute(Type valueType)
        {
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        }

        /// <summary>
        /// Gets the runtime type the field should resolve.
        /// </summary>
        public Type ValueType { get; }
    }

    /// <summary>
    /// Stores one value that can be authored directly or resolved from one blackboard reference.
    /// </summary>
    [Serializable]
    public class GraphValueSource
    {
        [SerializeField] private GraphValueSourceMode _mode;
        [SerializeField] private GraphBlackboardVariableReference _blackboardVariable = new();
        [SerializeReference] private GraphBlackboardValue _directValue;
        [SerializeField, HideInInspector] private string _expectedValueTypeName = string.Empty;

        /// <summary>
        /// Gets or sets the serialized variable-reference instance for derived authored value sources.
        /// </summary>
        protected GraphBlackboardVariableReference BlackboardVariableInternal
        {
            get => _blackboardVariable;
            set => _blackboardVariable = value;
        }

        /// <summary>
        /// Gets or sets the serialized direct-value wrapper for derived authored value sources.
        /// </summary>
        protected GraphBlackboardValue DirectValueInternal
        {
            get => _directValue;
            set => _directValue = value;
        }

        /// <summary>
        /// Gets or sets the serialized expected runtime type name for derived authored value sources.
        /// </summary>
        protected string ExpectedValueTypeNameInternal
        {
            get => _expectedValueTypeName;
            set => _expectedValueTypeName = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the currently selected value origin.
        /// </summary>
        public GraphValueSourceMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>
        /// Gets the referenced blackboard variable metadata.
        /// </summary>
        public GraphBlackboardVariableReference BlackboardVariable
        {
            get
            {
                _blackboardVariable ??= CreateBlackboardVariableReference();
                return _blackboardVariable;
            }
        }

        /// <summary>
        /// Gets the locally serialized direct payload wrapper.
        /// </summary>
        public GraphBlackboardValue DirectValue => _directValue;

        /// <summary>
        /// Resolves the graph family id used when creating new direct-value wrappers.
        /// Derived authored value sources can override this to expose family-specific
        /// wrappers through the shared value registry.
        /// </summary>
        /// <returns>The optional graph family id.</returns>
        protected virtual string GetFamilyId()
        {
            return null;
        }

        /// <summary>
        /// Creates the serialized variable-reference shell used by this value source.
        /// Derived authored value sources can override this to preserve family-specific
        /// compatibility APIs while storing the shared serialized shape.
        /// </summary>
        /// <returns>The authored variable-reference instance.</returns>
        protected virtual GraphBlackboardVariableReference CreateBlackboardVariableReference()
        {
            return new GraphBlackboardVariableReference();
        }

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
        /// <param name="familyId">Optional graph family id for family-specific wrappers.</param>
        /// <returns>The created value source.</returns>
        public static GraphValueSource CreateDirect<T>(T value, string familyId = null)
        {
            GraphValueSource source = new();
            source.SetDirectValue(value, familyId);
            return source;
        }

        /// <summary>
        /// Creates one value source initialized from one blackboard variable.
        /// </summary>
        /// <param name="entry">Referenced blackboard entry.</param>
        /// <returns>The created value source.</returns>
        public static GraphValueSource CreateBlackboard(GraphBlackboardEntry entry)
        {
            GraphValueSource source = new();
            source.BindBlackboardVariable(entry);
            return source;
        }

        /// <summary>
        /// Stores the runtime type this source should represent.
        /// </summary>
        /// <param name="valueType">Runtime type expected by the caller.</param>
        /// <param name="familyId">Optional graph family id for family-specific wrappers.</param>
        public void SetExpectedValueType(Type valueType, string familyId = null)
        {
            if (valueType == null)
            {
                return;
            }

            _expectedValueTypeName = valueType.AssemblyQualifiedName ?? string.Empty;
            EnsureDirectValueWrapper(valueType, familyId ?? GetFamilyId());
        }

        /// <summary>
        /// Switches the source to direct mode and stores one typed payload.
        /// </summary>
        /// <typeparam name="T">Runtime type represented by the payload.</typeparam>
        /// <param name="value">Direct value payload.</param>
        /// <param name="familyId">Optional graph family id for family-specific wrappers.</param>
        public void SetDirectValue<T>(T value, string familyId = null)
        {
            Type valueType = value == null ? typeof(T) : value.GetType();
            SetExpectedValueType(valueType, familyId ?? GetFamilyId());

            if (_directValue == null)
            {
                return;
            }

            _directValue.TrySetBoxedValue(value);
            _mode = GraphValueSourceMode.Direct;
        }

        /// <summary>
        /// Switches the source to blackboard mode and binds one variable reference.
        /// </summary>
        /// <param name="entry">Referenced blackboard entry.</param>
        public void BindBlackboardVariable(GraphBlackboardEntry entry)
        {
            BlackboardVariable.Bind(entry);

            Type valueType = entry?.Value?.GetExpectedValueType();

            if (valueType != null)
            {
                SetExpectedValueType(valueType);
            }

            _mode = GraphValueSourceMode.Blackboard;
        }

        /// <summary>
        /// Switches the source to blackboard mode using one legacy key binding.
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

            _mode = GraphValueSourceMode.Blackboard;
        }

        /// <summary>
        /// Attempts to resolve one typed value from the current source mode.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="blackboard">Graph-local blackboard used for local lookups.</param>
        /// <param name="scopeResolver">Optional resolver for non-local scopes.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue<T>(
            GraphBlackboard blackboard,
            IGraphBlackboardScopeResolver scopeResolver,
            out T value)
        {
            value = default;

            return TryGetValue(blackboard, scopeResolver, typeof(T), out object boxedValue)
                && TryUnbox(boxedValue, out value);
        }

        /// <summary>
        /// Attempts to resolve one typed value from the current source mode.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="blackboard">Graph-local blackboard used for local lookups.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue<T>(GraphBlackboard blackboard, out T value)
        {
            return TryGetValue(blackboard, null, out value);
        }

        /// <summary>
        /// Attempts to resolve one runtime type from the current source mode.
        /// </summary>
        /// <param name="blackboard">Graph-local blackboard used for local lookups.</param>
        /// <param name="scopeResolver">Optional resolver for non-local scopes.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue(
            GraphBlackboard blackboard,
            IGraphBlackboardScopeResolver scopeResolver,
            Type requestedType,
            out object value)
        {
            value = null;

            if (requestedType == null)
            {
                return false;
            }

            Type expectedType = ResolveResolutionType(requestedType);

            if (_mode == GraphValueSourceMode.Blackboard)
            {
                return BlackboardVariable.TryGetValue(
                    blackboard,
                    scopeResolver,
                    requestedType,
                    out value);
            }

            EnsureDirectValueWrapper(expectedType);

            return _directValue != null && _directValue.TryGetValue(requestedType, out value);
        }

        /// <summary>
        /// Attempts to resolve one runtime type from the current source mode.
        /// </summary>
        /// <param name="blackboard">Graph-local blackboard used for local lookups.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue(
            GraphBlackboard blackboard,
            Type requestedType,
            out object value)
        {
            return TryGetValue(blackboard, null, requestedType, out value);
        }

        /// <summary>
        /// Resolves one typed value or returns one fallback when resolution fails.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="blackboard">Graph-local blackboard used for local lookups.</param>
        /// <param name="scopeResolver">Optional resolver for non-local scopes.</param>
        /// <param name="fallbackValue">Fallback returned when resolution fails.</param>
        /// <returns>The resolved or fallback value.</returns>
        public T GetValueOrDefault<T>(
            GraphBlackboard blackboard,
            IGraphBlackboardScopeResolver scopeResolver,
            T fallbackValue = default)
        {
            return TryGetValue(blackboard, scopeResolver, out T value)
                ? value
                : fallbackValue;
        }

        /// <summary>
        /// Resolves one typed value or returns one fallback when resolution fails.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="blackboard">Graph-local blackboard used for local lookups.</param>
        /// <param name="fallbackValue">Fallback returned when resolution fails.</param>
        /// <returns>The resolved or fallback value.</returns>
        public T GetValueOrDefault<T>(GraphBlackboard blackboard, T fallbackValue = default)
        {
            return GetValueOrDefault(blackboard, null, fallbackValue);
        }

        /// <summary>
        /// Returns one compact human-readable summary of the configured value source.
        /// </summary>
        /// <returns>A readable label for graph summaries and editor chrome.</returns>
        public string GetSummary()
        {
            if (_mode == GraphValueSourceMode.Blackboard)
            {
                if (!string.IsNullOrWhiteSpace(BlackboardVariable.EntryKey))
                {
                    return $"@{BlackboardVariable.EntryKey}";
                }

                return BlackboardVariable.Scope == GraphBlackboardReferenceScope.GraphLocal
                    ? "Blackboard"
                    : $"{BlackboardVariable.Scope}:{BlackboardVariable.ScopeKey}";
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

        private void EnsureDirectValueWrapper(Type valueType, string familyId = null)
        {
            if (valueType == null)
            {
                return;
            }

            if (CanRepresentExpectedValueType(_directValue, valueType))
            {
                return;
            }

            if (GraphBlackboardValueRegistry.TryCreateValue(
                    valueType,
                    familyId ?? GetFamilyId(),
                    out GraphBlackboardValue directValue))
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
            return GraphSerializedTypeResolver.Resolve(serializedTypeName);
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
            GraphBlackboardValue value,
            Type expectedType)
        {
            if (value == null || expectedType == null)
            {
                return false;
            }

            if (value is GraphBlackboardUnityObjectValue objectValue)
            {
                return objectValue.GetExpectedValueType() == expectedType;
            }

            return value.CanStoreValueType(expectedType);
        }
    }
}