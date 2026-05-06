using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Discovers attributed animation bus event types and provides runtime and
    /// editor lookups for selection and dispatch.
    /// </summary>
    public static class AnimatorBusEventRegistry
    {
        #region State

        private static readonly Dictionary<string, AnimatorBusEventMetadata>
            _metadataByPath = new(StringComparer.Ordinal);

        private static readonly Dictionary<Type, AnimatorBusEventMetadata>
            _metadataByType = new();

        private static readonly Dictionary<Type, Action<IAnimatorBusEvent>>
            _dispatchersByType = new();

        private static readonly List<AnimatorBusEventMetadata> _orderedMetadata =
            new();

        private static bool _initialized;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Clears cached discovery state before a new runtime session starts.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _metadataByPath.Clear();
            _metadataByType.Clear();
            _dispatchersByType.Clear();
            _orderedMetadata.Clear();
            _initialized = false;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Rebuilds the event registry immediately.
        /// </summary>
        public static void Refresh()
        {
            ResetState();
            EnsureInitialized();
        }

        /// <summary>
        /// Gets all discovered event metadata ordered by logical path.
        /// </summary>
        /// <returns>Ordered discovered event metadata.</returns>
        public static IReadOnlyList<AnimatorBusEventMetadata> GetEventMetadata()
        {
            EnsureInitialized();
            return _orderedMetadata;
        }

        /// <summary>
        /// Tries to resolve event metadata from a logical path.
        /// </summary>
        /// <param name="path">Stable logical event path.</param>
        /// <param name="metadata">Resolved event metadata.</param>
        /// <returns>True when the path was resolved.</returns>
        public static bool TryGetMetadata(
            string path,
            out AnimatorBusEventMetadata metadata
        )
        {
            EnsureInitialized();
            return _metadataByPath.TryGetValue(path ?? string.Empty, out metadata);
        }

        /// <summary>
        /// Tries to resolve event metadata from a concrete event type.
        /// </summary>
        /// <param name="eventType">Concrete event type.</param>
        /// <param name="metadata">Resolved event metadata.</param>
        /// <returns>True when the type was resolved.</returns>
        public static bool TryGetMetadata(
            Type eventType,
            out AnimatorBusEventMetadata metadata
        )
        {
            metadata = null;
            EnsureInitialized();
            return eventType != null
                && _metadataByType.TryGetValue(eventType, out metadata);
        }

        /// <summary>
        /// Tries to resolve event metadata from a serialized reference.
        /// </summary>
        /// <param name="reference">Serialized event reference.</param>
        /// <param name="metadata">Resolved event metadata.</param>
        /// <returns>True when the reference was resolved.</returns>
        public static bool TryGetMetadata(
            AnimatorBusEventReference reference,
            out AnimatorBusEventMetadata metadata
        )
        {
            EnsureInitialized();

            metadata = null;
            if (reference == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(reference.EventTypeName))
            {
                Type resolvedType = Type.GetType(reference.EventTypeName, false);
                if (resolvedType != null
                    && _metadataByType.TryGetValue(resolvedType, out metadata))
                {
                    return true;
                }
            }

            return !string.IsNullOrWhiteSpace(reference.EventPath)
                && _metadataByPath.TryGetValue(reference.EventPath, out metadata);
        }

        /// <summary>
        /// Creates a new authored event instance from a serialized reference.
        /// </summary>
        /// <param name="reference">Serialized event reference.</param>
        /// <param name="eventInstance">Created event instance.</param>
        /// <returns>True when an event instance could be created.</returns>
        public static bool TryCreateEventInstance(
            AnimatorBusEventReference reference,
            out AnimatorBusEventBase eventInstance
        )
        {
            eventInstance = null;

            if (!TryGetMetadata(reference, out AnimatorBusEventMetadata metadata))
            {
                return false;
            }

            eventInstance = CreateEventInstance(metadata.EventType);
            return eventInstance != null;
        }

        /// <summary>
        /// Raises one animation bus event through the strongly typed HandyBus.
        /// </summary>
        /// <param name="animationEvent">Event instance to dispatch.</param>
        /// <returns>True when the dispatch succeeded.</returns>
        public static bool TryDispatch(IAnimatorBusEvent animationEvent)
        {
            if (animationEvent == null)
            {
                return false;
            }

            EnsureInitialized();

            Type eventType = animationEvent.GetType();
            if (!_dispatchersByType.TryGetValue(
                    eventType,
                    out Action<IAnimatorBusEvent> dispatcher
                ))
            {
                dispatcher = CreateDispatcher(eventType);
                if (dispatcher == null)
                {
                    return false;
                }

                _dispatchersByType.Add(eventType, dispatcher);
            }

            dispatcher(animationEvent);
            return true;
        }

        #endregion

        #region Discovery

        /// <summary>
        /// Ensures discovery ran before the registry is queried.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                RegisterAssembly(assemblies[index]);
            }

            _orderedMetadata.Sort(
                static (left, right) => string.CompareOrdinal(
                    left.Path,
                    right.Path
                )
            );
        }

        /// <summary>
        /// Tries to register every valid animation bus event type in one
        /// assembly.
        /// </summary>
        /// <param name="assembly">Assembly to inspect.</param>
        private static void RegisterAssembly(Assembly assembly)
        {
            Type[] loadableTypes = GetLoadableTypes(assembly);
            for (int index = 0; index < loadableTypes.Length; index++)
            {
                if (!TryCreateMetadata(
                        loadableTypes[index],
                        out AnimatorBusEventMetadata metadata
                    ))
                {
                    continue;
                }

                if (_metadataByPath.ContainsKey(metadata.Path))
                {
                    Debug.LogError(
                        $"[{nameof(AnimatorBusEventRegistry)}] Duplicate "
                        + $"AnimatorBusEvent path '{metadata.Path}' was "
                        + "ignored."
                    );
                    continue;
                }

                _metadataByPath.Add(metadata.Path, metadata);
                _metadataByType.Add(metadata.EventType, metadata);
                _orderedMetadata.Add(metadata);
            }
        }

        /// <summary>
        /// Tries to create metadata for one candidate event type.
        /// </summary>
        /// <param name="type">Candidate type.</param>
        /// <param name="metadata">Created metadata when successful.</param>
        /// <returns>True when the type is a valid animation bus event.</returns>
        private static bool TryCreateMetadata(
            Type type,
            out AnimatorBusEventMetadata metadata
        )
        {
            metadata = null;

            if (type == null
                || !type.IsClass
                || type.IsAbstract
                || !typeof(AnimatorBusEventBase).IsAssignableFrom(type))
            {
                return false;
            }

            AnimatorBusEventAttribute attribute =
                type.GetCustomAttribute<AnimatorBusEventAttribute>();

            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Path))
            {
                return false;
            }

            if (GetParameterlessConstructor(type) == null)
            {
                Debug.LogWarning(
                    $"[{nameof(AnimatorBusEventRegistry)}] {type.FullName} "
                    + "was skipped because it does not expose a "
                    + "parameterless constructor."
                );
                return false;
            }

            string displayName = string.IsNullOrWhiteSpace(attribute.DisplayName)
                ? type.Name
                : attribute.DisplayName;

            metadata = new AnimatorBusEventMetadata(
                type,
                attribute.Path,
                displayName,
                attribute.Description
            );

            return true;
        }

        /// <summary>
        /// Gets a parameterless constructor from a candidate event type.
        /// </summary>
        /// <param name="type">Candidate type.</param>
        /// <returns>Parameterless constructor when available.</returns>
        private static ConstructorInfo GetParameterlessConstructor(Type type)
        {
            return type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                Type.EmptyTypes,
                modifiers: null
            );
        }

        /// <summary>
        /// Gets every loadable type from one assembly, even when the assembly
        /// partially fails to load.
        /// </summary>
        /// <param name="assembly">Assembly to inspect.</param>
        /// <returns>Every loadable type from the assembly.</returns>
        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }

        #endregion

        #region Dispatch

        /// <summary>
        /// Creates one event instance for the provided concrete type.
        /// </summary>
        /// <param name="eventType">Concrete event type.</param>
        /// <returns>Created event instance when successful.</returns>
        private static AnimatorBusEventBase CreateEventInstance(Type eventType)
        {
            try
            {
                return Activator.CreateInstance(eventType, nonPublic: true)
                    as AnimatorBusEventBase;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        /// <summary>
        /// Creates one strongly typed HandyBus dispatcher for a concrete event
        /// type.
        /// </summary>
        /// <param name="eventType">Concrete event type.</param>
        /// <returns>Dispatcher delegate when successful.</returns>
        private static Action<IAnimatorBusEvent> CreateDispatcher(Type eventType)
        {
            MethodInfo method = typeof(AnimatorBusEventRegistry).GetMethod(
                nameof(DispatchTyped),
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (method == null)
            {
                return null;
            }

            MethodInfo closedMethod = method.MakeGenericMethod(eventType);
            return (Action<IAnimatorBusEvent>)Delegate.CreateDelegate(
                typeof(Action<IAnimatorBusEvent>),
                closedMethod
            );
        }

        /// <summary>
        /// Raises one typed event through the matching HandyBus channel.
        /// </summary>
        /// <typeparam name="T">Concrete event type.</typeparam>
        /// <param name="animationEvent">Event instance to dispatch.</param>
        private static void DispatchTyped<T>(IAnimatorBusEvent animationEvent)
            where T : class, IAnimatorBusEvent
        {
            HandyBus<T>.Raise((T)animationEvent);
        }

        #endregion
    }
}