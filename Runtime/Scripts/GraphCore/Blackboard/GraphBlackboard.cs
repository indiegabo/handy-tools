using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Serializable blackboard owned by one graph definition.
    /// </summary>
    [Serializable]
    public class GraphBlackboard
    {
        [SerializeField] private List<GraphBlackboardEntry> _entries = new();

        /// <summary>
        /// Gets the mutable serialized entry collection for derived authored blackboards.
        /// </summary>
        protected List<GraphBlackboardEntry> EntriesInternal => _entries;

        /// <summary>
        /// Gets the serialized blackboard entries.
        /// </summary>
        public IReadOnlyList<GraphBlackboardEntry> Entries => _entries;

        /// <summary>
        /// Resolves the graph family id used when creating new wrappers.
        /// Derived authored blackboards can override this to expose family-specific
        /// wrappers through the shared value registry.
        /// </summary>
        /// <returns>The optional graph family id.</returns>
        protected virtual string GetFamilyId()
        {
            return null;
        }

        /// <summary>
        /// Creates one authored entry instance for this blackboard.
        /// Derived authored blackboards can override this to preserve family-specific
        /// entry shells while storing them through the shared serialized shape.
        /// </summary>
        /// <param name="key">Authored blackboard key.</param>
        /// <param name="value">Stored blackboard payload.</param>
        /// <returns>The authored blackboard entry instance.</returns>
        protected virtual GraphBlackboardEntry CreateEntry(
            string key,
            GraphBlackboardValue value)
        {
            return new GraphBlackboardEntry(key, value);
        }

        /// <summary>
        /// Ensures every serialized entry owns one stable identifier.
        /// </summary>
        public void EnsureEntryIds()
        {
            HashSet<SerializableGuid> usedIds = new();

            for (int i = 0; i < _entries.Count; i++)
            {
                GraphBlackboardEntry entry = _entries[i];

                if (entry == null)
                {
                    continue;
                }

                entry.EnsureId();

                if (entry.Id == SerializableGuid.Empty || !usedIds.Add(entry.Id))
                {
                    entry.RegenerateId();
                    usedIds.Add(entry.Id);
                }
            }
        }

        /// <summary>
        /// Clears all authored entries from the blackboard.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>
        /// Attempts to resolve one typed value by authored key.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="value">Resolved value when the key exists.</param>
        /// <returns>True when the payload can be read as the requested type.</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;

            return TryGetEntry(key, out GraphBlackboardEntry entry)
                && entry.TryGetValue(out value);
        }

        /// <summary>
        /// Attempts to resolve one typed value by stable entry identifier.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="entryId">Stable blackboard entry identifier.</param>
        /// <param name="value">Resolved value when the entry exists.</param>
        /// <returns>True when the payload can be read as the requested type.</returns>
        public bool TryGetValue<T>(SerializableGuid entryId, out T value)
        {
            value = default;

            return TryGetEntry(entryId, out GraphBlackboardEntry entry)
                && entry.TryGetValue(out value);
        }

        /// <summary>
        /// Returns one existing value or creates a fresh value through one factory.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="factory">Factory used when the key is missing.</param>
        /// <param name="familyId">Optional graph family id for family-specific wrappers.</param>
        /// <returns>The existing or created value.</returns>
        public T GetOrCreateValue<T>(
            string key,
            Func<T> factory,
            string familyId = null)
        {
            if (TryGetValue(key, out T existing))
            {
                return existing;
            }

            T created = factory();
            SetValue(key, created, familyId);
            return created;
        }

        /// <summary>
        /// Stores one typed value under the provided key.
        /// </summary>
        /// <typeparam name="T">Value type being stored.</typeparam>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="value">Value instance to store.</param>
        /// <param name="familyId">Optional graph family id for family-specific wrappers.</param>
        public void SetValue<T>(string key, T value, string familyId = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            string resolvedFamilyId = familyId ?? GetFamilyId();

            Type valueType = value == null ? typeof(T) : value.GetType();

            if (!GraphBlackboardValueRegistry.TryCreateValue(
                    valueType,
                    resolvedFamilyId,
                    out GraphBlackboardValue replacementValue)
                || !replacementValue.TrySetBoxedValue(value))
            {
                return;
            }

            GraphBlackboardEntry entry = FindEntryByKey(key);

            if (entry == null)
            {
                _entries.Add(CreateEntry(key, replacementValue));
                return;
            }

            if (entry.Value != null
                && entry.Value.CanStoreValueType(valueType)
                && entry.Value.TrySetBoxedValue(value))
            {
                return;
            }

            entry.Value = replacementValue;
        }

        /// <summary>
        /// Stores one boxed value under the provided key.
        /// </summary>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="value">Boxed value instance to store.</param>
        /// <param name="valueType">Explicit runtime type when the boxed value is null.</param>
        /// <param name="familyId">Optional graph family id for family-specific wrappers.</param>
        /// <returns>True when the value could be written.</returns>
        public bool TrySetBoxedValue(
            string key,
            object value,
            Type valueType = null,
            string familyId = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string resolvedFamilyId = familyId ?? GetFamilyId();

            Type resolvedValueType = valueType ?? value?.GetType();

            if (resolvedValueType == null
                || !GraphBlackboardValueRegistry.TryCreateValue(
                    resolvedValueType,
                    resolvedFamilyId,
                    out GraphBlackboardValue replacementValue)
                || !replacementValue.TrySetBoxedValue(value))
            {
                return false;
            }

            GraphBlackboardEntry entry = FindEntryByKey(key);

            if (entry == null)
            {
                _entries.Add(CreateEntry(key, replacementValue));
                EnsureEntryIds();
                return true;
            }

            if (entry.TrySetBoxedValue(value))
            {
                return true;
            }

            entry.Value = replacementValue;
            return true;
        }

        /// <summary>
        /// Removes one entry by authored key.
        /// </summary>
        /// <param name="key">Blackboard entry key.</param>
        /// <returns>True when at least one entry was removed.</returns>
        public bool Remove(string key)
        {
            return _entries.RemoveAll(candidate => string.Equals(
                candidate?.Key,
                key,
                StringComparison.Ordinal)) > 0;
        }

        /// <summary>
        /// Removes one entry by stable identifier.
        /// </summary>
        /// <param name="entryId">Stable blackboard entry identifier.</param>
        /// <returns>True when at least one entry was removed.</returns>
        public bool Remove(SerializableGuid entryId)
        {
            return _entries.RemoveAll(candidate => candidate != null && candidate.Id == entryId) > 0;
        }

        /// <summary>
        /// Attempts to resolve one serialized entry by authored key.
        /// </summary>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="entry">Resolved entry when it exists.</param>
        /// <returns>True when the entry exists.</returns>
        public bool TryGetEntry(string key, out GraphBlackboardEntry entry)
        {
            entry = string.IsNullOrWhiteSpace(key) ? null : FindEntryByKey(key);
            return entry != null;
        }

        /// <summary>
        /// Attempts to resolve one serialized entry by stable identifier.
        /// </summary>
        /// <param name="entryId">Stable blackboard entry identifier.</param>
        /// <param name="entry">Resolved entry when it exists.</param>
        /// <returns>True when the entry exists.</returns>
        public bool TryGetEntry(SerializableGuid entryId, out GraphBlackboardEntry entry)
        {
            EnsureEntryIds();
            entry = entryId == SerializableGuid.Empty
                ? null
                : _entries.Find(candidate => candidate != null && candidate.Id == entryId);

            return entry != null;
        }

        private GraphBlackboardEntry FindEntryByKey(string key)
        {
            EnsureEntryIds();
            return _entries.Find(candidate => string.Equals(
                candidate?.Key,
                key,
                StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Represents one named blackboard entry that wraps one typed value payload.
    /// </summary>
    [Serializable]
    public class GraphBlackboardEntry
    {
        [SerializeField, HideInInspector] private SerializableGuid _id;
        [SerializeField] private string _key = string.Empty;
        [SerializeReference] private GraphBlackboardValue _value;

        /// <summary>
        /// Gets the stable entry identifier.
        /// </summary>
        public SerializableGuid Id => _id;

        /// <summary>
        /// Gets the authored blackboard key.
        /// </summary>
        public string Key => _key;

        /// <summary>
        /// Gets or sets the stored blackboard payload.
        /// </summary>
        public GraphBlackboardValue Value
        {
            get => _value;
            set => _value = value;
        }

        /// <summary>
        /// Initializes one empty entry for Unity serialization.
        /// </summary>
        public GraphBlackboardEntry()
        {
            EnsureId();
        }

        /// <summary>
        /// Initializes one entry with the provided key and payload.
        /// </summary>
        /// <param name="key">Authored blackboard key.</param>
        /// <param name="value">Stored blackboard payload.</param>
        public GraphBlackboardEntry(string key, GraphBlackboardValue value)
        {
            EnsureId();
            _key = key ?? string.Empty;
            _value = value;
        }

        /// <summary>
        /// Ensures the entry owns one stable identifier.
        /// </summary>
        public void EnsureId()
        {
            if (_id == SerializableGuid.Empty)
            {
                _id = SerializableGuid.NewGuid();
            }
        }

        /// <summary>
        /// Replaces the entry identifier with one fresh value.
        /// </summary>
        public void RegenerateId()
        {
            _id = SerializableGuid.NewGuid();
        }

        /// <summary>
        /// Replaces the entry identifier with one provided migration value.
        /// </summary>
        /// <param name="id">Identifier to assign.</param>
        public void AssignId(SerializableGuid id)
        {
            _id = id == SerializableGuid.Empty
                ? SerializableGuid.NewGuid()
                : id;
        }

        /// <summary>
        /// Attempts to resolve the stored payload as the requested type.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the payload can be read as the requested type.</returns>
        public bool TryGetValue<T>(out T value)
        {
            value = default;

            if (_value == null || !_value.TryGetValue(typeof(T), out object boxedValue))
            {
                return false;
            }

            value = (T)boxedValue;
            return true;
        }

        /// <summary>
        /// Resolves the stored payload as the requested type.
        /// </summary>
        /// <typeparam name="T">Requested runtime type.</typeparam>
        /// <returns>The resolved value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the payload cannot be read as the requested type.
        /// </exception>
        public T GetValue<T>()
        {
            if (!TryGetValue(out T value))
            {
                throw new InvalidOperationException(
                    $"Blackboard entry '{_key}' cannot be read as {typeof(T).FullName}.");
            }

            return value;
        }

        /// <summary>
        /// Attempts to replace the stored payload with one typed value.
        /// </summary>
        /// <typeparam name="T">Payload type.</typeparam>
        /// <param name="value">Value to store.</param>
        /// <returns>True when the existing wrapper accepted the value.</returns>
        public bool TrySetValue<T>(T value)
        {
            Type valueType = value == null ? typeof(T) : value.GetType();

            return _value != null
                && _value.CanStoreValueType(valueType)
                && _value.TrySetBoxedValue(value);
        }

        /// <summary>
        /// Attempts to replace the stored payload with one boxed value.
        /// </summary>
        /// <param name="value">Boxed value payload.</param>
        /// <returns>True when the existing wrapper accepted the value.</returns>
        public bool TrySetBoxedValue(object value)
        {
            Type valueType = value?.GetType() ?? _value?.GetExpectedValueType();

            return valueType != null
                && _value != null
                && _value.CanStoreValueType(valueType)
                && _value.TrySetBoxedValue(value);
        }
    }
}