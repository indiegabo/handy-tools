using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Renders one uGUI-based conversation presenter that can bind to any playback
    /// controller implementing <see cref="IConversationPlaybackController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class ConversationCanvasPresenter : ConversationPresenterComponent
    {
        #region Fields

        [SerializeField]
        private string _panelTitle = "Conversation";

        [SerializeField]
        private Text _titleText;

        [SerializeField]
        private GameObject _speakerPanel;

        [SerializeField]
        private Image _speakerPortraitImage;

        [SerializeField]
        private Text _speakerNameText;

        [SerializeField]
        private GameObject _listenerPanel;

        [SerializeField]
        private Image _listenerPortraitImage;

        [SerializeField]
        private Text _listenerNameText;

        [SerializeField]
        private Text _lineText;

        [SerializeField]
        private Text _statusText;

        [SerializeField]
        private Text _actionHintsText;

        #endregion

        #region Binding

        /// <summary>
        /// Refreshes the rendered line, participant panels, and action hints.
        /// </summary>
        protected override void RefreshPresentation()
        {
            ConversationSession session = Controller?.Session;
            ConversationActorData leftParticipant = session?.CurrentLeftParticipant;
            ConversationActorData rightParticipant = session?.CurrentRightParticipant;
            bool hasActiveLine = session?.HasActiveLine ?? false;
            bool showsParticipantPresentation = session?.CurrentLineUsesSpeakerPresentation
                ?? false;
            string conversationTitle = session?.Conversation?.Title;

            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrWhiteSpace(conversationTitle)
                    ? _panelTitle
                    : conversationTitle;
            }

            ApplyActorPresentation(
                _speakerPanel,
                _speakerPortraitImage,
                _speakerNameText,
                leftParticipant,
                hasActiveLine && showsParticipantPresentation,
                "Left Slot");
            ApplyActorPresentation(
                _listenerPanel,
                _listenerPortraitImage,
                _listenerNameText,
                rightParticipant,
                hasActiveLine && showsParticipantPresentation,
                "Right Slot");

            if (_lineText != null)
            {
                _lineText.text = hasActiveLine
                    ? session.CurrentLineText
                    : "No active line is being presented.";
            }

            if (_statusText != null)
            {
                string failureReason = Controller?.FailureReason ?? string.Empty;
                _statusText.text = string.IsNullOrWhiteSpace(failureReason)
                    ? (Controller?.StatusMessage ?? "Conversation playback unavailable.")
                    : $"{Controller.StatusMessage} {failureReason}";
            }

            if (_actionHintsText != null)
            {
                _actionHintsText.text = BuildActionHints();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Applies one actor portrait and display name to one participant panel.
        /// </summary>
        /// <param name="panel">Panel that should be shown or hidden.</param>
        /// <param name="portraitImage">Portrait image rendered inside the panel.</param>
        /// <param name="nameText">Name text rendered inside the panel.</param>
        /// <param name="actor">Runtime actor that should be presented.</param>
        /// <param name="visible">Whether the participant panel should remain visible.</param>
        /// <param name="fallbackCaption">Fallback caption used when the actor is unassigned.</param>
        private void ApplyActorPresentation(
            GameObject panel,
            Image portraitImage,
            Text nameText,
            ConversationActorData actor,
            bool visible,
            string fallbackCaption)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = ResolveActorLabel(actor, fallbackCaption);
                nameText.color = actor?.AccentColor ?? new Color(0.93f, 0.95f, 0.98f, 1f);
            }

            if (portraitImage == null)
            {
                return;
            }

            Sprite portrait = Controller?.ResolveActorPortrait(actor);
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        /// <summary>
        /// Builds the runtime action hint label shown under the conversation frame.
        /// </summary>
        /// <returns>The final action hint label.</returns>
        private string BuildActionHints()
        {
            string advanceHint = BuildActionHintLabel("Advance", Controller?.Table?.ContinueAction);
            string skipHint = BuildActionHintLabel("Skip", Controller?.Table?.SkipAction);
            string cancelHint = BuildActionHintLabel("Cancel", Controller?.Table?.CancelAction);
            return $"{advanceHint}    {skipHint}    {cancelHint}";
        }

        /// <summary>
        /// Resolves one readable actor label from one runtime conversant.
        /// </summary>
        /// <param name="actor">Runtime actor that should be labeled.</param>
        /// <param name="fallbackCaption">Fallback caption used when the actor is unassigned.</param>
        /// <returns>The resolved actor label.</returns>
        private static string ResolveActorLabel(
            ConversationActorData actor,
            string fallbackCaption)
        {
            if (actor == null)
            {
                return $"{fallbackCaption}: Unassigned";
            }

            if (!string.IsNullOrWhiteSpace(actor.DisplayName))
            {
                return actor.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(actor.Key))
            {
                return actor.Key;
            }

            return fallbackCaption;
        }

        /// <summary>
        /// Builds the action hint shown for one input-bound action.
        /// </summary>
        /// <param name="caption">Base caption rendered in the hint.</param>
        /// <param name="actionReference">Input action shown by the hint.</param>
        /// <returns>The final action hint.</returns>
        private static string BuildActionHintLabel(
            string caption,
            InputActionReference actionReference)
        {
            string bindingLabel = ResolveBindingLabel(actionReference);
            return string.IsNullOrWhiteSpace(bindingLabel)
                ? caption
                : $"{caption} [{bindingLabel}]";
        }

        /// <summary>
        /// Resolves one short binding label from one input action.
        /// </summary>
        /// <param name="actionReference">Action reference that should be displayed.</param>
        /// <returns>The resolved short binding label.</returns>
        private static string ResolveBindingLabel(InputActionReference actionReference)
        {
            InputAction action = actionReference?.action;

            if (action == null)
            {
                return string.Empty;
            }

            string displayString = action.GetBindingDisplayString();

            if (!string.IsNullOrWhiteSpace(displayString))
            {
                return displayString;
            }

            return action.name ?? string.Empty;
        }

        #endregion
    }
}