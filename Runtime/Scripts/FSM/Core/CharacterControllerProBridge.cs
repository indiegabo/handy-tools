using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Resolves Character Controller Pro types, callbacks, and members without
    /// introducing a hard package dependency into the core runtime assembly.
    /// </summary>
    internal static class CharacterControllerProBridge
    {
        #region Constants

        private const string CharacterActorTypeFullName =
            "Lightbug.CharacterControllerPro.Core.CharacterActor";

        private const string CharacterActorTypeAssemblyQualifiedName =
            "Lightbug.CharacterControllerPro.Core.CharacterActor, com.lightbug.character-controller-pro";

        #endregion

        #region Fields

        private static EventInfo s_onAnimatorIkEvent;
        private static EventInfo s_onPostSimulationEvent;
        private static EventInfo s_onPreSimulationEvent;

        private static Func<object, Vector3> s_getForward;
        private static Func<object, bool> s_getIs2D;
        private static Func<object, Vector3> s_getRight;
        private static Func<object, Vector3> s_getUp;
        private static Func<object, bool> s_getUpdateRootPosition;
        private static Func<object, bool> s_getUpdateRootRotation;
        private static Func<object, bool> s_getUseRootMotion;

        private static Action<object> s_resetIkWeights;
        private static Action<object, bool> s_setUpdateRootPosition;
        private static Action<object, bool> s_setUpdateRootRotation;
        private static Action<object, bool> s_setUseRootMotion;

        private static Type s_characterActorType;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the Character Controller Pro runtime type was resolved
        /// successfully.
        /// </summary>
        public static bool IsAvailable => CharacterActorType != null;

        /// <summary>
        /// Gets the resolved Character Controller Pro actor type.
        /// </summary>
        public static Type CharacterActorType =>
            s_characterActorType ??= ResolveRuntimeType(
                CharacterActorTypeFullName,
                CharacterActorTypeAssemblyQualifiedName);

        #endregion

        #region Public API

        /// <summary>
        /// Gets whether the provided component is a runtime CharacterActor.
        /// </summary>
        /// <param name="component">The candidate component.</param>
        /// <returns>True when the component is a CharacterActor instance.</returns>
        public static bool IsCharacterActor(Component component)
        {
            return component != null
                && CharacterActorType != null
                && CharacterActorType.IsInstanceOfType(component);
        }

        /// <summary>
        /// Subscribes a callback to the CharacterActor animator IK event.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="callback">The callback to subscribe.</param>
        public static void SubscribeAnimatorIk(Component actor, Action<int> callback)
        {
            EnsureEvents();
            s_onAnimatorIkEvent?.AddEventHandler(actor, callback);
        }

        /// <summary>
        /// Subscribes a callback to the CharacterActor post-simulation event.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="callback">The callback to subscribe.</param>
        public static void SubscribePostSimulation(
            Component actor,
            Action<float> callback)
        {
            EnsureEvents();
            s_onPostSimulationEvent?.AddEventHandler(actor, callback);
        }

        /// <summary>
        /// Subscribes a callback to the CharacterActor pre-simulation event.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="callback">The callback to subscribe.</param>
        public static void SubscribePreSimulation(
            Component actor,
            Action<float> callback)
        {
            EnsureEvents();
            s_onPreSimulationEvent?.AddEventHandler(actor, callback);
        }

        /// <summary>
        /// Tries to read the actor forward vector.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The resolved forward vector.</param>
        /// <returns>True when the value could be read.</returns>
        public static bool TryGetForward(Component actor, out Vector3 value)
        {
            EnsureMembers();
            return TryGetValue(actor, s_getForward, out value);
        }

        /// <summary>
        /// Tries to read whether the actor is configured as 2D.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The resolved 2D flag.</param>
        /// <returns>True when the value could be read.</returns>
        public static bool TryGetIs2D(Component actor, out bool value)
        {
            EnsureMembers();
            return TryGetValue(actor, s_getIs2D, out value);
        }

        /// <summary>
        /// Tries to read the actor right vector.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The resolved right vector.</param>
        /// <returns>True when the value could be read.</returns>
        public static bool TryGetRight(Component actor, out Vector3 value)
        {
            EnsureMembers();
            return TryGetValue(actor, s_getRight, out value);
        }

        /// <summary>
        /// Tries to read the actor up vector.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The resolved up vector.</param>
        /// <returns>True when the value could be read.</returns>
        public static bool TryGetUp(Component actor, out Vector3 value)
        {
            EnsureMembers();
            return TryGetValue(actor, s_getUp, out value);
        }

        /// <summary>
        /// Tries to read whether root-position updates are enabled.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The resolved flag.</param>
        /// <returns>True when the value could be read.</returns>
        public static bool TryGetUpdateRootPosition(Component actor, out bool value)
        {
            EnsureMembers();
            return TryGetValue(actor, s_getUpdateRootPosition, out value);
        }

        /// <summary>
        /// Tries to read whether root-rotation updates are enabled.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The resolved flag.</param>
        /// <returns>True when the value could be read.</returns>
        public static bool TryGetUpdateRootRotation(Component actor, out bool value)
        {
            EnsureMembers();
            return TryGetValue(actor, s_getUpdateRootRotation, out value);
        }

        /// <summary>
        /// Tries to read whether root motion is enabled.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The resolved flag.</param>
        /// <returns>True when the value could be read.</returns>
        public static bool TryGetUseRootMotion(Component actor, out bool value)
        {
            EnsureMembers();
            return TryGetValue(actor, s_getUseRootMotion, out value);
        }

        /// <summary>
        /// Tries to reset all IK weights on the actor.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <returns>True when the reset method was resolved and invoked.</returns>
        public static bool TryResetIKWeights(Component actor)
        {
            EnsureMembers();

            if (actor == null || s_resetIkWeights == null)
            {
                return false;
            }

            s_resetIkWeights(actor);
            return true;
        }

        /// <summary>
        /// Tries to set whether root-position updates are enabled.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>True when the setter was resolved and invoked.</returns>
        public static bool TrySetUpdateRootPosition(Component actor, bool value)
        {
            EnsureMembers();
            return TrySetValue(actor, s_setUpdateRootPosition, value);
        }

        /// <summary>
        /// Tries to set whether root-rotation updates are enabled.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>True when the setter was resolved and invoked.</returns>
        public static bool TrySetUpdateRootRotation(Component actor, bool value)
        {
            EnsureMembers();
            return TrySetValue(actor, s_setUpdateRootRotation, value);
        }

        /// <summary>
        /// Tries to set whether root motion is enabled.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>True when the setter was resolved and invoked.</returns>
        public static bool TrySetUseRootMotion(Component actor, bool value)
        {
            EnsureMembers();
            return TrySetValue(actor, s_setUseRootMotion, value);
        }

        /// <summary>
        /// Unsubscribes a callback from the CharacterActor animator IK event.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="callback">The callback to unsubscribe.</param>
        public static void UnsubscribeAnimatorIk(Component actor, Action<int> callback)
        {
            EnsureEvents();
            s_onAnimatorIkEvent?.RemoveEventHandler(actor, callback);
        }

        /// <summary>
        /// Unsubscribes a callback from the CharacterActor post-simulation event.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="callback">The callback to unsubscribe.</param>
        public static void UnsubscribePostSimulation(
            Component actor,
            Action<float> callback)
        {
            EnsureEvents();
            s_onPostSimulationEvent?.RemoveEventHandler(actor, callback);
        }

        /// <summary>
        /// Unsubscribes a callback from the CharacterActor pre-simulation event.
        /// </summary>
        /// <param name="actor">The candidate actor component.</param>
        /// <param name="callback">The callback to unsubscribe.</param>
        public static void UnsubscribePreSimulation(
            Component actor,
            Action<float> callback)
        {
            EnsureEvents();
            s_onPreSimulationEvent?.RemoveEventHandler(actor, callback);
        }

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
        /// Creates a compiled getter delegate for a nested property or field
        /// path rooted on the actor type.
        /// </summary>
        /// <typeparam name="T">The value type returned by the getter.</typeparam>
        /// <param name="rootType">The actor root type.</param>
        /// <param name="memberPath">The property or field path to traverse.</param>
        /// <returns>The compiled getter delegate, or null when unresolved.</returns>
        private static Func<object, T> CreateGetter<T>(
            Type rootType,
            params string[] memberPath)
        {
            if (rootType == null)
            {
                return null;
            }

            ParameterExpression rootParameter =
                Expression.Parameter(typeof(object), "root");

            Expression currentExpression =
                Expression.Convert(rootParameter, rootType);

            Type currentType = rootType;

            for (int index = 0; index < memberPath.Length; index++)
            {
                string memberName = memberPath[index];

                PropertyInfo property = currentType.GetProperty(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (property != null)
                {
                    currentExpression = Expression.Property(currentExpression, property);
                    currentType = property.PropertyType;
                    continue;
                }

                FieldInfo field = currentType.GetField(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field == null)
                {
                    return null;
                }

                currentExpression = Expression.Field(currentExpression, field);
                currentType = field.FieldType;
            }

            Expression body = currentType == typeof(T)
                ? currentExpression
                : Expression.Convert(currentExpression, typeof(T));

            return Expression.Lambda<Func<object, T>>(body, rootParameter).Compile();
        }

        /// <summary>
        /// Creates a compiled property setter delegate rooted on the actor type.
        /// </summary>
        /// <typeparam name="T">The value type assigned by the setter.</typeparam>
        /// <param name="rootType">The actor root type.</param>
        /// <param name="propertyName">The writable property name.</param>
        /// <returns>The compiled setter delegate, or null when unresolved.</returns>
        private static Action<object, T> CreateSetter<T>(
            Type rootType,
            string propertyName)
        {
            if (rootType == null)
            {
                return null;
            }

            PropertyInfo property = rootType.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property == null || !property.CanWrite)
            {
                return null;
            }

            ParameterExpression rootParameter =
                Expression.Parameter(typeof(object), "root");

            ParameterExpression valueParameter =
                Expression.Parameter(typeof(T), "value");

            BinaryExpression assignExpression = Expression.Assign(
                Expression.Property(Expression.Convert(rootParameter, rootType), property),
                valueParameter);

            return Expression.Lambda<Action<object, T>>(
                assignExpression,
                rootParameter,
                valueParameter)
                .Compile();
        }

        /// <summary>
        /// Creates a compiled parameterless method delegate rooted on the actor
        /// type.
        /// </summary>
        /// <param name="rootType">The actor root type.</param>
        /// <param name="methodName">The method name.</param>
        /// <returns>The compiled delegate, or null when unresolved.</returns>
        private static Action<object> CreateVoidMethod(Type rootType, string methodName)
        {
            if (rootType == null)
            {
                return null;
            }

            MethodInfo method = rootType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (method == null)
            {
                return null;
            }

            ParameterExpression rootParameter =
                Expression.Parameter(typeof(object), "root");

            MethodCallExpression callExpression = Expression.Call(
                Expression.Convert(rootParameter, rootType),
                method);

            return Expression.Lambda<Action<object>>(callExpression, rootParameter)
                .Compile();
        }

        /// <summary>
        /// Resolves and caches the CharacterActor events used by the brain.
        /// </summary>
        private static void EnsureEvents()
        {
            if (s_onPreSimulationEvent != null)
            {
                return;
            }

            s_onPreSimulationEvent = CharacterActorType?.GetEvent("OnPreSimulation");
            s_onPostSimulationEvent = CharacterActorType?.GetEvent("OnPostSimulation");
            s_onAnimatorIkEvent = CharacterActorType?.GetEvent("OnAnimatorIKEvent");
        }

        /// <summary>
        /// Resolves and caches the CharacterActor members used by the brain.
        /// </summary>
        private static void EnsureMembers()
        {
            if (s_getForward != null)
            {
                return;
            }

            s_getForward = CreateGetter<Vector3>(CharacterActorType, "Forward");
            s_getRight = CreateGetter<Vector3>(CharacterActorType, "Right");
            s_getUp = CreateGetter<Vector3>(CharacterActorType, "Up");
            s_getIs2D = CreateGetter<bool>(CharacterActorType, "Is2D");
            s_getUseRootMotion = CreateGetter<bool>(CharacterActorType, "UseRootMotion");
            s_setUseRootMotion = CreateSetter<bool>(CharacterActorType, "UseRootMotion");
            s_getUpdateRootPosition = CreateGetter<bool>(
                CharacterActorType,
                "UpdateRootPosition");
            s_setUpdateRootPosition = CreateSetter<bool>(
                CharacterActorType,
                "UpdateRootPosition");
            s_getUpdateRootRotation = CreateGetter<bool>(
                CharacterActorType,
                "UpdateRootRotation");
            s_setUpdateRootRotation = CreateSetter<bool>(
                CharacterActorType,
                "UpdateRootRotation");
            s_resetIkWeights = CreateVoidMethod(CharacterActorType, "ResetIKWeights");
        }

        /// <summary>
        /// Tries to resolve one getter-backed value from a candidate actor.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <param name="component">The candidate actor component.</param>
        /// <param name="getter">The cached getter delegate.</param>
        /// <param name="value">The resolved value when successful.</param>
        /// <returns>True when the actor and getter were valid.</returns>
        private static bool TryGetValue<T>(
            Component component,
            Func<object, T> getter,
            out T value)
        {
            value = default;

            if (component == null || getter == null)
            {
                return false;
            }

            value = getter(component);
            return true;
        }

        /// <summary>
        /// Tries to assign one setter-backed value on a candidate actor.
        /// </summary>
        /// <typeparam name="T">The assigned value type.</typeparam>
        /// <param name="component">The candidate actor component.</param>
        /// <param name="setter">The cached setter delegate.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>True when the actor and setter were valid.</returns>
        private static bool TrySetValue<T>(
            Component component,
            Action<object, T> setter,
            T value)
        {
            if (component == null || setter == null)
            {
                return false;
            }

            setter(component, value);
            return true;
        }

        #endregion
    }
}