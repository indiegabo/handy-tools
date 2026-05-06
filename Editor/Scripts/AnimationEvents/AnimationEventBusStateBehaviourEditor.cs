using IndieGabo.HandyTools.AnimationEventsModule;
using UnityEditor;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// UI Toolkit inspector for typed HandyBus state animation events.
    /// </summary>
    [CustomEditor(typeof(AnimationEventBusStateBehaviour))]
    public sealed class AnimationEventBusStateBehaviourEditor :
        AnimationStateBehaviourEditorBase
    {
        #region Abstract Members

        /// <summary>
        /// Updates the configuration validation state for the typed bus event.
        /// </summary>
        /// <param name="eventProperty">Serialized event trigger property.</param>
        /// <param name="message">Resolved validation message.</param>
        /// <param name="messageType">Severity associated with the message.</param>
        /// <returns>True when a validation message should be shown.</returns>
        protected override bool TryGetConfigurationStatus(
            SerializedProperty eventProperty,
            out string message,
            out HelpBoxMessageType messageType
        )
        {
            AnimatorBusEventMetadata metadata =
                AnimatorBusEventEditorUtility.GetSelectedMetadata(eventProperty);

            SerializedProperty payloadProperty = eventProperty
                .FindPropertyRelative(
                    AnimatorBusEventEditorUtility.EventPayloadFieldName
                );

            if (metadata == null)
            {
                message = "No AnimatorBusEvent is selected yet. Pick one from the event menu to author and dispatch a typed HandyBus event.";
                messageType = HelpBoxMessageType.Warning;
                return true;
            }

            if (payloadProperty == null || payloadProperty.managedReferenceValue == null)
            {
                message = "The selected AnimatorBusEvent has no payload instance yet. Re-select the event if the inspector did not create one automatically.";
                messageType = HelpBoxMessageType.Warning;
                return true;
            }

            if (payloadProperty.managedReferenceValue.GetType() != metadata.EventType)
            {
                message = "The current payload type does not match the selected AnimatorBusEvent type. Re-select the event to rebuild the payload.";
                messageType = HelpBoxMessageType.Warning;
                return true;
            }

            message = string.Empty;
            messageType = HelpBoxMessageType.None;
            return false;
        }

        #endregion
    }
}