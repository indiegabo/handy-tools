using System;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Implements one strongly typed blackboard wrapper for concrete runtime types.
    /// </summary>
    /// <typeparam name="TValue">Runtime type stored by the wrapper.</typeparam>
    [Serializable]
    public abstract class GraphBlackboardTypedValue<TValue> : GraphBlackboardValue
    {
        [SerializeField] private TValue _value;

        /// <summary>
        /// Gets or sets the stored runtime value.
        /// </summary>
        protected TValue Value
        {
            get => _value;
            set => _value = value;
        }

        /// <inheritdoc />
        public override Type GetExpectedValueType()
        {
            return typeof(TValue);
        }

        /// <inheritdoc />
        public override object GetBoxedValue()
        {
            return _value;
        }

        /// <inheritdoc />
        public override bool TrySetBoxedValue(object value)
        {
            if (value == null)
            {
                if (typeof(TValue).IsValueType
                    && Nullable.GetUnderlyingType(typeof(TValue)) == null)
                {
                    return false;
                }

                _value = default;
                return true;
            }

            if (value is not TValue typedValue)
            {
                return false;
            }

            _value = typedValue;
            return true;
        }
    }

    /// <summary>
    /// Stores one int payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Int", typeof(int), Order = 0)]
    public sealed class GraphBlackboardIntValue : GraphBlackboardTypedValue<int>
    {
    }

    /// <summary>
    /// Stores one long payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Long", typeof(long), Order = 10)]
    public sealed class GraphBlackboardLongValue : GraphBlackboardTypedValue<long>
    {
    }

    /// <summary>
    /// Stores one float payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Float", typeof(float), Order = 20)]
    public sealed class GraphBlackboardFloatValue : GraphBlackboardTypedValue<float>
    {
    }

    /// <summary>
    /// Stores one double payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Double", typeof(double), Order = 30)]
    public sealed class GraphBlackboardDoubleValue : GraphBlackboardTypedValue<double>
    {
    }

    /// <summary>
    /// Stores one string payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("String", typeof(string), Order = 40)]
    public sealed class GraphBlackboardStringValue : GraphBlackboardTypedValue<string>
    {
    }

    /// <summary>
    /// Stores one bool payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Bool", typeof(bool), Order = 50)]
    public sealed class GraphBlackboardBoolValue : GraphBlackboardTypedValue<bool>
    {
    }

    /// <summary>
    /// Stores one Vector2 payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Vector2", typeof(Vector2), Order = 60)]
    public sealed class GraphBlackboardVector2Value : GraphBlackboardTypedValue<Vector2>
    {
    }

    /// <summary>
    /// Stores one Vector2Int payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Vector2Int", typeof(Vector2Int), Order = 70)]
    public sealed class GraphBlackboardVector2IntValue : GraphBlackboardTypedValue<Vector2Int>
    {
    }

    /// <summary>
    /// Stores one Vector3 payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Vector3", typeof(Vector3), Order = 80)]
    public sealed class GraphBlackboardVector3Value : GraphBlackboardTypedValue<Vector3>
    {
    }

    /// <summary>
    /// Stores one Vector3Int payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Vector3Int", typeof(Vector3Int), Order = 90)]
    public sealed class GraphBlackboardVector3IntValue : GraphBlackboardTypedValue<Vector3Int>
    {
    }

    /// <summary>
    /// Stores one Vector4 payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Vector4", typeof(Vector4), Order = 100)]
    public sealed class GraphBlackboardVector4Value : GraphBlackboardTypedValue<Vector4>
    {
    }

    /// <summary>
    /// Stores one Quaternion payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Quaternion", typeof(Quaternion), Order = 110)]
    public sealed class GraphBlackboardQuaternionValue : GraphBlackboardTypedValue<Quaternion>
    {
    }

    /// <summary>
    /// Stores one Color payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Color", typeof(Color), Order = 120)]
    public sealed class GraphBlackboardColorValue : GraphBlackboardTypedValue<Color>
    {
    }

    /// <summary>
    /// Stores one Rect payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Rect", typeof(Rect), Order = 130)]
    public sealed class GraphBlackboardRectValue : GraphBlackboardTypedValue<Rect>
    {
    }

    /// <summary>
    /// Stores one RectInt payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("RectInt", typeof(RectInt), Order = 140)]
    public sealed class GraphBlackboardRectIntValue : GraphBlackboardTypedValue<RectInt>
    {
    }

    /// <summary>
    /// Stores one Bounds payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Bounds", typeof(Bounds), Order = 150)]
    public sealed class GraphBlackboardBoundsValue : GraphBlackboardTypedValue<Bounds>
    {
    }

    /// <summary>
    /// Stores one BoundsInt payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("BoundsInt", typeof(BoundsInt), Order = 160)]
    public sealed class GraphBlackboardBoundsIntValue : GraphBlackboardTypedValue<BoundsInt>
    {
    }

    /// <summary>
    /// Stores one AnimationCurve payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("AnimationCurve", typeof(AnimationCurve), Order = 170)]
    public sealed class GraphBlackboardAnimationCurveValue : GraphBlackboardTypedValue<AnimationCurve>
    {
    }

    /// <summary>
    /// Stores one Gradient payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Gradient", typeof(Gradient), Order = 180)]
    public sealed class GraphBlackboardGradientValue : GraphBlackboardTypedValue<Gradient>
    {
    }

    /// <summary>
    /// Stores one LayerMask payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("LayerMask", typeof(LayerMask), Order = 190)]
    public sealed class GraphBlackboardLayerMaskValue : GraphBlackboardTypedValue<LayerMask>
    {
    }

    /// <summary>
    /// Stores one enum payload together with its concrete runtime enum type.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Enum", typeof(Enum), Order = 200, SupportsDerivedTypes = true)]
    public sealed class GraphBlackboardEnumValue : GraphBlackboardValue
    {
        [SerializeField] private string _enumTypeName = string.Empty;
        [SerializeField] private string _valueName = string.Empty;

        /// <summary>
        /// Gets the serialized assembly-qualified enum type name.
        /// </summary>
        public string EnumTypeName => _enumTypeName;

        /// <summary>
        /// Gets the boxed enum value when the configured type is valid.
        /// </summary>
        public object EnumValue => GetBoxedValue();

        /// <inheritdoc />
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
        /// Replaces the configured enum type and resets the stored value.
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
        /// <returns>True when the value exists on the configured enum type.</returns>
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

        /// <inheritdoc />
        public override Type GetExpectedValueType()
        {
            return ResolveEnumType() ?? typeof(Enum);
        }

        /// <inheritdoc />
        public override bool CanStoreValueType(Type valueType)
        {
            Type enumType = ResolveEnumType();
            return valueType != null
                && valueType.IsEnum
                && (enumType == null || enumType == valueType);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
    [GraphBlackboardValueDescriptor(
        "Typed Object",
        typeof(UnityEngine.Object),
        Order = 210,
        SupportsDerivedTypes = true,
        HiddenFromPicker = true)]
    public class GraphBlackboardUnityObjectValue : GraphBlackboardValue
    {
        [SerializeField] private string _objectTypeName = string.Empty;
        [SerializeField] private UnityEngine.Object _value;

        /// <summary>
        /// Gets the serialized assembly-qualified object type name.
        /// </summary>
        public string ObjectTypeName => _objectTypeName;

        /// <summary>
        /// Gets the stored Unity object reference.
        /// </summary>
        public UnityEngine.Object Value => _value;

        /// <inheritdoc />
        public override void InitializeForValueType(Type valueType)
        {
            if (valueType == null || !typeof(UnityEngine.Object).IsAssignableFrom(valueType))
            {
                return;
            }

            _objectTypeName = valueType.AssemblyQualifiedName ?? valueType.FullName ?? string.Empty;

            if (!IsUnityObjectUnavailable(_value) && !valueType.IsInstanceOfType(_value))
            {
                _value = null;
            }
        }

        /// <summary>
        /// Replaces the allowed object reference type.
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
            Type fallbackType = IsUnityObjectUnavailable(_value)
                ? typeof(UnityEngine.Object)
                : _value.GetType();
            Type resolvedType = ResolveSerializedType(_objectTypeName, fallbackType);
            return resolvedType != null && typeof(UnityEngine.Object).IsAssignableFrom(resolvedType)
                ? resolvedType
                : typeof(UnityEngine.Object);
        }

        /// <inheritdoc />
        public override Type GetExpectedValueType()
        {
            return ResolveObjectType();
        }

        /// <inheritdoc />
        public override object GetBoxedValue()
        {
            return _value;
        }

        /// <inheritdoc />
        public override bool TryGetValue(Type requestedType, out object value)
        {
            value = null;

            if (requestedType == null)
            {
                return false;
            }

            if (IsUnityObjectUnavailable(_value))
            {
                Type configuredType = GetExpectedValueType();

                if (configuredType == null)
                {
                    return false;
                }

                return requestedType == typeof(object)
                    || requestedType.IsAssignableFrom(configuredType);
            }

            return TryResolveUnityObjectValue(_value, requestedType, out value);
        }

        /// <inheritdoc />
        public override bool TrySetBoxedValue(object value)
        {
            if (value == null)
            {
                _value = null;
                return true;
            }

            if (value is not UnityEngine.Object objectValue)
            {
                return false;
            }

            if (IsUnityObjectUnavailable(objectValue))
            {
                _value = null;
                return true;
            }

            Type expectedType = GetExpectedValueType();

            if (!TryCoerceUnityObjectValue(objectValue, expectedType, out UnityEngine.Object coercedValue))
            {
                return false;
            }

            _value = coercedValue;
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

            if (requestedType == typeof(object) || requestedType.IsInstanceOfType(objectValue))
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
    [GraphBlackboardValueDescriptor("Object", typeof(UnityEngine.Object), Order = 220)]
    public sealed class GraphBlackboardObjectValue : GraphBlackboardUnityObjectValue
    {
        /// <inheritdoc />
        public override void InitializeForValueType(Type valueType)
        {
            base.InitializeForValueType(typeof(UnityEngine.Object));
        }

        /// <inheritdoc />
        public override Type GetExpectedValueType()
        {
            return typeof(UnityEngine.Object);
        }
    }

    /// <summary>
    /// Stores one GameObject reference.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("GameObject", typeof(GameObject), Order = 230)]
    public sealed class GraphBlackboardGameObjectValue : GraphBlackboardUnityObjectValue
    {
        /// <inheritdoc />
        public override void InitializeForValueType(Type valueType)
        {
            base.InitializeForValueType(typeof(GameObject));
        }

        /// <inheritdoc />
        public override Type GetExpectedValueType()
        {
            return typeof(GameObject);
        }
    }

    /// <summary>
    /// Stores one ScriptableObject reference.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("ScriptableObject", typeof(ScriptableObject), Order = 240)]
    public sealed class GraphBlackboardScriptableObjectValue : GraphBlackboardUnityObjectValue
    {
        /// <inheritdoc />
        public override void InitializeForValueType(Type valueType)
        {
            base.InitializeForValueType(typeof(ScriptableObject));
        }

        /// <inheritdoc />
        public override Type GetExpectedValueType()
        {
            return typeof(ScriptableObject);
        }
    }
}