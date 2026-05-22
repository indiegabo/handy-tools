using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Renders one minimal built-in UI Toolkit presenter that can bind to any playback
    /// controller implementing <see cref="IConversationPlaybackController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ConversationDefaultPresenter : ConversationPresenterComponent
    {
        #region Constants

        private const string OverlayElementName = "conversation-default-presenter-overlay";

        private const string PanelElementName = "conversation-default-presenter-panel";

        #endregion

        #region Fields

        [SerializeField]
        private string _panelTitle = "Conversation";

        private UIDocument _document;

        private VisualElement _overlayRoot;

        private VisualElement _panelRoot;

        private Label _titleLabel;

        private VisualElement _speakerPanel;

        private Image _speakerPortraitImage;

        private Label _speakerNameLabel;

        private VisualElement _listenerPanel;

        private Image _listenerPortraitImage;

        private Label _listenerNameLabel;

        private Label _lineLabel;

        private Label _statusLabel;

        private Button _restartButton;

        private Button _advanceButton;

        private Button _skipButton;

        private Button _cancelButton;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Caches the attached document and builds the presenter subtree.
        /// </summary>
        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            BuildUi();
        }

        #endregion

        #region Binding

        /// <summary>
        /// Refreshes the rendered line, status, and action buttons from the bound controller.
        /// </summary>
        protected override void RefreshPresentation()
        {
            if (_panelRoot == null || _titleLabel == null)
            {
                BuildUi();

                if (_titleLabel == null)
                {
                    return;
                }
            }

            ConversationSession session = Controller?.Session;
            ConversationActorData leftParticipant = session?.CurrentLeftParticipant;
            ConversationActorData rightParticipant = session?.CurrentRightParticipant;
            bool hasActiveLine = session?.HasActiveLine ?? false;
            bool showsParticipantPresentation = session?.CurrentLineUsesSpeakerPresentation
                ?? false;
            bool canRestart = Controller != null && !Controller.IsLoading;
            bool canInterrupt = Controller != null && (Controller.IsLoading || hasActiveLine);
            string conversationTitle = session?.Conversation?.Title;

            _titleLabel.text = string.IsNullOrWhiteSpace(conversationTitle)
                ? _panelTitle
                : conversationTitle;

            ApplyActorPresentation(
                _speakerPanel,
                _speakerPortraitImage,
                _speakerNameLabel,
                leftParticipant,
                hasActiveLine && showsParticipantPresentation,
                "Left Slot");
            ApplyActorPresentation(
                _listenerPanel,
                _listenerPortraitImage,
                _listenerNameLabel,
                rightParticipant,
                hasActiveLine && showsParticipantPresentation,
                "Right Slot");

            _lineLabel.text = hasActiveLine
                ? session.CurrentLineText
                : "No active line is being presented.";

            string failureReason = Controller?.FailureReason ?? string.Empty;
            _statusLabel.text = string.IsNullOrWhiteSpace(failureReason)
                ? (Controller?.StatusMessage ?? "Conversation playback unavailable.")
                : $"{Controller.StatusMessage} {failureReason}";

            _restartButton?.SetEnabled(canRestart);

            if (_advanceButton != null)
            {
                _advanceButton.text = BuildActionButtonLabel(
                    "Advance",
                    Controller?.Table?.ContinueAction);
                _advanceButton.SetEnabled(hasActiveLine);
            }

            if (_skipButton != null)
            {
                _skipButton.text = BuildActionButtonLabel(
                    "Skip",
                    Controller?.Table?.SkipAction);
                _skipButton.SetEnabled(hasActiveLine);
            }

            if (_cancelButton != null)
            {
                _cancelButton.text = BuildActionButtonLabel(
                    "Cancel",
                    Controller?.Table?.CancelAction);
                _cancelButton.SetEnabled(canInterrupt);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Builds the presenter subtree inside the attached UIDocument.
        /// </summary>
        private void BuildUi()
        {
            if (_document?.rootVisualElement == null)
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            _overlayRoot = root.Q<VisualElement>(OverlayElementName);

            if (_overlayRoot == null)
            {
                _overlayRoot = new VisualElement
                {
                    name = OverlayElementName,
                };
                root.Add(_overlayRoot);
            }

            ConfigureOverlay(_overlayRoot);
            _overlayRoot.Clear();

            _panelRoot = new VisualElement
            {
                name = PanelElementName,
            };
            ConfigurePanel(_panelRoot);
            _overlayRoot.Add(_panelRoot);

            _titleLabel = CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter);
            _titleLabel.style.color = Color.white;
            _titleLabel.style.marginBottom = 18f;
            _panelRoot.Add(_titleLabel);

            VisualElement stageRow = new();
            stageRow.style.flexDirection = FlexDirection.Row;
            stageRow.style.alignItems = Align.Center;
            stageRow.style.justifyContent = Justify.Center;
            stageRow.style.width = new Length(100f, LengthUnit.Percent);
            _panelRoot.Add(stageRow);

            _speakerPanel = CreateActorPanel(
                "speaker",
                out _speakerPortraitImage,
                out _speakerNameLabel);
            _speakerPanel.style.marginRight = 20f;
            stageRow.Add(_speakerPanel);

            VisualElement centerColumn = new();
            centerColumn.style.flexGrow = 1f;
            centerColumn.style.minHeight = 220f;
            centerColumn.style.justifyContent = Justify.Center;
            centerColumn.style.alignItems = Align.Center;
            centerColumn.style.paddingLeft = 20f;
            centerColumn.style.paddingRight = 20f;
            centerColumn.style.paddingTop = 20f;
            centerColumn.style.paddingBottom = 20f;
            centerColumn.style.backgroundColor = new Color(0.11f, 0.13f, 0.16f, 0.94f);
            centerColumn.style.borderLeftWidth = 1f;
            centerColumn.style.borderRightWidth = 1f;
            centerColumn.style.borderTopWidth = 1f;
            centerColumn.style.borderBottomWidth = 1f;
            centerColumn.style.borderLeftColor = new Color(0.29f, 0.33f, 0.40f, 1f);
            centerColumn.style.borderRightColor = new Color(0.29f, 0.33f, 0.40f, 1f);
            centerColumn.style.borderTopColor = new Color(0.29f, 0.33f, 0.40f, 1f);
            centerColumn.style.borderBottomColor = new Color(0.29f, 0.33f, 0.40f, 1f);
            centerColumn.style.borderTopLeftRadius = 18f;
            centerColumn.style.borderTopRightRadius = 18f;
            centerColumn.style.borderBottomLeftRadius = 18f;
            centerColumn.style.borderBottomRightRadius = 18f;
            stageRow.Add(centerColumn);

            _lineLabel = CreateLabel(24, FontStyle.Normal, TextAnchor.MiddleCenter);
            _lineLabel.style.color = new Color(0.96f, 0.97f, 0.99f, 1f);
            _lineLabel.style.whiteSpace = WhiteSpace.Normal;
            _lineLabel.style.maxWidth = 620f;
            _lineLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            centerColumn.Add(_lineLabel);

            _statusLabel = CreateLabel(12, FontStyle.Normal, TextAnchor.MiddleCenter);
            _statusLabel.style.color = new Color(0.76f, 0.79f, 0.84f, 1f);
            _statusLabel.style.marginTop = 14f;
            centerColumn.Add(_statusLabel);

            _listenerPanel = CreateActorPanel(
                "listener",
                out _listenerPortraitImage,
                out _listenerNameLabel);
            _listenerPanel.style.marginLeft = 20f;
            stageRow.Add(_listenerPanel);

            VisualElement buttonRow = new();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.flexWrap = Wrap.Wrap;
            buttonRow.style.justifyContent = Justify.Center;
            buttonRow.style.marginTop = 18f;
            _panelRoot.Add(buttonRow);

            _restartButton = CreateButton("Restart", () => Controller?.Play());
            buttonRow.Add(_restartButton);

            _advanceButton = CreateButton(
                "Advance",
                () => Controller?.AdvanceConversation());
            _advanceButton.style.marginLeft = 10f;
            buttonRow.Add(_advanceButton);

            _skipButton = CreateButton(
                "Skip",
                () => Controller?.SkipConversation());
            _skipButton.style.marginLeft = 10f;
            buttonRow.Add(_skipButton);

            _cancelButton = CreateButton(
                "Cancel",
                () => Controller?.CancelConversation());
            _cancelButton.style.marginLeft = 10f;
            buttonRow.Add(_cancelButton);

            RefreshPresentation();
        }

        /// <summary>
        /// Configures the full-screen overlay used by the runtime presenter.
        /// </summary>
        /// <param name="overlayRoot">Overlay element that should cover the viewport.</param>
        private static void ConfigureOverlay(VisualElement overlayRoot)
        {
            overlayRoot.style.position = Position.Absolute;
            overlayRoot.style.left = 0f;
            overlayRoot.style.right = 0f;
            overlayRoot.style.top = 0f;
            overlayRoot.style.bottom = 0f;
            overlayRoot.style.alignItems = Align.Center;
            overlayRoot.style.justifyContent = Justify.FlexEnd;
            overlayRoot.style.paddingLeft = 24f;
            overlayRoot.style.paddingRight = 24f;
            overlayRoot.style.paddingTop = 24f;
            overlayRoot.style.paddingBottom = 24f;
        }

        /// <summary>
        /// Configures the main conversation panel container.
        /// </summary>
        /// <param name="panelRoot">Panel element that should receive the conversation UI.</param>
        private static void ConfigurePanel(VisualElement panelRoot)
        {
            panelRoot.style.width = new Length(100f, LengthUnit.Percent);
            panelRoot.style.maxWidth = 1120f;
            panelRoot.style.paddingLeft = 24f;
            panelRoot.style.paddingRight = 24f;
            panelRoot.style.paddingTop = 20f;
            panelRoot.style.paddingBottom = 20f;
            panelRoot.style.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.92f);
            panelRoot.style.borderLeftWidth = 1f;
            panelRoot.style.borderRightWidth = 1f;
            panelRoot.style.borderTopWidth = 1f;
            panelRoot.style.borderBottomWidth = 1f;
            panelRoot.style.borderLeftColor = new Color(0.22f, 0.27f, 0.34f, 1f);
            panelRoot.style.borderRightColor = new Color(0.22f, 0.27f, 0.34f, 1f);
            panelRoot.style.borderTopColor = new Color(0.22f, 0.27f, 0.34f, 1f);
            panelRoot.style.borderBottomColor = new Color(0.22f, 0.27f, 0.34f, 1f);
            panelRoot.style.borderTopLeftRadius = 22f;
            panelRoot.style.borderTopRightRadius = 22f;
            panelRoot.style.borderBottomLeftRadius = 22f;
            panelRoot.style.borderBottomRightRadius = 22f;
        }

        /// <summary>
        /// Creates one participant panel used for speaker or listener presentation.
        /// </summary>
        /// <param name="prefix">Element prefix used for generated names.</param>
        /// <param name="portraitImage">Resolved portrait element.</param>
        /// <param name="nameLabel">Resolved name label element.</param>
        /// <returns>The created participant panel.</returns>
        private static VisualElement CreateActorPanel(
            string prefix,
            out Image portraitImage,
            out Label nameLabel)
        {
            VisualElement panel = new()
            {
                name = $"{prefix}-panel",
            };

            panel.style.width = 176f;
            panel.style.minHeight = 220f;
            panel.style.flexShrink = 0f;
            panel.style.alignItems = Align.Center;
            panel.style.justifyContent = Justify.Center;
            panel.style.paddingLeft = 14f;
            panel.style.paddingRight = 14f;
            panel.style.paddingTop = 18f;
            panel.style.paddingBottom = 18f;
            panel.style.backgroundColor = new Color(0.09f, 0.10f, 0.14f, 0.82f);
            panel.style.borderLeftWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftColor = new Color(0.19f, 0.24f, 0.31f, 1f);
            panel.style.borderRightColor = new Color(0.19f, 0.24f, 0.31f, 1f);
            panel.style.borderTopColor = new Color(0.19f, 0.24f, 0.31f, 1f);
            panel.style.borderBottomColor = new Color(0.19f, 0.24f, 0.31f, 1f);
            panel.style.borderTopLeftRadius = 16f;
            panel.style.borderTopRightRadius = 16f;
            panel.style.borderBottomLeftRadius = 16f;
            panel.style.borderBottomRightRadius = 16f;

            portraitImage = new Image
            {
                name = $"{prefix}-portrait",
                scaleMode = ScaleMode.ScaleToFit,
            };
            portraitImage.style.width = 128f;
            portraitImage.style.height = 128f;
            portraitImage.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            portraitImage.style.borderTopLeftRadius = 12f;
            portraitImage.style.borderTopRightRadius = 12f;
            portraitImage.style.borderBottomLeftRadius = 12f;
            portraitImage.style.borderBottomRightRadius = 12f;
            panel.Add(portraitImage);

            nameLabel = CreateLabel(15, FontStyle.Bold, TextAnchor.MiddleCenter);
            nameLabel.style.marginTop = 10f;
            nameLabel.style.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            panel.Add(nameLabel);
            return panel;
        }

        /// <summary>
        /// Creates one shared runtime label.
        /// </summary>
        /// <param name="fontSize">Requested font size.</param>
        /// <param name="fontStyle">Requested font style.</param>
        /// <param name="alignment">Requested text alignment.</param>
        /// <returns>The created label.</returns>
        private static Label CreateLabel(
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            Label label = new();
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = fontStyle;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = alignment;
            return label;
        }

        /// <summary>
        /// Creates one shared presenter button.
        /// </summary>
        /// <param name="text">Initial button text.</param>
        /// <param name="clicked">Callback invoked after one click.</param>
        /// <returns>The created button.</returns>
        private static Button CreateButton(string text, System.Action clicked)
        {
            Button button = new(clicked)
            {
                text = text,
            };

            button.style.minWidth = 88f;
            button.style.height = 32f;
            return button;
        }

        /// <summary>
        /// Applies one actor portrait and name to one participant panel.
        /// </summary>
        /// <param name="panel">Panel that should be shown or hidden.</param>
        /// <param name="portraitImage">Portrait element rendered inside the panel.</param>
        /// <param name="nameLabel">Name label rendered inside the panel.</param>
        /// <param name="actor">Runtime actor that should be presented.</param>
        /// <param name="visible">Whether the participant panel should remain visible.</param>
        /// <param name="fallbackCaption">Fallback caption used when the actor is unassigned.</param>
        private void ApplyActorPresentation(
            VisualElement panel,
            Image portraitImage,
            Label nameLabel,
            ConversationActorData actor,
            bool visible,
            string fallbackCaption)
        {
            if (panel != null)
            {
                panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!visible)
            {
                return;
            }

            if (nameLabel != null)
            {
                nameLabel.text = ResolveActorLabel(actor, fallbackCaption);
                nameLabel.style.color = actor?.AccentColor ?? new Color(0.92f, 0.94f, 0.98f, 1f);
            }

            if (portraitImage == null)
            {
                return;
            }

            Sprite portrait = Controller?.ResolveActorPortrait(actor);
            portraitImage.sprite = portrait;
            portraitImage.style.opacity = portrait == null ? 0.28f : 1f;
            portraitImage.style.backgroundColor = portrait == null
                ? new Color(1f, 1f, 1f, 0.08f)
                : Color.clear;
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
        /// Builds the button label shown for one input-bound action.
        /// </summary>
        /// <param name="caption">Base caption rendered by the button.</param>
        /// <param name="actionReference">Input action shown by the button.</param>
        /// <returns>The final button label.</returns>
        private static string BuildActionButtonLabel(
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