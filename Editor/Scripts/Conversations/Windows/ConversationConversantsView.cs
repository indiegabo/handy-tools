using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.Editor;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Hosts the table-level conversant authoring surface used by the Conversations window.
    /// </summary>
    public sealed class ConversationConversantsView : VisualElement
    {
        private const string BaseFieldInputClassName = "unity-base-field__input";
        private static readonly Color SectionBackgroundColor =
            new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color BorderColor =
            new(0.24f, 0.24f, 0.24f, 1f);
        private static readonly Color MutedTextColor =
            new(0.66f, 0.66f, 0.66f, 1f);
        private static readonly Color SelectedRowColor =
            new(0.20f, 0.28f, 0.37f, 1f);
        private static readonly Color SelectedRowBorderColor =
            new(0.33f, 0.52f, 0.68f, 1f);
        private static readonly Color RowBorderColor =
            new(0.20f, 0.20f, 0.20f, 1f);

        private const float ListPaneWidth = 280f;
        private const float DetailMinWidth = 340f;
        private const float ButtonSpacing = 6f;
        private const float FieldSpacing = 10f;
        private const float ColumnSpacing = 16f;
        private const float IconButtonSize = 28f;
        private const float PortraitPanelWidth = 176f;
        private const float PortraitPreviewHeight = 196f;

        private ConversationTable _table;
        private SerializedObject _serializedTableObject;
        private string _selectedActorIdHex;

        private Label _summaryLabel;
        private ScrollView _listScrollView;
        private Button _createButton;
        private Button _duplicateButton;
        private Button _moveUpButton;
        private Button _moveDownButton;
        private Button _detailDeleteButton;
        private Label _detailTitleLabel;
        private Label _detailSubtitleLabel;
        private Label _actorIdValueLabel;
        private HandyClipboardCopyButton _copyActorIdButton;
        private VisualElement _detailContentRoot;
        private VisualElement _detailEmptyStateRoot;
        private Label _detailEmptyStateLabel;
        private VisualElement _validationContainer;
        private VisualElement _usageContainer;
        private Foldout _usageFoldout;
        private VisualElement _detailFormHost;

        /// <summary>
        /// Raised when structural list changes should refresh the parent window state.
        /// </summary>
        public event Action StructureChanged;

        /// <summary>
        /// Raised when non-structural conversant content changes should refresh dependent views.
        /// </summary>
        public event Action ContentChanged;

        /// <summary>
        /// Raised when the selected conversant changes.
        /// </summary>
        public event Action<string> SelectedActorChanged;

        /// <summary>
        /// Creates the hosted conversants surface.
        /// </summary>
        public ConversationConversantsView()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1f;
            style.minHeight = 0f;

            Add(CreateToolbarSection());
            Add(CreateSplitView());
        }

        /// <summary>
        /// Binds the view to one conversation table.
        /// </summary>
        /// <param name="table">Table that owns the conversants.</param>
        /// <param name="selectedActorIdHex">Serialized selected-actor id.</param>
        public void BindTable(ConversationTable table, string selectedActorIdHex)
        {
            if (!ReferenceEquals(_table, table))
            {
                _table = table;
                _serializedTableObject = _table == null
                    ? null
                    : new SerializedObject(_table);
            }

            _selectedActorIdHex = selectedActorIdHex ?? string.Empty;
            _serializedTableObject?.UpdateIfRequiredOrScript();
            ResolveSelectedActorOrFallback();
            Refresh();
        }

        /// <summary>
        /// Refreshes the current view state from the bound table.
        /// </summary>
        public void Refresh()
        {
            _serializedTableObject?.UpdateIfRequiredOrScript();
            RefreshToolbarState();
            RefreshSummary();
            RefreshActorList();
            RefreshActorDetails(rebuildForm: true);
        }

        /// <summary>
        /// Creates the shared toolbar section.
        /// </summary>
        /// <returns>The configured toolbar section.</returns>
        private VisualElement CreateToolbarSection()
        {
            VisualElement section = CreateSectionContainer();
            section.Add(CreateCaptionLabel("Conversants"));
            section.Add(CreateHintLabel(
                "Register the shared cast you want to reuse across this table. Line nodes reference these conversants directly as speaker and listener bindings."));

            VisualElement toolbarRow = new();
            toolbarRow.style.flexDirection = FlexDirection.Row;
            toolbarRow.style.alignItems = Align.Center;
            toolbarRow.style.flexWrap = Wrap.Wrap;
            toolbarRow.style.marginTop = 8f;

            _createButton = CreateIconButton(
                HandleCreateRequested,
                "Create a new conversant",
                "Toolbar Plus",
                "d_Toolbar Plus",
                "CreateAddNew");
            toolbarRow.Add(_createButton);

            _duplicateButton = CreateIconButton(
                HandleDuplicateRequested,
                "Duplicate the selected conversant",
                "TreeEditor.Duplicate",
                "d_TreeEditor.Duplicate",
                "Clipboard");
            _duplicateButton.style.marginLeft = ButtonSpacing;
            toolbarRow.Add(_duplicateButton);

            _moveUpButton = CreateIconButton(
                () => HandleMoveRequested(-1),
                "Move the selected conversant up",
                "ArrowNavigationUp",
                "d_ArrowNavigationUp",
                "tab_prev");
            _moveUpButton.style.marginLeft = ButtonSpacing;
            toolbarRow.Add(_moveUpButton);

            _moveDownButton = CreateIconButton(
                () => HandleMoveRequested(1),
                "Move the selected conversant down",
                "ArrowNavigationDown",
                "d_ArrowNavigationDown",
                "tab_next");
            _moveDownButton.style.marginLeft = ButtonSpacing;
            toolbarRow.Add(_moveDownButton);

            _summaryLabel = CreateHintLabel(string.Empty);
            _summaryLabel.style.marginLeft = 10f;
            _summaryLabel.style.flexGrow = 1f;
            toolbarRow.Add(_summaryLabel);

            section.Add(toolbarRow);
            return section;
        }

        /// <summary>
        /// Creates the split layout used by the list and detail panes.
        /// </summary>
        /// <returns>The configured split view.</returns>
        private TwoPaneSplitView CreateSplitView()
        {
            TwoPaneSplitView splitView = new(
                0,
                (int)ListPaneWidth,
                TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;
            splitView.style.minHeight = 0f;

            splitView.Add(CreateListPane());
            splitView.Add(CreateDetailPane());
            return splitView;
        }

        /// <summary>
        /// Creates the conversant list pane.
        /// </summary>
        /// <returns>The configured list pane.</returns>
        private VisualElement CreateListPane()
        {
            VisualElement pane = CreateSectionContainer();
            pane.style.flexGrow = 1f;
            pane.style.minWidth = ListPaneWidth;
            pane.style.minHeight = 0f;
            pane.style.borderRightWidth = 1f;
            pane.style.borderRightColor = BorderColor;

            _listScrollView = new ScrollView();
            _listScrollView.style.flexGrow = 1f;
            _listScrollView.style.minHeight = 0f;
            pane.Add(_listScrollView);

            return pane;
        }

        /// <summary>
        /// Creates the selected-conversant detail pane.
        /// </summary>
        /// <returns>The configured detail pane.</returns>
        private VisualElement CreateDetailPane()
        {
            VisualElement pane = CreateSectionContainer();
            pane.style.flexGrow = 1f;
            pane.style.minWidth = DetailMinWidth;
            pane.style.minHeight = 0f;

            _detailEmptyStateRoot = new VisualElement();
            _detailEmptyStateRoot.style.flexGrow = 1f;
            _detailEmptyStateRoot.style.minHeight = 0f;
            _detailEmptyStateRoot.style.justifyContent = Justify.Center;
            _detailEmptyStateRoot.style.alignItems = Align.Center;
            _detailEmptyStateRoot.style.paddingLeft = 24f;
            _detailEmptyStateRoot.style.paddingRight = 24f;

            _detailEmptyStateLabel = CreateHintLabel("No conversant selected");
            _detailEmptyStateLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _detailEmptyStateLabel.style.fontSize = 13f;
            _detailEmptyStateLabel.style.maxWidth = 320f;
            _detailEmptyStateRoot.Add(_detailEmptyStateLabel);
            pane.Add(_detailEmptyStateRoot);

            _detailContentRoot = new VisualElement();
            _detailContentRoot.style.flexDirection = FlexDirection.Column;
            _detailContentRoot.style.flexGrow = 1f;
            _detailContentRoot.style.minHeight = 0f;
            _detailContentRoot.style.display = DisplayStyle.None;

            VisualElement detailHeader = new();
            detailHeader.style.flexDirection = FlexDirection.Row;
            detailHeader.style.alignItems = Align.FlexStart;

            VisualElement titleContainer = new();
            titleContainer.style.flexDirection = FlexDirection.Column;
            titleContainer.style.flexGrow = 1f;

            _detailTitleLabel = new Label("No Conversant Selected");
            _detailTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _detailTitleLabel.style.fontSize = 13f;
            titleContainer.Add(_detailTitleLabel);

            _detailSubtitleLabel = CreateHintLabel(string.Empty);
            _detailSubtitleLabel.style.marginTop = 2f;
            titleContainer.Add(_detailSubtitleLabel);
            detailHeader.Add(titleContainer);

            _detailDeleteButton = CreateIconButton(
                HandleDeleteRequested,
                "Delete the selected conversant",
                "TreeEditor.Trash",
                "d_TreeEditor.Trash",
                "Toolbar Minus",
                "d_Toolbar Minus");
            _detailDeleteButton.style.marginLeft = 8f;
            detailHeader.Add(_detailDeleteButton);

            _detailContentRoot.Add(detailHeader);

            VisualElement idContainer = new();
            idContainer.style.marginTop = 8f;
            idContainer.style.flexDirection = FlexDirection.Column;

            VisualElement idHeader = new();
            idHeader.style.flexDirection = FlexDirection.Row;
            idHeader.style.alignItems = Align.Center;
            idContainer.Add(idHeader);

            idHeader.Add(CreateCaptionLabel("ID"));

            Label readOnlyHintLabel = CreateHintLabel("Read-only");
            readOnlyHintLabel.style.marginLeft = 8f;
            idHeader.Add(readOnlyHintLabel);

            VisualElement idRow = new();
            idRow.style.flexDirection = FlexDirection.Row;
            idRow.style.alignItems = Align.Center;
            idRow.style.marginTop = 4f;
            idRow.style.minWidth = 0f;
            idContainer.Add(idRow);

            _actorIdValueLabel = new Label("-");
            _actorIdValueLabel.style.paddingLeft = 8f;
            _actorIdValueLabel.style.paddingRight = 8f;
            _actorIdValueLabel.style.paddingTop = 6f;
            _actorIdValueLabel.style.paddingBottom = 6f;
            _actorIdValueLabel.style.flexGrow = 1f;
            _actorIdValueLabel.style.flexShrink = 1f;
            _actorIdValueLabel.style.minWidth = 0f;
            _actorIdValueLabel.style.whiteSpace = WhiteSpace.NoWrap;
            _actorIdValueLabel.style.overflow = Overflow.Hidden;
            _actorIdValueLabel.style.textOverflow = TextOverflow.Ellipsis;
            _actorIdValueLabel.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f, 1f);
            _actorIdValueLabel.style.borderLeftWidth = 1f;
            _actorIdValueLabel.style.borderRightWidth = 1f;
            _actorIdValueLabel.style.borderTopWidth = 1f;
            _actorIdValueLabel.style.borderBottomWidth = 1f;
            _actorIdValueLabel.style.borderLeftColor = BorderColor;
            _actorIdValueLabel.style.borderRightColor = BorderColor;
            _actorIdValueLabel.style.borderTopColor = BorderColor;
            _actorIdValueLabel.style.borderBottomColor = BorderColor;
            _actorIdValueLabel.style.borderTopLeftRadius = 4f;
            _actorIdValueLabel.style.borderTopRightRadius = 4f;
            _actorIdValueLabel.style.borderBottomLeftRadius = 4f;
            _actorIdValueLabel.style.borderBottomRightRadius = 4f;
            _actorIdValueLabel.tooltip =
                "This stable conversant id is generated automatically and cannot be edited here.";
            idRow.Add(_actorIdValueLabel);

            _copyActorIdButton = new HandyClipboardCopyButton(
                () => ResolveSelectedActorOrFallback()?.ActorId.ToHexString() ?? string.Empty,
                "Copy conversant ID to clipboard");
            _copyActorIdButton.style.marginLeft = ButtonSpacing;
            idRow.Add(_copyActorIdButton);

            _detailContentRoot.Add(idContainer);

            _validationContainer = new VisualElement();
            _validationContainer.style.marginTop = 8f;
            _validationContainer.style.display = DisplayStyle.None;
            _detailContentRoot.Add(_validationContainer);

            _detailFormHost = new VisualElement();
            _detailFormHost.style.marginTop = 12f;
            _detailContentRoot.Add(_detailFormHost);

            _usageFoldout = new Foldout
            {
                text = "Used In",
                value = false,
            };
            _usageFoldout.style.marginTop = 16f;

            _usageContainer = new VisualElement();
            _usageContainer.style.marginTop = 4f;
            _usageFoldout.Add(_usageContainer);
            _detailContentRoot.Add(_usageFoldout);

            pane.Add(_detailContentRoot);

            return pane;
        }

        /// <summary>
        /// Refreshes the current toolbar button state.
        /// </summary>
        private void RefreshToolbarState()
        {
            ConversationActorDefinition selectedActor = ResolveSelectedActorOrFallback();
            bool hasTable = _table != null;
            bool hasSelection = selectedActor != null;
            int selectedIndex = GetSelectedActorIndex();
            int actorCount = _table?.Actors?.Count ?? 0;

            _createButton?.SetEnabled(hasTable);
            _duplicateButton?.SetEnabled(hasSelection);
            _moveUpButton?.SetEnabled(hasSelection && selectedIndex > 0);
            _moveDownButton?.SetEnabled(hasSelection && selectedIndex >= 0
                && selectedIndex < actorCount - 1);
            _detailDeleteButton?.SetEnabled(hasSelection);
        }

        /// <summary>
        /// Refreshes the summary shown in the toolbar.
        /// </summary>
        private void RefreshSummary()
        {
            if (_summaryLabel == null)
            {
                return;
            }

            if (_table == null)
            {
                _summaryLabel.text =
                    "Choose a conversation table above to start adding conversants.";
                return;
            }

            int actorCount = _table.Actors?.Count ?? 0;
            _summaryLabel.text = actorCount == 1
                ? "1 conversant in this table"
                : $"{actorCount} conversants in this table";
        }

        /// <summary>
        /// Rebuilds the conversant list.
        /// </summary>
        private void RefreshActorList()
        {
            if (_listScrollView == null)
            {
                return;
            }

            _listScrollView.Clear();

            if (_table == null)
            {
                _listScrollView.Add(CreateStateLabel(
                    "Choose a conversation table above to start filling your shared cast."));
                return;
            }

            if (_table.Actors == null || _table.Actors.Count == 0)
            {
                _listScrollView.Add(CreateStateLabel(
                    "No conversants yet. Create the first one here and reuse it across your conversations."));
                return;
            }

            SerializableGuid selectedActorId = GetSelectedActorId();

            for (int index = 0; index < _table.Actors.Count; index++)
            {
                ConversationActorDefinition actor = _table.Actors[index];

                if (actor == null)
                {
                    continue;
                }

                _listScrollView.Add(CreateActorListRow(
                    actor,
                    actor.ActorId == selectedActorId));
            }
        }

        /// <summary>
        /// Creates one conversant row for the list pane.
        /// </summary>
        /// <param name="actor">Conversant represented by the row.</param>
        /// <param name="isSelected">Whether the row is currently selected.</param>
        /// <returns>The configured row.</returns>
        private VisualElement CreateActorListRow(
            ConversationActorDefinition actor,
            bool isSelected)
        {
            List<ActorUsageRecord> usage = BuildActorUsage(actor.ActorId);
            int conversationCount = CountDistinctConversationUsage(usage);

            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Column;
            row.style.minWidth = 0f;
            row.style.paddingLeft = 10f;
            row.style.paddingRight = 10f;
            row.style.paddingTop = 8f;
            row.style.paddingBottom = 8f;
            row.style.marginBottom = 4f;
            row.style.borderLeftWidth = 1f;
            row.style.borderRightWidth = 1f;
            row.style.borderTopWidth = 1f;
            row.style.borderBottomWidth = 1f;
            row.style.borderLeftColor = isSelected ? SelectedRowBorderColor : RowBorderColor;
            row.style.borderRightColor = isSelected ? SelectedRowBorderColor : RowBorderColor;
            row.style.borderTopColor = isSelected ? SelectedRowBorderColor : RowBorderColor;
            row.style.borderBottomColor = isSelected ? SelectedRowBorderColor : RowBorderColor;
            row.style.backgroundColor = isSelected ? SelectedRowColor : Color.clear;
            row.style.borderTopLeftRadius = 6f;
            row.style.borderTopRightRadius = 6f;
            row.style.borderBottomLeftRadius = 6f;
            row.style.borderBottomRightRadius = 6f;

            row.AddManipulator(new Clickable(() => HandleActorSelectionRequested(actor.ActorId)));

            Label titleLabel = new(string.IsNullOrWhiteSpace(actor.DisplayName)
                ? actor.Key
                : actor.DisplayName.Trim());
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 12f;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            titleLabel.style.flexShrink = 1f;
            row.Add(titleLabel);

            Label subtitleLabel = new($"@{actor.Key}");
            subtitleLabel.style.color = MutedTextColor;
            subtitleLabel.style.fontSize = 11f;
            subtitleLabel.style.marginTop = 2f;
            subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
            subtitleLabel.style.flexShrink = 1f;
            row.Add(subtitleLabel);

            string usageLabel = conversationCount == 1
                ? "Used in 1 conversation"
                : $"Used in {conversationCount} conversations";

            Label footerLabel = new(usageLabel);
            footerLabel.style.color = MutedTextColor;
            footerLabel.style.fontSize = 10f;
            footerLabel.style.marginTop = 4f;
            footerLabel.style.whiteSpace = WhiteSpace.Normal;
            footerLabel.style.flexShrink = 1f;
            row.Add(footerLabel);

            return row;
        }

        /// <summary>
        /// Refreshes the selected-conversant detail pane.
        /// </summary>
        /// <param name="rebuildForm">Whether the detail form should be rebuilt from scratch.</param>
        private void RefreshActorDetails(bool rebuildForm)
        {
            ConversationActorDefinition actor = ResolveSelectedActorOrFallback();
            SerializedProperty actorProperty = GetSelectedActorProperty();

            if (_detailTitleLabel == null
                || _detailSubtitleLabel == null
                || _actorIdValueLabel == null
                || _copyActorIdButton == null
                || _detailContentRoot == null
                || _detailEmptyStateRoot == null
                || _detailEmptyStateLabel == null
                || _validationContainer == null
                || _detailFormHost == null
                || _usageFoldout == null
                || _usageContainer == null)
            {
                return;
            }

            if (_table == null)
            {
                ShowDetailEmptyState("Choose a conversation table.");
                return;
            }

            if (actor == null || actorProperty == null)
            {
                ShowDetailEmptyState("No conversant selected");
                return;
            }

            _detailEmptyStateRoot.style.display = DisplayStyle.None;
            _detailContentRoot.style.display = DisplayStyle.Flex;

            _detailTitleLabel.text = string.IsNullOrWhiteSpace(actor.DisplayName)
                ? actor.Key
                : actor.DisplayName.Trim();
            _detailSubtitleLabel.text =
                "Fill the data you want this conversant to carry across the conversations in your table.";
            _actorIdValueLabel.text = actor.ActorId.ToHexString();
            _copyActorIdButton.RefreshState();

            RefreshValidationMessages(actor, actorProperty);

            if (rebuildForm)
            {
                RebuildDetailForm(actorProperty);
            }

            RefreshUsageList(actor.ActorId);
        }

        /// <summary>
        /// Shows one centered detail-pane empty state and hides the authored conversant form.
        /// </summary>
        /// <param name="message">State message rendered in the detail pane.</param>
        private void ShowDetailEmptyState(string message)
        {
            _detailEmptyStateLabel.text = string.IsNullOrWhiteSpace(message)
                ? "No conversant selected"
                : message;
            _detailEmptyStateRoot.style.display = DisplayStyle.Flex;
            _detailContentRoot.style.display = DisplayStyle.None;
            _actorIdValueLabel.text = "-";
            _copyActorIdButton.RefreshState();
            _validationContainer.Clear();
            _validationContainer.style.display = DisplayStyle.None;
            _detailFormHost.Clear();
            _usageContainer.Clear();
            _usageFoldout.SetEnabled(false);
            _usageFoldout.text = "Used In";
        }

        /// <summary>
        /// Rebuilds the editable detail form for the selected conversant.
        /// </summary>
        /// <param name="actorProperty">Serialized conversant property.</param>
        private void RebuildDetailForm(SerializedProperty actorProperty)
        {
            _detailFormHost.Clear();

            VisualElement formLayout = new();
            formLayout.style.flexDirection = FlexDirection.Row;
            formLayout.style.alignItems = Align.FlexStart;
            formLayout.style.flexWrap = Wrap.NoWrap;
            formLayout.style.minWidth = 0f;

            VisualElement leftColumn = new();
            leftColumn.style.flexDirection = FlexDirection.Column;
            leftColumn.style.flexGrow = 1f;
            leftColumn.style.flexShrink = 1f;
            leftColumn.style.flexBasis = 0f;
            leftColumn.style.minWidth = 0f;

            leftColumn.Add(CreateHintedPropertyField(
                actorProperty.FindPropertyRelative("_key"),
                "Key",
                "Use lowercase letters, numbers, '-' or '_'. Spaces become '-' and uppercase letters are converted automatically."));
            leftColumn.Add(CreateBoundPropertyField(
                actorProperty.FindPropertyRelative("_displayName"),
                "Display Name"));
            leftColumn.Add(CreateBoundPropertyField(
                actorProperty.FindPropertyRelative("_themeColor"),
                "Color"));
            leftColumn.Add(CreateBoundPropertyField(
                actorProperty.FindPropertyRelative("_notes"),
                "Notes"));
            formLayout.Add(leftColumn);

            VisualElement portraitColumn = CreatePortraitColumn(
                actorProperty.FindPropertyRelative("_portrait"));
            portraitColumn.style.marginLeft = ColumnSpacing;
            portraitColumn.style.alignSelf = Align.FlexStart;
            formLayout.Add(portraitColumn);

            _detailFormHost.Add(formLayout);
        }

        /// <summary>
        /// Creates the portrait authoring column.
        /// </summary>
        /// <param name="portraitProperty">Serialized portrait property.</param>
        /// <returns>The configured portrait column.</returns>
        private VisualElement CreatePortraitColumn(SerializedProperty portraitProperty)
        {
            VisualElement column = new();
            column.style.flexDirection = FlexDirection.Column;
            column.style.width = PortraitPanelWidth;
            column.style.minWidth = PortraitPanelWidth;
            column.style.flexShrink = 0f;

            column.Add(CreateCaptionLabel("Sprite"));

            VisualElement previewFrame = new();
            previewFrame.style.height = PortraitPreviewHeight;
            previewFrame.style.marginTop = 4f;
            previewFrame.style.flexShrink = 0f;
            previewFrame.style.justifyContent = Justify.Center;
            previewFrame.style.alignItems = Align.Center;
            previewFrame.style.paddingLeft = 8f;
            previewFrame.style.paddingRight = 8f;
            previewFrame.style.paddingTop = 8f;
            previewFrame.style.paddingBottom = 8f;
            previewFrame.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f, 1f);
            previewFrame.style.borderLeftWidth = 1f;
            previewFrame.style.borderRightWidth = 1f;
            previewFrame.style.borderTopWidth = 1f;
            previewFrame.style.borderBottomWidth = 1f;
            previewFrame.style.borderLeftColor = BorderColor;
            previewFrame.style.borderRightColor = BorderColor;
            previewFrame.style.borderTopColor = BorderColor;
            previewFrame.style.borderBottomColor = BorderColor;
            previewFrame.style.borderTopLeftRadius = 6f;
            previewFrame.style.borderTopRightRadius = 6f;
            previewFrame.style.borderBottomLeftRadius = 6f;
            previewFrame.style.borderBottomRightRadius = 6f;
            previewFrame.tooltip = "Drop a sprite here or use the picker below.";

            Label emptyLabel = CreateHintLabel("Drop or pick a sprite");
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.whiteSpace = WhiteSpace.Normal;
            emptyLabel.pickingMode = PickingMode.Ignore;
            previewFrame.Add(emptyLabel);

            Image portraitPreview = new()
            {
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };
            portraitPreview.style.width = Length.Percent(100f);
            portraitPreview.style.height = Length.Percent(100f);
            previewFrame.Add(portraitPreview);

            ObjectField portraitField = null;
            Button clearPortraitButton = null;

            previewFrame.RegisterCallback<DragUpdatedEvent>(
                evt => HandlePortraitDragUpdated(evt),
                TrickleDown.TrickleDown);
            previewFrame.RegisterCallback<DragPerformEvent>(
                evt => HandlePortraitDragPerform(
                    evt,
                    portraitProperty,
                    portraitField,
                    portraitPreview,
                    emptyLabel,
                    clearPortraitButton),
                TrickleDown.TrickleDown);
            column.Add(previewFrame);

            VisualElement actionRow = new();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            actionRow.style.marginTop = FieldSpacing;
            actionRow.style.width = Length.Percent(100f);
            actionRow.style.minWidth = 0f;
            actionRow.style.overflow = Overflow.Hidden;
            column.Add(actionRow);

            portraitField = new()
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                value = portraitProperty?.objectReferenceValue,
            };
            portraitField.style.width = 0f;
            portraitField.style.flexBasis = 0f;
            portraitField.style.flexGrow = 1f;
            portraitField.style.flexShrink = 1f;
            portraitField.style.minWidth = 0f;
            portraitField.style.overflow = Overflow.Hidden;
            ConstrainObjectFieldLayout(portraitField);
            actionRow.Add(portraitField);

            clearPortraitButton = CreateIconButton(
                () => HandlePortraitChanged(
                    portraitProperty,
                    portraitField,
                    portraitPreview,
                    emptyLabel,
                    clearPortraitButton,
                    null),
                "Remove the current sprite",
                "TreeEditor.Trash",
                "d_TreeEditor.Trash",
                "Toolbar Minus",
                "d_Toolbar Minus");
            clearPortraitButton.style.marginLeft = ButtonSpacing;
            clearPortraitButton.style.flexShrink = 0f;
            actionRow.Add(clearPortraitButton);

            portraitField.RegisterValueChangedCallback(
                evt => HandlePortraitChanged(
                    portraitProperty,
                    portraitField,
                    portraitPreview,
                    emptyLabel,
                    clearPortraitButton,
                    evt.newValue as Sprite));

            RefreshPortraitPreview(
                portraitPreview,
                emptyLabel,
                portraitProperty?.objectReferenceValue as Sprite);
            clearPortraitButton.SetEnabled(portraitProperty?.objectReferenceValue != null);
            return column;
        }

        /// <summary>
        /// Reacts to one drag update over the portrait preview.
        /// </summary>
        /// <param name="evt">Drag payload.</param>
        private static void HandlePortraitDragUpdated(DragUpdatedEvent evt)
        {
            if (!TryResolveDraggedSprite(out _))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.StopImmediatePropagation();
        }

        /// <summary>
        /// Applies one dropped sprite into the portrait field.
        /// </summary>
        /// <param name="evt">Drag payload.</param>
        /// <param name="portraitProperty">Serialized portrait property.</param>
        /// <param name="portraitField">Bound portrait object field.</param>
        /// <param name="portraitPreview">Preview image.</param>
        /// <param name="emptyLabel">Empty-state label.</param>
        /// <param name="clearPortraitButton">Clear button.</param>
        private void HandlePortraitDragPerform(
            DragPerformEvent evt,
            SerializedProperty portraitProperty,
            ObjectField portraitField,
            Image portraitPreview,
            Label emptyLabel,
            Button clearPortraitButton)
        {
            if (!TryResolveDraggedSprite(out Sprite sprite))
            {
                return;
            }

            DragAndDrop.AcceptDrag();
            evt.StopImmediatePropagation();
            HandlePortraitChanged(
                portraitProperty,
                portraitField,
                portraitPreview,
                emptyLabel,
                clearPortraitButton,
                sprite);
        }

        /// <summary>
        /// Stores one portrait change and refreshes the preview UI.
        /// </summary>
        /// <param name="portraitProperty">Serialized portrait property.</param>
        /// <param name="portraitField">Bound portrait object field.</param>
        /// <param name="portraitPreview">Preview image.</param>
        /// <param name="emptyLabel">Empty-state label.</param>
        /// <param name="clearPortraitButton">Clear button.</param>
        /// <param name="sprite">New portrait sprite.</param>
        private void HandlePortraitChanged(
            SerializedProperty portraitProperty,
            ObjectField portraitField,
            Image portraitPreview,
            Label emptyLabel,
            Button clearPortraitButton,
            Sprite sprite)
        {
            if (_table == null
                || _serializedTableObject == null
                || portraitProperty == null)
            {
                return;
            }

            Undo.RecordObject(
                _table,
                sprite == null ? "Remove Conversant Portrait" : "Change Conversant Portrait");
            portraitProperty.objectReferenceValue = sprite;
            _serializedTableObject.ApplyModifiedProperties();
            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);
            _serializedTableObject.UpdateIfRequiredOrScript();

            portraitField.SetValueWithoutNotify(sprite);
            RefreshPortraitPreview(portraitPreview, emptyLabel, sprite);
            clearPortraitButton?.SetEnabled(sprite != null);
            ContentChanged?.Invoke();
        }

        /// <summary>
        /// Refreshes the portrait preview visuals.
        /// </summary>
        /// <param name="portraitPreview">Preview image.</param>
        /// <param name="emptyLabel">Empty-state label.</param>
        /// <param name="sprite">Current sprite.</param>
        private static void RefreshPortraitPreview(
            Image portraitPreview,
            Label emptyLabel,
            Sprite sprite)
        {
            if (portraitPreview == null || emptyLabel == null)
            {
                return;
            }

            portraitPreview.sprite = sprite;
            portraitPreview.style.display = sprite == null
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            emptyLabel.style.display = sprite == null
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>
        /// Attempts to resolve one sprite from the current drag payload.
        /// </summary>
        /// <param name="sprite">Resolved sprite when found.</param>
        /// <returns>True when a valid sprite is available.</returns>
        private static bool TryResolveDraggedSprite(out Sprite sprite)
        {
            sprite = null;

            if (DragAndDrop.objectReferences == null
                || DragAndDrop.objectReferences.Length <= 0)
            {
                return false;
            }

            UnityEngine.Object candidate = DragAndDrop.objectReferences[0];

            if (candidate is Sprite directSprite)
            {
                sprite = directSprite;
                return true;
            }

            string assetPath = AssetDatabase.GetAssetPath(candidate);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite assetSprite)
                {
                    sprite = assetSprite;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Constrains one object field so long asset names do not break the layout.
        /// </summary>
        /// <param name="field">Object field that should be constrained.</param>
        private static void ConstrainObjectFieldLayout(ObjectField field)
        {
            if (field == null)
            {
                return;
            }

            VisualElement inputElement = field.Q(className: BaseFieldInputClassName);

            if (inputElement != null)
            {
                inputElement.style.marginLeft = 0f;
                inputElement.style.flexGrow = 1f;
                inputElement.style.flexShrink = 1f;
                inputElement.style.minWidth = 0f;
                inputElement.style.overflow = Overflow.Hidden;
            }

            List<TextElement> textElements = field.Query<TextElement>().ToList();

            for (int index = 0; index < textElements.Count; index++)
            {
                TextElement textElement = textElements[index];

                if (textElement == null)
                {
                    continue;
                }

                textElement.style.minWidth = 0f;
                textElement.style.flexShrink = 1f;
                textElement.style.whiteSpace = WhiteSpace.NoWrap;
                textElement.style.overflow = Overflow.Hidden;
                textElement.style.textOverflow = TextOverflow.Ellipsis;
            }
        }

        /// <summary>
        /// Refreshes inline validation messages for the selected conversant.
        /// </summary>
        /// <param name="actor">Selected conversant.</param>
        /// <param name="actorProperty">Serialized conversant property.</param>
        private void RefreshValidationMessages(
            ConversationActorDefinition actor,
            SerializedProperty actorProperty)
        {
            _validationContainer.Clear();

            if (actor == null || actorProperty == null)
            {
                _validationContainer.style.display = DisplayStyle.None;
                return;
            }

            if (HasDuplicateActorId(actor.ActorId))
            {
                _validationContainer.Add(new HelpBox(
                    "This conversant shares the same stable id with another record in the table.",
                    HelpBoxMessageType.Error));
            }

            if (HasDuplicateActorKey(actor.Key, actor.ActorId))
            {
                _validationContainer.Add(new HelpBox(
                    "This conversant key collides with another record in the table.",
                    HelpBoxMessageType.Error));
            }

            if (string.IsNullOrWhiteSpace(actor.DisplayName))
            {
                _validationContainer.Add(new HelpBox(
                    "Display Name is empty. Runtime presenters may have to fall back to the key.",
                    HelpBoxMessageType.Warning));
            }

            SerializedProperty portraitProperty = actorProperty.FindPropertyRelative("_portrait");

            if (HasMissingObjectReference(portraitProperty))
            {
                _validationContainer.Add(new HelpBox(
                    "Portrait points to a missing asset reference.",
                    HelpBoxMessageType.Warning));
            }

            List<ActorUsageRecord> usage = BuildActorUsage(actor.ActorId);

            if (usage.Count == 0)
            {
                _validationContainer.Add(new HelpBox(
                    "This conversant is currently unused by authored Line nodes.",
                    HelpBoxMessageType.Info));
            }

            _validationContainer.style.display = _validationContainer.childCount > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>
        /// Refreshes the usage list for the selected conversant.
        /// </summary>
        /// <param name="actorId">Selected conversant identifier.</param>
        private void RefreshUsageList(SerializableGuid actorId)
        {
            _usageContainer.Clear();
            _usageFoldout.SetEnabled(actorId != SerializableGuid.Empty);

            if (actorId == SerializableGuid.Empty)
            {
                return;
            }

            List<ActorUsageRecord> usage = BuildActorUsage(actorId);
            _usageFoldout.text = usage.Count == 1
                ? "Used In (1)"
                : $"Used In ({usage.Count})";

            if (usage.Count == 0)
            {
                _usageContainer.Add(CreateStateLabel(
                    "This conversant is not used in any Line node yet."));
                return;
            }

            for (int index = 0; index < usage.Count; index++)
            {
                ActorUsageRecord record = usage[index];
                VisualElement row = new();
                row.style.flexDirection = FlexDirection.Column;
                row.style.paddingTop = 4f;
                row.style.paddingBottom = 4f;

                Label conversationLabel = new(record.ConversationPath);
                conversationLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                row.Add(conversationLabel);

                Label bindingLabel = new($"Binding: {record.RoleLabel}");
                bindingLabel.style.color = MutedTextColor;
                bindingLabel.style.fontSize = 11f;
                row.Add(bindingLabel);

                Label previewLabel = new(record.LinePreview);
                previewLabel.style.color = MutedTextColor;
                previewLabel.style.fontSize = 11f;
                previewLabel.style.whiteSpace = WhiteSpace.Normal;
                row.Add(previewLabel);

                _usageContainer.Add(row);
            }
        }

        /// <summary>
        /// Handles one conversant selection request from the list.
        /// </summary>
        /// <param name="actorId">Selected conversant identifier.</param>
        private void HandleActorSelectionRequested(SerializableGuid actorId)
        {
            SetSelectedActor(actorId);
            Refresh();
        }

        /// <summary>
        /// Creates one new conversant in the bound table.
        /// </summary>
        private void HandleCreateRequested()
        {
            if (_table == null)
            {
                return;
            }

            Undo.RecordObject(_table, "Create Conversant");
            ConversationActorDefinition createdActor = _table.CreateActor();
            CommitStructuralChange(createdActor?.ActorId ?? SerializableGuid.Empty);
        }

        /// <summary>
        /// Duplicates the selected conversant.
        /// </summary>
        private void HandleDuplicateRequested()
        {
            if (_table == null)
            {
                return;
            }

            Undo.RecordObject(_table, "Duplicate Conversant");
            ConversationActorDefinition duplicate = _table.DuplicateActor(GetSelectedActorId());
            CommitStructuralChange(duplicate?.ActorId ?? GetSelectedActorId());
        }

        /// <summary>
        /// Deletes the selected conversant.
        /// </summary>
        private void HandleDeleteRequested()
        {
            if (_table == null
                || !_table.TryGetActorIndex(GetSelectedActorId(), out int actorIndex)
                || !_table.TryGetActor(GetSelectedActorId(), out ConversationActorDefinition actor))
            {
                return;
            }

            List<ActorUsageRecord> usage = BuildActorUsage(actor.ActorId);

            if (usage.Count > 0)
            {
                bool shouldDelete = EditorUtility.DisplayDialog(
                    "Delete Conversant",
                    $"'{actor.Key}' is still used in {usage.Count} line binding(s). Delete it anyway?",
                    "Delete Anyway",
                    "Cancel");

                if (!shouldDelete)
                {
                    return;
                }
            }

            Undo.RecordObject(_table, "Delete Conversant");

            if (!_table.RemoveActor(actor.ActorId))
            {
                return;
            }

            SerializableGuid fallbackActorId = SerializableGuid.Empty;

            if (_table.Actors.Count > 0)
            {
                int fallbackIndex = Mathf.Clamp(actorIndex, 0, _table.Actors.Count - 1);
                fallbackActorId = _table.Actors[fallbackIndex]?.ActorId ?? SerializableGuid.Empty;
            }

            CommitStructuralChange(fallbackActorId);
        }

        /// <summary>
        /// Moves the selected conversant within the shared cast ordering.
        /// </summary>
        /// <param name="direction">Requested list movement direction.</param>
        private void HandleMoveRequested(int direction)
        {
            if (_table == null || _serializedTableObject == null)
            {
                return;
            }

            SerializedProperty actorsProperty = _serializedTableObject.FindProperty("_actors");

            if (actorsProperty == null
                || !_table.TryGetActorIndex(GetSelectedActorId(), out int actorIndex))
            {
                return;
            }

            int targetIndex = Mathf.Clamp(actorIndex + direction, 0, actorsProperty.arraySize - 1);

            if (targetIndex == actorIndex)
            {
                return;
            }

            Undo.RecordObject(
                _table,
                direction < 0 ? "Move Conversant Up" : "Move Conversant Down");
            actorsProperty.MoveArrayElement(actorIndex, targetIndex);
            _serializedTableObject.ApplyModifiedProperties();
            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);
            _serializedTableObject.UpdateIfRequiredOrScript();
            Refresh();
            StructureChanged?.Invoke();
        }

        /// <summary>
        /// Reacts to one serialized conversant property change.
        /// </summary>
        /// <param name="evt">Serialized property change payload.</param>
        private void HandleActorPropertyChanged(SerializedPropertyChangeEvent evt)
        {
            if (_table == null || _serializedTableObject == null)
            {
                return;
            }

            if (evt?.changedProperty != null
                && string.Equals(evt.changedProperty.name, "_key", StringComparison.Ordinal))
            {
                string normalizedKey = ConversationActorDefinition.NormalizeKey(
                    evt.changedProperty.stringValue,
                    fallbackToDefault: false);

                if (!string.Equals(
                        evt.changedProperty.stringValue,
                        normalizedKey,
                        StringComparison.Ordinal))
                {
                    evt.changedProperty.stringValue = normalizedKey;
                }
            }

            _serializedTableObject.ApplyModifiedProperties();
            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);
            _serializedTableObject.UpdateIfRequiredOrScript();
            RefreshSummary();
            RefreshActorList();
            RefreshActorDetails(rebuildForm: false);
            ContentChanged?.Invoke();
        }

        /// <summary>
        /// Finalizes one structural conversant change.
        /// </summary>
        /// <param name="selectedActorId">Conversant that should remain selected.</param>
        private void CommitStructuralChange(SerializableGuid selectedActorId)
        {
            _table?.EnsureAuthoringIds();

            if (_table != null)
            {
                EditorUtility.SetDirty(_table);
            }

            _serializedTableObject = _table == null
                ? null
                : new SerializedObject(_table);

            SetSelectedActor(selectedActorId);
            Refresh();
            StructureChanged?.Invoke();
        }

        /// <summary>
        /// Gets the currently selected conversant index.
        /// </summary>
        /// <returns>The current list index, or -1 when no conversant is selected.</returns>
        private int GetSelectedActorIndex()
        {
            return _table != null && _table.TryGetActorIndex(GetSelectedActorId(), out int index)
                ? index
                : -1;
        }

        /// <summary>
        /// Gets the serialized property of the selected conversant.
        /// </summary>
        /// <returns>The serialized property when found; otherwise null.</returns>
        private SerializedProperty GetSelectedActorProperty()
        {
            if (_table == null
                || _serializedTableObject == null
                || !_table.TryGetActorIndex(GetSelectedActorId(), out int index))
            {
                return null;
            }

            SerializedProperty actorsProperty = _serializedTableObject.FindProperty("_actors");

            if (actorsProperty == null || index < 0 || index >= actorsProperty.arraySize)
            {
                return null;
            }

            return actorsProperty.GetArrayElementAtIndex(index);
        }

        /// <summary>
        /// Resolves the selected conversant or falls back to the first table entry.
        /// </summary>
        /// <returns>The resolved conversant when found.</returns>
        private ConversationActorDefinition ResolveSelectedActorOrFallback()
        {
            if (_table == null)
            {
                SetSelectedActor(SerializableGuid.Empty);
                return null;
            }

            if (_table.TryGetActor(GetSelectedActorId(), out ConversationActorDefinition actor))
            {
                return actor;
            }

            if (_table.Actors != null && _table.Actors.Count > 0)
            {
                ConversationActorDefinition firstActor = _table.Actors[0];
                SetSelectedActor(firstActor?.ActorId ?? SerializableGuid.Empty);
                return firstActor;
            }

            SetSelectedActor(SerializableGuid.Empty);
            return null;
        }

        /// <summary>
        /// Gets the selected conversant id.
        /// </summary>
        /// <returns>The selected conversant id, or empty when none is selected.</returns>
        private SerializableGuid GetSelectedActorId()
        {
            return string.IsNullOrWhiteSpace(_selectedActorIdHex)
                ? SerializableGuid.Empty
                : SerializableGuid.FromHexString(_selectedActorIdHex);
        }

        /// <summary>
        /// Stores one selected conversant id and notifies the host window.
        /// </summary>
        /// <param name="actorId">Selected conversant identifier.</param>
        private void SetSelectedActor(SerializableGuid actorId)
        {
            string actorIdHex = actorId == SerializableGuid.Empty
                ? string.Empty
                : actorId.ToHexString();

            if (string.Equals(_selectedActorIdHex, actorIdHex, StringComparison.Ordinal))
            {
                return;
            }

            _selectedActorIdHex = actorIdHex;
            SelectedActorChanged?.Invoke(_selectedActorIdHex);
        }

        /// <summary>
        /// Builds all authored line-node usages for the provided conversant id.
        /// </summary>
        /// <param name="actorId">Conversant identifier that should be inspected.</param>
        /// <returns>The resolved usage records.</returns>
        private List<ActorUsageRecord> BuildActorUsage(SerializableGuid actorId)
        {
            List<ActorUsageRecord> usage = new();

            if (_table == null || actorId == SerializableGuid.Empty)
            {
                return usage;
            }

            IReadOnlyList<ConversationDefinition> conversations = _table.Conversations;

            for (int conversationIndex = 0; conversationIndex < conversations.Count; conversationIndex++)
            {
                ConversationDefinition conversation = conversations[conversationIndex];

                if (conversation?.Graph?.Nodes == null)
                {
                    continue;
                }

                string conversationLabel = string.IsNullOrWhiteSpace(conversation.Title)
                    ? "Conversation"
                    : conversation.Title;

                for (int nodeIndex = 0; nodeIndex < conversation.Graph.Nodes.Count; nodeIndex++)
                {
                    if (conversation.Graph.Nodes[nodeIndex] is not ConversationLineNode lineNode)
                    {
                        continue;
                    }

                    if (lineNode.SpeakerActorId == actorId)
                    {
                        usage.Add(new ActorUsageRecord(
                            conversationLabel,
                            "Speaker",
                            BuildLinePreview(lineNode)));
                    }

                    if (lineNode.ListenerActorId == actorId)
                    {
                        usage.Add(new ActorUsageRecord(
                            conversationLabel,
                            "Listener",
                            BuildLinePreview(lineNode)));
                    }
                }
            }

            return usage;
        }

        /// <summary>
        /// Counts how many distinct conversations appear in the provided usage set.
        /// </summary>
        /// <param name="usage">Usage records that should be counted.</param>
        /// <returns>The distinct conversation count.</returns>
        private static int CountDistinctConversationUsage(IReadOnlyList<ActorUsageRecord> usage)
        {
            if (usage == null || usage.Count == 0)
            {
                return 0;
            }

            HashSet<string> conversationPaths = new(StringComparer.Ordinal);

            for (int index = 0; index < usage.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(usage[index].ConversationPath))
                {
                    conversationPaths.Add(usage[index].ConversationPath);
                }
            }

            return conversationPaths.Count;
        }

        /// <summary>
        /// Gets whether the provided conversant id collides with another table entry.
        /// </summary>
        /// <param name="actorId">Conversant id that should be checked.</param>
        /// <returns>True when another table entry shares the same id.</returns>
        private bool HasDuplicateActorId(SerializableGuid actorId)
        {
            if (_table == null || actorId == SerializableGuid.Empty)
            {
                return false;
            }

            int matches = 0;

            for (int index = 0; index < _table.Actors.Count; index++)
            {
                if (_table.Actors[index] != null && _table.Actors[index].ActorId == actorId)
                {
                    matches++;
                }
            }

            return matches > 1;
        }

        /// <summary>
        /// Gets whether the provided conversant key collides with another table entry.
        /// </summary>
        /// <param name="key">Conversant key that should be checked.</param>
        /// <param name="actorId">Current conversant identifier.</param>
        /// <returns>True when another table entry shares the same key.</returns>
        private bool HasDuplicateActorKey(string key, SerializableGuid actorId)
        {
            if (_table == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            for (int index = 0; index < _table.Actors.Count; index++)
            {
                ConversationActorDefinition candidate = _table.Actors[index];

                if (candidate == null || candidate.ActorId == actorId)
                {
                    continue;
                }

                if (string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets whether one serialized object property points to a missing object reference.
        /// </summary>
        /// <param name="property">Serialized property that should be inspected.</param>
        /// <returns>True when the property points to one missing asset.</returns>
        private static bool HasMissingObjectReference(SerializedProperty property)
        {
            if (property == null || property.objectReferenceValue != null)
            {
                return false;
            }

#pragma warning disable CS0618
            return property.objectReferenceInstanceIDValue != 0;
#pragma warning restore CS0618
        }

        /// <summary>
        /// Creates one compact icon button.
        /// </summary>
        /// <param name="onClick">Action executed when the button is clicked.</param>
        /// <param name="tooltip">Tooltip shown by the button.</param>
        /// <param name="iconNames">Candidate icon names resolved through Unity editor resources.</param>
        /// <returns>The configured button.</returns>
        private static Button CreateIconButton(
            Action onClick,
            string tooltip,
            params string[] iconNames)
        {
            Button button = new(onClick)
            {
                tooltip = tooltip ?? string.Empty,
            };

            button.text = string.Empty;
            button.style.width = IconButtonSize;
            button.style.minWidth = IconButtonSize;
            button.style.height = IconButtonSize;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;

            Texture2D icon = LoadIcon(iconNames);

            if (icon != null)
            {
                Image image = new()
                {
                    image = icon,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore,
                };

                image.style.width = 16f;
                image.style.height = 16f;
                button.Add(image);
            }

            return button;
        }

        /// <summary>
        /// Resolves one editor icon from the provided candidate names.
        /// </summary>
        /// <param name="iconNames">Candidate icon names.</param>
        /// <returns>The first resolved icon texture.</returns>
        private static Texture2D LoadIcon(params string[] iconNames)
        {
            for (int index = 0; index < iconNames.Length; index++)
            {
                string iconName = iconNames[index];

                if (string.IsNullOrWhiteSpace(iconName))
                {
                    continue;
                }

                Texture2D texture = EditorGUIUtility.FindTexture(iconName);

                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates one bound property field.
        /// </summary>
        /// <param name="property">Serialized property that should be rendered.</param>
        /// <param name="label">Display label shown by the field.</param>
        /// <returns>The configured property field.</returns>
        private PropertyField CreateBoundPropertyField(
            SerializedProperty property,
            string label)
        {
            PropertyField field = new()
            {
                label = label,
            };
            field.style.marginTop = FieldSpacing;
            field.BindProperty(property.Copy());
            field.RegisterCallback<SerializedPropertyChangeEvent>(HandleActorPropertyChanged);
            return field;
        }

        /// <summary>
        /// Creates one property field with an inline hint message.
        /// </summary>
        /// <param name="property">Serialized property that should be rendered.</param>
        /// <param name="label">Display label shown by the field.</param>
        /// <param name="hint">Hint text rendered above the field.</param>
        /// <returns>The configured field container.</returns>
        private VisualElement CreateHintedPropertyField(
            SerializedProperty property,
            string label,
            string hint)
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;
            container.style.marginTop = FieldSpacing;
            container.style.minWidth = 0f;

            Label hintLabel = CreateHintLabel(hint);
            hintLabel.style.marginBottom = 4f;
            container.Add(hintLabel);

            PropertyField field = new()
            {
                label = label,
            };
            field.BindProperty(property.Copy());
            field.RegisterCallback<SerializedPropertyChangeEvent>(HandleActorPropertyChanged);
            container.Add(field);
            return container;
        }

        /// <summary>
        /// Creates one section container used by the surface panels.
        /// </summary>
        /// <returns>The configured section container.</returns>
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
        /// Creates one section caption label.
        /// </summary>
        /// <param name="text">Caption text.</param>
        /// <returns>The configured label.</returns>
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
        /// <returns>The configured label.</returns>
        private static Label CreateHintLabel(string text)
        {
            Label label = new(text);
            label.style.fontSize = 11f;
            label.style.color = MutedTextColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 1f;
            return label;
        }

        /// <summary>
        /// Creates one empty-state label.
        /// </summary>
        /// <param name="text">State text.</param>
        /// <returns>The configured label.</returns>
        private static Label CreateStateLabel(string text)
        {
            Label label = CreateHintLabel(text);
            label.style.marginTop = 8f;
            return label;
        }

        /// <summary>
        /// Builds one readable preview for the provided dialogue line.
        /// </summary>
        /// <param name="lineNode">Dialogue line that should be summarized.</param>
        /// <returns>The rendered line preview.</returns>
        private static string BuildLinePreview(ConversationLineNode lineNode)
        {
            if (lineNode == null)
            {
                return "Line";
            }

            string preview = string.IsNullOrWhiteSpace(lineNode.LineText)
                ? lineNode.GetSummary()
                : lineNode.LineText.Trim();

            if (string.IsNullOrWhiteSpace(preview))
            {
                return "Empty line";
            }

            return preview.Length <= 72 ? preview : preview[..69] + "...";
        }

        /// <summary>
        /// Stores one authored conversant usage record.
        /// </summary>
        private readonly struct ActorUsageRecord
        {
            /// <summary>
            /// Creates one usage record describing where a conversant is referenced.
            /// </summary>
            /// <param name="conversationPath">Conversation path that owns the usage.</param>
            /// <param name="roleLabel">Speaker or listener role label.</param>
            /// <param name="linePreview">Short preview of the referenced line.</param>
            public ActorUsageRecord(
                string conversationPath,
                string roleLabel,
                string linePreview)
            {
                ConversationPath = conversationPath ?? string.Empty;
                RoleLabel = roleLabel ?? string.Empty;
                LinePreview = linePreview ?? string.Empty;
            }

            /// <summary>
            /// Gets the authored conversation path that references the conversant.
            /// </summary>
            public string ConversationPath { get; }

            /// <summary>
            /// Gets the role label associated with the usage.
            /// </summary>
            public string RoleLabel { get; }

            /// <summary>
            /// Gets the line preview associated with the usage.
            /// </summary>
            public string LinePreview { get; }
        }
    }
}
