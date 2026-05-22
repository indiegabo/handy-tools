using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Provides one reusable blackboard overlay shell that hosts may mount
    /// explicitly when their graph workflow needs it.
    /// </summary>
    public abstract class GraphBlackboardOverlayView : VisualElement
    {
        private readonly Label _bindingLabel;
        private readonly Label _entryCountLabel;
        private readonly Button _collapseButton;
        private readonly VisualElement _contentContainer;
        private readonly HelpBox _stateBox;
        private readonly ScrollView _entriesContainer;

        private bool _isExpanded = true;

        /// <summary>
        /// Initializes one reusable blackboard overlay shell.
        /// </summary>
        /// <param name="titleText">Header title shown above the entry list.</param>
        /// <param name="addButtonText">Toolbar label used for the add-entry action.</param>
        /// <param name="addButtonTooltip">Tooltip shown for the add-entry button.</param>
        protected GraphBlackboardOverlayView(
            string titleText,
            string addButtonText = "Add",
            string addButtonTooltip = "Add blackboard entry.")
        {
            style.flexDirection = FlexDirection.Column;
            style.minWidth = 280f;
            style.maxWidth = 360f;
            style.paddingLeft = 10f;
            style.paddingRight = 10f;
            style.paddingTop = 8f;
            style.paddingBottom = 10f;
            style.backgroundColor = new StyleColor(new Color(0.07f, 0.07f, 0.07f, 0.86f));
            style.borderLeftWidth = 1f;
            style.borderRightWidth = 1f;
            style.borderTopWidth = 1f;
            style.borderBottomWidth = 1f;
            style.borderLeftColor = new Color(0.28f, 0.28f, 0.28f, 0.85f);
            style.borderRightColor = new Color(0.28f, 0.28f, 0.28f, 0.85f);
            style.borderTopColor = new Color(0.28f, 0.28f, 0.28f, 0.85f);
            style.borderBottomColor = new Color(0.28f, 0.28f, 0.28f, 0.85f);
            style.borderTopLeftRadius = 10f;
            style.borderTopRightRadius = 10f;
            style.borderBottomLeftRadius = 10f;
            style.borderBottomRightRadius = 10f;
            style.overflow = Overflow.Hidden;

            VisualElement header = new();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingBottom = 8f;
            header.style.marginBottom = 8f;
            header.style.borderBottomWidth = 1f;
            header.style.borderBottomColor = new Color(0.26f, 0.26f, 0.26f, 0.75f);

            VisualElement titleColumn = new();
            titleColumn.style.flexGrow = 1f;

            Label titleLabel = new(titleText);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 13f;
            titleLabel.style.marginBottom = 1f;
            titleColumn.Add(titleLabel);

            _bindingLabel = new("Graph (Unbound)");
            _bindingLabel.style.fontSize = 10f;
            _bindingLabel.style.color = new Color(0.80f, 0.80f, 0.80f, 0.82f);
            _bindingLabel.style.marginBottom = 1f;
            titleColumn.Add(_bindingLabel);

            _entryCountLabel = new("No entries");
            _entryCountLabel.style.fontSize = 10f;
            _entryCountLabel.style.color = new Color(0.67f, 0.67f, 0.67f, 0.78f);
            titleColumn.Add(_entryCountLabel);

            header.Add(titleColumn);

            _collapseButton = new Button(ToggleExpanded)
            {
                text = "v",
            };
            _collapseButton.tooltip = "Collapse or expand the blackboard panel.";
            _collapseButton.style.width = 24f;
            _collapseButton.style.height = 20f;
            _collapseButton.style.minWidth = 24f;
            _collapseButton.style.paddingLeft = 0f;
            _collapseButton.style.paddingRight = 0f;
            header.Add(_collapseButton);

            Add(header);

            _contentContainer = new VisualElement();
            _contentContainer.style.flexDirection = FlexDirection.Column;
            _contentContainer.style.display = DisplayStyle.Flex;

            VisualElement toolbar = new();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.justifyContent = Justify.FlexEnd;
            toolbar.style.marginBottom = 6f;

            Button addButton = new(HandleAddRequested)
            {
                text = addButtonText,
            };
            addButton.tooltip = addButtonTooltip;
            addButton.style.height = 24f;
            addButton.style.width = 56f;
            addButton.style.minWidth = 56f;
            addButton.style.alignSelf = Align.FlexEnd;
            toolbar.Add(addButton);

            _stateBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _stateBox.style.marginBottom = 6f;
            ApplyOverlayInformativeBoxStyle(_stateBox);
            HideOverlayStateBox();

            _entriesContainer = new ScrollView(ScrollViewMode.Vertical);
            _entriesContainer.style.maxHeight = 320f;
            _entriesContainer.style.flexGrow = 1f;
            _entriesContainer.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _entriesContainer.contentContainer.style.flexDirection = FlexDirection.Column;
            _entriesContainer.contentContainer.style.flexGrow = 1f;
            _entriesContainer.contentContainer.style.paddingRight = 2f;

            _contentContainer.Add(toolbar);
            _contentContainer.Add(_stateBox);
            _contentContainer.Add(_entriesContainer);
            Add(_contentContainer);
        }

        /// <summary>
        /// Called when the host requests one new blackboard entry.
        /// </summary>
        protected virtual void HandleAddRequested()
        {
        }

        /// <summary>
        /// Updates the header with the current graph binding and entry count.
        /// </summary>
        /// <param name="bindingLabel">Current binding label shown below the title.</param>
        /// <param name="entryCount">Current number of authored entries.</param>
        protected void UpdateOverlayHeader(string bindingLabel, int entryCount)
        {
            _bindingLabel.text = bindingLabel ?? string.Empty;
            _entryCountLabel.text = entryCount switch
            {
                <= 0 => "No entries",
                1 => "1 entry",
                _ => $"{entryCount} entries",
            };
        }

        /// <summary>
        /// Enables or disables the mutable overlay content.
        /// </summary>
        /// <param name="isEnabled">True when the overlay should be editable.</param>
        protected void SetOverlayContentEnabled(bool isEnabled)
        {
            _contentContainer.SetEnabled(isEnabled);
        }

        /// <summary>
        /// Clears all rendered entry rows.
        /// </summary>
        protected void ClearOverlayEntries()
        {
            _entriesContainer.Clear();
        }

        /// <summary>
        /// Adds one entry row to the rendered overlay list.
        /// </summary>
        /// <param name="entryElement">Entry element to append.</param>
        protected void AddOverlayEntry(VisualElement entryElement)
        {
            if (entryElement != null)
            {
                _entriesContainer.Add(entryElement);
            }
        }

        /// <summary>
        /// Displays one overlay status message.
        /// </summary>
        /// <param name="message">Message shown above the entry list.</param>
        /// <param name="messageType">Severity of the message.</param>
        protected void ShowOverlayStateBox(string message, HelpBoxMessageType messageType)
        {
            _stateBox.text = message;
            _stateBox.messageType = messageType;
            _stateBox.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hides the current overlay status message.
        /// </summary>
        protected void HideOverlayStateBox()
        {
            _stateBox.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Applies the shared spacing used by overlay help boxes.
        /// </summary>
        /// <param name="helpBox">Help box to style.</param>
        protected static void ApplyOverlayInformativeBoxStyle(HelpBox helpBox)
        {
            helpBox.style.paddingLeft = 12f;
            helpBox.style.paddingRight = 12f;
            helpBox.style.paddingTop = 10f;
            helpBox.style.paddingBottom = 10f;
        }

        /// <summary>
        /// Applies the shared card-like styling used by blackboard entry rows.
        /// </summary>
        /// <param name="container">Entry container to style.</param>
        protected static void ApplyOverlayEntryContainerStyle(VisualElement container)
        {
            container.style.marginBottom = 6f;
            container.style.paddingLeft = 8f;
            container.style.paddingRight = 8f;
            container.style.paddingTop = 6f;
            container.style.paddingBottom = 6f;
            container.style.alignSelf = Align.Stretch;
            container.style.flexShrink = 0f;
            container.style.backgroundColor = new StyleColor(
                new Color32(0x38, 0x38, 0x38, 0xD8));
            container.style.borderLeftWidth = 1f;
            container.style.borderRightWidth = 1f;
            container.style.borderTopWidth = 1f;
            container.style.borderBottomWidth = 1f;
            container.style.borderLeftColor = new Color(0.30f, 0.30f, 0.30f, 0.80f);
            container.style.borderRightColor = new Color(0.30f, 0.30f, 0.30f, 0.80f);
            container.style.borderTopColor = new Color(0.30f, 0.30f, 0.30f, 0.80f);
            container.style.borderBottomColor = new Color(0.30f, 0.30f, 0.30f, 0.80f);
            container.style.borderTopLeftRadius = 6f;
            container.style.borderTopRightRadius = 6f;
            container.style.borderBottomLeftRadius = 6f;
            container.style.borderBottomRightRadius = 6f;
        }

        /// <summary>
        /// Applies the foldout layout that keeps one entry vertically stacked.
        /// </summary>
        /// <param name="foldout">Foldout used to edit one entry.</param>
        protected static void ApplyOverlayFoldoutStyle(Foldout foldout)
        {
            foldout.style.flexDirection = FlexDirection.Column;
            foldout.style.overflow = Overflow.Hidden;
            foldout.contentContainer.style.flexDirection = FlexDirection.Column;
            foldout.contentContainer.style.marginTop = 6f;

            Toggle toggle = foldout.Q<Toggle>();

            if (toggle != null)
            {
                toggle.style.minWidth = 0f;
                toggle.style.flexGrow = 1f;
            }

            Label label = toggle?.Q<Label>();

            if (label != null)
            {
                label.style.minWidth = 0f;
                label.style.flexShrink = 1f;
                label.style.whiteSpace = WhiteSpace.Normal;
            }
        }

        /// <summary>
        /// Creates one vertical field section that avoids horizontal overflow.
        /// </summary>
        /// <param name="labelText">Caption shown above the field.</param>
        /// <param name="field">Field element rendered below the caption.</param>
        /// <returns>The composed vertical section.</returns>
        protected static VisualElement CreateOverlayFieldSection(
            string labelText,
            VisualElement field)
        {
            VisualElement section = new();
            section.style.flexDirection = FlexDirection.Column;
            section.style.alignSelf = Align.Stretch;
            section.style.marginBottom = 6f;

            Label label = new(labelText);
            label.style.fontSize = 10f;
            label.style.marginBottom = 3f;
            section.Add(label);

            ApplyOverlayFieldControlStyle(field);
            section.Add(field);
            return section;
        }

        /// <summary>
        /// Applies one full-width layout to one editor field and hides inline labels.
        /// </summary>
        /// <param name="field">Field element to normalize.</param>
        protected static void ApplyOverlayFieldControlStyle(VisualElement field)
        {
            if (field == null)
            {
                return;
            }

            field.style.alignSelf = Align.Stretch;
            field.style.flexGrow = 1f;
            field.style.flexShrink = 1f;
            field.style.minWidth = 0f;

            Label inlineLabel = field.Q<Label>(className: "unity-base-field__label");

            if (inlineLabel != null)
            {
                inlineLabel.style.display = DisplayStyle.None;
            }
        }

        private void ToggleExpanded()
        {
            _isExpanded = !_isExpanded;
            _contentContainer.style.display = _isExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _collapseButton.text = _isExpanded ? "v" : ">";
        }
    }
}