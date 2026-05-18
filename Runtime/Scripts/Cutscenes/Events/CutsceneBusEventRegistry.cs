using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    /// <summary>
    /// Discovers attributed cutscene bus event types and provides runtime and
    /// editor lookups for selection, dispatch, and subscriptions.
    /// </summary>
    public static class CutsceneBusEventRegistry
    {
        #region State

        private static readonly Dictionary<string, CutsceneBusEventMetadata>
            _metadataByPath = new(StringComparer.Ordinal);

        private static readonly Dictionary<Type, CutsceneBusEventMetadata>
            _metadataByType = new();

        private static readonly Dictionary<Type, Action<IEvent>>
            _dispatchersByType = new();

        private static readonly Dictionary<Type, Func<Action<IEvent>, IDisposable>>
            _subscriptionFactoriesByType = new();

        private static readonly List<CutsceneBusEventMetadata> _orderedMetadata =
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
            _subscriptionFactoriesByType.Clear();
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
        public static IReadOnlyList<CutsceneBusEventMetadata> GetEventMetadata()
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
            out CutsceneBusEventMetadata metadata)
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
            out CutsceneBusEventMetadata metadata)
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
            CutsceneBusEventReference reference,
            out CutsceneBusEventMetadata metadata)
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
        /// Copies one metadata selection into a serialized reference.
        /// </summary>
        /// <param name="reference">Target reference to update.</param>
        /// <param name="metadata">Selected metadata entry.</param>
        public static void ApplySelection(
            CutsceneBusEventReference reference,
            CutsceneBusEventMetadata metadata)
        {
            if (reference == null)
            {
                return;
            }

            reference.Assign(
                metadata?.Path ?? string.Empty,
                metadata?.EventType.AssemblyQualifiedName ?? string.Empty);
        }

        /// <summary>
        /// Creates a new authored event instance from a serialized reference.
        /// </summary>
        /// <param name="reference">Serialized event reference.</param>
        /// <param name="eventInstance">Created event instance.</param>
        /// <returns>True when an event instance could be created.</returns>
        public static bool TryCreateEventInstance(
            CutsceneBusEventReference reference,
            out IEvent eventInstance)
        {
            eventInstance = null;

            if (!TryGetMetadata(reference, out CutsceneBusEventMetadata metadata))
            {
                return false;
            }

            eventInstance = CreateEventInstance(metadata.EventType);
            return eventInstance != null;
        }

        /// <summary>
        /// Raises one cutscene bus event through the strongly typed HandyBus.
        /// </summary>
        /// <param name="cutsceneEvent">Event instance to dispatch.</param>
        /// <returns>True when the dispatch succeeded.</returns>
        public static bool TryDispatch(IEvent cutsceneEvent)
        {
            if (cutsceneEvent == null)
            {
                return false;
            }

            EnsureInitialized();

            Type eventType = cutsceneEvent.GetType();
            if (!_dispatchersByType.TryGetValue(
                    eventType,
                    out Action<IEvent> dispatcher))
            {
                dispatcher = CreateDispatcher(eventType);
                if (dispatcher == null)
                {
                    return false;
                }

                _dispatchersByType.Add(eventType, dispatcher);
            }

            dispatcher(cutsceneEvent);
            return true;
        }

        /// <summary>
        /// Creates one HandyBus subscription for the event referenced by the
        /// provided selector.
        /// </summary>
        /// <param name="reference">Serialized event reference.</param>
        /// <param name="onEvent">Callback invoked when the event fires.</param>
        /// <param name="subscription">Created subscription handle.</param>
        /// <returns>True when the subscription was created.</returns>
        public static bool TrySubscribe(
            CutsceneBusEventReference reference,
            Action<IEvent> onEvent,
            out IDisposable subscription)
        {
            subscription = null;

            if (onEvent == null
                || !TryGetMetadata(reference, out CutsceneBusEventMetadata metadata))
            {
                return false;
            }

            Type eventType = metadata.EventType;
            if (!_subscriptionFactoriesByType.TryGetValue(
                    eventType,
                    out Func<Action<IEvent>, IDisposable> factory))
            {
                factory = CreateSubscriptionFactory(eventType);
                if (factory == null)
                {
                    return false;
                }

                _subscriptionFactoriesByType.Add(eventType, factory);
            }

            subscription = factory(onEvent);
            return subscription != null;
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
                    right.Path));
        }

        /// <summary>
        /// Tries to register every valid cutscene bus event type in one
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
                        out CutsceneBusEventMetadata metadata))
                {
                    continue;
                }

                if (_metadataByPath.ContainsKey(metadata.Path))
                {
                    Debug.LogError(
                        $"[{nameof(CutsceneBusEventRegistry)}] Duplicate "
                        + $"CutsceneBusEvent path '{metadata.Path}' was "
                        + "ignored.");
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
        /// <returns>True when the type is a valid cutscene bus event.</returns>
        private static bool TryCreateMetadata(
            Type type,
            out CutsceneBusEventMetadata metadata)
        {
            metadata = null;

            if (type == null
                || !type.IsClass
                || type.IsAbstract
                || !typeof(IEvent).IsAssignableFrom(type))
            {
                return false;
            }

            CutsceneBusEventAttribute attribute =
                type.GetCustomAttribute<CutsceneBusEventAttribute>();

            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Path))
            {
                return false;
            }

            if (GetParameterlessConstructor(type) == null)
            {
                Debug.LogWarning(
                    $"[{nameof(CutsceneBusEventRegistry)}] {type.FullName} "
                    + "was skipped because it does not expose a "
                    + "parameterless constructor.");
                return false;
            }

            string displayName = string.IsNullOrWhiteSpace(attribute.DisplayName)
                ? type.Name
                : attribute.DisplayName;

            metadata = new CutsceneBusEventMetadata(
                type,
                attribute.Path,
                displayName,
                attribute.Description);

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
                modifiers: null);
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
        private static IEvent CreateEventInstance(Type eventType)
        {
            try
            {
                return Activator.CreateInstance(eventType, nonPublic: true) as IEvent;
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
        private static Action<IEvent> CreateDispatcher(Type eventType)
        {
            MethodInfo method = typeof(CutsceneBusEventRegistry).GetMethod(
                nameof(DispatchTyped),
                BindingFlags.Static | BindingFlags.NonPublic);

            if (method == null)
            {
                return null;
            }

            MethodInfo closedMethod = method.MakeGenericMethod(eventType);
            return (Action<IEvent>)Delegate.CreateDelegate(
                typeof(Action<IEvent>),
                closedMethod);
        }

        /// <summary>
        /// Creates one strongly typed HandyBus subscription factory for a
        /// concrete event type.
        /// </summary>
        /// <param name="eventType">Concrete event type.</param>
        /// <returns>Subscription factory when successful.</returns>
        private static Func<Action<IEvent>, IDisposable> CreateSubscriptionFactory(
            Type eventType)
        {
            MethodInfo method = typeof(CutsceneBusEventRegistry).GetMethod(
                nameof(CreateTypedSubscription),
                BindingFlags.Static | BindingFlags.NonPublic);

            if (method == null)
            {
                return null;
            }

            MethodInfo closedMethod = method.MakeGenericMethod(eventType);
            return (Func<Action<IEvent>, IDisposable>)Delegate.CreateDelegate(
                typeof(Func<Action<IEvent>, IDisposable>),
                closedMethod);
        }

        /// <summary>
        /// Raises one typed event through the matching HandyBus channel.
        /// </summary>
        /// <typeparam name="T">Concrete event type.</typeparam>
        /// <param name="cutsceneEvent">Event instance to dispatch.</param>
        private static void DispatchTyped<T>(IEvent cutsceneEvent)
            where T : class, IEvent
        {
            HandyBus<T>.Raise((T)cutsceneEvent);
        }

        /// <summary>
        /// Creates one subscription that forwards the received typed event as
        /// the common IEvent abstraction.
        /// </summary>
        /// <typeparam name="T">Concrete event type.</typeparam>
        /// <param name="onEvent">Callback invoked when the event fires.</param>
        /// <returns>Disposable subscription handle.</returns>
        private static IDisposable CreateTypedSubscription<T>(Action<IEvent> onEvent)
            where T : class, IEvent
        {
            return HandyBus<T>.Subscribe(eventPayload => onEvent(eventPayload));
        }

        #endregion
    }
}