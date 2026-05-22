using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.Editor;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using RuntimeGraphCore = IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Hosts the Conversations node inspector inside the graph window.
    /// </summary>
    public sealed class ConversationGraphInspectorView : VisualElement
    {
        private readonly struct ActorOption
        {
            /// <summary>
            /// Stores one conversant option rendered in a binding picker.
            /// </summary>
            /// <param name="actorId">Conversant identifier represented by the option.</param>
            /// <param name="displayName">Primary display name rendered by the option.</param>
            /// <param name="key">Secondary key or detail line rendered by the option.</param>
            /// <param name="portrait">Optional portrait preview rendered by the option.</param>
            /// <param name="themeColor">Theme color used by placeholder rendering when no portrait exists.</param>
            /// <param name="isUnassigned">Whether the option represents the explicit unassigned state.</param>
            /// <param name="isMissing">Whether the option represents one missing conversant binding.</param>
            public ActorOption(
                SerializableGuid actorId,
                string displayName,
                string key,
                Sprite portrait,
                Color themeColor,
                bool isUnassigned = false,
                bool isMissing = false)
            {
                ActorId = actorId;
                DisplayName = displayName ?? string.Empty;
                Key = key ?? string.Empty;
                Portrait = portrait;
                ThemeColor = themeColor;
                IsUnassigned = isUnassigned;
                IsMissing = isMissing;
            }

            /// <summary>
            /// Gets the conversant identifier represented by the option.
            /// </summary>
            public SerializableGuid ActorId { get; }

            /// <summary>
            /// Gets the primary display name rendered by the option.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Gets the secondary key or detail line rendered by the option.
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// Gets the optional portrait preview rendered by the option.
            /// </summary>
            public Sprite Portrait { get; }

            /// <summary>
            /// Gets the theme color used by placeholder rendering when no portrait exists.
            /// </summary>
            public Color ThemeColor { get; }

            /// <summary>
            /// Gets whether the option represents the explicit unassigned state.
            /// </summary>
            public bool IsUnassigned { get; }

            /// <summary>
            /// Gets whether the option represents one missing conversant binding.
            /// </summary>
            public bool IsMissing { get; }

            /// <summary>
            /// Gets the primary title rendered by the card.
            /// </summary>
            public string PrimaryText => string.IsNullOrWhiteSpace(DisplayName)
                ? (string.IsNullOrWhiteSpace(Key) ? "Conversant" : Key)
                : DisplayName.Trim();

            /// <summary>
            /// Gets the secondary detail line rendered by the card.
            /// </summary>
            public string SecondaryText
            {
                get
                {
                    if (IsUnassigned)
                    {
                        return "No conversant selected";
                    }

                    if (IsMissing)
                    {
                        return string.IsNullOrWhiteSpace(Key) ? "Missing binding" : Key;
                    }

                    return string.IsNullOrWhiteSpace(Key)
                        ? "No key"
                        : $"@{Key}";
                }
            }

            /// <summary>
            /// Checks whether the option matches the provided search query.
            /// </summary>
            /// <param name="searchQuery">Search query entered in the popup.</param>
            /// <returns>True when the option should remain visible.</returns>
            public bool MatchesSearchQuery(string searchQuery)
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    return true;
                }

                string normalizedQuery = searchQuery.Trim();
                return PrimaryText.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0
                    || DisplayName.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0
                    || Key.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0
                    || SecondaryText.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary>
        /// Renders the popup picker used by speaker and listener bindings.
        /// </summary>
        private sealed class ActorBindingPopupContent : PopupWindowContent
        {
            private const float WindowWidth = 320f;
            private const float MaxWindowHeight = 280f;
            private const float PopupPadding = 4f;
            private const float RowHeight = 48f;
            private const float RowSpacing = 4f;
            private const float SearchFieldHeight = 20f;
            private const float SearchFieldHorizontalPadding = 6f;
            private const float SearchFieldVerticalPadding = 4f;
            private const float SearchFieldSpacing = 6f;
            private const string SearchFieldControlName =
                "ConversationActorBindingPopupSearchField";

            private readonly IReadOnlyList<ActorOption> _options;
            private readonly SerializableGuid _selectedActorId;
            private readonly Action<SerializableGuid> _onSelected;
            private readonly List<ActorOption> _filteredOptions = new();

            private Vector2 _scrollPosition;
            private string _searchQuery = string.Empty;
            private bool _focusSearchField = true;

            /// <summary>
            /// Creates one popup picker content instance.
            /// </summary>
            /// <param name="options">Options that should be displayed by the popup.</param>
            /// <param name="selectedActorId">Currently selected conversant id.</param>
            /// <param name="onSelected">Callback invoked after one option is chosen.</param>
            public ActorBindingPopupContent(
                IReadOnlyList<ActorOption> options,
                SerializableGuid selectedActorId,
                Action<SerializableGuid> onSelected)
            {
                _options = options ?? Array.Empty<ActorOption>();
                _selectedActorId = selectedActorId;
                _onSelected = onSelected;
            }

            /// <summary>
            /// Gets the popup window size.
            /// </summary>
            /// <returns>The popup window size.</returns>
            public override Vector2 GetWindowSize()
            {
                float listContentHeight = (_options.Count * RowHeight)
                    + (Mathf.Max(0, _options.Count - 1) * RowSpacing)
                    + PopupPadding;
                float contentHeight = (PopupPadding * 2f)
                    + (SearchFieldVerticalPadding * 2f)
                    + SearchFieldHeight
                    + SearchFieldSpacing
                    + listContentHeight;
                return new Vector2(
                    WindowWidth,
                    Mathf.Clamp(
                        contentHeight,
                        (PopupPadding * 2f)
                            + (SearchFieldVerticalPadding * 2f)
                            + SearchFieldHeight
                            + SearchFieldSpacing
                            + RowHeight,
                        MaxWindowHeight));
            }

            /// <summary>
            /// Reacts when the popup window becomes visible.
            /// </summary>
            public override void OnOpen()
            {
                _focusSearchField = true;
            }

            /// <summary>
            /// Draws the popup content.
            /// </summary>
            /// <param name="rect">Popup content rect.</param>
            public override void OnGUI(Rect rect)
            {
                if (_options.Count <= 0)
                {
                    EditorGUI.HelpBox(rect, "No conversants available.", MessageType.Info);
                    return;
                }

                Rect searchRect = new(
                    PopupPadding + SearchFieldHorizontalPadding,
                    PopupPadding + SearchFieldVerticalPadding,
                    Mathf.Max(
                        0f,
                        rect.width
                            - (PopupPadding * 2f)
                            - (SearchFieldHorizontalPadding * 2f)),
                    SearchFieldHeight);
                DrawSearchField(searchRect);

                IReadOnlyList<ActorOption> visibleOptions = BuildVisibleOptions();
                Rect listRect = new(
                    0f,
                    searchRect.yMax + SearchFieldVerticalPadding + SearchFieldSpacing,
                    rect.width,
                    Mathf.Max(
                        0f,
                        rect.height
                            - searchRect.yMax
                            - SearchFieldVerticalPadding
                            - SearchFieldSpacing));

                if (visibleOptions.Count <= 0)
                {
                    EditorGUI.HelpBox(listRect, "No conversants match this search.", MessageType.Info);
                    return;
                }

                float contentHeight = (visibleOptions.Count * RowHeight)
                    + (Mathf.Max(0, visibleOptions.Count - 1) * RowSpacing);
                float scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth > 0f
                    ? GUI.skin.verticalScrollbar.fixedWidth
                    : 16f;
                bool needsVerticalScroll = contentHeight > listRect.height;
                Rect viewRect = new(
                    0f,
                    0f,
                    Mathf.Max(0f, listRect.width - (needsVerticalScroll ? scrollbarWidth : 0f)),
                    contentHeight);

                _scrollPosition = GUI.BeginScrollView(listRect, _scrollPosition, viewRect);

                for (int index = 0; index < visibleOptions.Count; index++)
                {
                    ActorOption option = visibleOptions[index];
                    Rect rowRect = new(
                        0f,
                        index * (RowHeight + RowSpacing),
                        viewRect.width,
                        RowHeight);
                    bool isHovered = rowRect.Contains(Event.current.mousePosition);
                    bool isSelected = option.ActorId == _selectedActorId;

                    DrawActorOptionCard(
                        rowRect,
                        option,
                        isSelected,
                        isHovered,
                        showDropdownIndicator: false);

                    if (!GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                    {
                        continue;
                    }

                    _onSelected?.Invoke(option.ActorId);
                    editorWindow?.Close();
                    GUIUtility.ExitGUI();
                }

                GUI.EndScrollView();
            }

            /// <summary>
            /// Draws the search box used to filter popup options.
            /// </summary>
            /// <param name="rect">Search box rect.</param>
            private void DrawSearchField(Rect rect)
            {
                GUI.SetNextControlName(SearchFieldControlName);
                string nextSearchQuery = EditorGUI.TextField(rect, _searchQuery ?? string.Empty);

                if (!string.Equals(nextSearchQuery, _searchQuery, StringComparison.Ordinal))
                {
                    _searchQuery = nextSearchQuery ?? string.Empty;
                    _scrollPosition = Vector2.zero;
                }

                if (_focusSearchField && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.FocusTextInControl(SearchFieldControlName);
                    _focusSearchField = false;
                }

                if (!string.IsNullOrWhiteSpace(_searchQuery))
                {
                    return;
                }

                GUI.Label(
                    new Rect(rect.x + 4f, rect.y + 1f, rect.width - 8f, rect.height),
                    "Search by name or key",
                    CreateSearchPlaceholderStyle());
            }

            /// <summary>
            /// Builds the filtered list of visible conversant options.
            /// </summary>
            /// <returns>The options that match the current search query.</returns>
            private IReadOnlyList<ActorOption> BuildVisibleOptions()
            {
                if (string.IsNullOrWhiteSpace(_searchQuery))
                {
                    return _options;
                }

                _filteredOptions.Clear();

                for (int index = 0; index < _options.Count; index++)
                {
                    ActorOption option = _options[index];

                    if (option.MatchesSearchQuery(_searchQuery))
                    {
                        _filteredOptions.Add(option);
                    }
                }

                return _filteredOptions;
            }
        }

        private readonly struct DragRelayField
        {
            /// <summary>
            /// Stores one field that can receive a relayed blackboard drop.
            /// </summary>
            /// <param name="field">Hovered field element.</param>
            /// <param name="propertyPath">Serialized property path represented by the field.</param>
            public DragRelayField(PropertyField field, string propertyPath)
            {
                Field = field;
                PropertyPath = propertyPath;
            }

            /// <summary>
            /// Gets the field element rendered in the inspector.
            /// </summary>
            public PropertyField Field { get; }

            /// <summary>
            /// Gets the serialized property path represented by the field.
            /// </summary>
            public string PropertyPath { get; }
        }

        private readonly ScrollView _content;
        private readonly List<DragRelayField> _dragRelayFields = new();

        private ConversationTable _table;
        private ConversationDefinition _conversation;
        private SerializableGuid _selectedNodeId;
        private bool _isRefreshingInspector;
        private bool _hasPendingInspectorChangedNotification;

        /// <summary>
        /// Raised after one inspector field change mutates the authored graph.
        /// </summary>
        public event Action<SerializableGuid> InspectorChanged;

        /// <summary>
        /// Creates the hosted UI Toolkit hierarchy.
        /// </summary>
        public ConversationGraphInspectorView()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1f;
            style.minWidth = 0f;

            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.style.flexGrow = 1f;
            _content.style.minWidth = 0f;
            _content.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(_content);
        }

        /// <summary>
        /// Binds the inspector to the current table and selected node.
        /// </summary>
        /// <param name="table">Conversation table that owns the graph.</param>
        /// <param name="conversation">Currently selected authored conversation.</param>
        /// <param name="selectedNodeId">Currently selected node id.</param>
        public void BindSelection(
            ConversationTable table,
            ConversationDefinition conversation,
            SerializableGuid selectedNodeId)
        {
            _table = table;
            _conversation = conversation;
            _selectedNodeId = selectedNodeId;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the inspector content from the current selection.
        /// </summary>
        public void Refresh()
        {
            _content.Unbind();
            _content.Clear();
            _dragRelayFields.Clear();

            if (_table == null)
            {
                _content.Add(new HelpBox(
                    "Choose a conversation table above to start editing your conversations.",
                    HelpBoxMessageType.Info));
                return;
            }

            if (_conversation == null)
            {
                BuildTableSummary();
                return;
            }

            if (_selectedNodeId == SerializableGuid.Empty)
            {
                BuildConversationSummary();
                return;
            }

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();
            SerializedProperty nodeProperty = FindNodeProperty(
                serializedTable,
                _table,
                _conversation,
                _selectedNodeId);

            if (nodeProperty == null)
            {
                _selectedNodeId = SerializableGuid.Empty;
                BuildConversationSummary();
                return;
            }

            _isRefreshingInspector = true;

            try
            {
                BuildSelectedNodeInspector(nodeProperty.Copy());
                _content.Bind(serializedTable);
            }
            finally
            {
                _isRefreshingInspector = false;
            }
        }

        /// <summary>
        /// Attempts to complete one active blackboard drag session over the hovered field.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        /// <returns>True when one hovered field accepted the binding.</returns>
        internal bool TryPerformBlackboardSessionDrop(Vector2 mousePosition)
        {
            if (!worldBound.Contains(mousePosition)
                || !TryGetHoveredRelayProperty(mousePosition, out SerializedProperty property)
                || !GraphBlackboardBindingDrawerUtility.TryHandleUISessionPerform(property))
            {
                return false;
            }

            HandleRelayedMutation();
            return true;
        }

        /// <summary>
        /// Checks whether the hovered field can currently accept the active blackboard drag.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        /// <returns>True when one hovered field can accept the session drop.</returns>
        internal bool CanAcceptBlackboardSessionDrop(Vector2 mousePosition)
        {
            return worldBound.Contains(mousePosition)
                && TryGetHoveredRelayProperty(mousePosition, out SerializedProperty property)
                && GraphBlackboardBindingDrawerUtility.TryHandleUISessionUpdated(property);
        }

        /// <summary>
        /// Builds the summary shown when no node is selected.
        /// </summary>
        private void BuildTableSummary()
        {
            _content.Add(CreateHeaderLabel(_table.DisplayName));
            _content.Add(CreateValueRow(
                "Conversations",
                _table.Conversations?.Count.ToString() ?? "0"));
            _content.Add(CreateValueRow(
                "Conversants",
                CountActors().ToString()));
            _content.Add(new HelpBox(
                "Choose a conversation above to edit its flow here. Use the Conversants tab for the shared cast and the Input tab for table-level advance, cancel, and skip actions.",
                HelpBoxMessageType.Info));
        }

        /// <summary>
        /// Builds the summary shown when one conversation is selected but no node is selected.
        /// </summary>
        private void BuildConversationSummary()
        {
            _content.Add(CreateHeaderLabel(_conversation.Title));
            _content.Add(CreateValueRow(
                "Available Conversants",
                (_table?.Actors?.Count ?? 0).ToString()));
            _content.Add(new HelpBox(
                "Choose a node in the graph to edit its details here.",
                HelpBoxMessageType.Info));
        }

        /// <summary>
        /// Builds the selected node inspector fields.
        /// </summary>
        /// <param name="nodeProperty">Serialized property for the selected node.</param>
        private void BuildSelectedNodeInspector(SerializedProperty nodeProperty)
        {
            RuntimeGraphCore.GraphNodeBase selectedNode = null;

            if (_conversation.Graph.TryGetNode(
                _selectedNodeId,
                out RuntimeGraphCore.GraphNodeBase node))
            {
                selectedNode = node;
                _content.Add(CreateHeaderLabel(node.DisplayTitle));

                bool isTextNode = node is ConversationLineNode
                    || node is ConversationNarrationLineNode;
                string summary = isTextNode ? string.Empty : node.GetSummary();

                if (!string.IsNullOrWhiteSpace(summary))
                {
                    _content.Add(new HelpBox(summary, HelpBoxMessageType.None));
                }
            }

            if (selectedNode is ConversationLineNode spokenLineNode)
            {
                BuildSpokenLineNodeInspector(nodeProperty.Copy(), spokenLineNode);
                return;
            }

            if (selectedNode is ConversationNarrationLineNode narrationLineNode)
            {
                BuildNarrationLineNodeInspector(nodeProperty.Copy(), narrationLineNode);
                return;
            }

            VisualElement propertiesBody = AddInspectorSection("Properties");
            SerializedProperty iterator = nodeProperty.Copy();
            SerializedProperty endProperty = nodeProperty.GetEndProperty();
            int childDepth = nodeProperty.depth + 1;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren)
                && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;

                if (iterator.depth != childDepth)
                {
                    continue;
                }

                if (string.Equals(iterator.name, "_position", StringComparison.Ordinal))
                {
                    continue;
                }

                AddPropertyField(iterator.Copy(), propertiesBody);
            }
        }

        /// <summary>
        /// Builds the dedicated inspector layout used by spoken-line nodes.
        /// </summary>
        /// <param name="nodeProperty">Serialized property for the selected node.</param>
        /// <param name="lineNode">Concrete spoken-line node being edited.</param>
        private void BuildSpokenLineNodeInspector(
            SerializedProperty nodeProperty,
            ConversationLineNode lineNode)
        {
            SerializedProperty lineTextProperty = nodeProperty.FindPropertyRelative("_lineText");
            SerializedProperty speakerProperty = nodeProperty.FindPropertyRelative(
                "_speakerActorId");
            SerializedProperty listenerProperty = nodeProperty.FindPropertyRelative(
                "_listenerActorId");
            SerializedProperty speakerSlotProperty = nodeProperty.FindPropertyRelative(
                "_speakerSlot");
            SerializedProperty listenerSlotProperty = nodeProperty.FindPropertyRelative(
                "_listenerSlot");

            VisualElement identityBody = AddInspectorSection("Identity");
            AddReadOnlyTextIdField(identityBody);

            VisualElement participantsBody = AddInspectorSection("Participants");

            if (_table?.Actors == null || _table.Actors.Count == 0)
            {
                participantsBody.Add(new HelpBox(
                    "This table does not have conversants yet. Open the Conversants tab to create speaker and listener entries before wiring this spoken line.",
                    HelpBoxMessageType.Info));
            }

            VisualElement speakerBody = CreateInspectorGroup(
                participantsBody,
                "Speaker");
            AddActorBindingField(
                "Speaker",
                speakerProperty,
                allowUnassigned: true,
                missingLabel: "Missing Speaker Binding",
                speakerBody);
            AddPropertyField(
                speakerSlotProperty,
                speakerBody,
                "Presenter Slot",
                "Controls which presenter side displays the speaker. Auto keeps the speaker on the left.");

            VisualElement listenerBody = CreateInspectorGroup(
                participantsBody,
                "Listener");
            AddActorBindingField(
                "Listener",
                listenerProperty,
                allowUnassigned: true,
                missingLabel: "Missing Listener Binding",
                listenerBody);
            AddPropertyField(
                listenerSlotProperty,
                listenerBody,
                "Presenter Slot",
                "Controls which presenter side displays the listener. Auto keeps the listener on the right.");

            if (lineNode.SpeakerActorId != SerializableGuid.Empty
                && lineNode.ListenerActorId != SerializableGuid.Empty
                && lineNode.ResolvedSpeakerSlot == lineNode.ResolvedListenerSlot)
            {
                participantsBody.Add(new HelpBox(
                    "Both conversants currently resolve to the same presenter side. Only one participant can occupy each side at runtime, so conflicts prioritize the speaker on the left slot and the listener on the right slot.",
                    HelpBoxMessageType.Warning));
            }

            VisualElement valuesBody = AddInspectorSection("Values");
            AddLineTextField(lineTextProperty, "Text", valuesBody);
        }

        /// <summary>
        /// Builds the dedicated inspector layout used by narration-line nodes.
        /// </summary>
        /// <param name="nodeProperty">Serialized property for the selected node.</param>
        /// <param name="lineNode">Concrete narration-line node being edited.</param>
        private void BuildNarrationLineNodeInspector(
            SerializedProperty nodeProperty,
            ConversationNarrationLineNode lineNode)
        {
            _ = lineNode;

            VisualElement identityBody = AddInspectorSection("Identity");
            AddReadOnlyTextIdField(identityBody);

            VisualElement valuesBody = AddInspectorSection("Values");
            valuesBody.Add(new HelpBox(
                "Narration lines present text without speaker or listener character composition.",
                HelpBoxMessageType.Info));
            AddLineTextField(
                nodeProperty.FindPropertyRelative("_lineText"),
                "Text",
                valuesBody);
        }

        /// <summary>
        /// Adds the read-only text identifier field used by authored text nodes.
        /// </summary>
        private void AddReadOnlyTextIdField(VisualElement parent)
        {
            if (parent == null
                || _conversation == null
                || _selectedNodeId == SerializableGuid.Empty)
            {
                return;
            }

            string textId = ConversationTextIdUtility.Build(
                _conversation.ConversationId,
                _selectedNodeId);
            VisualElement textIdRow = new();
            textIdRow.style.flexDirection = FlexDirection.Row;
            textIdRow.style.alignItems = Align.Center;
            textIdRow.style.marginBottom = 4f;
            textIdRow.style.minWidth = 0f;

            Label textIdValueLabel = new(textId);
            textIdValueLabel.style.paddingLeft = 8f;
            textIdValueLabel.style.paddingRight = 8f;
            textIdValueLabel.style.paddingTop = 6f;
            textIdValueLabel.style.paddingBottom = 6f;
            textIdValueLabel.style.flexGrow = 1f;
            textIdValueLabel.style.flexShrink = 1f;
            textIdValueLabel.style.minWidth = 0f;
            textIdValueLabel.style.whiteSpace = WhiteSpace.NoWrap;
            textIdValueLabel.style.overflow = Overflow.Hidden;
            textIdValueLabel.style.textOverflow = TextOverflow.Ellipsis;
            textIdValueLabel.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f, 1f);
            textIdValueLabel.style.borderLeftWidth = 1f;
            textIdValueLabel.style.borderRightWidth = 1f;
            textIdValueLabel.style.borderTopWidth = 1f;
            textIdValueLabel.style.borderBottomWidth = 1f;
            textIdValueLabel.style.borderLeftColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            textIdValueLabel.style.borderRightColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            textIdValueLabel.style.borderTopColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            textIdValueLabel.style.borderBottomColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            textIdValueLabel.style.borderTopLeftRadius = 4f;
            textIdValueLabel.style.borderTopRightRadius = 4f;
            textIdValueLabel.style.borderBottomLeftRadius = 4f;
            textIdValueLabel.style.borderBottomRightRadius = 4f;
            textIdValueLabel.tooltip =
                "Stable authored text identifier reserved for future localization lookup.";

            HandyClipboardCopyButton copyButton = new(
                () => textId,
                "Copy text ID to clipboard");
            copyButton.style.marginLeft = 6f;

            textIdRow.Add(textIdValueLabel);
            textIdRow.Add(copyButton);
            parent.Add(textIdRow);
        }

        /// <summary>
        /// Adds the shared text field used by presentable line nodes.
        /// </summary>
        /// <param name="property">Serialized text property.</param>
        /// <param name="label">Displayed field label.</param>
        private void AddLineTextField(
            SerializedProperty property,
            string label,
            VisualElement parent)
        {
            if (property == null || parent == null)
            {
                return;
            }

            string propertyPath = property.propertyPath;
            VisualElement fieldContainer = new();
            fieldContainer.style.flexDirection = FlexDirection.Column;
            fieldContainer.style.minWidth = 0f;
            fieldContainer.style.marginBottom = 4f;

            Label fieldLabel = new(label);
            fieldLabel.style.marginBottom = 2f;
            fieldContainer.Add(fieldLabel);

            TextField lineTextField = new()
            {
                multiline = true,
            };
            lineTextField.SetValueWithoutNotify(property.stringValue);
            lineTextField.style.minHeight = 72f;
            lineTextField.style.flexGrow = 1f;
            lineTextField.style.minWidth = 0f;
            lineTextField.labelElement.style.display = DisplayStyle.None;
            lineTextField.RegisterValueChangedCallback(
                evt =>
                {
                    if (string.Equals(
                        evt.previousValue,
                        evt.newValue,
                        StringComparison.Ordinal))
                    {
                        return;
                    }

                    ApplyLineTextValue(propertyPath, evt.newValue);
                });
            fieldContainer.Add(lineTextField);
            parent.Add(fieldContainer);
        }

        /// <summary>
        /// Applies one edited line-text value back into the serialized node property.
        /// </summary>
        /// <param name="propertyPath">Serialized property path that should be updated.</param>
        /// <param name="value">Edited authored line text.</param>
        private void ApplyLineTextValue(string propertyPath, string value)
        {
            if (_table == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();

            SerializedProperty property = serializedTable.FindProperty(propertyPath);

            if (property == null
                || property.propertyType != SerializedPropertyType.String
                || string.Equals(property.stringValue, value, StringComparison.Ordinal))
            {
                return;
            }

            property.stringValue = value ?? string.Empty;
            serializedTable.ApplyModifiedProperties();
            HandleRelayedMutation();
        }

        /// <summary>
        /// Adds one bound property field to the inspector and tracks it as a drag relay target.
        /// </summary>
        /// <param name="property">Property that should be displayed.</param>
        private void AddPropertyField(
            SerializedProperty property,
            VisualElement parent,
            string labelOverride = null,
            string tooltip = null)
        {
            if (property == null || parent == null)
            {
                return;
            }

            PropertyField field = string.IsNullOrWhiteSpace(labelOverride)
                ? new PropertyField(property)
                : new PropertyField(property, labelOverride);
            field.tooltip = tooltip ?? string.Empty;
            field.style.marginBottom = 6f;
            field.RegisterCallback<SerializedPropertyChangeEvent>(HandleSerializedPropertyChanged);
            _dragRelayFields.Add(new DragRelayField(field, property.propertyPath));
            parent.Add(field);
        }

        /// <summary>
        /// Adds one bound property field to the root inspector content.
        /// </summary>
        /// <param name="property">Property that should be displayed.</param>
        private void AddPropertyField(SerializedProperty property)
        {
            AddPropertyField(property, _content);
        }

        /// <summary>
        /// Reacts to one property mutation produced by the UI Toolkit inspector.
        /// </summary>
        /// <param name="evt">Serialized property change payload.</param>
        private void HandleSerializedPropertyChanged(SerializedPropertyChangeEvent evt)
        {
            HandleRelayedMutation();
        }

        /// <summary>
        /// Finalizes one relayed inspector mutation and notifies the host window.
        /// </summary>
        private void HandleRelayedMutation()
        {
            if (_table == null || _isRefreshingInspector)
            {
                return;
            }

            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);

            if (_hasPendingInspectorChangedNotification)
            {
                return;
            }

            _hasPendingInspectorChangedNotification = true;
            EditorApplication.delayCall += DispatchInspectorChanged;
        }

        /// <summary>
        /// Dispatches one deferred inspector-changed notification after the active UI event completes.
        /// </summary>
        private void DispatchInspectorChanged()
        {
            EditorApplication.delayCall -= DispatchInspectorChanged;
            _hasPendingInspectorChangedNotification = false;

            if (_table == null)
            {
                return;
            }

            InspectorChanged?.Invoke(_selectedNodeId);
        }

        /// <summary>
        /// Adds one conversant dropdown used by the line-node inspector.
        /// </summary>
        /// <param name="label">Displayed field label.</param>
        /// <param name="property">Serialized actor-id property.</param>
        /// <param name="allowUnassigned">Whether an explicit unassigned option should be available.</param>
        /// <param name="missingLabel">Label used when the current binding points to a missing slot.</param>
        private void AddActorBindingField(
            string label,
            SerializedProperty property,
            bool allowUnassigned,
            string missingLabel,
            VisualElement parent)
        {
            if (property == null || parent == null)
            {
                return;
            }

            string propertyPath = property.propertyPath;
            string tooltip = label == "Speaker"
                ? "Required conversant that speaks the spoken line."
                : "Optional conversant that listens to the spoken line.";
            IMGUIContainer field = new(() => DrawActorBindingField(
                label,
                propertyPath,
                allowUnassigned,
                missingLabel,
                tooltip));
            field.tooltip = tooltip;
            field.style.height = 44f;
            field.style.marginBottom = 4f;
            parent.Add(field);
        }

        /// <summary>
        /// Adds one styled section to the inspector and returns the body container.
        /// </summary>
        /// <param name="title">Section caption.</param>
        /// <param name="hint">Optional section helper text.</param>
        /// <returns>The section body container.</returns>
        private VisualElement AddInspectorSection(string title)
        {
            VisualElement section = CreateInspectorSection(title, out VisualElement body);
            _content.Add(section);
            return body;
        }

        /// <summary>
        /// Creates one nested inspector group and returns its body container.
        /// </summary>
        /// <param name="parent">Parent element that should receive the group.</param>
        /// <param name="title">Group caption.</param>
        /// <param name="hint">Optional helper text.</param>
        /// <returns>The nested group body container.</returns>
        private static VisualElement CreateInspectorGroup(
            VisualElement parent,
            string title)
        {
            VisualElement group = CreateInspectorGroup(title, out VisualElement body);
            parent?.Add(group);
            return body;
        }

        /// <summary>
        /// Draws one actor-binding picker row.
        /// </summary>
        /// <param name="label">Displayed field label.</param>
        /// <param name="propertyPath">Serialized property path of the bound actor id.</param>
        /// <param name="allowUnassigned">Whether one explicit unassigned option should be shown.</param>
        /// <param name="missingLabel">Label used when the current binding points to one missing actor.</param>
        /// <param name="tooltip">Tooltip shown on the field.</param>
        private void DrawActorBindingField(
            string label,
            string propertyPath,
            bool allowUnassigned,
            string missingLabel,
            string tooltip)
        {
            if (_table == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();

            SerializedProperty property = serializedTable.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            SerializableGuid currentActorId = ReadSerializableGuidProperty(property);
            List<ActorOption> options = BuildActorOptions(
                currentActorId,
                allowUnassigned,
                missingLabel);
            int currentIndex = ResolveActorOptionIndex(options, currentActorId);

            if (currentIndex < 0 || currentIndex >= options.Count)
            {
                currentIndex = 0;
            }

            ActorOption currentOption = options[currentIndex];
            Rect rowRect = GUILayoutUtility.GetRect(0f, 42f, GUILayout.ExpandWidth(true));
            Rect labelRect = new(rowRect.x, rowRect.y + 11f, 88f, EditorGUIUtility.singleLineHeight);
            Rect buttonRect = new(rowRect.x + 90f, rowRect.y, rowRect.width - 90f, rowRect.height);

            EditorGUI.LabelField(labelRect, new GUIContent(label, tooltip));

            DrawActorOptionCard(
                buttonRect,
                currentOption,
                isSelected: true,
                buttonRect.Contains(Event.current.mousePosition),
                showDropdownIndicator: true);

            if (!GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
            {
                return;
            }

            UnityEditor.PopupWindow.Show(
                buttonRect,
                new ActorBindingPopupContent(
                    options,
                    currentActorId,
                    actorId => ApplyActorBindingSelection(propertyPath, actorId)));
        }

        /// <summary>
        /// Applies one selected conversant binding back into the serialized node property.
        /// </summary>
        /// <param name="propertyPath">Serialized property path that should be updated.</param>
        /// <param name="actorId">Selected conversant id.</param>
        private void ApplyActorBindingSelection(string propertyPath, SerializableGuid actorId)
        {
            if (_table == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();

            SerializedProperty property = serializedTable.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            WriteSerializableGuidProperty(property, actorId);
            serializedTable.ApplyModifiedProperties();
            HandleRelayedMutation();
            Refresh();
        }

        /// <summary>
        /// Resolves the hovered property field under the given mouse position.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        /// <param name="property">Resolved serialized property.</param>
        /// <returns>True when a hovered relay field maps to a live property.</returns>
        private bool TryGetHoveredRelayProperty(
            Vector2 mousePosition,
            out SerializedProperty property)
        {
            property = null;

            if (_table == null)
            {
                return false;
            }

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();

            for (int index = 0; index < _dragRelayFields.Count; index++)
            {
                DragRelayField relayField = _dragRelayFields[index];

                if (!relayField.Field.worldBound.Contains(mousePosition))
                {
                    continue;
                }

                property = serializedTable.FindProperty(relayField.PropertyPath);

                if (property != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves the serialized node property for one authored node id.
        /// </summary>
        /// <param name="serializedTable">Serialized table wrapper.</param>
        /// <param name="nodeId">Selected authored node id.</param>
        /// <returns>The serialized node property when found; otherwise null.</returns>
        private static SerializedProperty FindNodeProperty(
            SerializedObject serializedTable,
            ConversationTable table,
            ConversationDefinition conversation,
            SerializableGuid nodeId)
        {
            if (serializedTable == null
                || table == null
                || conversation == null
                || !table.TryGetConversationIndex(
                    conversation.ConversationId,
                    out int conversationIndex))
            {
                return null;
            }

            SerializedProperty conversationsProperty = serializedTable.FindProperty("_conversations");

            if (conversationsProperty == null
                || conversationIndex < 0
                || conversationIndex >= conversationsProperty.arraySize)
            {
                return null;
            }

            SerializedProperty conversationProperty = conversationsProperty.GetArrayElementAtIndex(
                conversationIndex);
            SerializedProperty graphProperty = conversationProperty?.FindPropertyRelative("_graph");
            SerializedProperty nodesProperty = graphProperty?.FindPropertyRelative("_nodes");

            if (nodesProperty == null)
            {
                return null;
            }

            for (int index = 0; index < nodesProperty.arraySize; index++)
            {
                SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(index);

                if (nodeProperty?.managedReferenceValue is RuntimeGraphCore.GraphNodeBase node
                    && node.Id == nodeId)
                {
                    return nodeProperty;
                }
            }

            return null;
        }

        /// <summary>
        /// Counts the shared conversants declared on the currently bound table.
        /// </summary>
        /// <returns>The total conversant count.</returns>
        private int CountActors()
        {
            return _table?.Actors?.Count ?? 0;
        }

        /// <summary>
        /// Builds the available conversant dropdown options for one line binding.
        /// </summary>
        /// <param name="currentActorId">Currently selected conversant id.</param>
        /// <param name="allowUnassigned">Whether an explicit unassigned option should be available.</param>
        /// <param name="missingLabel">Label used when the current binding points to a missing slot.</param>
        /// <returns>The dropdown options.</returns>
        private List<ActorOption> BuildActorOptions(
            SerializableGuid currentActorId,
            bool allowUnassigned,
            string missingLabel)
        {
            List<ActorOption> options = new();

            if (allowUnassigned)
            {
                options.Add(new ActorOption(
                    SerializableGuid.Empty,
                    "Unassigned",
                    "No conversant selected",
                    null,
                    new Color(0.32f, 0.32f, 0.32f, 1f),
                    isUnassigned: true));
            }

            bool currentSelectionResolved = currentActorId == SerializableGuid.Empty;

            if (_table?.Actors != null)
            {
                for (int index = 0; index < _table.Actors.Count; index++)
                {
                    ConversationActorDefinition actor = _table.Actors[index];

                    if (actor == null)
                    {
                        continue;
                    }

                    options.Add(new ActorOption(
                        actor.ActorId,
                        string.IsNullOrWhiteSpace(actor.DisplayName)
                            ? actor.Key
                            : actor.DisplayName.Trim(),
                        actor.Key,
                        actor.Portrait,
                        actor.ThemeColor));
                    currentSelectionResolved |= actor.ActorId == currentActorId;
                }
            }

            if (!currentSelectionResolved)
            {
                string shortHexId = currentActorId
                    .ToHexString()
                    .ToLowerInvariant();
                shortHexId = shortHexId.Length <= 8 ? shortHexId : shortHexId[..8];
                options.Add(new ActorOption(
                    currentActorId,
                    missingLabel,
                    shortHexId,
                    null,
                    new Color(0.62f, 0.29f, 0.25f, 1f),
                    isMissing: true));
            }

            return options;
        }

        /// <summary>
        /// Resolves the selected option index for the provided conversant id.
        /// </summary>
        /// <param name="options">Available conversant options.</param>
        /// <param name="actorId">Conversant id that should become selected.</param>
        /// <returns>The resolved option index.</returns>
        private static int ResolveActorOptionIndex(
            IReadOnlyList<ActorOption> options,
            SerializableGuid actorId)
        {
            for (int index = 0; index < options.Count; index++)
            {
                if (options[index].ActorId == actorId)
                {
                    return index;
                }
            }

            return 0;
        }

        /// <summary>
        /// Draws one actor option card used by the current selection field and popup rows.
        /// </summary>
        /// <param name="rect">Target rect that should host the card.</param>
        /// <param name="option">Option that should be rendered.</param>
        /// <param name="isSelected">Whether the card represents the current selection.</param>
        /// <param name="isHovered">Whether the pointer currently hovers the card.</param>
        /// <param name="showDropdownIndicator">Whether one dropdown arrow should be rendered.</param>
        private static void DrawActorOptionCard(
            Rect rect,
            ActorOption option,
            bool isSelected,
            bool isHovered,
            bool showDropdownIndicator)
        {
            Rect cardRect = new(rect.x, rect.y, rect.width, rect.height - 1f);
            Color backgroundColor = ResolveActorCardBackgroundColor(isSelected, isHovered);
            Color borderColor = ResolveActorCardBorderColor(isSelected);
            Color primaryTextColor = ResolveActorCardPrimaryTextColor();
            Color secondaryTextColor = ResolveActorCardSecondaryTextColor(isSelected, option.IsMissing);

            EditorGUI.DrawRect(cardRect, backgroundColor);
            DrawRectOutline(cardRect, borderColor);

            Rect contentRect = new(
                cardRect.x + 6f,
                cardRect.y + 6f,
                cardRect.width - 12f,
                cardRect.height - 12f);
            float dropdownWidth = showDropdownIndicator ? 16f : 0f;
            Rect previewRect = new(
                contentRect.x,
                contentRect.y + ((contentRect.height - 28f) * 0.5f),
                28f,
                28f);
            Rect textRect = new(
                previewRect.xMax + 8f,
                contentRect.y,
                Mathf.Max(0f, contentRect.width - 36f - dropdownWidth),
                contentRect.height);

            DrawActorPreview(previewRect, option, isSelected);

            Rect titleRect = new(textRect.x, textRect.y - 1f, textRect.width, 16f);
            Rect subtitleRect = new(textRect.x, textRect.y + 14f, textRect.width, 14f);
            GUI.Label(titleRect, option.PrimaryText, CreateActorCardTitleStyle(primaryTextColor));
            GUI.Label(
                subtitleRect,
                option.SecondaryText,
                CreateActorCardSubtitleStyle(secondaryTextColor));

            if (!showDropdownIndicator)
            {
                return;
            }

            Rect arrowRect = new(cardRect.xMax - 18f, cardRect.y + 1f, 14f, cardRect.height - 2f);
            GUI.Label(arrowRect, "v", CreateActorCardArrowStyle(primaryTextColor));
        }

        /// <summary>
        /// Draws the actor portrait preview or one fallback placeholder.
        /// </summary>
        /// <param name="rect">Target preview rect.</param>
        /// <param name="option">Option that owns the preview data.</param>
        /// <param name="isSelected">Whether the card represents the current selection.</param>
        private static void DrawActorPreview(Rect rect, ActorOption option, bool isSelected)
        {
            EditorGUI.DrawRect(rect, ResolveActorPreviewBackgroundColor(option, isSelected));
            DrawRectOutline(rect, ResolveActorCardBorderColor(isSelected));

            if (option.Portrait != null && option.Portrait.texture != null)
            {
                Texture2D texture = option.Portrait.texture;
                Rect textureRect = option.Portrait.textureRect;
                Rect uvRect = new(
                    textureRect.x / texture.width,
                    textureRect.y / texture.height,
                    textureRect.width / texture.width,
                    textureRect.height / texture.height);
                Rect fittedRect = FitRect(rect, textureRect.width, textureRect.height);
                GUI.DrawTextureWithTexCoords(fittedRect, texture, uvRect, alphaBlend: true);
                return;
            }

            string placeholderText = option.IsUnassigned
                ? "-"
                : option.IsMissing
                    ? "!"
                    : option.PrimaryText[..1].ToUpperInvariant();
            GUI.Label(
                rect,
                placeholderText,
                CreateActorCardPlaceholderStyle(ResolveActorCardPrimaryTextColor()));
        }

        /// <summary>
        /// Fits one source size into the provided target rect while preserving aspect ratio.
        /// </summary>
        /// <param name="targetRect">Target rect that should contain the fitted result.</param>
        /// <param name="sourceWidth">Source width.</param>
        /// <param name="sourceHeight">Source height.</param>
        /// <returns>The fitted rect.</returns>
        private static Rect FitRect(Rect targetRect, float sourceWidth, float sourceHeight)
        {
            if (sourceWidth <= 0f || sourceHeight <= 0f)
            {
                return targetRect;
            }

            float sourceAspect = sourceWidth / sourceHeight;
            float targetAspect = targetRect.width / targetRect.height;

            if (sourceAspect >= targetAspect)
            {
                float fittedHeight = targetRect.width / sourceAspect;
                return new Rect(
                    targetRect.x,
                    targetRect.y + ((targetRect.height - fittedHeight) * 0.5f),
                    targetRect.width,
                    fittedHeight);
            }

            float fittedWidth = targetRect.height * sourceAspect;
            return new Rect(
                targetRect.x + ((targetRect.width - fittedWidth) * 0.5f),
                targetRect.y,
                fittedWidth,
                targetRect.height);
        }

        /// <summary>
        /// Draws one one-pixel outline around the provided rect.
        /// </summary>
        /// <param name="rect">Target rect.</param>
        /// <param name="color">Outline color.</param>
        private static void DrawRectOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        /// <summary>
        /// Resolves the background color used by actor cards.
        /// </summary>
        /// <param name="isSelected">Whether the card represents the current selection.</param>
        /// <param name="isHovered">Whether the pointer currently hovers the card.</param>
        /// <returns>The resolved card background color.</returns>
        private static Color ResolveActorCardBackgroundColor(bool isSelected, bool isHovered)
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (isSelected)
                {
                    return isHovered
                        ? new Color(0.25f, 0.25f, 0.25f, 1f)
                        : new Color(0.21f, 0.21f, 0.21f, 1f);
                }

                return isHovered
                    ? new Color(0.24f, 0.24f, 0.24f, 1f)
                    : new Color(0.19f, 0.19f, 0.19f, 1f);
            }

            if (isSelected)
            {
                return isHovered
                    ? new Color(0.78f, 0.78f, 0.78f, 1f)
                    : new Color(0.72f, 0.72f, 0.72f, 1f);
            }

            return isHovered
                ? new Color(0.85f, 0.85f, 0.85f, 1f)
                : new Color(0.91f, 0.91f, 0.91f, 1f);
        }

        /// <summary>
        /// Resolves the border color used by actor cards.
        /// </summary>
        /// <param name="isSelected">Whether the card represents the current selection.</param>
        /// <returns>The resolved border color.</returns>
        private static Color ResolveActorCardBorderColor(bool isSelected)
        {
            if (EditorGUIUtility.isProSkin)
            {
                return isSelected
                    ? new Color(0.43f, 0.43f, 0.43f, 1f)
                    : new Color(0.31f, 0.31f, 0.31f, 1f);
            }

            return isSelected
                ? new Color(0.5f, 0.5f, 0.5f, 1f)
                : new Color(0.71f, 0.71f, 0.71f, 1f);
        }

        /// <summary>
        /// Resolves the primary text color used by actor cards.
        /// </summary>
        /// <returns>The resolved primary text color.</returns>
        private static Color ResolveActorCardPrimaryTextColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.94f, 0.94f, 0.94f, 1f)
                : new Color(0.15f, 0.15f, 0.15f, 1f);
        }

        /// <summary>
        /// Resolves the secondary text color used by actor cards.
        /// </summary>
        /// <param name="isSelected">Whether the card represents the current selection.</param>
        /// <param name="isMissing">Whether the card represents one missing conversant binding.</param>
        /// <returns>The resolved secondary text color.</returns>
        private static Color ResolveActorCardSecondaryTextColor(bool isSelected, bool isMissing)
        {
            if (isMissing)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 0.74f, 0.68f, 1f)
                    : new Color(0.56f, 0.18f, 0.12f, 1f);
            }

            if (EditorGUIUtility.isProSkin)
            {
                return isSelected
                    ? new Color(0.84f, 0.89f, 0.95f, 1f)
                    : new Color(0.68f, 0.68f, 0.68f, 1f);
            }

            return isSelected
                ? new Color(0.27f, 0.32f, 0.39f, 1f)
                : new Color(0.38f, 0.38f, 0.38f, 1f);
        }

        /// <summary>
        /// Resolves the preview placeholder background color used by actor cards.
        /// </summary>
        /// <param name="option">Option that owns the preview.</param>
        /// <param name="isSelected">Whether the card represents the current selection.</param>
        /// <returns>The resolved preview background color.</returns>
        private static Color ResolveActorPreviewBackgroundColor(
            ActorOption option,
            bool isSelected)
        {
            if (option.IsMissing)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.46f, 0.19f, 0.17f, 1f)
                    : new Color(0.82f, 0.65f, 0.63f, 1f);
            }

            if (option.IsUnassigned)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.28f, 0.28f, 0.28f, 1f)
                    : new Color(0.8f, 0.8f, 0.8f, 1f);
            }

            Color themeColor = option.ThemeColor.a <= 0f
                ? Color.white
                : option.ThemeColor;

            if (EditorGUIUtility.isProSkin)
            {
                return Color.Lerp(themeColor, Color.black, isSelected ? 0.2f : 0.35f);
            }

            return Color.Lerp(themeColor, Color.white, isSelected ? 0.1f : 0.28f);
        }

        /// <summary>
        /// Creates the title style used by actor cards.
        /// </summary>
        /// <param name="textColor">Text color that should be applied.</param>
        /// <returns>The configured title style.</returns>
        private static GUIStyle CreateActorCardTitleStyle(Color textColor)
        {
            GUIStyle style = new(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
            };
            style.normal.textColor = textColor;
            return style;
        }

        /// <summary>
        /// Creates the subtitle style used by actor cards.
        /// </summary>
        /// <param name="textColor">Text color that should be applied.</param>
        /// <returns>The configured subtitle style.</returns>
        private static GUIStyle CreateActorCardSubtitleStyle(Color textColor)
        {
            GUIStyle style = new(EditorStyles.miniLabel)
            {
                clipping = TextClipping.Clip,
            };
            style.normal.textColor = textColor;
            return style;
        }

        /// <summary>
        /// Creates the placeholder style rendered over an empty search field.
        /// </summary>
        /// <returns>The configured placeholder style.</returns>
        private static GUIStyle CreateSearchPlaceholderStyle()
        {
            GUIStyle style = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
            };
            style.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.56f, 0.56f, 0.56f, 1f)
                : new Color(0.45f, 0.45f, 0.45f, 1f);
            return style;
        }

        /// <summary>
        /// Creates the centered placeholder style used when one preview image is missing.
        /// </summary>
        /// <param name="textColor">Text color that should be applied.</param>
        /// <returns>The configured placeholder style.</returns>
        private static GUIStyle CreateActorCardPlaceholderStyle(Color textColor)
        {
            GUIStyle style = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };
            style.normal.textColor = textColor;
            return style;
        }

        /// <summary>
        /// Creates the dropdown arrow style rendered on the current selection card.
        /// </summary>
        /// <param name="textColor">Text color that should be applied.</param>
        /// <returns>The configured arrow style.</returns>
        private static GUIStyle CreateActorCardArrowStyle(Color textColor)
        {
            GUIStyle style = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = textColor;
            return style;
        }

        /// <summary>
        /// Reads one SerializableGuid value from the provided serialized property.
        /// </summary>
        /// <param name="property">Serialized SerializableGuid property.</param>
        /// <returns>The resolved SerializableGuid value.</returns>
        private static SerializableGuid ReadSerializableGuidProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return SerializableGuid.Empty;
            }

            SerializedProperty part1Property = property.FindPropertyRelative(nameof(SerializableGuid.Part1));
            SerializedProperty part2Property = property.FindPropertyRelative(nameof(SerializableGuid.Part2));
            SerializedProperty part3Property = property.FindPropertyRelative(nameof(SerializableGuid.Part3));
            SerializedProperty part4Property = property.FindPropertyRelative(nameof(SerializableGuid.Part4));

            if (part1Property == null
                || part2Property == null
                || part3Property == null
                || part4Property == null)
            {
                return SerializableGuid.Empty;
            }

            return new SerializableGuid(
                part1Property.uintValue,
                part2Property.uintValue,
                part3Property.uintValue,
                part4Property.uintValue);
        }

        /// <summary>
        /// Writes one SerializableGuid value into the provided serialized property.
        /// </summary>
        /// <param name="property">Serialized SerializableGuid property.</param>
        /// <param name="value">SerializableGuid value that should be stored.</param>
        private static void WriteSerializableGuidProperty(
            SerializedProperty property,
            SerializableGuid value)
        {
            if (property == null)
            {
                return;
            }

            SerializedProperty part1Property = property.FindPropertyRelative(nameof(SerializableGuid.Part1));
            SerializedProperty part2Property = property.FindPropertyRelative(nameof(SerializableGuid.Part2));
            SerializedProperty part3Property = property.FindPropertyRelative(nameof(SerializableGuid.Part3));
            SerializedProperty part4Property = property.FindPropertyRelative(nameof(SerializableGuid.Part4));

            if (part1Property == null
                || part2Property == null
                || part3Property == null
                || part4Property == null)
            {
                return;
            }

            part1Property.uintValue = value.Part1;
            part2Property.uintValue = value.Part2;
            part3Property.uintValue = value.Part3;
            part4Property.uintValue = value.Part4;
        }

        /// <summary>
        /// Creates one boxed inspector section with a caption and indented body.
        /// </summary>
        /// <param name="title">Section caption.</param>
        /// <param name="body">Resolved section body container.</param>
        /// <returns>The configured section element.</returns>
        private static VisualElement CreateInspectorSection(
            string title,
            out VisualElement body)
        {
            VisualElement section = new();
            section.style.flexDirection = FlexDirection.Column;
            section.style.marginBottom = 10f;
            section.style.paddingLeft = 10f;
            section.style.paddingRight = 10f;
            section.style.paddingTop = 9f;
            section.style.paddingBottom = 10f;
            section.style.backgroundColor = ResolveInspectorSectionBackgroundColor();
            section.style.borderLeftWidth = 1f;
            section.style.borderRightWidth = 1f;
            section.style.borderTopWidth = 1f;
            section.style.borderBottomWidth = 1f;
            section.style.borderLeftColor = ResolveInspectorSectionBorderColor();
            section.style.borderRightColor = ResolveInspectorSectionBorderColor();
            section.style.borderTopColor = ResolveInspectorSectionBorderColor();
            section.style.borderBottomColor = ResolveInspectorSectionBorderColor();
            section.style.borderTopLeftRadius = 8f;
            section.style.borderTopRightRadius = 8f;
            section.style.borderBottomLeftRadius = 8f;
            section.style.borderBottomRightRadius = 8f;

            section.Add(CreateSectionCaptionLabel(title));

            body = new VisualElement();
            body.style.flexDirection = FlexDirection.Column;
            body.style.marginTop = 8f;
            body.style.paddingLeft = 10f;
            body.style.borderLeftWidth = 1f;
            body.style.borderLeftColor = ResolveInspectorAccentColor();
            body.style.minWidth = 0f;
            section.Add(body);
            return section;
        }

        /// <summary>
        /// Creates one nested participant group used inside the Participants section.
        /// </summary>
        /// <param name="title">Group caption.</param>
        /// <param name="body">Resolved group body container.</param>
        /// <returns>The configured group element.</returns>
        private static VisualElement CreateInspectorGroup(
            string title,
            out VisualElement body)
        {
            VisualElement group = new();
            group.style.flexDirection = FlexDirection.Column;
            group.style.marginBottom = 8f;
            group.style.paddingLeft = 9f;
            group.style.paddingRight = 9f;
            group.style.paddingTop = 8f;
            group.style.paddingBottom = 8f;
            group.style.backgroundColor = ResolveInspectorGroupBackgroundColor();
            group.style.borderLeftWidth = 1f;
            group.style.borderRightWidth = 1f;
            group.style.borderTopWidth = 1f;
            group.style.borderBottomWidth = 1f;
            group.style.borderLeftColor = ResolveInspectorSectionBorderColor();
            group.style.borderRightColor = ResolveInspectorSectionBorderColor();
            group.style.borderTopColor = ResolveInspectorSectionBorderColor();
            group.style.borderBottomColor = ResolveInspectorSectionBorderColor();
            group.style.borderTopLeftRadius = 6f;
            group.style.borderTopRightRadius = 6f;
            group.style.borderBottomLeftRadius = 6f;
            group.style.borderBottomRightRadius = 6f;

            Label titleLabel = new(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 11f;
            titleLabel.style.color = ResolveInspectorBodyTextColor();
            group.Add(titleLabel);

            body = new VisualElement();
            body.style.flexDirection = FlexDirection.Column;
            body.style.marginTop = 8f;
            body.style.minWidth = 0f;
            group.Add(body);
            return group;
        }

        /// <summary>
        /// Creates one small caption label used by inspector sections.
        /// </summary>
        /// <param name="text">Caption text.</param>
        /// <returns>The configured caption label.</returns>
        private static Label CreateSectionCaptionLabel(string text)
        {
            Label label = new(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 10f;
            label.style.color = ResolveInspectorMutedTextColor();
            return label;
        }

        /// <summary>
        /// Resolves the background color used by boxed inspector sections.
        /// </summary>
        /// <returns>The resolved background color.</returns>
        private static Color ResolveInspectorSectionBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.17f, 0.17f, 0.97f)
                : new Color(0.90f, 0.90f, 0.90f, 1f);
        }

        /// <summary>
        /// Resolves the background color used by nested participant groups.
        /// </summary>
        /// <returns>The resolved background color.</returns>
        private static Color ResolveInspectorGroupBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, 0.98f)
                : new Color(0.94f, 0.94f, 0.94f, 1f);
        }

        /// <summary>
        /// Resolves the border color used by inspector sections and groups.
        /// </summary>
        /// <returns>The resolved border color.</returns>
        private static Color ResolveInspectorSectionBorderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.30f, 0.30f, 0.30f, 1f)
                : new Color(0.66f, 0.66f, 0.66f, 1f);
        }

        /// <summary>
        /// Resolves the accent color used by section indentation rails.
        /// </summary>
        /// <returns>The resolved accent color.</returns>
        private static Color ResolveInspectorAccentColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.42f, 0.42f, 0.42f, 1f)
                : new Color(0.58f, 0.58f, 0.58f, 1f);
        }

        /// <summary>
        /// Resolves the muted text color used by section helper labels.
        /// </summary>
        /// <returns>The resolved muted text color.</returns>
        private static Color ResolveInspectorMutedTextColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.78f, 0.78f, 0.78f, 1f)
                : new Color(0.28f, 0.28f, 0.28f, 1f);
        }

        /// <summary>
        /// Resolves the primary text color used inside nested inspector groups.
        /// </summary>
        /// <returns>The resolved body text color.</returns>
        private static Color ResolveInspectorBodyTextColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.91f, 0.91f, 0.91f, 1f)
                : new Color(0.18f, 0.18f, 0.18f, 1f);
        }

        /// <summary>
        /// Creates one section header label.
        /// </summary>
        /// <param name="text">Header text.</param>
        /// <returns>The configured label.</returns>
        private static Label CreateHeaderLabel(string text)
        {
            Label label = new(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 13f;
            label.style.marginBottom = 6f;
            return label;
        }

        /// <summary>
        /// Creates one compact value row with a title and value.
        /// </summary>
        /// <param name="title">Displayed title.</param>
        /// <param name="value">Displayed value.</param>
        /// <returns>The configured row element.</returns>
        private static VisualElement CreateValueRow(string title, string value)
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2f;

            Label titleLabel = new($"{title}:");
            titleLabel.style.minWidth = 90f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            Label valueLabel = new(string.IsNullOrWhiteSpace(value) ? "-" : value);
            valueLabel.style.flexGrow = 1f;

            row.Add(titleLabel);
            row.Add(valueLabel);
            return row;
        }
    }
}