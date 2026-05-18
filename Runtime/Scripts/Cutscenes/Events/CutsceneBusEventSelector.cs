using System;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    /// <summary>
    /// Defines how a cutscene node resolves the HandyBus event it should use.
    /// </summary>
    [Serializable]
    public sealed class CutsceneBusEventSelector
    {
        #region Types

        /// <summary>
        /// Enumerates the supported event-resolution modes.
        /// </summary>
        public enum EventSelectionMode
        {
            CustomName = 0,
            RegisteredEvent = 1,
        }

        #endregion

        #region Constants

        private const string DefaultEventName = "cutscene.event";

        #endregion

        #region Inspector

        [SerializeField] private EventSelectionMode _selectionMode;

        [SerializeField] private string _eventName = DefaultEventName;

        [SerializeField] private CutsceneBusEventReference _eventReference = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the authored selection mode.
        /// </summary>
        public EventSelectionMode SelectionMode => _selectionMode;

        /// <summary>
        /// Gets the authored custom event name.
        /// </summary>
        public string EventName => _eventName ?? string.Empty;

        /// <summary>
        /// Gets the authored registered-event reference.
        /// </summary>
        public CutsceneBusEventReference EventReference => _eventReference;

        #endregion

        #region Public API

        /// <summary>
        /// Gets a concise summary for inspector and graph presentation.
        /// </summary>
        /// <returns>One authored event label.</returns>
        public string GetSummary()
        {
            if (_selectionMode == EventSelectionMode.RegisteredEvent
                && CutsceneBusEventRegistry.TryGetMetadata(
                    _eventReference,
                    out CutsceneBusEventMetadata metadata))
            {
                return string.IsNullOrWhiteSpace(metadata.DisplayName)
                    ? metadata.Path
                    : metadata.DisplayName;
            }

            if (_selectionMode == EventSelectionMode.RegisteredEvent)
            {
                return "Registered Event";
            }

            return string.IsNullOrWhiteSpace(_eventName)
                ? DefaultEventName
                : _eventName;
        }

        /// <summary>
        /// Configures the selector to dispatch or listen by custom event name.
        /// </summary>
        /// <param name="eventName">Custom HandyBus event name.</param>
        public void UseCustomName(string eventName)
        {
            _selectionMode = EventSelectionMode.CustomName;
            _eventName = string.IsNullOrWhiteSpace(eventName)
                ? DefaultEventName
                : eventName;
        }

        /// <summary>
        /// Configures the selector to dispatch or listen by one registered
        /// event path when that path exists.
        /// </summary>
        /// <param name="eventPath">Registered event path.</param>
        /// <returns>True when the path could be resolved.</returns>
        public bool UseRegisteredEvent(string eventPath)
        {
            if (!CutsceneBusEventRegistry.TryGetMetadata(
                    eventPath,
                    out CutsceneBusEventMetadata metadata))
            {
                return false;
            }

            _selectionMode = EventSelectionMode.RegisteredEvent;
            _eventReference ??= new CutsceneBusEventReference();

            CutsceneBusEventRegistry.ApplySelection(_eventReference, metadata);
            return true;
        }

        #endregion
    }
}