using System;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Events;
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Wait For Event", "Wait For Event")]
    public sealed class CutsceneWaitForEventNode : CutsceneNodeBase
    {
        private const string RuntimeStateKey = "RuntimeState";

        [SerializeField] private CutsceneBusEventSelector _eventSelector = new();

        /// <summary>
        /// Exposes the authored event selector for runtime migration.
        /// </summary>
        internal CutsceneBusEventSelector EventSelector => _eventSelector ??= new CutsceneBusEventSelector();

        /// <summary>
        /// Configures the node to wait for one custom named cutscene event.
        /// </summary>
        /// <param name="eventName">Custom event name routed by the string channel.</param>
        public void UseCustomEventName(string eventName)
        {
            _eventSelector.UseCustomName(eventName);
        }

        /// <summary>
        /// Configures the node to wait for one registered cutscene event.
        /// </summary>
        /// <param name="eventPath">Registered event path.</param>
        /// <returns>True when the event path could be resolved.</returns>
        public bool UseRegisteredEvent(string eventPath)
        {
            return _eventSelector.UseRegisteredEvent(eventPath);
        }

        public override string GetSummary()
        {
            return EventSelector.GetSummary();
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            RuntimeState state = context.GetOrCreateNodeState(RuntimeStateKey, () => new RuntimeState());
            state.Received = false;

            state.Subscription?.Dispose();
            state.Subscription = null;

            if (state.Binding != null)
            {
                HandyBus<CutsceneExternalEventRaisedEvent>.Deregister(state.Binding);
                state.Binding = null;
            }

            var executionId = context.CurrentNodeExecutionId;

            if (_eventSelector.SelectionMode
                == CutsceneBusEventSelector.EventSelectionMode.RegisteredEvent)
            {
                if (!CutsceneBusEventRegistry.TrySubscribe(
                        _eventSelector.EventReference,
                        _ =>
                        {
                            state.Received = true;
                            context.TryCompleteNode(
                                executionId,
                                CutsceneNodeResult.Success());
                        },
                        out IDisposable subscription))
                {
                    context.TryComplete(CutsceneNodeResult.Failure(
                        "Wait For Event node requires one valid registered event selection."));
                    return;
                }

                state.Subscription = subscription;
                context.SetNodeState(RuntimeStateKey, state);
                return;
            }

            state.Binding = new EventBinding<CutsceneExternalEventRaisedEvent>(cutsceneEvent =>
            {
                if (!string.Equals(
                        cutsceneEvent.EventName,
                        _eventSelector.EventName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                state.Received = true;
                context.TryCompleteNode(executionId, CutsceneNodeResult.Success());
            });

            HandyBus<CutsceneExternalEventRaisedEvent>.Register(state.Binding);
            context.SetNodeState(RuntimeStateKey, state);
        }

        public override void OnExit(CutsceneExecutionContext context)
        {
            if (context.TryGetNodeState(RuntimeStateKey, out RuntimeState state) && state.Binding != null)
            {
                HandyBus<CutsceneExternalEventRaisedEvent>.Deregister(state.Binding);
                state.Binding = null;
                context.SetNodeState(RuntimeStateKey, state);
            }

            if (context.TryGetNodeState(RuntimeStateKey, out state)
                && state.Subscription != null)
            {
                state.Subscription.Dispose();
                state.Subscription = null;
                context.SetNodeState(RuntimeStateKey, state);
            }
        }

        private sealed class RuntimeState
        {
            public bool Received;
            public EventBinding<CutsceneExternalEventRaisedEvent> Binding;
            public IDisposable Subscription;
        }
    }
}