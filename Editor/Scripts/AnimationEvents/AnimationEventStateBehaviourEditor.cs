using IndieGabo.HandyTools.AnimationEventsModule;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// UI Toolkit inspector for string-based state animation events.
    /// </summary>
    [CustomEditor(typeof(AnimationEventStateBehaviour))]
    public sealed class AnimationEventStateBehaviourEditor :
        AnimationStateBehaviourEditorBase
    {
        #region Abstract Members

        /// <summary>
        /// Updates the configuration validation state for the local event.
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
            SerializedProperty eventNameProperty = eventProperty
                .FindPropertyRelative("_eventName");

            if (eventNameProperty == null
                || string.IsNullOrWhiteSpace(eventNameProperty.stringValue))
            {
                message = "Event Name is empty. The state will not trigger any local callback until you fill it.";
                messageType = HelpBoxMessageType.Warning;
                return true;
            }

            if (AnimationStateBehaviourPreviewSession.TryResolvePreviewAnimator(
                    out Animator previewAnimator,
                    out _,
                    out _,
                    out _
                )
                && previewAnimator.GetComponent<AnimationEventReceiver>() == null)
            {
                message = "The selected preview target has an Animator but no AnimationEventReceiver. Local callbacks will have nowhere to land on that object.";
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