using System;
using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Defines one typed HandyBus animation event emitted by a state each time
    /// the configured threshold is reached within a loop.
    /// </summary>
    [Serializable]
    public sealed class AnimationEventBusStateTrigger : AnimationStateEventTrigger
    {
        #region Inspector

        [SerializeField] private AnimatorBusEventReference _eventReference = new();

        [SerializeReference] private AnimatorBusEventBase _eventPayload;

        #endregion

        #region State

        [NonSerialized] private bool _hasTriggered;

        [NonSerialized] private int _currentLoop;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the serialized event identity selected in the inspector.
        /// </summary>
        public AnimatorBusEventReference EventReference => _eventReference;

        /// <summary>
        /// Gets the authored payload configured for this trigger.
        /// </summary>
        public AnimatorBusEventBase EventPayload => _eventPayload;

        #endregion

        #region Public API

        /// <summary>
        /// Resets the per-state runtime trigger flag.
        /// </summary>
        public override void ResetRuntimeState()
        {
            base.ResetRuntimeState();
            _hasTriggered = false;
            _currentLoop = -1;
        }

        /// <summary>
        /// Emits the configured typed event once when the normalized time
        /// crosses the configured threshold within the current loop.
        /// </summary>
        /// <param name="animator">Animator that owns the active state.</param>
        /// <param name="stateInfo">Animator state info at dispatch time.</param>
        /// <param name="layerIndex">Animator layer index.</param>
        /// <param name="currentTime">Current full normalized state time.</param>
        /// <returns>
        /// True when the trigger threshold was crossed during this call.
        /// </returns>
        public bool TryTrigger(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex,
            float currentTime
        )
        {
            int loop = Mathf.Max(0, Mathf.FloorToInt(currentTime));
            float normalizedLoopTime = Mathf.Repeat(currentTime, 1f);

            if (_currentLoop != loop)
            {
                _currentLoop = loop;
                _hasTriggered = false;
            }

            if (_hasTriggered || normalizedLoopTime < TriggerTime)
            {
                return false;
            }

            _hasTriggered = true;

            if (!TryCreateDispatchEvent(
                    animator,
                    stateInfo,
                    layerIndex,
                    out AnimatorBusEventBase dispatchEvent
                ))
            {
                return true;
            }

            AnimatorBusEventRegistry.TryDispatch(dispatchEvent);
            return true;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates a dispatch-time event copy with the current animator context.
        /// </summary>
        /// <param name="animator">Animator that owns the active state.</param>
        /// <param name="stateInfo">Animator state info at dispatch time.</param>
        /// <param name="layerIndex">Animator layer index.</param>
        /// <param name="dispatchEvent">Created dispatch-time event copy.</param>
        /// <returns>True when the event could be created.</returns>
        private bool TryCreateDispatchEvent(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex,
            out AnimatorBusEventBase dispatchEvent
        )
        {
            dispatchEvent = null;

            if (!TryResolveSourceEvent(
                    out AnimatorBusEventBase sourceEvent,
                    out string eventPath
                ))
            {
                return false;
            }

            dispatchEvent = sourceEvent.CreateDispatchCopy();
            dispatchEvent.SetRuntimeContext(eventPath, animator, stateInfo, layerIndex);
            return true;
        }

        /// <summary>
        /// Resolves the authored payload or creates a default instance from the
        /// selected event reference.
        /// </summary>
        /// <param name="sourceEvent">Resolved authored payload.</param>
        /// <param name="eventPath">Resolved stable logical event path.</param>
        /// <returns>True when the event source could be resolved.</returns>
        private bool TryResolveSourceEvent(
            out AnimatorBusEventBase sourceEvent,
            out string eventPath
        )
        {
            sourceEvent = null;
            eventPath = _eventReference != null ? _eventReference.EventPath : string.Empty;

            if (AnimatorBusEventRegistry.TryGetMetadata(
                    _eventReference,
                    out AnimatorBusEventMetadata metadata
                ))
            {
                eventPath = metadata.Path;

                if (_eventPayload != null
                    && metadata.EventType.IsInstanceOfType(_eventPayload))
                {
                    sourceEvent = _eventPayload;
                    return true;
                }

                sourceEvent = CreateDefaultPayload(metadata);
                return sourceEvent != null;
            }

            if (_eventPayload == null)
            {
                return false;
            }

            sourceEvent = _eventPayload;

            if (AnimatorBusEventRegistry.TryGetMetadata(
                    _eventPayload.GetType(),
                    out AnimatorBusEventMetadata payloadMetadata
                ))
            {
                eventPath = payloadMetadata.Path;
            }

            return true;
        }

        /// <summary>
        /// Creates a default payload instance for one selected metadata entry.
        /// </summary>
        /// <param name="metadata">Resolved event metadata.</param>
        /// <returns>Default payload instance when successful.</returns>
        private AnimatorBusEventBase CreateDefaultPayload(
            AnimatorBusEventMetadata metadata
        )
        {
            if (metadata == null)
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(
                    metadata.EventType,
                    nonPublic: true
                ) as AnimatorBusEventBase;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        #endregion
    }
}