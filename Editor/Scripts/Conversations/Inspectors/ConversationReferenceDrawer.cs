using System;
using System.Collections.Generic;
using System.Globalization;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Inspectors
{
    /// <summary>
    /// Draws one searchable conversation picker that resolves authored conversations from every
    /// ConversationTable asset in the project.
    /// </summary>
    [CustomPropertyDrawer(typeof(ConversationReference))]
    public sealed class ConversationReferenceDrawer : PropertyDrawer
    {
        #region Constants

        private const float ClearButtonWidth = 22f;
        private const float ButtonSpacing = 4f;
        private const float FieldLabelMinimumWidth = 120f;
        private const float DropdownWidth = 440f;
        private const float DropdownMaxHeight = 360f;
        private const float DropdownVisibleItemCount = 6f;
        private const float DropdownSearchHeight = 24f;
        private const float DropdownSearchBottomSpacing = 8f;
        private const float DropdownOuterPadding = 8f;
        private const float DropdownItemMarginBottom = 6f;
        private const float DropdownItemAccentWidth = 4f;
        private const float DropdownItemCornerRadius = 6f;
        private const float DropdownItemPrimaryFontSize = 12f;
        private const float DropdownItemSecondaryFontSize = 11f;
        private const float DropdownMinimumVisibleItemCount = 3f;
        private const float ClosedFieldVerticalPadding = 4f;
        private const float ClosedFieldHorizontalPadding = 10f;
        private const float ClosedFieldMinimumHeight = 26f;

        #endregion

        #region Public API

        /// <summary>
        /// Creates the UI Toolkit property UI for the conversation picker.
        /// </summary>
        /// <param name="property">Serialized property being drawn.</param>
        /// <returns>The configured property visual element.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty tableProperty = property.FindPropertyRelative("_table");
            SerializedProperty conversationIdProperty =
                property.FindPropertyRelative("_conversationId");
            SerializedProperty conversationTitleProperty =
                property.FindPropertyRelative("_conversationTitle");

            if (tableProperty == null
                || conversationIdProperty == null
                || conversationTitleProperty == null)
            {
                return CreateInvalidDrawerFallback(property.displayName);
            }

            VisualElement root = new();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.marginBottom = 2f;

            Label fieldLabel = new(property.displayName);
            fieldLabel.style.minWidth = Mathf.Max(
                FieldLabelMinimumWidth,
                EditorGUIUtility.labelWidth - 18f);
            fieldLabel.style.marginRight = 4f;
            fieldLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            fieldLabel.style.flexShrink = 0f;
            root.Add(fieldLabel);

            VisualElement inputRow = new();
            inputRow.style.flexDirection = FlexDirection.Row;
            inputRow.style.flexGrow = 1f;
            inputRow.style.alignItems = Align.Center;
            root.Add(inputRow);

            Button pickerButton = CreatePickerButton(out Label pickerLabel);
            Button clearButton = CreateClearButton();

            inputRow.Add(pickerButton);
            inputRow.Add(clearButton);

            void RefreshPresentation()
            {
                property.serializedObject.UpdateIfRequiredOrScript();

                string displayLabel = BuildCurrentSelectionLabel(
                    tableProperty,
                    conversationIdProperty,
                    conversationTitleProperty,
                    float.PositiveInfinity,
                    EditorStyles.label);

                pickerLabel.text = displayLabel;
                pickerButton.tooltip = BuildCurrentSelectionTooltip(
                    tableProperty,
                    conversationIdProperty,
                    conversationTitleProperty);
                clearButton.style.display = tableProperty.objectReferenceValue != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            pickerButton.clicked += () =>
            {
                ConversationReferenceDropdownWindow.ShowDropdown(
                    GetScreenRect(pickerButton),
                    BuildOptions(),
                    tableProperty.objectReferenceValue as ConversationTable,
                    ReadSerializableGuid(conversationIdProperty),
                    option =>
                    {
                        ApplySelection(
                            property.serializedObject,
                            property.propertyPath,
                            option);
                        RefreshPresentation();
                    });
            };

            clearButton.clicked += () =>
            {
                ClearSelection(property.serializedObject, property.propertyPath);
                RefreshPresentation();
            };

            root.TrackPropertyValue(tableProperty, _ => RefreshPresentation());
            root.TrackPropertyValue(conversationIdProperty, _ => RefreshPresentation());
            root.TrackPropertyValue(conversationTitleProperty, _ => RefreshPresentation());
            RefreshPresentation();
            return root;
        }

        /// <summary>
        /// Draws an IMGUI fallback for hosts that do not support UI Toolkit property drawers.
        /// </summary>
        /// <param name="position">Drawing rect.</param>
        /// <param name="property">Serialized property being drawn.</param>
        /// <param name="label">Inspector label.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty tableProperty = property.FindPropertyRelative("_table");
            SerializedProperty conversationIdProperty =
                property.FindPropertyRelative("_conversationId");
            SerializedProperty conversationTitleProperty =
                property.FindPropertyRelative("_conversationTitle");

            if (tableProperty == null
                || conversationIdProperty == null
                || conversationTitleProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Conversation picker unavailable.");
                EditorGUI.EndProperty();
                return;
            }

            Rect contentRect = EditorGUI.PrefixLabel(
                position,
                GUIUtility.GetControlID(FocusType.Passive),
                label);
            bool hasSelection = tableProperty.objectReferenceValue != null;
            Rect clearButtonRect = new(
                contentRect.xMax - ClearButtonWidth,
                contentRect.y,
                ClearButtonWidth,
                contentRect.height);
            Rect pickerButtonRect = new(
                contentRect.x,
                contentRect.y,
                hasSelection
                    ? Mathf.Max(0f, contentRect.width - ClearButtonWidth - ButtonSpacing)
                    : contentRect.width,
                contentRect.height);

            string displayLabel = BuildCurrentSelectionLabel(
                tableProperty,
                conversationIdProperty,
                conversationTitleProperty,
                pickerButtonRect.width - 18f,
                EditorStyles.popup);
            string selectionTooltip = BuildCurrentSelectionTooltip(
                tableProperty,
                conversationIdProperty,
                conversationTitleProperty);

            if (GUI.Button(
                    pickerButtonRect,
                    new GUIContent(displayLabel, selectionTooltip),
                    EditorStyles.popup))
            {
                ConversationReferenceDropdownWindow.ShowDropdown(
                    GUIUtility.GUIToScreenRect(pickerButtonRect),
                    BuildOptions(),
                    tableProperty.objectReferenceValue as ConversationTable,
                    ReadSerializableGuid(conversationIdProperty),
                    option => ApplySelection(
                        property.serializedObject,
                        property.propertyPath,
                        option));
            }

            if (hasSelection
                && GUI.Button(clearButtonRect, "X", EditorStyles.miniButton))
            {
                ClearSelection(property.serializedObject, property.propertyPath);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets the property height used by the IMGUI fallback.
        /// </summary>
        /// <param name="property">Serialized property being drawn.</param>
        /// <param name="label">Inspector label.</param>
        /// <returns>The required property height.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _ = property;
            _ = label;
            return EditorGUIUtility.singleLineHeight;
        }

        #endregion

        #region Dropdown Window

        /// <summary>
        /// Stores one searchable authored conversation option.
        /// </summary>
        private sealed class ConversationOption
        {
            /// <summary>
            /// Creates one searchable conversation option.
            /// </summary>
            /// <param name="table">Table that owns the option.</param>
            /// <param name="conversationId">Stable conversation identifier.</param>
            /// <param name="conversationTitle">Authored conversation title path.</param>
            /// <param name="displayLabel">Display label shown in the picker.</param>
            /// <param name="searchText">Searchable text tokens.</param>
            public ConversationOption(
                ConversationTable table,
                SerializableGuid conversationId,
                string conversationTitle,
                string displayLabel,
                string searchText)
            {
                Table = table;
                ConversationId = conversationId;
                ConversationTitle = conversationTitle ?? string.Empty;
                DisplayLabel = displayLabel ?? string.Empty;
                SearchText = searchText ?? string.Empty;
                PrimaryLabel = BuildConversationLeafLabel(ConversationTitle);
                SecondaryLabel = BuildSecondaryContextLabel(
                    ResolveTableDisplayName(table),
                    ConversationTitle,
                    float.PositiveInfinity,
                    EditorStyles.miniLabel);
            }

            /// <summary>
            /// Gets the table that owns the option.
            /// </summary>
            public ConversationTable Table { get; }

            /// <summary>
            /// Gets the stable authored conversation identifier.
            /// </summary>
            public SerializableGuid ConversationId { get; }

            /// <summary>
            /// Gets the authored title path used by the option.
            /// </summary>
            public string ConversationTitle { get; }

            /// <summary>
            /// Gets the display label exposed by the picker.
            /// </summary>
            public string DisplayLabel { get; }

            /// <summary>
            /// Gets the searchable label tokens used by the picker search.
            /// </summary>
            public string SearchText { get; }

            /// <summary>
            /// Gets the primary title rendered by the option card.
            /// </summary>
            public string PrimaryLabel { get; }

            /// <summary>
            /// Gets the secondary metadata rendered by the option card.
            /// </summary>
            public string SecondaryLabel { get; }

            /// <summary>
            /// Gets whether the option should remain visible for the provided search query.
            /// </summary>
            /// <param name="searchQuery">Current search query.</param>
            /// <returns>True when the option matches the search.</returns>
            public bool Matches(string searchQuery)
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    return true;
                }

                return SearchText.IndexOf(searchQuery.Trim(), StringComparison.OrdinalIgnoreCase)
                    >= 0;
            }
        }

        /// <summary>
        /// Hosts the UI Toolkit dropdown used by the conversation picker.
        /// </summary>
        private sealed class ConversationReferenceDropdownWindow : EditorWindow
        {
            private readonly List<ConversationOption> _filteredOptions = new();

            private IReadOnlyList<ConversationOption> _options = Array.Empty<ConversationOption>();
            private ConversationTable _selectedTable;
            private SerializableGuid _selectedConversationId;
            private Action<ConversationOption> _onSelected;
            private string _searchQuery = string.Empty;
            private ToolbarSearchField _searchField;
            private ScrollView _optionsScrollView;

            /// <summary>
            /// Shows the dropdown window anchored to the provided screen rect.
            /// </summary>
            /// <param name="anchorRect">Anchor rect in screen space.</param>
            /// <param name="options">Options that should be displayed by the dropdown.</param>
            /// <param name="selectedTable">Currently selected table.</param>
            /// <param name="selectedConversationId">Currently selected conversation id.</param>
            /// <param name="onSelected">Callback invoked after one option is selected.</param>
            public static void ShowDropdown(
                Rect anchorRect,
                IReadOnlyList<ConversationOption> options,
                ConversationTable selectedTable,
                SerializableGuid selectedConversationId,
                Action<ConversationOption> onSelected)
            {
                ConversationReferenceDropdownWindow window =
                    CreateInstance<ConversationReferenceDropdownWindow>();
                window.Initialize(options, selectedTable, selectedConversationId, onSelected);
                window.ShowAsDropDown(anchorRect, window.CalculateWindowSize());
                window.Focus();
            }

            /// <summary>
            /// Rebuilds the window UI when the editor creates the root visual tree.
            /// </summary>
            public void CreateGUI()
            {
                rootVisualElement.Clear();
                rootVisualElement.style.flexDirection = FlexDirection.Column;
                rootVisualElement.style.paddingLeft = DropdownOuterPadding;
                rootVisualElement.style.paddingRight = DropdownOuterPadding;
                rootVisualElement.style.paddingTop = DropdownOuterPadding;
                rootVisualElement.style.paddingBottom = DropdownOuterPadding;
                rootVisualElement.style.backgroundColor = GetDropdownWindowBackgroundColor();

                _searchField = new ToolbarSearchField();
                _searchField.value = _searchQuery;
                _searchField.style.height = DropdownSearchHeight;
                _searchField.style.marginBottom = DropdownSearchBottomSpacing;
                _searchField.style.width = Length.Percent(100f);
                _searchField.style.alignSelf = Align.Stretch;
                _searchField.style.flexShrink = 0f;
                _searchField.RegisterValueChangedCallback(evt =>
                {
                    _searchQuery = evt.newValue ?? string.Empty;
                    RefreshOptionCards();
                });
                rootVisualElement.Add(_searchField);

                _optionsScrollView = new ScrollView(ScrollViewMode.Vertical);
                _optionsScrollView.style.flexGrow = 1f;
                _optionsScrollView.style.paddingBottom = 4f;
                rootVisualElement.Add(_optionsScrollView);

                RefreshOptionCards();
                rootVisualElement.schedule.Execute(() => _searchField.Focus()).ExecuteLater(0);
            }

            /// <summary>
            /// Stores the window state required to render the dropdown.
            /// </summary>
            /// <param name="options">Options that should be displayed by the dropdown.</param>
            /// <param name="selectedTable">Currently selected table.</param>
            /// <param name="selectedConversationId">Currently selected conversation id.</param>
            /// <param name="onSelected">Callback invoked after one option is selected.</param>
            private void Initialize(
                IReadOnlyList<ConversationOption> options,
                ConversationTable selectedTable,
                SerializableGuid selectedConversationId,
                Action<ConversationOption> onSelected)
            {
                _options = options ?? Array.Empty<ConversationOption>();
                _selectedTable = selectedTable;
                _selectedConversationId = selectedConversationId;
                _onSelected = onSelected;
            }

            /// <summary>
            /// Calculates the dropdown window size from the current option set.
            /// </summary>
            /// <returns>The dropdown window size.</returns>
            private Vector2 CalculateWindowSize()
            {
                float visibleItemCount = Mathf.Clamp(
                    _options.Count,
                    DropdownMinimumVisibleItemCount,
                    DropdownVisibleItemCount);
                float itemBlockHeight = (visibleItemCount * GetDropdownItemBlockHeight())
                    + (visibleItemCount * DropdownItemMarginBottom);
                float height = (DropdownOuterPadding * 2f)
                    + DropdownSearchHeight
                    + DropdownSearchBottomSpacing
                    + itemBlockHeight;
                return new Vector2(DropdownWidth, Mathf.Min(DropdownMaxHeight, height));
            }

            /// <summary>
            /// Rebuilds the visible option cards for the current search query.
            /// </summary>
            private void RefreshOptionCards()
            {
                if (_optionsScrollView == null)
                {
                    return;
                }

                _optionsScrollView.Clear();
                IReadOnlyList<ConversationOption> visibleOptions = BuildVisibleOptions();

                if (visibleOptions.Count <= 0)
                {
                    HelpBox helpBox = new(
                        "No conversations match this search.",
                        HelpBoxMessageType.Info);
                    helpBox.style.marginTop = 2f;
                    _optionsScrollView.Add(helpBox);
                    return;
                }

                for (int index = 0; index < visibleOptions.Count; index++)
                {
                    ConversationOption option = visibleOptions[index];
                    bool isSelected = ReferenceEquals(option.Table, _selectedTable)
                        && option.ConversationId == _selectedConversationId;
                    _optionsScrollView.Add(CreateOptionCard(option, isSelected));
                }
            }

            /// <summary>
            /// Builds the option list visible for the current search query.
            /// </summary>
            /// <returns>The filtered options visible in the dropdown.</returns>
            private IReadOnlyList<ConversationOption> BuildVisibleOptions()
            {
                if (string.IsNullOrWhiteSpace(_searchQuery))
                {
                    return _options;
                }

                _filteredOptions.Clear();

                for (int index = 0; index < _options.Count; index++)
                {
                    ConversationOption option = _options[index];

                    if (option.Matches(_searchQuery))
                    {
                        _filteredOptions.Add(option);
                    }
                }

                return _filteredOptions;
            }

            /// <summary>
            /// Creates one option card rendered inside the dropdown scroll view.
            /// </summary>
            /// <param name="option">Option represented by the card.</param>
            /// <param name="isSelected">Whether the option is currently selected.</param>
            /// <returns>The configured option card element.</returns>
            private VisualElement CreateOptionCard(ConversationOption option, bool isSelected)
            {
                Button button = new(() => SelectOption(option));
                button.tooltip = option.DisplayLabel;
                button.style.paddingLeft = 0f;
                button.style.paddingRight = 0f;
                button.style.paddingTop = 0f;
                button.style.paddingBottom = 0f;
                button.style.marginBottom = DropdownItemMarginBottom;
                button.style.borderLeftWidth = 0f;
                button.style.borderRightWidth = 0f;
                button.style.borderTopWidth = 0f;
                button.style.borderBottomWidth = 0f;
                button.style.backgroundColor = Color.clear;
                button.style.unityTextAlign = TextAnchor.MiddleLeft;

                VisualElement card = new();
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Stretch;
                card.style.minHeight = GetDropdownItemBlockHeight();
                card.style.borderTopWidth = 1f;
                card.style.borderRightWidth = 1f;
                card.style.borderBottomWidth = 1f;
                card.style.borderLeftWidth = 1f;
                card.style.borderTopLeftRadius = DropdownItemCornerRadius;
                card.style.borderTopRightRadius = DropdownItemCornerRadius;
                card.style.borderBottomLeftRadius = DropdownItemCornerRadius;
                card.style.borderBottomRightRadius = DropdownItemCornerRadius;

                VisualElement accent = new();
                accent.style.width = DropdownItemAccentWidth;
                accent.style.borderTopLeftRadius = DropdownItemCornerRadius;
                accent.style.borderBottomLeftRadius = DropdownItemCornerRadius;
                card.Add(accent);

                VisualElement textColumn = new();
                textColumn.style.flexDirection = FlexDirection.Column;
                textColumn.style.flexGrow = 1f;
                textColumn.style.paddingLeft = 10f;
                textColumn.style.paddingRight = 10f;
                textColumn.style.paddingTop = 8f;
                textColumn.style.paddingBottom = 10f;
                card.Add(textColumn);

                Label primaryLabel = new(option.PrimaryLabel);
                primaryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                primaryLabel.style.fontSize = DropdownItemPrimaryFontSize;
                primaryLabel.style.marginBottom = 4f;
                primaryLabel.style.whiteSpace = WhiteSpace.NoWrap;
                primaryLabel.style.overflow = Overflow.Hidden;
                primaryLabel.style.textOverflow = TextOverflow.Ellipsis;
                textColumn.Add(primaryLabel);

                Label secondaryLabel = new(option.SecondaryLabel);
                secondaryLabel.style.fontSize = DropdownItemSecondaryFontSize;
                secondaryLabel.style.whiteSpace = WhiteSpace.NoWrap;
                secondaryLabel.style.overflow = Overflow.Hidden;
                secondaryLabel.style.textOverflow = TextOverflow.Ellipsis;
                textColumn.Add(secondaryLabel);

                ApplyCardVisualState(card, accent, primaryLabel, secondaryLabel, isSelected, false);

                button.RegisterCallback<MouseEnterEvent>(_ =>
                    ApplyCardVisualState(card, accent, primaryLabel, secondaryLabel, isSelected, true));
                button.RegisterCallback<MouseLeaveEvent>(_ =>
                    ApplyCardVisualState(card, accent, primaryLabel, secondaryLabel, isSelected, false));
                button.Add(card);
                return button;
            }

            /// <summary>
            /// Applies the correct visual state to one option card.
            /// </summary>
            /// <param name="card">Card container.</param>
            /// <param name="accent">Accent stripe element.</param>
            /// <param name="primaryLabel">Primary title label.</param>
            /// <param name="secondaryLabel">Secondary metadata label.</param>
            /// <param name="isSelected">Whether the option is selected.</param>
            /// <param name="isHovered">Whether the option is hovered.</param>
            private static void ApplyCardVisualState(
                VisualElement card,
                VisualElement accent,
                Label primaryLabel,
                Label secondaryLabel,
                bool isSelected,
                bool isHovered)
            {
                card.style.backgroundColor = GetDropdownCardBackgroundColor(isSelected, isHovered);
                Color borderColor = GetDropdownCardBorderColor(isSelected, isHovered);
                card.style.borderTopColor = borderColor;
                card.style.borderRightColor = borderColor;
                card.style.borderBottomColor = borderColor;
                card.style.borderLeftColor = borderColor;
                accent.style.backgroundColor = GetDropdownCardAccentColor(isSelected, isHovered);
                primaryLabel.style.color = GetDropdownPrimaryTextColor(isSelected, isHovered);
                secondaryLabel.style.color = GetDropdownSecondaryTextColor(isSelected, isHovered);
            }

            /// <summary>
            /// Applies the selected option and closes the dropdown window.
            /// </summary>
            /// <param name="option">Selected conversation option.</param>
            private void SelectOption(ConversationOption option)
            {
                _onSelected?.Invoke(option);
                Close();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates one minimal fallback when the drawer cannot resolve its serialized children.
        /// </summary>
        /// <param name="labelText">Display label shown by the fallback.</param>
        /// <returns>The fallback visual element.</returns>
        private static VisualElement CreateInvalidDrawerFallback(string labelText)
        {
            VisualElement root = new();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;

            Label label = new(labelText);
            label.style.minWidth = Mathf.Max(FieldLabelMinimumWidth, EditorGUIUtility.labelWidth);
            label.style.marginRight = 4f;
            root.Add(label);

            HelpBox helpBox = new(
                "Conversation picker unavailable.",
                HelpBoxMessageType.Warning);
            helpBox.style.flexGrow = 1f;
            root.Add(helpBox);
            return root;
        }

        /// <summary>
        /// Creates the main picker button used by the property drawer.
        /// </summary>
        /// <param name="pickerLabel">Primary button label that should be refreshed by the drawer.</param>
        /// <returns>The configured picker button.</returns>
        private static Button CreatePickerButton(out Label pickerLabel)
        {
            Button button = new();
            button.style.flexGrow = 1f;
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.SpaceBetween;
            button.style.paddingLeft = ClosedFieldHorizontalPadding;
            button.style.paddingRight = ClosedFieldHorizontalPadding;
            button.style.paddingTop = ClosedFieldVerticalPadding;
            button.style.paddingBottom = ClosedFieldVerticalPadding;
            button.style.minHeight = ClosedFieldMinimumHeight;
            button.style.backgroundColor = GetClosedFieldBackgroundColor();
            button.style.borderTopColor = GetClosedFieldBorderColor();
            button.style.borderRightColor = GetClosedFieldBorderColor();
            button.style.borderBottomColor = GetClosedFieldBorderColor();
            button.style.borderLeftColor = GetClosedFieldBorderColor();
            button.style.unityTextAlign = TextAnchor.MiddleLeft;

            pickerLabel = new();
            pickerLabel.style.flexGrow = 1f;
            pickerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            pickerLabel.style.whiteSpace = WhiteSpace.NoWrap;
            pickerLabel.style.overflow = Overflow.Hidden;
            pickerLabel.style.textOverflow = TextOverflow.Ellipsis;
            button.Add(pickerLabel);

            Label chevronLabel = new("▾");
            chevronLabel.style.marginLeft = 8f;
            chevronLabel.style.color = GetClosedFieldChevronColor();
            button.Add(chevronLabel);

            return button;
        }

        /// <summary>
        /// Creates the clear button used by the property drawer.
        /// </summary>
        /// <returns>The configured clear button.</returns>
        private static Button CreateClearButton()
        {
            Button button = new() { text = "X" };
            button.style.width = ClearButtonWidth;
            button.style.minWidth = ClearButtonWidth;
            button.style.minHeight = ClosedFieldMinimumHeight;
            button.style.marginLeft = ButtonSpacing;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            return button;
        }

        /// <summary>
        /// Gets the screen rect used to anchor the dropdown window from one UI Toolkit element.
        /// </summary>
        /// <param name="anchor">Anchor element.</param>
        /// <returns>The anchor rect in screen space.</returns>
        private static Rect GetScreenRect(VisualElement anchor)
        {
            if (anchor == null)
            {
                return new Rect(0f, 0f, DropdownWidth, EditorGUIUtility.singleLineHeight);
            }

            Rect worldBound = anchor.worldBound;
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(
                worldBound.xMin,
                worldBound.yMin));
            return new Rect(screenPoint.x, screenPoint.y, worldBound.width, worldBound.height);
        }

        /// <summary>
        /// Builds the option list from every ConversationTable asset found in the project.
        /// </summary>
        /// <returns>The authored conversation options available to the picker.</returns>
        private static IReadOnlyList<ConversationOption> BuildOptions()
        {
            List<ConversationOption> options = new();
            string[] tableGuids = AssetDatabase.FindAssets("t:ConversationTable");

            for (int tableIndex = 0; tableIndex < tableGuids.Length; tableIndex++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(tableGuids[tableIndex]);
                ConversationTable table = AssetDatabase.LoadAssetAtPath<ConversationTable>(assetPath);

                if (table == null || table.Conversations == null)
                {
                    continue;
                }

                string tableDisplayName = ResolveTableDisplayName(table);

                for (int conversationIndex = 0;
                     conversationIndex < table.Conversations.Count;
                     conversationIndex++)
                {
                    ConversationDefinition conversation = table.Conversations[conversationIndex];

                    if (conversation == null)
                    {
                        continue;
                    }

                    options.Add(new ConversationOption(
                        table,
                        conversation.ConversationId,
                        conversation.Title,
                        BuildConversationTooltip(tableDisplayName, conversation.Title),
                        BuildSearchText(table, conversation.Title)));
                }
            }

            options.Sort((left, right) =>
                string.Compare(left.DisplayLabel, right.DisplayLabel, StringComparison.OrdinalIgnoreCase));
            return options;
        }

        /// <summary>
        /// Applies one selected authored conversation option to the serialized property.
        /// </summary>
        /// <param name="serializedObject">Serialized object that owns the property.</param>
        /// <param name="propertyPath">Serialized property path.</param>
        /// <param name="option">Selected conversation option.</param>
        private static void ApplySelection(
            SerializedObject serializedObject,
            string propertyPath,
            ConversationOption option)
        {
            if (serializedObject == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty property = serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            SerializedProperty tableProperty = property.FindPropertyRelative("_table");
            SerializedProperty conversationIdProperty = property.FindPropertyRelative("_conversationId");
            SerializedProperty conversationTitleProperty =
                property.FindPropertyRelative("_conversationTitle");

            if (tableProperty == null
                || conversationIdProperty == null
                || conversationTitleProperty == null)
            {
                return;
            }

            if (option?.Table != null)
            {
                option.Table.EnsureAuthoringIds();
                EditorUtility.SetDirty(option.Table);
            }

            SerializableGuid conversationId = option?.ConversationId ?? SerializableGuid.Empty;

            if (option?.Table != null
                && conversationId == SerializableGuid.Empty
                && !string.IsNullOrWhiteSpace(option.ConversationTitle)
                && ConversationAuthoredRuntimeBuilder.TryResolveConversation(
                    option.Table,
                    option.ConversationTitle,
                    out ConversationDefinition resolvedConversation,
                    out _))
            {
                conversationId = resolvedConversation.ConversationId;
            }

            tableProperty.objectReferenceValue = option?.Table;
            WriteSerializableGuid(conversationIdProperty, conversationId);
            conversationTitleProperty.stringValue = option?.ConversationTitle ?? string.Empty;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Clears the stored conversation selection.
        /// </summary>
        /// <param name="serializedObject">Serialized object that owns the property.</param>
        /// <param name="propertyPath">Serialized property path.</param>
        private static void ClearSelection(
            SerializedObject serializedObject,
            string propertyPath)
        {
            if (serializedObject == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty property = serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            SerializedProperty tableProperty = property.FindPropertyRelative("_table");
            SerializedProperty conversationIdProperty = property.FindPropertyRelative("_conversationId");
            SerializedProperty conversationTitleProperty =
                property.FindPropertyRelative("_conversationTitle");

            if (tableProperty == null
                || conversationIdProperty == null
                || conversationTitleProperty == null)
            {
                return;
            }

            tableProperty.objectReferenceValue = null;
            WriteSerializableGuid(conversationIdProperty, SerializableGuid.Empty);
            conversationTitleProperty.stringValue = string.Empty;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Builds the current selection label shown by the picker field.
        /// </summary>
        /// <param name="tableProperty">Serialized table property.</param>
        /// <param name="conversationIdProperty">Serialized conversation-id property.</param>
        /// <param name="conversationTitleProperty">Serialized conversation-title property.</param>
        /// <param name="availableWidth">Available label width.</param>
        /// <param name="style">Style used for width measurement.</param>
        /// <returns>The label shown by the picker field.</returns>
        private static string BuildCurrentSelectionLabel(
            SerializedProperty tableProperty,
            SerializedProperty conversationIdProperty,
            SerializedProperty conversationTitleProperty,
            float availableWidth,
            GUIStyle style)
        {
            ConversationTable table = tableProperty.objectReferenceValue as ConversationTable;

            if (table == null)
            {
                return "Select Conversation...";
            }

            SerializableGuid conversationId = ReadSerializableGuid(conversationIdProperty);
            string conversationTitle = conversationTitleProperty.stringValue ?? string.Empty;

            if (conversationId != SerializableGuid.Empty
                && table.TryGetConversation(conversationId, out ConversationDefinition conversation)
                && conversation != null)
            {
                conversationTitle = conversation.Title;
            }

            if (string.IsNullOrWhiteSpace(conversationTitle))
            {
                return TruncateMiddle("Conversation", availableWidth, style);
            }

            return TruncateMiddle(
                BuildConversationLeafLabel(conversationTitle),
                availableWidth,
                style);
        }

        /// <summary>
        /// Builds the current selection tooltip shown by the picker field.
        /// </summary>
        /// <param name="tableProperty">Serialized table property.</param>
        /// <param name="conversationIdProperty">Serialized conversation-id property.</param>
        /// <param name="conversationTitleProperty">Serialized conversation-title property.</param>
        /// <returns>The full tooltip shown by the picker field.</returns>
        private static string BuildCurrentSelectionTooltip(
            SerializedProperty tableProperty,
            SerializedProperty conversationIdProperty,
            SerializedProperty conversationTitleProperty)
        {
            ConversationTable table = tableProperty.objectReferenceValue as ConversationTable;

            if (table == null)
            {
                return "Select one authored conversation.";
            }

            SerializableGuid conversationId = ReadSerializableGuid(conversationIdProperty);
            string conversationTitle = conversationTitleProperty.stringValue ?? string.Empty;

            if (conversationId != SerializableGuid.Empty
                && table.TryGetConversation(conversationId, out ConversationDefinition conversation)
                && conversation != null)
            {
                conversationTitle = conversation.Title;
            }

            return BuildConversationTooltip(
                ResolveTableDisplayName(table),
                conversationTitle);
        }

        /// <summary>
        /// Builds one searchable text payload for the provided table and conversation title.
        /// </summary>
        /// <param name="table">Table shown in the picker.</param>
        /// <param name="conversationTitle">Authored conversation title path.</param>
        /// <returns>The combined search text.</returns>
        private static string BuildSearchText(
            ConversationTable table,
            string conversationTitle)
        {
            string tableDisplayName = ResolveTableDisplayName(table);
            string assetName = table != null ? table.name : string.Empty;
            string[] titleSegments = SplitTitleSegments(conversationTitle);
            string leafSegment = titleSegments.Length > 0
                ? titleSegments[titleSegments.Length - 1]
                : string.Empty;
            return $"{tableDisplayName} {assetName} {conversationTitle} {leafSegment}";
        }

        /// <summary>
        /// Resolves the effective table display name used by editor-facing pickers.
        /// </summary>
        /// <param name="table">Table that should be named.</param>
        /// <returns>The human-readable table name.</returns>
        private static string ResolveTableDisplayName(ConversationTable table)
        {
            return table == null ? string.Empty : table.DisplayName;
        }

        /// <summary>
        /// Builds the metadata line shown below the primary conversation title.
        /// </summary>
        /// <param name="tableName">Table name shown by the picker.</param>
        /// <param name="conversationTitle">Authored conversation title path.</param>
        /// <param name="availableWidth">Available width used to shorten the label when needed.</param>
        /// <param name="style">Style used for width measurement.</param>
        /// <returns>The metadata line shown below the title.</returns>
        private static string BuildSecondaryContextLabel(
            string tableName,
            string conversationTitle,
            float availableWidth,
            GUIStyle style)
        {
            string[] segments = SplitTitleSegments(conversationTitle);

            if (segments.Length <= 1)
            {
                return TruncateMiddle(tableName, availableWidth, style);
            }

            string[] groupSegments = new string[segments.Length - 1];
            Array.Copy(segments, groupSegments, segments.Length - 1);
            string groupPath = BuildGroupPath(groupSegments);
            string fullLabel = $"{tableName} • {groupPath}";

            if (FitsWithinWidth(fullLabel, availableWidth, style))
            {
                return fullLabel;
            }

            string shortenedGroupPath = $"{groupSegments[0]}...";
            string shortenedLabel = $"{tableName} • {shortenedGroupPath}";

            if (FitsWithinWidth(shortenedLabel, availableWidth, style))
            {
                return shortenedLabel;
            }

            return TruncateMiddle(shortenedLabel, availableWidth, style);
        }

        /// <summary>
        /// Builds the full tooltip payload shown for one authored conversation option.
        /// </summary>
        /// <param name="tableName">Table name shown by the picker.</param>
        /// <param name="conversationTitle">Authored conversation title path.</param>
        /// <returns>The multi-line tooltip payload.</returns>
        private static string BuildConversationTooltip(
            string tableName,
            string conversationTitle)
        {
            string primaryLabel = BuildConversationLeafLabel(conversationTitle);
            string secondaryLabel = BuildSecondaryContextLabel(
                tableName,
                conversationTitle,
                float.PositiveInfinity,
                EditorStyles.miniLabel);
            return $"{primaryLabel}\n{secondaryLabel}";
        }

        /// <summary>
        /// Builds the primary conversation label shown as the selection title.
        /// </summary>
        /// <param name="conversationTitle">Authored conversation title path.</param>
        /// <returns>The primary conversation title.</returns>
        private static string BuildConversationLeafLabel(string conversationTitle)
        {
            string[] segments = SplitTitleSegments(conversationTitle);
            return segments.Length > 0
                ? HumanizeLeafSegment(segments[segments.Length - 1])
                : "Conversation";
        }

        /// <summary>
        /// Builds the normalized group path used in the popup metadata row.
        /// </summary>
        /// <param name="groupSegments">Conversation title path segments before the leaf.</param>
        /// <returns>The formatted group path.</returns>
        private static string BuildGroupPath(string[] groupSegments)
        {
            return string.Join(" -> ", groupSegments ?? Array.Empty<string>());
        }

        /// <summary>
        /// Splits one authored title path into normalized segments.
        /// </summary>
        /// <param name="conversationTitle">Authored conversation title path.</param>
        /// <returns>The normalized title-path segments.</returns>
        private static string[] SplitTitleSegments(string conversationTitle)
        {
            return (conversationTitle ?? string.Empty).Split(
                new[] { '/', '|' },
                StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Builds the leaf display segment shown to the picker.
        /// </summary>
        /// <param name="leafSegment">Leaf segment that should be humanized.</param>
        /// <returns>The formatted leaf segment.</returns>
        private static string HumanizeLeafSegment(string leafSegment)
        {
            string normalizedSegment = leafSegment?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedSegment)
                || normalizedSegment.IndexOf(' ') < 0)
            {
                return string.IsNullOrWhiteSpace(normalizedSegment)
                    ? "Conversation"
                    : normalizedSegment;
            }

            string[] words = normalizedSegment.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            for (int index = 0; index < words.Length; index++)
            {
                string word = words[index];

                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                words[index] = char.ToUpper(
                    word[0],
                    CultureInfo.InvariantCulture) + word[1..];
            }

            return string.Join(" ", words);
        }

        /// <summary>
        /// Gets whether the provided label fits inside the available width.
        /// </summary>
        /// <param name="value">Label that should be measured.</param>
        /// <param name="availableWidth">Available label width.</param>
        /// <param name="style">Style used for width measurement.</param>
        /// <returns>True when the label fits inside the available width.</returns>
        private static bool FitsWithinWidth(string value, float availableWidth, GUIStyle style)
        {
            if (float.IsPositiveInfinity(availableWidth) || availableWidth <= 0f)
            {
                return true;
            }

            return style.CalcSize(new GUIContent(value ?? string.Empty)).x <= availableWidth;
        }

        /// <summary>
        /// Truncates one label from the middle until it fits inside the available width.
        /// </summary>
        /// <param name="value">Label that should be truncated.</param>
        /// <param name="availableWidth">Available label width.</param>
        /// <param name="style">Style used for width measurement.</param>
        /// <returns>The truncated label.</returns>
        private static string TruncateMiddle(string value, float availableWidth, GUIStyle style)
        {
            const string Ellipsis = "...";

            if (string.IsNullOrWhiteSpace(value)
                || FitsWithinWidth(value, availableWidth, style)
                || float.IsPositiveInfinity(availableWidth)
                || availableWidth <= 0f)
            {
                return value ?? string.Empty;
            }

            int prefixLength = Mathf.Max(1, (value.Length - Ellipsis.Length) / 2);
            int suffixLength = Mathf.Max(1, value.Length - prefixLength - Ellipsis.Length);

            while (prefixLength > 0 && suffixLength > 0)
            {
                string candidate = value[..prefixLength]
                    + Ellipsis
                    + value[^suffixLength..];

                if (FitsWithinWidth(candidate, availableWidth, style))
                {
                    return candidate;
                }

                if (prefixLength >= suffixLength)
                {
                    prefixLength--;
                }
                else
                {
                    suffixLength--;
                }
            }

            return Ellipsis;
        }

        /// <summary>
        /// Reads one SerializableGuid value from the serialized property.
        /// </summary>
        /// <param name="guidProperty">SerializableGuid serialized property.</param>
        /// <returns>The resolved SerializableGuid value.</returns>
        private static SerializableGuid ReadSerializableGuid(SerializedProperty guidProperty)
        {
            if (guidProperty == null)
            {
                return SerializableGuid.Empty;
            }

            SerializedProperty part1Property = guidProperty.FindPropertyRelative("Part1");
            SerializedProperty part2Property = guidProperty.FindPropertyRelative("Part2");
            SerializedProperty part3Property = guidProperty.FindPropertyRelative("Part3");
            SerializedProperty part4Property = guidProperty.FindPropertyRelative("Part4");

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
        /// Writes one SerializableGuid value into the serialized property.
        /// </summary>
        /// <param name="guidProperty">SerializableGuid serialized property.</param>
        /// <param name="value">Value that should be stored.</param>
        private static void WriteSerializableGuid(
            SerializedProperty guidProperty,
            SerializableGuid value)
        {
            if (guidProperty == null)
            {
                return;
            }

            SerializedProperty part1Property = guidProperty.FindPropertyRelative("Part1");
            SerializedProperty part2Property = guidProperty.FindPropertyRelative("Part2");
            SerializedProperty part3Property = guidProperty.FindPropertyRelative("Part3");
            SerializedProperty part4Property = guidProperty.FindPropertyRelative("Part4");

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
        /// Gets the closed-field background color.
        /// </summary>
        /// <returns>The closed-field background color.</returns>
        private static Color GetClosedFieldBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.19f, 0.19f, 0.2f, 1f)
                : new Color(0.94f, 0.94f, 0.95f, 1f);
        }

        /// <summary>
        /// Gets the closed-field border color.
        /// </summary>
        /// <returns>The closed-field border color.</returns>
        private static Color GetClosedFieldBorderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.29f, 0.31f, 1f)
                : new Color(0.76f, 0.78f, 0.8f, 1f);
        }

        /// <summary>
        /// Gets the closed-field chevron color.
        /// </summary>
        /// <returns>The closed-field chevron color.</returns>
        private static Color GetClosedFieldChevronColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.73f, 0.73f, 0.75f, 1f)
                : new Color(0.38f, 0.4f, 0.43f, 1f);
        }

        /// <summary>
        /// Gets the dropdown window background color.
        /// </summary>
        /// <returns>The dropdown window background color.</returns>
        private static Color GetDropdownWindowBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.1f, 0.1f, 0.11f, 1f)
                : new Color(0.96f, 0.96f, 0.97f, 1f);
        }

        /// <summary>
        /// Gets the dropdown card background color.
        /// </summary>
        /// <param name="isSelected">Whether the option is selected.</param>
        /// <param name="isHovered">Whether the option is hovered.</param>
        /// <returns>The dropdown card background color.</returns>
        private static Color GetDropdownCardBackgroundColor(bool isSelected, bool isHovered)
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (isSelected)
                {
                    return new Color(0.19f, 0.19f, 0.2f, 1f);
                }

                if (isHovered)
                {
                    return new Color(0.16f, 0.16f, 0.17f, 1f);
                }

                return new Color(0.13f, 0.13f, 0.14f, 1f);
            }

            if (isSelected)
            {
                return new Color(0.9f, 0.9f, 0.91f, 1f);
            }

            if (isHovered)
            {
                return new Color(0.94f, 0.94f, 0.95f, 1f);
            }

            return new Color(0.98f, 0.98f, 0.99f, 1f);
        }

        /// <summary>
        /// Gets the dropdown card border color.
        /// </summary>
        /// <param name="isSelected">Whether the option is selected.</param>
        /// <param name="isHovered">Whether the option is hovered.</param>
        /// <returns>The dropdown card border color.</returns>
        private static Color GetDropdownCardBorderColor(bool isSelected, bool isHovered)
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (isSelected)
                {
                    return new Color(0.42f, 0.43f, 0.46f, 1f);
                }

                if (isHovered)
                {
                    return new Color(0.31f, 0.32f, 0.34f, 1f);
                }

                return new Color(0.23f, 0.24f, 0.26f, 1f);
            }

            if (isSelected)
            {
                return new Color(0.62f, 0.64f, 0.67f, 1f);
            }

            if (isHovered)
            {
                return new Color(0.78f, 0.79f, 0.82f, 1f);
            }

            return new Color(0.84f, 0.85f, 0.87f, 1f);
        }

        /// <summary>
        /// Gets the dropdown card accent color.
        /// </summary>
        /// <param name="isSelected">Whether the option is selected.</param>
        /// <param name="isHovered">Whether the option is hovered.</param>
        /// <returns>The dropdown card accent color.</returns>
        private static Color GetDropdownCardAccentColor(bool isSelected, bool isHovered)
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (isSelected)
                {
                    return new Color(0.63f, 0.64f, 0.67f, 1f);
                }

                if (isHovered)
                {
                    return new Color(0.45f, 0.46f, 0.49f, 1f);
                }

                return new Color(0.31f, 0.32f, 0.35f, 1f);
            }

            if (isSelected)
            {
                return new Color(0.47f, 0.49f, 0.53f, 1f);
            }

            if (isHovered)
            {
                return new Color(0.67f, 0.69f, 0.72f, 1f);
            }

            return new Color(0.79f, 0.8f, 0.83f, 1f);
        }

        /// <summary>
        /// Gets the dropdown primary text color.
        /// </summary>
        /// <param name="isSelected">Whether the option is selected.</param>
        /// <param name="isHovered">Whether the option is hovered.</param>
        /// <returns>The dropdown primary text color.</returns>
        private static Color GetDropdownPrimaryTextColor(bool isSelected, bool isHovered)
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (isSelected)
                {
                    return new Color(0.96f, 0.96f, 0.97f, 1f);
                }

                if (isHovered)
                {
                    return new Color(0.93f, 0.93f, 0.94f, 1f);
                }

                return new Color(0.89f, 0.89f, 0.9f, 1f);
            }

            if (isSelected)
            {
                return new Color(0.18f, 0.19f, 0.22f, 1f);
            }

            if (isHovered)
            {
                return new Color(0.16f, 0.17f, 0.19f, 1f);
            }

            return new Color(0.2f, 0.21f, 0.24f, 1f);
        }

        /// <summary>
        /// Gets the dropdown secondary text color.
        /// </summary>
        /// <param name="isSelected">Whether the option is selected.</param>
        /// <param name="isHovered">Whether the option is hovered.</param>
        /// <returns>The dropdown secondary text color.</returns>
        private static Color GetDropdownSecondaryTextColor(bool isSelected, bool isHovered)
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (isSelected)
                {
                    return new Color(0.76f, 0.77f, 0.79f, 1f);
                }

                if (isHovered)
                {
                    return new Color(0.69f, 0.7f, 0.72f, 1f);
                }

                return new Color(0.6f, 0.61f, 0.63f, 1f);
            }

            if (isSelected)
            {
                return new Color(0.34f, 0.36f, 0.39f, 1f);
            }

            if (isHovered)
            {
                return new Color(0.4f, 0.42f, 0.45f, 1f);
            }

            return new Color(0.48f, 0.49f, 0.52f, 1f);
        }

        /// <summary>
        /// Gets the minimum card block height used by each dropdown item.
        /// </summary>
        /// <returns>The minimum dropdown item height.</returns>
        private static float GetDropdownItemBlockHeight()
        {
            return 52f;
        }

        #endregion
    }
}