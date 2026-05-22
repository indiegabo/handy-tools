using System;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Stores one stable reference to one blackboard entry or one host-provided scope.
    /// </summary>
    [Serializable]
    public class GraphBlackboardVariableReference
    {
        [SerializeField] private GraphBlackboardReferenceScope _scope;
        [SerializeField, HideInInspector] private string _scopeKey = string.Empty;
        [SerializeField, HideInInspector] private SerializableGuid _entryId;
        [SerializeField, HideInInspector] private string _entryKey = string.Empty;
        [SerializeField, HideInInspector] private string _valueTypeName = string.Empty;

        /// <summary>
        /// Gets the targeted reference scope.
        /// </summary>
        public GraphBlackboardReferenceScope Scope => _scope;

        /// <summary>
        /// Gets the optional non-local scope key.
        /// </summary>
        public string ScopeKey => _scopeKey;

        /// <summary>
        /// Gets the stable blackboard entry identifier.
        /// </summary>
        public SerializableGuid EntryId => _entryId;

        /// <summary>
        /// Gets the cached authored key used for labels and migration fallback.
        /// </summary>
        public string EntryKey => _entryKey;

        /// <summary>
        /// Gets whether the reference currently points to one variable binding.
        /// </summary>
        public bool IsAssigned => _entryId != SerializableGuid.Empty
            || !string.IsNullOrWhiteSpace(_entryKey)
            || !string.IsNullOrWhiteSpace(_scopeKey);

        /// <summary>
        /// Gets the cached runtime value type represented by the reference.
        /// </summary>
        public Type ValueType => ResolveSerializedType(_valueTypeName);

        /// <summary>
        /// Clears the stored variable reference.
        /// </summary>
        public void Clear()
        {
            _scope = GraphBlackboardReferenceScope.GraphLocal;
            _scopeKey = string.Empty;
            _entryId = SerializableGuid.Empty;
            _entryKey = string.Empty;
            _valueTypeName = string.Empty;
        }

        /// <summary>
        /// Assigns the reference from one authored graph-local blackboard entry.
        /// </summary>
        /// <param name="entry">Entry to reference.</param>
        public void Bind(GraphBlackboardEntry entry)
        {
            if (entry == null)
            {
                Clear();
                return;
            }

            entry.EnsureId();
            _scope = GraphBlackboardReferenceScope.GraphLocal;
            _scopeKey = string.Empty;
            _entryId = entry.Id;
            _entryKey = entry.Key ?? string.Empty;
            _valueTypeName = entry.Value?.GetExpectedValueType()?.AssemblyQualifiedName
                ?? string.Empty;
        }

        /// <summary>
        /// Stores one legacy graph-local binding that can be resolved by key.
        /// </summary>
        /// <param name="entryKey">Legacy blackboard key.</param>
        /// <param name="valueType">Expected runtime value type.</param>
        public void BindLegacy(string entryKey, Type valueType)
        {
            _scope = GraphBlackboardReferenceScope.GraphLocal;
            _scopeKey = string.Empty;
            _entryId = SerializableGuid.Empty;
            _entryKey = entryKey ?? string.Empty;
            _valueTypeName = valueType?.AssemblyQualifiedName ?? string.Empty;
        }

        /// <summary>
        /// Stores one non-local binding for future external or persistent scope resolution.
        /// </summary>
        /// <param name="scope">Target reference scope.</param>
        /// <param name="scopeKey">Opaque scope identifier interpreted by the host.</param>
        /// <param name="entryId">Stable entry identifier when known.</param>
        /// <param name="entryKey">Entry key fallback used for labels and migration.</param>
        /// <param name="valueType">Expected runtime value type.</param>
        public void BindScoped(
            GraphBlackboardReferenceScope scope,
            string scopeKey,
            SerializableGuid entryId,
            string entryKey,
            Type valueType)
        {
            _scope = scope;
            _scopeKey = scopeKey ?? string.Empty;
            _entryId = entryId;
            _entryKey = entryKey ?? string.Empty;
            _valueTypeName = valueType?.AssemblyQualifiedName ?? string.Empty;
        }

        /// <summary>
        /// Attempts to resolve the referenced entry from one graph-local blackboard.
        /// </summary>
        /// <param name="blackboard">Graph-local blackboard.</param>
        /// <param name="entry">Resolved entry when available.</param>
        /// <returns>True when the local reference could be resolved.</returns>
        public bool TryResolveEntry(
            GraphBlackboard blackboard,
            out GraphBlackboardEntry entry)
        {
            entry = null;

            if (_scope != GraphBlackboardReferenceScope.GraphLocal || blackboard == null)
            {
                return false;
            }

            if (_entryId != SerializableGuid.Empty && blackboard.TryGetEntry(_entryId, out entry))
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
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="blackboard">Graph-local blackboard.</param>
        /// <param name="scopeResolver">Optional resolver for non-local scopes.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
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
        /// Attempts to resolve the referenced payload as one typed value.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="blackboard">Graph-local blackboard.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
        public bool TryGetValue<T>(GraphBlackboard blackboard, out T value)
        {
            return TryGetValue(blackboard, null, out value);
        }

        /// <summary>
        /// Attempts to resolve the referenced payload as one runtime type.
        /// </summary>
        /// <param name="blackboard">Graph-local blackboard.</param>
        /// <param name="scopeResolver">Optional resolver for non-local scopes.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
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

            if (_scope == GraphBlackboardReferenceScope.GraphLocal)
            {
                return TryResolveEntry(blackboard, out GraphBlackboardEntry entry)
                    && entry.Value != null
                    && entry.Value.TryGetValue(requestedType, out value);
            }

            return scopeResolver != null
                && scopeResolver.TryGetValue(this, requestedType, out value);
        }

        /// <summary>
        /// Attempts to resolve the referenced payload as one runtime type.
        /// </summary>
        /// <param name="blackboard">Graph-local blackboard.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
        public bool TryGetValue(
            GraphBlackboard blackboard,
            Type requestedType,
            out object value)
        {
            return TryGetValue(blackboard, null, requestedType, out value);
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
    }
}