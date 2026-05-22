using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.GraphCore;
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
    public sealed class CutsceneGraphBlackboard : GraphBlackboard, ISerializationCallbackReceiver
    {
        private readonly CutsceneTypedReadOnlyListAdapter<GraphBlackboardEntry, CutsceneGraphBlackboardEntry>
            _entriesView;

        /// <summary>
        /// Initializes one cutscene-authored blackboard shell.
        /// </summary>
        public CutsceneGraphBlackboard()
        {
            _entriesView = new CutsceneTypedReadOnlyListAdapter<GraphBlackboardEntry, CutsceneGraphBlackboardEntry>(
                () => base.Entries);
        }

        /// <summary>
        /// Gets a read-only view of the serialized entries.
        /// </summary>
        public new IReadOnlyList<CutsceneGraphBlackboardEntry> Entries
        {
            get
            {
                NormalizeEntryShapes();
                return _entriesView;
            }
        }

        /// <inheritdoc />
        protected override string GetFamilyId()
        {
            return CutsceneGraphFamily.Id;
        }

        /// <inheritdoc />
        protected override GraphBlackboardEntry CreateEntry(
            string key,
            GraphBlackboardValue value)
        {
            return new CutsceneGraphBlackboardEntry(key, value);
        }

        /// <summary>
        public bool TryGetEntry(string key, out CutsceneGraphBlackboardEntry entry)
        {
            NormalizeEntryShapes();
            entry = null;

            if (!base.TryGetEntry(key, out GraphBlackboardEntry candidate)
                || candidate is not CutsceneGraphBlackboardEntry cutsceneEntry)
            {
                return false;
            }

            entry = cutsceneEntry;
            return true;
        }

        /// <summary>
        /// Attempts to resolve one serialized entry by stable identifier.
        /// </summary>
        /// <param name="entryId">Stable blackboard entry identifier.</param>
        /// <param name="entry">Resolved entry when it exists.</param>
        /// <returns>True when the entry exists.</returns>
        public bool TryGetEntry(
            SerializableGuid entryId,
            out CutsceneGraphBlackboardEntry entry)
        {
            NormalizeEntryShapes();
            entry = null;

            if (!base.TryGetEntry(entryId, out GraphBlackboardEntry candidate)
                || candidate is not CutsceneGraphBlackboardEntry cutsceneEntry)
            {
                return false;
            }

            entry = cutsceneEntry;
            return true;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            NormalizeEntryShapes();
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            NormalizeEntryShapes();
        }

        private void NormalizeEntryShapes()
        {
            for (int index = 0; index < EntriesInternal.Count; index++)
            {
                GraphBlackboardEntry entry = EntriesInternal[index];

                if (entry == null)
                {
                    continue;
                }

                GraphBlackboardValue normalizedValue = NormalizeLegacyValue(entry.Value);

                if (entry is not CutsceneGraphBlackboardEntry cutsceneEntry)
                {
                    cutsceneEntry = new CutsceneGraphBlackboardEntry(
                        entry.Key,
                        normalizedValue);
                    cutsceneEntry.AssignId(entry.Id);
                    EntriesInternal[index] = cutsceneEntry;
                    continue;
                }

                if (!ReferenceEquals(cutsceneEntry.Value, normalizedValue))
                {
                    cutsceneEntry.Value = normalizedValue;
                }
            }

            EnsureEntryIds();
        }

        private static GraphBlackboardValue NormalizeLegacyValue(GraphBlackboardValue value)
        {
            if (value is not CutsceneGraphBlackboardValue legacyValue)
            {
                return value;
            }

            return CutsceneGraphCoreRuntimeMigrationUtility.CreateGraphBlackboardValue(legacyValue)
                ?? value;
        }
    }

    /// <summary>
    /// Represents one named blackboard entry that wraps a typed value payload.
    /// </summary>
    [Serializable]
    public class CutsceneGraphBlackboardEntry : GraphBlackboardEntry
    {
        /// <summary>
        /// Initializes an empty entry for Unity serialization.
        /// </summary>
        public CutsceneGraphBlackboardEntry()
        {
        }

        /// <summary>
        /// Initializes one entry with the provided key and value.
        /// </summary>
        /// <param name="key">Authored blackboard key.</param>
        /// <param name="value">Stored blackboard payload.</param>
        public CutsceneGraphBlackboardEntry(
            string key,
            GraphBlackboardValue value)
            : base(key, value)
        {
        }

        /// <summary>
        /// Initializes one entry with one legacy cutscene wrapper.
        /// </summary>
        /// <param name="key">Authored blackboard key.</param>
        /// <param name="value">Legacy cutscene wrapper payload.</param>
        public CutsceneGraphBlackboardEntry(
            string key,
            CutsceneGraphBlackboardValue value)
            : base(key, value)
        {
        }
    }

    /// <summary>
    /// Base class for graph blackboard value wrappers.
    /// </summary>
    [Serializable]
    public abstract class CutsceneGraphBlackboardValue : GraphBlackboardValue
    {
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

            if (!IsUnityObjectUnavailable(Value) && !valueType.IsInstanceOfType(Value))
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
            Type fallbackType = IsUnityObjectUnavailable(Value)
                ? typeof(UnityEngine.Object)
                : Value.GetType();
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

            if (IsUnityObjectUnavailable(Value))
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

            if (IsUnityObjectUnavailable(objectValue))
            {
                Value = null;
                return true;
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

            if (IsUnityObjectUnavailable(objectValue) || requestedType == null)
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
                Component component;

                try
                {
                    component = componentOwner.GetComponent(requestedType);
                }
                catch (InvalidOperationException)
                {
                    component = null;
                }

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

            if (IsUnityObjectUnavailable(objectValue) || expectedType == null)
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
                Component component;

                try
                {
                    component = componentOwner.GetComponent(expectedType);
                }
                catch (InvalidOperationException)
                {
                    component = null;
                }

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
            if (IsUnityObjectUnavailable(objectValue))
            {
                gameObject = null;
                return false;
            }

            switch (objectValue)
            {
                case GameObject ownerGameObject:
                    gameObject = ownerGameObject;
                    return true;

                case Component component:
                    try
                    {
                        gameObject = component.gameObject;
                        return !IsUnityObjectUnavailable(gameObject);
                    }
                    catch (InvalidOperationException)
                    {
                        gameObject = null;
                        return false;
                    }

                default:
                    gameObject = null;
                    return false;
            }
        }

        private static bool IsUnityObjectUnavailable(UnityEngine.Object objectValue)
        {
            if (ReferenceEquals(objectValue, null))
            {
                return true;
            }

            try
            {
                return objectValue == null;
            }
            catch (InvalidOperationException)
            {
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
