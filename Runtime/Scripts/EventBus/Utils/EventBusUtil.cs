using System;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace IndieGabo.HandyTools.HandyBus
{
    public static class EventBusUtil
    {
        public static IReadOnlyList<Type> EventTypes { get; set; }
        public static IReadOnlyList<Type> EventBusTypes { get; set; }

#if UNITY_EDITOR
        public static PlayModeStateChange PlayModeState { get; set; }

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
                ClearAllBusses();
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
            EventTypes = PredefinedAssemblyUtil.GetTypes(typeof(IEvent));
            EventBusTypes = InitializeBusses();
        }

        static List<Type> InitializeBusses()
        {
            List<Type> eventBusTypes = new();
            var typedef = typeof(EventBus<>);
            foreach (var eventType in EventTypes)
            {
                var busType = typedef.MakeGenericType(eventType);
                eventBusTypes.Add(busType);
            }

            return eventBusTypes;
        }

        public static void ClearAllBusses()
        {
            if (EventBusTypes == null)
            {
                return;
            }

            for (int i = 0; i < EventBusTypes.Count; i++)
            {
                var busType = EventBusTypes[i];
                var clearMethod = busType.GetMethod(
                    "Clear",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                clearMethod.Invoke(null, null);
            }
        }
    }

}