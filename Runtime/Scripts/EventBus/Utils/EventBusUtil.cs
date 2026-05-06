using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace IndieGabo.HandyTools.HandyBusModule
{
    /// <summary>
    /// Discovers HandyBus types and clears them during editor and runtime
    /// initialization boundaries.
    /// </summary>
    public static class HandyBusUtil
    {
        private static readonly Dictionary<Type, RegisteredBus> _registeredBuses = new();
        private static readonly List<Type> _eventTypes = new();
        private static readonly List<Type> _handyBusTypes = new();

        /// <summary>
        /// Gets all registered event types.
        /// </summary>
        public static IReadOnlyList<Type> EventTypes => _eventTypes;

        /// <summary>
        /// Gets all registered closed HandyBus types.
        /// </summary>
        public static IReadOnlyList<Type> HandyBusTypes => _handyBusTypes;

#if UNITY_EDITOR
        /// <summary>
        /// Gets or sets the current editor play mode state.
        /// </summary>
        public static PlayModeStateChange PlayModeState { get; set; }

        /// <summary>
        /// Hooks editor play mode transitions used to clear runtime HandyBus instances.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
        }

        static void OnPlayModeStateChange(PlayModeStateChange state)
        {
            PlayModeState = state;

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAllBuses();
            }
        }
#endif

        /// <summary>
        /// Clears all registered HandyBus state for a new runtime session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
            ClearAllBuses();
        }

        internal static void RegisterBus(
            Type eventType,
            Type handyBusType,
            Action clearAction
        )
        {
            if (eventType == null)
            {
                throw new ArgumentNullException(nameof(eventType));
            }

            if (handyBusType == null)
            {
                throw new ArgumentNullException(nameof(handyBusType));
            }

            if (clearAction == null)
            {
                throw new ArgumentNullException(nameof(clearAction));
            }

            if (_registeredBuses.ContainsKey(eventType))
            {
                return;
            }

            _registeredBuses.Add(
                eventType,
                new RegisteredBus(handyBusType, clearAction)
            );

            _eventTypes.Add(eventType);
            _handyBusTypes.Add(handyBusType);
        }

        /// <summary>
        /// Clears all registered HandyBus bindings.
        /// </summary>
        public static void ClearAllBuses()
        {
            if (_registeredBuses.Count == 0)
            {
                return;
            }

            foreach (RegisteredBus registeredBus in _registeredBuses.Values)
            {
                registeredBus.Clear();
            }
        }

        private readonly struct RegisteredBus
        {
            private readonly Action _clearAction;

            public RegisteredBus(Type busType, Action clearAction)
            {
                BusType = busType;
                _clearAction = clearAction;
            }

            public Type BusType { get; }

            public void Clear()
            {
                _clearAction();
            }
        }
    }

}