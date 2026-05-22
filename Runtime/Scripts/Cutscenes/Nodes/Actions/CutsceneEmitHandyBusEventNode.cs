using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Events;
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Emit HandyBus Event", "Emit HandyBus Event")]
    public sealed class CutsceneEmitHandyBusEventNode : CutsceneNodeBase
    {
        [SerializeField] private CutsceneBusEventSelector _eventSelector = new();

        /// <summary>
        /// Exposes the authored event selector for runtime migration.
        /// </summary>
        internal CutsceneBusEventSelector EventSelector => _eventSelector ??= new CutsceneBusEventSelector();

        /// <summary>
        /// Configures the node to raise one custom named cutscene event.
        /// </summary>
        /// <param name="eventName">Custom event name routed by the string channel.</param>
        public void UseCustomEventName(string eventName)
        {
            _eventSelector.UseCustomName(eventName);
        }

        /// <summary>
        /// Configures the node to raise one registered cutscene event.
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
            if (_eventSelector.SelectionMode
                == CutsceneBusEventSelector.EventSelectionMode.RegisteredEvent)
            {
                if (!CutsceneBusEventRegistry.TryCreateEventInstance(
                        _eventSelector.EventReference,
                        out IEvent cutsceneEvent)
                    || !CutsceneBusEventRegistry.TryDispatch(cutsceneEvent))
                {
                    context.TryComplete(CutsceneNodeResult.Failure(
                        "Emit HandyBus Event node requires one valid registered event selection."));
                    return;
                }

                context.TryComplete(CutsceneNodeResult.Success());
                return;
            }

            HandyBus<CutsceneExternalEventRaisedEvent>.Raise(
                new CutsceneExternalEventRaisedEvent(
                    context.Director,
                    _eventSelector.EventName));
            context.TryComplete(CutsceneNodeResult.Success());
        }
    }
}