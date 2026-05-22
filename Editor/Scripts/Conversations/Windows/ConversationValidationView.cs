using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.Editor.ConversationsModule.Validation;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Hosts the table-wide validation surface for the Conversations window.
    /// </summary>
    public sealed class ConversationValidationView : VisualElement
    {
        private static readonly Color SectionBackgroundColor =
            new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color BorderColor =
            new(0.24f, 0.24f, 0.24f, 1f);
        private static readonly Color MutedTextColor =
            new(0.66f, 0.66f, 0.66f, 1f);
        private static readonly Color IssueCardBackgroundColor =
            new(0.14f, 0.14f, 0.14f, 1f);
        private static readonly Color ErrorColor =
            new(0.76f, 0.27f, 0.25f, 1f);
        private static readonly Color WarningColor =
            new(0.87f, 0.64f, 0.24f, 1f);
        private static readonly Color SuccessColor =
            new(0.32f, 0.67f, 0.39f, 1f);
        private static readonly Color InfoColor =
            new(0.35f, 0.57f, 0.79f, 1f);

        private const float SectionSpacing = 8f;
        private const int ValidationDebounceMilliseconds = 140;

        private ConversationTable _table;
        private IReadOnlyList<ConversationValidationIssue> _issues =
            Array.Empty<ConversationValidationIssue>();
        private int _requestedValidationVersion;

        private Label _summaryLabel;
        private ScrollView _issuesScrollView;
        private VisualElement _issuesContainer;

        /// <summary>
        /// Raised when one issue requests editor navigation.
        /// </summary>
        public event Action<ConversationValidationIssue> NavigateRequested;

        /// <summary>
        /// Raised after the current summary changes.
        /// </summary>
        public event Action<ConversationValidationSummary> SummaryChanged;

        /// <summary>
        /// Creates the hosted validation surface.
        /// </summary>
        public ConversationValidationView()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1f;
            style.minHeight = 0f;
            style.marginTop = 6f;

            Add(CreateHeaderSection());
            Add(CreateBodySection());
        }

        /// <summary>
        /// Binds the view to one conversation table.
        /// </summary>
        /// <param name="table">Currently selected table.</param>
        public void BindTable(ConversationTable table)
        {
            _table = table;
            RequestValidation(immediate: true);
        }

        /// <summary>
        /// Requests one validation pass.
        /// </summary>
        /// <param name="immediate">True to run on the next UI tick without debounce.</param>
        public void RequestValidation(bool immediate = false)
        {
            int requestedVersion = ++_requestedValidationVersion;
            schedule.Execute(
                    () =>
                    {
                        if (requestedVersion != _requestedValidationVersion)
                        {
                            return;
                        }

                        RunValidation();
                    })
                .ExecuteLater(immediate ? 0 : ValidationDebounceMilliseconds);
        }

        /// <summary>
        /// Creates the shared validation header.
        /// </summary>
        /// <returns>The configured header section.</returns>
        private VisualElement CreateHeaderSection()
        {
            VisualElement section = CreateSectionContainer();
            section.Add(CreateCaptionLabel("Validation"));
            section.Add(CreateHintLabel(
                "Review setup problems, missing bindings, and graph issues here before you ship or export."));

            VisualElement toolbarRow = new();
            toolbarRow.style.flexDirection = FlexDirection.Row;
            toolbarRow.style.alignItems = Align.Center;
            toolbarRow.style.marginTop = 8f;
            section.Add(toolbarRow);

            _summaryLabel = CreateHintLabel(string.Empty);
            _summaryLabel.style.flexGrow = 1f;
            toolbarRow.Add(_summaryLabel);

            HandyIconButton refreshButton = new(
                () => RequestValidation(immediate: true),
                "Refresh validation now",
                "R",
                "Refresh",
                "d_Refresh",
                "TreeEditor.Refresh",
                "d_TreeEditor.Refresh");
            toolbarRow.Add(refreshButton);
            return section;
        }

        /// <summary>
        /// Creates the scrolling validation-content section.
        /// </summary>
        /// <returns>The configured body section.</returns>
        private VisualElement CreateBodySection()
        {
            VisualElement section = CreateSectionContainer();
            section.style.flexGrow = 1f;
            section.style.minHeight = 0f;

            _issuesScrollView = new ScrollView();
            _issuesScrollView.style.flexGrow = 1f;
            _issuesScrollView.style.minHeight = 0f;
            section.Add(_issuesScrollView);

            _issuesContainer = new VisualElement();
            _issuesContainer.style.flexDirection = FlexDirection.Column;
            _issuesContainer.style.flexGrow = 1f;
            _issuesContainer.style.minHeight = 0f;
            _issuesScrollView.Add(_issuesContainer);
            return section;
        }

        /// <summary>
        /// Executes the current validation pass and rebuilds the rendered issue groups.
        /// </summary>
        private void RunValidation()
        {
            _issues = ConversationTableValidator.Validate(_table);
            ConversationValidationSummary summary =
                ConversationValidationSummary.FromIssues(_issues);
            _summaryLabel.text = BuildSummaryText(summary);
            RebuildIssueGroups(summary);
            SummaryChanged?.Invoke(summary);
        }

        /// <summary>
        /// Rebuilds the grouped issue presentation.
        /// </summary>
        /// <param name="summary">Current validation summary.</param>
        private void RebuildIssueGroups(ConversationValidationSummary summary)
        {
            _issuesContainer.Clear();

            if (_table == null)
            {
                _issuesContainer.Add(CreateStateHelpBox(
                    "Choose a conversation table above to inspect validation results.",
                    HelpBoxMessageType.None));
                return;
            }

            if (!summary.HasIssues)
            {
                _issuesContainer.Add(CreateStateHelpBox(
                    "No validation issues. The current table has no active errors, warnings, or info messages.",
                    HelpBoxMessageType.None));
                return;
            }

            AddSeverityGroup(
                ConversationValidationSeverity.Error,
                "Errors",
                expandedByDefault: true);
            AddSeverityGroup(
                ConversationValidationSeverity.Warning,
                "Warnings",
                expandedByDefault: true);
            AddSeverityGroup(
                ConversationValidationSeverity.Info,
                "Info",
                expandedByDefault: false);
        }

        /// <summary>
        /// Adds one severity group when matching issues exist.
        /// </summary>
        /// <param name="severity">Severity that should be grouped.</param>
        /// <param name="title">Section title for the group.</param>
        /// <param name="expandedByDefault">Whether the group should start expanded.</param>
        private void AddSeverityGroup(
            ConversationValidationSeverity severity,
            string title,
            bool expandedByDefault)
        {
            List<ConversationValidationIssue> groupIssues = new();

            for (int index = 0; index < _issues.Count; index++)
            {
                if (_issues[index].Severity == severity)
                {
                    groupIssues.Add(_issues[index]);
                }
            }

            if (groupIssues.Count == 0)
            {
                return;
            }

            Foldout foldout = new()
            {
                text = $"{title} ({groupIssues.Count})",
                value = expandedByDefault,
            };
            foldout.style.marginTop = SectionSpacing;
            foldout.style.paddingLeft = 4f;
            foldout.style.paddingRight = 4f;
            _issuesContainer.Add(foldout);

            for (int index = 0; index < groupIssues.Count; index++)
            {
                foldout.Add(CreateIssueCard(groupIssues[index]));
            }
        }

        /// <summary>
        /// Creates one issue card with message, context, and actions.
        /// </summary>
        /// <param name="issue">Issue that should be rendered.</param>
        /// <returns>The configured issue card.</returns>
        private VisualElement CreateIssueCard(ConversationValidationIssue issue)
        {
            VisualElement card = new();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.FlexStart;
            card.style.minWidth = 0f;
            card.style.marginTop = 6f;
            card.style.paddingLeft = 10f;
            card.style.paddingRight = 10f;
            card.style.paddingTop = 8f;
            card.style.paddingBottom = 8f;
            card.style.backgroundColor = IssueCardBackgroundColor;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;
            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftColor = BorderColor;
            card.style.borderRightColor = BorderColor;
            card.style.borderTopColor = BorderColor;
            card.style.borderBottomColor = BorderColor;
            card.style.borderTopLeftRadius = 6f;
            card.style.borderTopRightRadius = 6f;
            card.style.borderBottomLeftRadius = 6f;
            card.style.borderBottomRightRadius = 6f;

            VisualElement severityBar = new();
            severityBar.style.width = 4f;
            severityBar.style.minWidth = 4f;
            severityBar.style.alignSelf = Align.Stretch;
            severityBar.style.marginRight = 8f;
            severityBar.style.backgroundColor = ResolveSeverityColor(issue.Severity);
            severityBar.style.borderTopLeftRadius = 4f;
            severityBar.style.borderBottomLeftRadius = 4f;
            card.Add(severityBar);

            VisualElement textColumn = new();
            textColumn.style.flexDirection = FlexDirection.Column;
            textColumn.style.flexGrow = 1f;
            textColumn.style.flexShrink = 1f;
            textColumn.style.minWidth = 0f;
            card.Add(textColumn);

            Label messageLabel = new(issue.Message);
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.flexShrink = 1f;
            textColumn.Add(messageLabel);

            string contextText = BuildIssueContextText(issue);

            if (!string.IsNullOrWhiteSpace(contextText))
            {
                Label contextLabel = CreateHintLabel(contextText);
                contextLabel.style.marginTop = 4f;
                textColumn.Add(contextLabel);
            }

            VisualElement actionsColumn = new();
            actionsColumn.style.flexDirection = FlexDirection.Row;
            actionsColumn.style.alignItems = Align.Center;
            actionsColumn.style.marginLeft = 10f;
            actionsColumn.style.flexShrink = 0f;
            card.Add(actionsColumn);

            if (issue.CanNavigate)
            {
                HandyIconButton navigateButton = new(
                    () => NavigateRequested?.Invoke(issue),
                    "Go to the affected content",
                    ">",
                    "tab_next",
                    "d_tab_next");
                actionsColumn.Add(navigateButton);
            }

            HandyClipboardCopyButton copyButton = new(
                issue.BuildClipboardText,
                "Copy issue details");

            if (issue.CanNavigate)
            {
                copyButton.style.marginLeft = 6f;
            }

            actionsColumn.Add(copyButton);
            return card;
        }

        /// <summary>
        /// Builds the short human-readable summary shown in the header.
        /// </summary>
        /// <param name="summary">Current validation summary.</param>
        /// <returns>The rendered summary text.</returns>
        private string BuildSummaryText(ConversationValidationSummary summary)
        {
            if (_table == null)
            {
                return "Choose a table to inspect validation results.";
            }

            if (!summary.HasIssues)
            {
                return "No active validation issues in this table.";
            }

            return $"{summary.ErrorCount} error(s) • {summary.WarningCount} warning(s) • {summary.InfoCount} info item(s)";
        }

        /// <summary>
        /// Builds one human-readable context line for the provided issue.
        /// </summary>
        /// <param name="issue">Issue that should be described.</param>
        /// <returns>The rendered context text.</returns>
        private string BuildIssueContextText(ConversationValidationIssue issue)
        {
            List<string> parts = new();
            ConversationDefinition conversation = null;

            if (issue.ActorId != SerializableGuid.Empty && _table != null)
            {
                if (_table.TryGetActor(issue.ActorId, out ConversationActorDefinition actor)
                    && actor != null)
                {
                    parts.Add($"Conversant: {BuildActorLabel(actor)}");
                }
                else
                {
                    parts.Add($"Conversant Id: {issue.ActorId.ToHexString()}");
                }
            }

            if (issue.ConversationId != SerializableGuid.Empty
                && _table != null
                && _table.TryGetConversation(issue.ConversationId, out conversation)
                && conversation != null)
            {
                string conversationLabel = string.IsNullOrWhiteSpace(conversation.Title)
                    ? "Conversation"
                    : conversation.Title;
                parts.Add($"Conversation: {conversationLabel}");

                if (issue.NodeId != SerializableGuid.Empty
                    && conversation.Graph.TryGetNode(issue.NodeId, out GraphNodeBase node)
                    && node != null)
                {
                    parts.Add($"Node: {node.DisplayTitle}");
                }
            }

            return string.Join(" • ", parts);
        }

        /// <summary>
        /// Builds one short display label for the provided conversant.
        /// </summary>
        /// <param name="actor">Conversant that should be represented.</param>
        /// <returns>The display label used in the validation context.</returns>
        private static string BuildActorLabel(ConversationActorDefinition actor)
        {
            if (actor == null)
            {
                return "Conversant";
            }

            string displayName = string.IsNullOrWhiteSpace(actor.DisplayName)
                ? actor.Key
                : actor.DisplayName.Trim();

            return string.IsNullOrWhiteSpace(actor.Key)
                ? displayName
                : string.Equals(displayName, actor.Key, StringComparison.OrdinalIgnoreCase)
                    ? displayName
                    : $"{displayName} ({actor.Key})";
        }

        /// <summary>
        /// Resolves the color used for the provided severity indicator.
        /// </summary>
        /// <param name="severity">Severity that should be represented.</param>
        /// <returns>The display color for the severity.</returns>
        private static Color ResolveSeverityColor(ConversationValidationSeverity severity)
        {
            return severity switch
            {
                ConversationValidationSeverity.Error => ErrorColor,
                ConversationValidationSeverity.Warning => WarningColor,
                ConversationValidationSeverity.Info => InfoColor,
                _ => SuccessColor,
            };
        }

        /// <summary>
        /// Creates one boxed empty-state help box.
        /// </summary>
        /// <param name="text">State text.</param>
        /// <param name="messageType">Help-box visual kind.</param>
        /// <returns>The configured help box.</returns>
        private static HelpBox CreateStateHelpBox(string text, HelpBoxMessageType messageType)
        {
            HelpBox helpBox = new(text, messageType);
            helpBox.style.marginTop = SectionSpacing;
            return helpBox;
        }

        /// <summary>
        /// Creates one standard section container.
        /// </summary>
        /// <returns>The styled section container.</returns>
        private static VisualElement CreateSectionContainer()
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft = 10f;
            container.style.paddingRight = 10f;
            container.style.paddingTop = 8f;
            container.style.paddingBottom = 8f;
            container.style.backgroundColor = SectionBackgroundColor;
            container.style.borderBottomWidth = 1f;
            container.style.borderBottomColor = BorderColor;
            return container;
        }

        /// <summary>
        /// Creates one muted section-caption label.
        /// </summary>
        /// <param name="text">Caption text.</param>
        /// <returns>The configured caption label.</returns>
        private static Label CreateCaptionLabel(string text)
        {
            Label label = new(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 10f;
            label.style.color = MutedTextColor;
            return label;
        }

        /// <summary>
        /// Creates one muted hint label.
        /// </summary>
        /// <param name="text">Hint text.</param>
        /// <returns>The configured hint label.</returns>
        private static Label CreateHintLabel(string text)
        {
            Label label = new(text);
            label.style.fontSize = 11f;
            label.style.color = MutedTextColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 1f;
            return label;
        }
    }
}
