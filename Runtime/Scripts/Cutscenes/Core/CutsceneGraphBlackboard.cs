using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Serializable blackboard owned by a <see cref="CutsceneGraph"/>.
    /// Provides typed storage for common Unity-serializable values together
    /// with extensible wrapper registration for additional value kinds.
    /// </summary>
    [Serializable]
    public sealed class CutsceneGraphBlackboard
    {
        [SerializeField]
        private List<CutsceneGraphBlackboardEntry> _entries = new();

        /// <summary>
        /// Gets a read-only view of the serialized entries.
        /// </summary>
        public IReadOnlyList<CutsceneGraphBlackboardEntry> Entries => _entries;

        /// <summary>
        /// Ensures every serialized entry owns one stable identifier.
        /// </summary>
        public void EnsureEntryIds()
        {
            HashSet<SerializableGuid> usedIds = new();

            for (int i = 0; i < _entries.Count; i++)
            {
                CutsceneGraphBlackboardEntry entry = _entries[i];

                if (entry == null)
                {
                    continue;
                }

                entry.EnsureId();

                if (entry.Id == SerializableGuid.Empty
                    || !usedIds.Add(entry.Id))
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
        /// Attempts to resolve one typed value from the blackboard.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="value">Resolved value when the key exists.</param>
        /// <returns>True when the stored entry can be read as <typeparamref name="T"/>.</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;

            return TryGetEntry(key, out CutsceneGraphBlackboardEntry entry)
                && entry.TryGetValue(out value);
        }

        /// <summary>
        /// Attempts to resolve one typed value from the blackboard by entry id.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="entryId">Stable blackboard entry identifier.</param>
        /// <param name="value">Resolved value when the entry exists.</param>
        /// <returns>True when the stored entry can be read as <typeparamref name="T"/>.</returns>
        public bool TryGetValue<T>(SerializableGuid entryId, out T value)
        {
            value = default;

            return TryGetEntry(entryId, out CutsceneGraphBlackboardEntry entry)
                && entry.TryGetValue(out value);
        }

        /// <summary>
        /// Returns one existing value or creates a fresh value through the provided factory.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="factory">Factory used when the key is missing.</param>
        /// <returns>The existing or created value.</returns>
        public T GetOrCreateValue<T>(string key, Func<T> factory)
        {
            if (TryGetValue(key, out T existing))
            {
                return existing;
            }

            T created = factory();
            SetValue(key, created);
            return created;
        }

        /// <summary>
        /// Stores one value under the provided key.
        /// Unsupported value types are ignored without mutating existing data.
        /// </summary>
        /// <typeparam name="T">Value type being stored.</typeparam>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="value">Value instance to store.</param>
        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            Type valueType = value == null ? typeof(T) : value.GetType();

            if (!CutsceneBlackboardValueRegistry.TryCreateValue(
                    valueType,
                    out CutsceneGraphBlackboardValue replacementValue))
            {
                return;
            }

            if (!replacementValue.TrySetBoxedValue(value))
            {
                return;
            }

            CutsceneGraphBlackboardEntry entry = FindEntryByKey(key);

            if (entry == null)
            {
                _entries.Add(new CutsceneGraphBlackboardEntry(key, replacementValue));
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
        /// Unsupported value types are ignored without mutating existing data.
        /// </summary>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="value">Boxed value instance to store.</param>
        /// <param name="valueType">Explicit runtime type when the boxed value is null.</param>
        /// <returns>True when the value could be written.</returns>
        public bool TrySetBoxedValue(
            string key,
            object value,
            Type valueType = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            Type resolvedValueType = valueType ?? value?.GetType();

            if (resolvedValueType == null
                || !CutsceneBlackboardValueRegistry.TryCreateValue(
                    resolvedValueType,
                    out CutsceneGraphBlackboardValue replacementValue)
                || !replacementValue.TrySetBoxedValue(value))
            {
                return false;
            }

            CutsceneGraphBlackboardEntry entry = FindEntryByKey(key);

            if (entry == null)
            {
                _entries.Add(new CutsceneGraphBlackboardEntry(key, replacementValue));
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
        /// Removes one entry by key.
        /// </summary>
        /// <param name="key">Blackboard entry key.</param>
        /// <returns>True when at least one entry was removed.</returns>
        public bool Remove(string key)
        {
            return _entries.RemoveAll(candidate => string.Equals(
                candidate.Key,
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
            return _entries.RemoveAll(candidate => candidate != null
                && candidate.Id == entryId) > 0;
        }

        /// <summary>
        /// Attempts to resolve one serialized entry by authored key.
        /// </summary>
        /// <param name="key">Blackboard entry key.</param>
        /// <param name="entry">Resolved entry when it exists.</param>
        /// <returns>True when the entry exists.</returns>
        public bool TryGetEntry(string key, out CutsceneGraphBlackboardEntry entry)
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
        public bool TryGetEntry(SerializableGuid entryId, out CutsceneGraphBlackboardEntry entry)
        {
            EnsureEntryIds();
            entry = entryId == SerializableGuid.Empty ? null : _entries.Find(
                candidate => candidate != null && candidate.Id == entryId);

            return entry != null;
        }

        private CutsceneGraphBlackboardEntry FindEntryByKey(string key)
        {
            EnsureEntryIds();
            return _entries.Find(candidate => string.Equals(
                candidate?.Key,
                key,
                StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Represents one named blackboard entry that wraps a typed value payload.
    /// </summary>
    [Serializable]
    public sealed class CutsceneGraphBlackboardEntry
    {
        [SerializeField, HideInInspector]
        private SerializableGuid _id;

        [SerializeField]
        private string _key = string.Empty;

        [SerializeReference]
        private CutsceneGraphBlackboardValue _value;

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
        public CutsceneGraphBlackboardValue Value
        {
            get => _value;
            set => _value = value;
        }

        /// <summary>
        /// Initializes an empty entry for Unity serialization.
        /// </summary>
        public CutsceneGraphBlackboardEntry()
        {
            EnsureId();
        }

        /// <summary>
        /// Initializes one entry with the provided key and value.
        /// </summary>
        /// <param name="key">Authored blackboard key.</param>
        /// <param name="value">Stored blackboard payload.</param>
        public CutsceneGraphBlackboardEntry(
            string key,
            CutsceneGraphBlackboardValue value)
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
        /// Replaces the entry identifier with a fresh value.
        /// </summary>
        public void RegenerateId()
        {
            _id = SerializableGuid.NewGuid();
        }

        /// <summary>
        /// Attempts to resolve the stored payload as the requested type.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the payload can be read as <typeparamref name="T"/>.</returns>
        public bool TryGetValue<T>(out T value)
        {
            value = default;

            if (_value == null
                || !_value.TryGetValue(typeof(T), out object boxedValue))
            {
                return false;
            }

            value = (T)boxedValue;
            return true;
        }

        /// <summary>
        /// Resolves the stored payload as the requested type.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <returns>The resolved value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the payload cannot be read as <typeparamref name="T"/>.
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

    /// <summary>
    /// Base class for graph blackboard value wrappers.
    /// </summary>
    [Serializable]
    public abstract class CutsceneGraphBlackboardValue
    {
        /// <summary>
        /// Initializes the wrapper for the requested runtime type.
        /// </summary>
        /// <param name="valueType">Runtime type the wrapper should represent.</param>
        public virtual void InitializeForValueType(Type valueType)
        {
        }

        /// <summary>
        /// Gets the runtime type currently represented by the wrapper.
        /// </summary>
        /// <returns>The represented runtime type.</returns>
        public abstract Type GetExpectedValueType();

        /// <summary>
        /// Determines whether the wrapper can store values of the requested type.
        /// </summary>
        /// <param name="valueType">Runtime type being requested.</param>
        /// <returns>True when the wrapper can store the requested type.</returns>
        public virtual bool CanStoreValueType(Type valueType)
        {
            if (valueType == null)
            {
                return false;
            }

            Type expectedType = GetExpectedValueType();

            if (expectedType == null)
            {
                return false;
            }

            return expectedType == valueType
                || expectedType.IsAssignableFrom(valueType);
        }

        /// <summary>
        /// Resolves the boxed value stored by the wrapper.
        /// </summary>
        /// <returns>The boxed value payload.</returns>
        public abstract object GetBoxedValue();

        /// <summary>
        /// Attempts to replace the stored value payload.
        /// </summary>
        /// <param name="value">Boxed value payload.</param>
        /// <returns>True when the payload was accepted.</returns>
        public abstract bool TrySetBoxedValue(object value);

        /// <summary>
        /// Attempts to resolve the stored payload as the requested runtime type.
        /// </summary>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the stored payload matches the requested type.</returns>
        public virtual bool TryGetValue(Type requestedType, out object value)
        {
            value = null;

            if (requestedType == null)
            {
                return false;
            }

            object boxedValue = GetBoxedValue();

            if (boxedValue == null)
            {
                if (!requestedType.IsValueType
                    || Nullable.GetUnderlyingType(requestedType) != null
                    || requestedType == typeof(object))
                {
                    return true;
                }

                return false;
            }

            if (requestedType == typeof(object)
                || requestedType.IsInstanceOfType(boxedValue))
            {
                value = boxedValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves one runtime type from its serialized assembly-qualified name.
        /// </summary>
        /// <param name="serializedTypeName">Serialized type name.</param>
        /// <param name="fallbackType">Fallback type when the name cannot be resolved.</param>
        /// <returns>The resolved runtime type.</returns>
        protected static Type ResolveSerializedType(
            string serializedTypeName,
            Type fallbackType)
        {
            return string.IsNullOrWhiteSpace(serializedTypeName)
                ? fallbackType
                : Type.GetType(serializedTypeName) ?? fallbackType;
        }
    }

    /// <summary>
    /// Stores one int payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Int", typeof(int), Order = 0)]
    public sealed class CutsceneGraphBlackboardIntValue : CutsceneGraphBlackboardValue
    {
        public int Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(int);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not int typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one long payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Long", typeof(long), Order = 10)]
    public sealed class CutsceneGraphBlackboardLongValue : CutsceneGraphBlackboardValue
    {
        public long Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(long);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not long typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one float payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Float", typeof(float), Order = 20)]
    public sealed class CutsceneGraphBlackboardFloatValue : CutsceneGraphBlackboardValue
    {
        public float Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(float);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not float typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one double payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Double", typeof(double), Order = 30)]
    public sealed class CutsceneGraphBlackboardDoubleValue : CutsceneGraphBlackboardValue
    {
        public double Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(double);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not double typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one string payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("String", typeof(string), Order = 40)]
    public sealed class CutsceneGraphBlackboardStringValue : CutsceneGraphBlackboardValue
    {
        public string Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(string);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value != null && value is not string)
            {
                return false;
            }

            Value = value as string;
            return true;
        }
    }

    /// <summary>
    /// Stores one bool payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Bool", typeof(bool), Order = 50)]
    public sealed class CutsceneGraphBlackboardBoolValue : CutsceneGraphBlackboardValue
    {
        public bool Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(bool);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not bool typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Vector2 payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Vector2", typeof(Vector2), Order = 60)]
    public sealed class CutsceneGraphBlackboardVector2Value : CutsceneGraphBlackboardValue
    {
        public Vector2 Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Vector2);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Vector2 typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Vector2Int payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Vector2Int", typeof(Vector2Int), Order = 70)]
    public sealed class CutsceneGraphBlackboardVector2IntValue : CutsceneGraphBlackboardValue
    {
        public Vector2Int Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Vector2Int);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Vector2Int typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Vector3 payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Vector3", typeof(Vector3), Order = 80)]
    public sealed class CutsceneGraphBlackboardVector3Value : CutsceneGraphBlackboardValue
    {
        public Vector3 Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Vector3);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Vector3 typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Vector3Int payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Vector3Int", typeof(Vector3Int), Order = 90)]
    public sealed class CutsceneGraphBlackboardVector3IntValue : CutsceneGraphBlackboardValue
    {
        public Vector3Int Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Vector3Int);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Vector3Int typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Vector4 payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Vector4", typeof(Vector4), Order = 100)]
    public sealed class CutsceneGraphBlackboardVector4Value : CutsceneGraphBlackboardValue
    {
        public Vector4 Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Vector4);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Vector4 typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Quaternion payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Quaternion", typeof(Quaternion), Order = 110)]
    public sealed class CutsceneGraphBlackboardQuaternionValue : CutsceneGraphBlackboardValue
    {
        public Quaternion Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Quaternion);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Quaternion typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Color payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Color", typeof(Color), Order = 120)]
    public sealed class CutsceneGraphBlackboardColorValue : CutsceneGraphBlackboardValue
    {
        public Color Value = Color.white;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Color);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Color typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Rect payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Rect", typeof(Rect), Order = 130)]
    public sealed class CutsceneGraphBlackboardRectValue : CutsceneGraphBlackboardValue
    {
        public Rect Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Rect);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Rect typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one RectInt payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("RectInt", typeof(RectInt), Order = 140)]
    public sealed class CutsceneGraphBlackboardRectIntValue : CutsceneGraphBlackboardValue
    {
        public RectInt Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(RectInt);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not RectInt typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one Bounds payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Bounds", typeof(Bounds), Order = 150)]
    public sealed class CutsceneGraphBlackboardBoundsValue : CutsceneGraphBlackboardValue
    {
        public Bounds Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Bounds);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not Bounds typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one BoundsInt payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("BoundsInt", typeof(BoundsInt), Order = 160)]
    public sealed class CutsceneGraphBlackboardBoundsIntValue : CutsceneGraphBlackboardValue
    {
        public BoundsInt Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(BoundsInt);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not BoundsInt typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one AnimationCurve payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("AnimationCurve", typeof(AnimationCurve), Order = 170)]
    public sealed class CutsceneGraphBlackboardAnimationCurveValue : CutsceneGraphBlackboardValue
    {
        public AnimationCurve Value = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(AnimationCurve);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value != null && value is not AnimationCurve)
            {
                return false;
            }

            Value = value as AnimationCurve;
            return true;
        }
    }

    /// <summary>
    /// Stores one Gradient payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Gradient", typeof(Gradient), Order = 180)]
    public sealed class CutsceneGraphBlackboardGradientValue : CutsceneGraphBlackboardValue
    {
        public Gradient Value = new();

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(Gradient);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value != null && value is not Gradient)
            {
                return false;
            }

            Value = value as Gradient;
            return true;
        }
    }

    /// <summary>
    /// Stores one LayerMask payload.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("LayerMask", typeof(LayerMask), Order = 190)]
    public sealed class CutsceneGraphBlackboardLayerMaskValue : CutsceneGraphBlackboardValue
    {
        public LayerMask Value;

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(LayerMask);

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value is not LayerMask typedValue)
            {
                return false;
            }

            Value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one enum payload together with the concrete enum type.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Enum", typeof(Enum), Order = 200, SupportsDerivedTypes = true)]
    public sealed class CutsceneGraphBlackboardEnumValue : CutsceneGraphBlackboardValue
    {
        [SerializeField]
        private string _enumTypeName = string.Empty;

        [SerializeField]
        private string _valueName = string.Empty;

        /// <summary>
        /// Gets the serialized assembly-qualified enum type name.
        /// </summary>
        public string EnumTypeName => _enumTypeName;

        /// <summary>
        /// Gets the boxed enum value when the configured type is valid.
        /// </summary>
        public object EnumValue => GetBoxedValue();

        /// <inheritdoc/>
        public override void InitializeForValueType(Type valueType)
        {
            if (valueType == null || !valueType.IsEnum)
            {
                return;
            }

            _enumTypeName = valueType.AssemblyQualifiedName ?? valueType.FullName ?? string.Empty;
            string[] valueNames = Enum.GetNames(valueType);
            _valueName = valueNames.Length <= 0 ? string.Empty : valueNames[0];
        }

        /// <summary>
        /// Replaces the configured enum type and resets the stored value to the first declared enum value.
        /// </summary>
        /// <param name="enumType">Concrete enum type to store.</param>
        public void SetEnumType(Type enumType)
        {
            InitializeForValueType(enumType);
        }

        /// <summary>
        /// Attempts to assign one named enum value.
        /// </summary>
        /// <param name="valueName">Declared enum value name.</param>
        /// <returns>True when the value name exists on the configured enum type.</returns>
        public bool TrySetEnumValueByName(string valueName)
        {
            Type enumType = ResolveEnumType();

            if (enumType == null
                || string.IsNullOrWhiteSpace(valueName)
                || !Enum.IsDefined(enumType, valueName))
            {
                return false;
            }

            _valueName = valueName;
            return true;
        }

        /// <summary>
        /// Resolves the configured enum type.
        /// </summary>
        /// <returns>The concrete enum type when available.</returns>
        public Type ResolveEnumType()
        {
            Type resolvedType = ResolveSerializedType(_enumTypeName, null);
            return resolvedType != null && resolvedType.IsEnum
                ? resolvedType
                : null;
        }

        /// <inheritdoc/>
        public override Type GetExpectedValueType()
        {
            return ResolveEnumType() ?? typeof(Enum);
        }

        /// <inheritdoc/>
        public override bool CanStoreValueType(Type valueType)
        {
            Type enumType = ResolveEnumType();
            return valueType != null
                && valueType.IsEnum
                && (enumType == null || enumType == valueType);
        }

        /// <inheritdoc/>
        public override object GetBoxedValue()
        {
            Type enumType = ResolveEnumType();

            if (enumType == null || string.IsNullOrWhiteSpace(_valueName))
            {
                return null;
            }

            try
            {
                return Enum.Parse(enumType, _valueName);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value == null)
            {
                return false;
            }

            Type valueType = value.GetType();

            if (!valueType.IsEnum)
            {
                return false;
            }

            Type currentEnumType = ResolveEnumType();

            if (currentEnumType != null && currentEnumType != valueType)
            {
                return false;
            }

            InitializeForValueType(valueType);
            _valueName = value.ToString();
            return true;
        }
    }

    /// <summary>
    /// Stores one Unity object reference together with the concrete allowed object type.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor(
        "Typed Object",
        typeof(UnityEngine.Object),
        Order = 210,
        SupportsDerivedTypes = true,
        HiddenFromPicker = true)]
    public class CutsceneGraphBlackboardUnityObjectValue : CutsceneGraphBlackboardValue
    {
        [SerializeField]
        private string _objectTypeName = string.Empty;

        public UnityEngine.Object Value;

        /// <summary>
        /// Gets the serialized assembly-qualified object type name.
        /// </summary>
        public string ObjectTypeName => _objectTypeName;

        /// <inheritdoc/>
        public override void InitializeForValueType(Type valueType)
        {
            if (valueType == null
                || !typeof(UnityEngine.Object).IsAssignableFrom(valueType))
            {
                return;
            }

            _objectTypeName = valueType.AssemblyQualifiedName ?? valueType.FullName ?? string.Empty;

            if (Value != null && !valueType.IsInstanceOfType(Value))
            {
                Value = null;
            }
        }

        /// <summary>
        /// Replaces the allowed object reference type and clears any incompatible current value.
        /// </summary>
        /// <param name="objectType">Concrete Unity object type allowed by the slot.</param>
        public void SetObjectType(Type objectType)
        {
            InitializeForValueType(objectType);
        }

        /// <summary>
        /// Resolves the configured object type.
        /// </summary>
        /// <returns>The concrete Unity object type when available.</returns>
        public Type ResolveObjectType()
        {
            Type fallbackType = Value != null
                ? Value.GetType()
                : typeof(UnityEngine.Object);
            Type resolvedType = ResolveSerializedType(_objectTypeName, fallbackType);
            return resolvedType != null
                && typeof(UnityEngine.Object).IsAssignableFrom(resolvedType)
                ? resolvedType
                : typeof(UnityEngine.Object);
        }

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => ResolveObjectType();

        /// <inheritdoc/>
        public override object GetBoxedValue() => Value;

        /// <inheritdoc/>
        public override bool TryGetValue(Type requestedType, out object value)
        {
            value = null;

            if (requestedType == null)
            {
                return false;
            }

            if (Value == null)
            {
                Type configuredType = GetExpectedValueType();

                if (configuredType == null)
                {
                    return false;
                }

                return requestedType == typeof(object)
                    || requestedType.IsAssignableFrom(configuredType);
            }

            return TryResolveUnityObjectValue(Value, requestedType, out value);
        }

        /// <inheritdoc/>
        public override bool TrySetBoxedValue(object value)
        {
            if (value == null)
            {
                Value = null;
                return true;
            }

            if (value is not UnityEngine.Object objectValue)
            {
                return false;
            }

            Type expectedType = GetExpectedValueType();

            if (!TryCoerceUnityObjectValue(objectValue, expectedType, out UnityEngine.Object coercedValue))
            {
                return false;
            }

            Value = coercedValue;
            return true;
        }

        private static bool TryResolveUnityObjectValue(
            UnityEngine.Object objectValue,
            Type requestedType,
            out object value)
        {
            value = null;

            if (objectValue == null || requestedType == null)
            {
                return false;
            }

            if (requestedType == typeof(object)
                || requestedType.IsInstanceOfType(objectValue))
            {
                value = objectValue;
                return true;
            }

            if (requestedType == typeof(GameObject)
                && TryResolveOwnerGameObject(objectValue, out GameObject ownerGameObject))
            {
                value = ownerGameObject;
                return true;
            }

            if (typeof(Component).IsAssignableFrom(requestedType)
                && TryResolveOwnerGameObject(objectValue, out GameObject componentOwner))
            {
                Component component = componentOwner.GetComponent(requestedType);

                if (component != null)
                {
                    value = component;
                    return true;
                }
            }

            return false;
        }

        private static bool TryCoerceUnityObjectValue(
            UnityEngine.Object objectValue,
            Type expectedType,
            out UnityEngine.Object coercedValue)
        {
            coercedValue = null;

            if (objectValue == null || expectedType == null)
            {
                return false;
            }

            if (expectedType.IsInstanceOfType(objectValue))
            {
                coercedValue = objectValue;
                return true;
            }

            if (expectedType == typeof(GameObject)
                && TryResolveOwnerGameObject(objectValue, out GameObject ownerGameObject))
            {
                coercedValue = ownerGameObject;
                return true;
            }

            if (typeof(Component).IsAssignableFrom(expectedType)
                && TryResolveOwnerGameObject(objectValue, out GameObject componentOwner))
            {
                Component component = componentOwner.GetComponent(expectedType);

                if (component != null)
                {
                    coercedValue = component;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveOwnerGameObject(
            UnityEngine.Object objectValue,
            out GameObject gameObject)
        {
            switch (objectValue)
            {
                case GameObject ownerGameObject:
                    gameObject = ownerGameObject;
                    return true;

                case Component component:
                    gameObject = component.gameObject;
                    return true;

                default:
                    gameObject = null;
                    return false;
            }
        }
    }

    /// <summary>
    /// Stores one Unity object reference without restricting the concrete runtime subtype.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("Object", typeof(UnityEngine.Object), Order = 220)]
    public sealed class CutsceneGraphBlackboardObjectValue : CutsceneGraphBlackboardUnityObjectValue
    {
        /// <inheritdoc/>
        public override void InitializeForValueType(Type valueType)
        {
            base.InitializeForValueType(typeof(UnityEngine.Object));
        }

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(UnityEngine.Object);
    }

    /// <summary>
    /// Stores one GameObject reference.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("GameObject", typeof(GameObject), Order = 230)]
    public sealed class CutsceneGraphBlackboardGameObjectValue : CutsceneGraphBlackboardUnityObjectValue
    {
        /// <inheritdoc/>
        public override void InitializeForValueType(Type valueType)
        {
            base.InitializeForValueType(typeof(GameObject));
        }

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(GameObject);
    }

    /// <summary>
    /// Stores one ScriptableObject reference.
    /// </summary>
    [Serializable]
    [CutsceneBlackboardValueDescriptor("ScriptableObject", typeof(ScriptableObject), Order = 240)]
    public sealed class CutsceneGraphBlackboardScriptableObjectValue : CutsceneGraphBlackboardUnityObjectValue
    {
        /// <inheritdoc/>
        public override void InitializeForValueType(Type valueType)
        {
            base.InitializeForValueType(typeof(ScriptableObject));
        }

        /// <inheritdoc/>
        public override Type GetExpectedValueType() => typeof(ScriptableObject);
    }
}
