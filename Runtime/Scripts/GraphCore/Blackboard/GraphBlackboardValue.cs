using System;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Base class for blackboard payload wrappers stored through SerializeReference.
    /// </summary>
    [Serializable]
    public abstract class GraphBlackboardValue
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
        /// Gets whether the wrapper can store values of the requested type.
        /// </summary>
        /// <param name="valueType">Runtime type being requested.</param>
        /// <returns>True when the wrapper can store the type.</returns>
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
        /// <returns>The boxed payload.</returns>
        public abstract object GetBoxedValue();

        /// <summary>
        /// Attempts to replace the stored payload.
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
        /// <param name="fallbackType">Fallback type when the name is invalid.</param>
        /// <returns>The resolved runtime type.</returns>
        protected static Type ResolveSerializedType(
            string serializedTypeName,
            Type fallbackType)
        {
            return GraphSerializedTypeResolver.Resolve(serializedTypeName, fallbackType);
        }
    }
}