using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Resolves Simple Blackboard types and methods without introducing a hard
    /// package dependency.
    /// </summary>
    internal static class SimpleBlackboardRuntimeBridge
    {
        #region Constants

        private const string ContainerTypeFullName =
            "Zor.SimpleBlackboard.Components.SimpleBlackboardContainer";

        private const string ContainerTypeAssemblyQualifiedName =
            "Zor.SimpleBlackboard.Components.SimpleBlackboardContainer, Zor.SimpleBlackboard";

        private const string BlackboardTypeFullName =
            "Zor.SimpleBlackboard.Core.Blackboard";

        private const string BlackboardTypeAssemblyQualifiedName =
            "Zor.SimpleBlackboard.Core.Blackboard, Zor.SimpleBlackboard";

        private const string PropertyNameTypeFullName =
            "Zor.SimpleBlackboard.Core.BlackboardPropertyName";

        private const string PropertyNameTypeAssemblyQualifiedName =
            "Zor.SimpleBlackboard.Core.BlackboardPropertyName, Zor.SimpleBlackboard";

        #endregion

        #region Fields

        private static readonly Dictionary<string, object> s_propertyNames =
            new(StringComparer.Ordinal);

        private static readonly Dictionary<Type, Delegate> s_tryGetValueDelegates =
            new();

        private static readonly Dictionary<Type, Delegate> s_setValueDelegates =
            new();

        private static Type s_containerType;
        private static Type s_blackboardType;
        private static Type s_propertyNameListType;
        private static Type s_propertyNameType;
        private static ConstructorInfo s_propertyNameConstructor;
        private static PropertyInfo s_blackboardProperty;
        private static PropertyInfo s_propertyNameNameProperty;
        private static MethodInfo s_getPropertyNamesMethod;
        private static MethodInfo s_getValueTypeMethod;
        private static MethodInfo s_recreateBlackboardMethod;
        private static MethodInfo s_tryGetStructValueMethodDefinition;
        private static MethodInfo s_tryGetClassValueMethodDefinition;
        private static MethodInfo s_setStructValueMethodDefinition;
        private static MethodInfo s_setClassValueMethodDefinition;
        private static MethodInfo s_tryGetObjectValueMethod;
        private static MethodInfo s_containsValueMethod;
        private static TryGetObjectValueDelegate s_tryGetObjectValueDelegate;
        private static ContainsValueDelegate s_containsValueDelegate;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the Simple Blackboard runtime types were resolved
        /// successfully.
        /// </summary>
        public static bool IsAvailable =>
            ContainerType != null
            && BlackboardType != null
            && PropertyNameType != null
            && PropertyNameConstructor != null
            && BlackboardProperty != null;

        /// <summary>
        /// Gets the resolved Simple Blackboard container type.
        /// </summary>
        public static Type ContainerType =>
            s_containerType ??= ResolveRuntimeType(
                ContainerTypeFullName,
                ContainerTypeAssemblyQualifiedName);

        #endregion

        #region Public API

        /// <summary>
        /// Tries to resolve the runtime blackboard object from a container
        /// component.
        /// </summary>
        /// <param name="container">The candidate container component.</param>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <returns>True if a valid blackboard was resolved.</returns>
        public static bool TryGetBlackboard(Component container, out object blackboard)
        {
            blackboard = null;

            if (!IsAvailable
                || container == null
                || !ContainerType.IsInstanceOfType(container))
            {
                return false;
            }

            blackboard = BlackboardProperty.GetValue(container);
            return blackboard != null;
        }

        /// <summary>
        /// Tries to read a typed value from a resolved blackboard instance.
        /// </summary>
        /// <typeparam name="T">The value type to read.</typeparam>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <param name="value">The resolved value if found.</param>
        /// <returns>
        /// True if the value exists and matches the requested type.
        /// </returns>
        public static bool TryGetValue<T>(
            object blackboard,
            string propertyName,
            out T value)
        {
            value = default;

            if (!TryGetPropertyName(propertyName, out object boxedPropertyName)
                || blackboard == null)
            {
                return false;
            }

            if (!s_tryGetValueDelegates.TryGetValue(typeof(T), out Delegate cachedDelegate))
            {
                cachedDelegate = CreateTryGetValueDelegate<T>();
                s_tryGetValueDelegates.Add(typeof(T), cachedDelegate);
            }

            if (cachedDelegate is not TryGetValueDelegate<T> tryGetDelegate)
            {
                return false;
            }

            return tryGetDelegate(blackboard, boxedPropertyName, out value);
        }

        /// <summary>
        /// Writes a typed value into a resolved blackboard instance.
        /// </summary>
        /// <typeparam name="T">The value type to write.</typeparam>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if the value was written successfully.</returns>
        public static bool SetValue<T>(object blackboard, string propertyName, T value)
        {
            if (!TryGetPropertyName(propertyName, out object boxedPropertyName)
                || blackboard == null)
            {
                return false;
            }

            if (!s_setValueDelegates.TryGetValue(typeof(T), out Delegate cachedDelegate))
            {
                cachedDelegate = CreateSetValueDelegate<T>();
                s_setValueDelegates.Add(typeof(T), cachedDelegate);
            }

            if (cachedDelegate is not SetValueDelegate<T> setValueDelegate)
            {
                return false;
            }

            setValueDelegate(blackboard, boxedPropertyName, value);
            return true;
        }

        /// <summary>
        /// Tries to read an untyped value from a resolved blackboard instance.
        /// </summary>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <param name="value">The resolved value if found.</param>
        /// <returns>True if the property exists.</returns>
        public static bool TryGetObjectValue(
            object blackboard,
            string propertyName,
            out object value)
        {
            value = null;

            if (!TryGetPropertyName(propertyName, out object boxedPropertyName)
                || blackboard == null)
            {
                return false;
            }

            EnsureMethods();

            if (s_tryGetObjectValueDelegate == null)
            {
                return false;
            }

            return s_tryGetObjectValueDelegate(
                blackboard,
                boxedPropertyName,
                out value);
        }

        /// <summary>
        /// Gets whether a resolved blackboard contains a property.
        /// </summary>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <param name="propertyName">The blackboard property name.</param>
        /// <returns>True if the property exists.</returns>
        public static bool ContainsValue(object blackboard, string propertyName)
        {
            if (!TryGetPropertyName(propertyName, out object boxedPropertyName)
                || blackboard == null)
            {
                return false;
            }

            EnsureMethods();

            return s_containsValueDelegate != null
                && s_containsValueDelegate(blackboard, boxedPropertyName);
        }

        /// <summary>
        /// Recreates the runtime blackboard owned by a container component.
        /// </summary>
        /// <param name="container">The candidate Simple Blackboard container.</param>
        /// <returns>True when the container recreated its runtime blackboard.</returns>
        public static bool RecreateBlackboard(Component container)
        {
            if (!IsAvailable
                || container == null
                || !ContainerType.IsInstanceOfType(container))
            {
                return false;
            }

            s_recreateBlackboardMethod ??=
                ContainerType.GetMethod(
                    "RecreateBlackboard",
                    BindingFlags.Instance | BindingFlags.Public);

            if (s_recreateBlackboardMethod == null)
            {
                return false;
            }

            s_recreateBlackboardMethod.Invoke(container, Array.Empty<object>());
            return true;
        }

        /// <summary>
        /// Tries to enumerate the available blackboard property names and their
        /// value types.
        /// </summary>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <param name="propertyMetadata">
        /// Receives the discovered property metadata.
        /// </param>
        /// <returns>
        /// True when the blackboard metadata APIs were resolved and invoked.
        /// </returns>
        public static bool TryGetPropertyMetadata(
            object blackboard,
            Dictionary<string, Type> propertyMetadata)
        {
            propertyMetadata?.Clear();

            if (!IsAvailable
                || blackboard == null
                || propertyMetadata == null
                || !BlackboardType.IsInstanceOfType(blackboard))
            {
                return false;
            }

            EnsureMetadataMethods();

            if (s_getPropertyNamesMethod == null
                || s_getValueTypeMethod == null
                || s_propertyNameNameProperty == null)
            {
                return false;
            }

            IList propertyNames = CreatePropertyNameListInstance();

            if (propertyNames == null)
            {
                return false;
            }

            s_getPropertyNamesMethod.Invoke(blackboard, new object[] { propertyNames });

            foreach (object propertyName in propertyNames)
            {
                string name = s_propertyNameNameProperty.GetValue(propertyName) as string;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                Type valueType =
                    s_getValueTypeMethod.Invoke(blackboard, new[] { propertyName }) as Type;

                if (valueType != null)
                {
                    propertyMetadata[name] = valueType;
                }
            }

            return true;
        }

        #endregion

        #region Private Properties

        /// <summary>
        /// Gets the resolved Simple Blackboard blackboard type.
        /// </summary>
        private static Type BlackboardType =>
            s_blackboardType ??= ResolveRuntimeType(
                BlackboardTypeFullName,
                BlackboardTypeAssemblyQualifiedName);

        /// <summary>
        /// Gets the resolved Simple Blackboard property-name type.
        /// </summary>
        private static Type PropertyNameType =>
            s_propertyNameType ??= ResolveRuntimeType(
                PropertyNameTypeFullName,
                PropertyNameTypeAssemblyQualifiedName);

        /// <summary>
        /// Gets the constructor used to box one blackboard property name.
        /// </summary>
        private static ConstructorInfo PropertyNameConstructor =>
            s_propertyNameConstructor ??=
                PropertyNameType?.GetConstructor(new[] { typeof(string) });

        /// <summary>
        /// Gets the Simple Blackboard container property that exposes the
        /// runtime blackboard instance.
        /// </summary>
        private static PropertyInfo BlackboardProperty =>
            s_blackboardProperty ??=
                ContainerType?.GetProperty(
                    "blackboard",
                    BindingFlags.Instance | BindingFlags.Public);

        #endregion

        #region Internal Helpers

        /// <summary>
        /// Resolves a runtime type by assembly-qualified name first and then by
        /// scanning loaded assemblies.
        /// </summary>
        /// <param name="fullTypeName">The runtime full type name.</param>
        /// <param name="assemblyQualifiedTypeName">
        /// The assembly-qualified runtime type name.
        /// </param>
        /// <returns>The resolved type, or null when it cannot be found.</returns>
        private static Type ResolveRuntimeType(
            string fullTypeName,
            string assemblyQualifiedTypeName)
        {
            if (!string.IsNullOrWhiteSpace(assemblyQualifiedTypeName))
            {
                Type qualifiedType = Type.GetType(assemblyQualifiedTypeName);
                if (qualifiedType != null)
                {
                    return qualifiedType;
                }
            }

            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            Type directType = Type.GetType(fullTypeName);
            if (directType != null)
            {
                return directType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type resolvedType = assemblies[index].GetType(fullTypeName);
                if (resolvedType != null)
                {
                    return resolvedType;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves or creates the boxed Simple Blackboard property-name value.
        /// </summary>
        /// <param name="propertyName">The authored property name.</param>
        /// <param name="boxedPropertyName">The boxed runtime property name.</param>
        /// <returns>True when the property name could be resolved.</returns>
        private static bool TryGetPropertyName(
            string propertyName,
            out object boxedPropertyName)
        {
            boxedPropertyName = null;

            if (!IsAvailable || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            if (s_propertyNames.TryGetValue(propertyName, out boxedPropertyName))
            {
                return true;
            }

            boxedPropertyName = PropertyNameConstructor.Invoke(new object[] { propertyName });
            s_propertyNames.Add(propertyName, boxedPropertyName);
            return true;
        }

        /// <summary>
        /// Resolves and caches the generic and non-generic Simple Blackboard
        /// value-access methods.
        /// </summary>
        private static void EnsureMethods()
        {
            if (s_tryGetStructValueMethodDefinition != null)
            {
                return;
            }

            MethodInfo[] methods = BlackboardType?.GetMethods(
                BindingFlags.Instance | BindingFlags.Public);

            if (methods == null)
            {
                return;
            }

            foreach (MethodInfo method in methods)
            {
                if (method.Name == "TryGetStructValue"
                    && method.IsGenericMethodDefinition)
                {
                    s_tryGetStructValueMethodDefinition = method;
                }

                if (method.Name == "TryGetClassValue"
                    && method.IsGenericMethodDefinition)
                {
                    s_tryGetClassValueMethodDefinition = method;
                }

                if (method.Name == "SetStructValue"
                    && method.IsGenericMethodDefinition)
                {
                    s_setStructValueMethodDefinition = method;
                }

                if (method.Name == "SetClassValue"
                    && method.IsGenericMethodDefinition)
                {
                    s_setClassValueMethodDefinition = method;
                }

                if (method.Name == "TryGetObjectValue"
                    && !method.IsGenericMethod
                    && method.GetParameters().Length == 2)
                {
                    s_tryGetObjectValueMethod = method;
                }

                if (method.Name == "ContainsObjectValue"
                    && !method.IsGenericMethod
                    && method.GetParameters().Length == 1)
                {
                    s_containsValueMethod = method;
                }
            }

            if (s_tryGetObjectValueDelegate == null
                && s_tryGetObjectValueMethod != null)
            {
                s_tryGetObjectValueDelegate =
                    CreateTryGetObjectValueDelegate(s_tryGetObjectValueMethod);
            }

            if (s_containsValueDelegate == null && s_containsValueMethod != null)
            {
                s_containsValueDelegate =
                    CreateContainsValueDelegate(s_containsValueMethod);
            }
        }

        /// <summary>
        /// Resolves the metadata-related Simple Blackboard methods.
        /// </summary>
        private static void EnsureMetadataMethods()
        {
            if (s_getPropertyNamesMethod != null
                && s_getValueTypeMethod != null
                && s_propertyNameNameProperty != null)
            {
                return;
            }

            s_getPropertyNamesMethod ??=
                BlackboardType?.GetMethod(
                    "GetPropertyNames",
                    BindingFlags.Instance | BindingFlags.Public);

            s_getValueTypeMethod ??=
                BlackboardType?.GetMethod(
                    "GetValueType",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { PropertyNameType },
                    null);

            s_propertyNameNameProperty ??=
                PropertyNameType?.GetProperty(
                    "name",
                    BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Creates the runtime list instance required by the metadata API.
        /// </summary>
        /// <returns>
        /// A list instance compatible with the runtime property-name type.
        /// </returns>
        private static IList CreatePropertyNameListInstance()
        {
            Type propertyNameType = PropertyNameType;

            if (propertyNameType == null)
            {
                return null;
            }

            s_propertyNameListType ??= typeof(List<>).MakeGenericType(propertyNameType);
            return Activator.CreateInstance(s_propertyNameListType) as IList;
        }

        /// <summary>
        /// Creates the cached typed getter delegate for one value type.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <returns>The runtime getter delegate.</returns>
        private static Delegate CreateTryGetValueDelegate<T>()
        {
            EnsureMethods();

            MethodInfo methodDefinition = typeof(T).IsValueType
                ? s_tryGetStructValueMethodDefinition
                : s_tryGetClassValueMethodDefinition;

            if (methodDefinition == null)
            {
                return new TryGetValueDelegate<T>(FallbackTryGetValue);
            }

            MethodInfo closedMethod = methodDefinition.MakeGenericMethod(typeof(T));
            return CreateTryGetDelegate<T>(closedMethod);
        }

        /// <summary>
        /// Creates the cached typed setter delegate for one value type.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <returns>The runtime setter delegate.</returns>
        private static Delegate CreateSetValueDelegate<T>()
        {
            EnsureMethods();

            MethodInfo methodDefinition = typeof(T).IsValueType
                ? s_setStructValueMethodDefinition
                : s_setClassValueMethodDefinition;

            if (methodDefinition == null)
            {
                return new SetValueDelegate<T>(FallbackSetValue);
            }

            MethodInfo closedMethod = methodDefinition.MakeGenericMethod(typeof(T));
            return CreateSetDelegate<T>(closedMethod);
        }

        /// <summary>
        /// Creates the untyped object-value getter delegate.
        /// </summary>
        /// <param name="methodInfo">The runtime method to wrap.</param>
        /// <returns>The compiled delegate.</returns>
        private static TryGetObjectValueDelegate CreateTryGetObjectValueDelegate(
            MethodInfo methodInfo)
        {
            ParameterExpression blackboardParameter =
                Expression.Parameter(typeof(object), "blackboard");

            ParameterExpression propertyNameParameter =
                Expression.Parameter(typeof(object), "propertyName");

            ParameterExpression valueParameter =
                Expression.Parameter(typeof(object).MakeByRefType(), "value");

            MethodCallExpression body = Expression.Call(
                Expression.Convert(blackboardParameter, BlackboardType),
                methodInfo,
                Expression.Unbox(propertyNameParameter, PropertyNameType),
                valueParameter);

            return Expression.Lambda<TryGetObjectValueDelegate>(
                body,
                blackboardParameter,
                propertyNameParameter,
                valueParameter)
                .Compile();
        }

        /// <summary>
        /// Creates the property-existence delegate.
        /// </summary>
        /// <param name="methodInfo">The runtime method to wrap.</param>
        /// <returns>The compiled delegate.</returns>
        private static ContainsValueDelegate CreateContainsValueDelegate(
            MethodInfo methodInfo)
        {
            ParameterExpression blackboardParameter =
                Expression.Parameter(typeof(object), "blackboard");

            ParameterExpression propertyNameParameter =
                Expression.Parameter(typeof(object), "propertyName");

            MethodCallExpression body = Expression.Call(
                Expression.Convert(blackboardParameter, BlackboardType),
                methodInfo,
                Expression.Unbox(propertyNameParameter, PropertyNameType));

            return Expression.Lambda<ContainsValueDelegate>(
                body,
                blackboardParameter,
                propertyNameParameter)
                .Compile();
        }

        /// <summary>
        /// Provides a safe fallback when no typed getter could be resolved.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <param name="propertyName">The boxed property name.</param>
        /// <param name="value">The fallback value.</param>
        /// <returns>Always false.</returns>
        private static bool FallbackTryGetValue<T>(
            object blackboard,
            object propertyName,
            out T value)
        {
            value = default;
            return false;
        }

        /// <summary>
        /// Provides a safe fallback when no typed setter could be resolved.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <param name="blackboard">The resolved blackboard instance.</param>
        /// <param name="propertyName">The boxed property name.</param>
        /// <param name="value">The ignored value.</param>
        private static void FallbackSetValue<T>(
            object blackboard,
            object propertyName,
            T value)
        {
        }

        /// <summary>
        /// Creates the typed runtime getter delegate.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <param name="methodInfo">The runtime method to wrap.</param>
        /// <returns>The compiled delegate.</returns>
        private static TryGetValueDelegate<T> CreateTryGetDelegate<T>(
            MethodInfo methodInfo)
        {
            ParameterExpression blackboardParameter =
                Expression.Parameter(typeof(object), "blackboard");

            ParameterExpression propertyNameParameter =
                Expression.Parameter(typeof(object), "propertyName");

            ParameterExpression valueParameter =
                Expression.Parameter(typeof(T).MakeByRefType(), "value");

            MethodCallExpression body = Expression.Call(
                Expression.Convert(blackboardParameter, BlackboardType),
                methodInfo,
                Expression.Unbox(propertyNameParameter, PropertyNameType),
                valueParameter);

            return Expression.Lambda<TryGetValueDelegate<T>>(
                body,
                blackboardParameter,
                propertyNameParameter,
                valueParameter)
                .Compile();
        }

        /// <summary>
        /// Creates the typed runtime setter delegate.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <param name="methodInfo">The runtime method to wrap.</param>
        /// <returns>The compiled delegate.</returns>
        private static SetValueDelegate<T> CreateSetDelegate<T>(MethodInfo methodInfo)
        {
            ParameterExpression blackboardParameter =
                Expression.Parameter(typeof(object), "blackboard");

            ParameterExpression propertyNameParameter =
                Expression.Parameter(typeof(object), "propertyName");

            ParameterExpression valueParameter =
                Expression.Parameter(typeof(T), "value");

            MethodCallExpression body = Expression.Call(
                Expression.Convert(blackboardParameter, BlackboardType),
                methodInfo,
                Expression.Unbox(propertyNameParameter, PropertyNameType),
                valueParameter);

            return Expression.Lambda<SetValueDelegate<T>>(
                body,
                blackboardParameter,
                propertyNameParameter,
                valueParameter)
                .Compile();
        }

        #endregion

        #region Delegates

        /// <summary>
        /// Represents one typed blackboard getter delegate.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        private delegate bool TryGetValueDelegate<T>(
            object blackboard,
            object propertyName,
            out T value);

        /// <summary>
        /// Represents one untyped object-value getter delegate.
        /// </summary>
        private delegate bool TryGetObjectValueDelegate(
            object blackboard,
            object propertyName,
            out object value);

        /// <summary>
        /// Represents one blackboard property-existence delegate.
        /// </summary>
        private delegate bool ContainsValueDelegate(
            object blackboard,
            object propertyName);

        /// <summary>
        /// Represents one typed blackboard setter delegate.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        private delegate void SetValueDelegate<T>(
            object blackboard,
            object propertyName,
            T value);

        #endregion
    }
}