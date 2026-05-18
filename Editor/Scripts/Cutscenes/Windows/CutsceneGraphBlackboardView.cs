using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    /// <summary>
    /// Renders one dedicated editor surface for authoring graph blackboard
    /// entries inside the cutscene graph window.
    /// </summary>
    public sealed class CutsceneGraphBlackboardView : VisualElement
    {
        private static readonly Color DragSourceIdleColor = new(0f, 0f, 0f, 0f);
        private static readonly Color DragSourceHoverColor = new(0.23f, 0.23f, 0.23f, 0.72f);
        private const string BaseFieldLabelClassName = "unity-base-field__label";
        private const string DefaultEntryKeyName = "Entry";
        private const string GraphPropertyPath = "_graph";
        private const string BlackboardPropertyName = "_blackboard";
        private const string EntriesPropertyName = "_entries";
        private const string KeyPropertyName = "_key";
        private const string ValuePropertyName = "_value";
        private const string ValueFieldName = "Value";
        private const string ObjectTypeNamePropertyName = "_objectTypeName";
        private const string EnumTypeNamePropertyName = "_enumTypeName";
        private const string EnumValueNamePropertyName = "_valueName";

        private static IReadOnlyList<Type> s_pickableObjectTypes;
        private static IReadOnlyList<Type> s_pickableEnumTypes;

        private readonly HelpBox _stateBox;
        private readonly ScrollView _entriesContainer;
        private readonly VisualElement _contentContainer;
        private readonly Label _bindingLabel;
        private readonly Label _entryCountLabel;
        private readonly Button _collapseButton;
        private readonly Dictionary<string, bool> _entryExpansionStates =
            new(StringComparer.OrdinalIgnoreCase);

        private CutsceneDirector _director;
        private bool _isExpanded = true;

        /// <summary>
        /// Raised after the view commits one change to the serialized graph.
        /// </summary>
        public event Action BlackboardChanged;

        /// <summary>
        /// Creates the UI Toolkit hierarchy used by the blackboard editor.
        /// </summary>
        public CutsceneGraphBlackboardView()
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

            Label titleLabel = new("Blackboard");
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

            _contentContainer = new();
            _contentContainer.style.flexDirection = FlexDirection.Column;
            _contentContainer.style.display = DisplayStyle.Flex;

            VisualElement toolbar = new();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.justifyContent = Justify.FlexEnd;
            toolbar.style.marginBottom = 6f;

            Button addButton = new(HandleAddEntry)
            {
                text = "Add",
            };
            addButton.tooltip = "Add blackboard entry.";
            addButton.style.height = 24f;
            addButton.style.width = 56f;
            addButton.style.minWidth = 56f;
            addButton.style.alignSelf = Align.FlexEnd;
            toolbar.Add(addButton);

            _stateBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _stateBox.style.marginBottom = 6f;
            ApplyInformativeBoxStyle(_stateBox);
            HideStateBox();

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

            Refresh();
        }

        /// <summary>
        /// Binds the view to one director and rebuilds the visible rows.
        /// </summary>
        /// <param name="director">Director that owns the target graph.</param>
        public void BindDirector(CutsceneDirector director)
        {
            _entryExpansionStates.Clear();
            _director = director;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the entry list from the currently bound director.
        /// </summary>
        public void Refresh()
        {
            _entriesContainer.Clear();

            if (_director == null)
            {
                _contentContainer.SetEnabled(false);
                RefreshHeader(0);
                HideStateBox();
                return;
            }

            _contentContainer.SetEnabled(true);

            SerializedObject serializedDirector = new(_director);
            serializedDirector.UpdateIfRequiredOrScript();
            SerializedProperty entriesProperty = FindEntriesProperty(serializedDirector);

            if (entriesProperty == null)
            {
                RefreshHeader(0);
                ShowStateBox(
                    "The bound director does not expose a serialized blackboard entries list.",
                    HelpBoxMessageType.Error);
                return;
            }

            RefreshHeader(entriesProperty.arraySize);
            HideStateBox();

            for (int index = 0; index < entriesProperty.arraySize; index++)
            {
                SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);

                if (entryProperty == null)
                {
                    continue;
                }

                _entriesContainer.Add(CreateEntryElement(entryProperty, index));
            }
        }

        /// <summary>
        /// Adds one new string entry to the blackboard.
        /// </summary>
        private void HandleAddEntry()
        {
            if (_director == null)
            {
                return;
            }

            ModifyEntries(
                "Add Cutscene Blackboard Entry",
                entriesProperty =>
                {
                    int entryIndex = entriesProperty.arraySize;
                    entriesProperty.InsertArrayElementAtIndex(entryIndex);

                    SerializedProperty entryProperty =
                        entriesProperty.GetArrayElementAtIndex(entryIndex);
                    SerializedProperty keyProperty =
                        entryProperty.FindPropertyRelative(KeyPropertyName);
                    SerializedProperty valueProperty =
                        entryProperty.FindPropertyRelative(ValuePropertyName);

                    keyProperty.stringValue =
                        MakeUniqueKey(entriesProperty, DefaultEntryKeyName, entryIndex);
                    valueProperty.managedReferenceValue = CreateDefaultValueInstance();
                });
        }

        /// <summary>
        /// Creates one authored entry foldout.
        /// </summary>
        /// <param name="entryProperty">Serialized entry property.</param>
        /// <param name="entryIndex">Stable array index used for callbacks.</param>
        /// <returns>One visual element that edits the entry.</returns>
        private VisualElement CreateEntryElement(
            SerializedProperty entryProperty,
            int entryIndex)
        {
            SerializedProperty keyProperty =
                entryProperty.FindPropertyRelative(KeyPropertyName);
            SerializedProperty valueProperty =
                entryProperty.FindPropertyRelative(ValuePropertyName);
            string entryKey = ResolveEntryTitle(keyProperty);
            string entryTypeDisplayName = GetEntryTypeDisplayName(valueProperty);

            Foldout entryFoldout = new()
            {
                text = entryKey,
                value = ResolveEntryExpansionState(entryKey),
            };
            bool suppressNextFoldoutToggle = false;
            ApplyEntryContainerStyle(entryFoldout);
            ApplyFoldoutStyle(entryFoldout);
            entryFoldout.RegisterValueChangedCallback(evt =>
            {
                if (suppressNextFoldoutToggle)
                {
                    suppressNextFoldoutToggle = false;
                    entryFoldout.SetValueWithoutNotify(evt.previousValue);
                    PersistEntryExpansionState(entryKey, evt.previousValue);
                    return;
                }

                PersistEntryExpansionState(entryKey, evt.newValue);
            });
            RegisterEntryDrag(
                entryFoldout,
                entryIndex,
                () => suppressNextFoldoutToggle = false,
                () => suppressNextFoldoutToggle = true);

            TextField keyField = new()
            {
                value = keyProperty?.stringValue ?? string.Empty,
                isDelayed = true,
            };
            keyField.RegisterValueChangedCallback(
                evt => HandleEntryKeyChanged(entryIndex, entryKey, evt.newValue));

            DropdownField typeField = new(
                GetEntryTypeDisplayNames(),
                Mathf.Max(0, GetEntryTypeDisplayNames().IndexOf(entryTypeDisplayName)));
            typeField.RegisterValueChangedCallback(
                evt => HandleEntryTypeChanged(entryIndex, evt.newValue));

            entryFoldout.Add(CreateFieldSection("Name", keyField));
            entryFoldout.Add(CreateFieldSection("Type", typeField));

            if (valueProperty?.managedReferenceValue is CutsceneGraphBlackboardEnumValue)
            {
                entryFoldout.Add(CreateFieldSection(
                    "Enum Type",
                    CreateEnumTypeButton(entryIndex, valueProperty)));
            }

            VisualElement valueSection = CreateFieldSection(
                "Value",
                CreateValueField(entryIndex, valueProperty));
            RegisterEntryValueDrag(valueSection, entryIndex);
            entryFoldout.Add(valueSection);

            Button removeButton = new(() => RemoveEntry(entryIndex, entryKey))
            {
                text = "Remove",
            };
            removeButton.style.alignSelf = Align.FlexEnd;
            entryFoldout.Add(removeButton);
            return entryFoldout;
        }

        /// <summary>
        /// Enables drag-and-drop initiation from one foldout header so node inspectors can bind variables directly.
        /// </summary>
        /// <param name="entryFoldout">Foldout that represents the authored blackboard variable.</param>
        /// <param name="entryIndex">Current array index for the represented entry.</param>
        private void RegisterEntryDrag(
            Foldout entryFoldout,
            int entryIndex,
            Action handlePointerDown,
            Action handleDragStarted)
        {
            Toggle headerToggle = entryFoldout.Q<Toggle>();

            if (headerToggle == null)
            {
                return;
            }

            headerToggle.tooltip =
                "Drag this variable into node inspector fields to create a blackboard binding.";
            ApplyDragSourceHintStyle(headerToggle);

            RegisterEntryDragSource(
                headerToggle,
                entryIndex,
                handlePointerDown,
                handleDragStarted);
        }

        /// <summary>
        /// Enables drag-and-drop initiation from one entry value section so authors can drag the visible value itself.
        /// </summary>
        /// <param name="valueSection">Section that renders the entry value editor.</param>
        /// <param name="entryIndex">Current array index for the represented entry.</param>
        private void RegisterEntryValueDrag(VisualElement valueSection, int entryIndex)
        {
            if (valueSection == null)
            {
                return;
            }

            valueSection.tooltip =
                "Drag this value into node inspector fields to bind the blackboard variable.";
            ApplyDragSourceHintStyle(valueSection);
            RegisterEntryDragSource(valueSection, entryIndex);
        }

        /// <summary>
        /// Starts one blackboard-variable drag operation from the provided visual element after a small pointer threshold.
        /// </summary>
        /// <param name="dragSource">Visual element that should begin the drag.</param>
        /// <param name="entryIndex">Current array index for the represented entry.</param>
        private void RegisterEntryDragSource(
            VisualElement dragSource,
            int entryIndex,
            Action handlePointerDown = null,
            Action handleDragStarted = null)
        {
            if (dragSource == null)
            {
                return;
            }

            Vector2 dragStartPosition = default;
            bool isPointerDown = false;

            dragSource.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                handlePointerDown?.Invoke();
                isPointerDown = true;
                dragStartPosition = evt.mousePosition;
                dragSource.CaptureMouse();
                SetDragSourceVisualState(dragSource, DragSourceHoverColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseUpEvent>(evt =>
            {
                isPointerDown = false;
                SetDragSourceVisualState(dragSource, DragSourceIdleColor);

                if (dragSource.HasMouseCapture())
                {
                    dragSource.ReleaseMouse();
                }
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseCaptureOutEvent>(evt =>
            {
                isPointerDown = false;
                SetDragSourceVisualState(dragSource, DragSourceIdleColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (CutsceneBlackboardDragAndDrop.HasActiveDrag)
                {
                    return;
                }

                SetDragSourceVisualState(dragSource, DragSourceHoverColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (isPointerDown || CutsceneBlackboardDragAndDrop.HasActiveDrag)
                {
                    return;
                }

                SetDragSourceVisualState(dragSource, DragSourceIdleColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!isPointerDown || evt.pressedButtons == 0)
                {
                    return;
                }

                if ((evt.mousePosition - dragStartPosition).sqrMagnitude < 16f)
                {
                    return;
                }

                if (!TryGetEntry(entryIndex, out CutsceneGraphBlackboardEntry entry))
                {
                    return;
                }

                CutsceneBlackboardDragAndDrop.BeginDrag(_director, entry);
                handleDragStarted?.Invoke();
                isPointerDown = false;
                SetDragSourceVisualState(dragSource, DragSourceHoverColor);

                if (dragSource.HasMouseCapture())
                {
                    dragSource.ReleaseMouse();
                }

                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
        }

        /// <summary>
        /// Applies the shared hover affordance used by drag-enabled blackboard elements.
        /// </summary>
        /// <param name="dragSource">Element that should look draggable.</param>
        private static void ApplyDragSourceHintStyle(VisualElement dragSource)
        {
            if (dragSource == null)
            {
                return;
            }

            dragSource.style.borderLeftWidth = 2f;
            dragSource.style.borderLeftColor = new Color(0.43f, 0.43f, 0.43f, 0.5f);
            dragSource.style.paddingLeft = Mathf.Max(dragSource.resolvedStyle.paddingLeft, 2f);
        }

        /// <summary>
        /// Updates the drag affordance color for one blackboard element.
        /// </summary>
        /// <param name="dragSource">Element being updated.</param>
        /// <param name="backgroundColor">Background color to apply.</param>
        private static void SetDragSourceVisualState(
            VisualElement dragSource,
            Color backgroundColor)
        {
            if (dragSource == null)
            {
                return;
            }

            dragSource.style.backgroundColor = new StyleColor(backgroundColor);
            dragSource.style.borderLeftColor = backgroundColor.a <= 0.01f
                ? new Color(0.43f, 0.43f, 0.43f, 0.5f)
                : new Color(0.74f, 0.88f, 0.78f, 0.95f);
        }

        /// <summary>
        /// Attempts to resolve one runtime blackboard entry from the bound director by the visible array index.
        /// </summary>
        /// <param name="entryIndex">Current array index in the serialized blackboard list.</param>
        /// <param name="entry">Resolved blackboard entry when available.</param>
        /// <returns>True when the entry exists.</returns>
        private bool TryGetEntry(
            int entryIndex,
            out CutsceneGraphBlackboardEntry entry)
        {
            entry = null;

            if (_director == null || entryIndex < 0)
            {
                return false;
            }

            IReadOnlyList<CutsceneGraphBlackboardEntry> entries =
                _director.Graph.Blackboard.Entries;

            if (entries == null || entryIndex >= entries.Count)
            {
                return false;
            }

            entry = entries[entryIndex];
            return entry != null;
        }

        /// <summary>
        /// Creates the editor field that matches the serialized entry payload.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="valueProperty">Serialized managed reference property.</param>
        /// <returns>The value editor element.</returns>
        private VisualElement CreateValueField(
            int entryIndex,
            SerializedProperty valueProperty)
        {
            if (valueProperty?.managedReferenceValue is CutsceneGraphBlackboardUnityObjectValue)
            {
                return CreateObjectValueField(entryIndex, valueProperty);
            }

            if (valueProperty?.managedReferenceValue is CutsceneGraphBlackboardEnumValue)
            {
                return CreateEnumValueField(entryIndex, valueProperty);
            }

            SerializedProperty boxedValueProperty =
                valueProperty?.FindPropertyRelative(ValueFieldName);

            if (boxedValueProperty != null)
            {
                PropertyField propertyField = new(boxedValueProperty, string.Empty);
                propertyField.Bind(valueProperty.serializedObject);
                return propertyField;
            }

            PropertyField fallbackField = new(valueProperty, string.Empty);
            fallbackField.Bind(valueProperty.serializedObject);
            return fallbackField;
        }

        /// <summary>
        /// Creates the type-selection button used by typed Unity object entries.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="valueProperty">Serialized managed reference property.</param>
        /// <returns>The type-selection button.</returns>
        private VisualElement CreateObjectTypeButton(
            int entryIndex,
            SerializedProperty valueProperty)
        {
            Type objectType = ResolveObjectType(valueProperty);
            Button button = new()
            {
                text = GetReadableTypeName(objectType),
            };
            button.clicked += () => ShowObjectTypeMenu(button, entryIndex, objectType);
            button.style.alignSelf = Align.Stretch;
            return button;
        }

        /// <summary>
        /// Creates the restricted object field used by typed Unity object entries.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="valueProperty">Serialized managed reference property.</param>
        /// <returns>The object field element.</returns>
        private VisualElement CreateObjectValueField(
            int entryIndex,
            SerializedProperty valueProperty)
        {
            Type objectType = ResolveObjectType(valueProperty);
            SerializedProperty boxedValueProperty =
                valueProperty.FindPropertyRelative(ValueFieldName);

            ObjectField objectField = new()
            {
                objectType = objectType,
                allowSceneObjects = true,
                value = boxedValueProperty?.objectReferenceValue,
            };
            objectField.RegisterValueChangedCallback(
                evt => SetEntryValue(
                    entryIndex,
                    value => value.objectReferenceValue = evt.newValue));
            return objectField;
        }

        /// <summary>
        /// Creates the type-selection button used by enum entries.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="valueProperty">Serialized managed reference property.</param>
        /// <returns>The type-selection button.</returns>
        private VisualElement CreateEnumTypeButton(
            int entryIndex,
            SerializedProperty valueProperty)
        {
            Type enumType = ResolveEnumType(valueProperty);
            Button button = new()
            {
                text = enumType == null ? "Choose Enum Type" : GetReadableTypeName(enumType),
            };
            button.clicked += () => ShowEnumTypeMenu(button, entryIndex, enumType);
            button.style.alignSelf = Align.Stretch;
            return button;
        }

        /// <summary>
        /// Creates the dropdown used to author the current enum value.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="valueProperty">Serialized managed reference property.</param>
        /// <returns>The enum value field or one warning when the enum type is missing.</returns>
        private VisualElement CreateEnumValueField(
            int entryIndex,
            SerializedProperty valueProperty)
        {
            Type enumType = ResolveEnumType(valueProperty);

            if (enumType == null)
            {
                return new HelpBox(
                    "Choose one enum type before editing the value.",
                    HelpBoxMessageType.Warning);
            }

            string[] enumValueNames = Enum.GetNames(enumType);

            if (enumValueNames.Length == 0)
            {
                return new HelpBox(
                    "The selected enum type does not expose any named values.",
                    HelpBoxMessageType.Warning);
            }

            string currentValueName = ResolveCurrentEnumValueName(valueProperty, enumType);
            int selectedIndex = Array.IndexOf(enumValueNames, currentValueName);

            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            DropdownField enumValueField = new(
                enumValueNames.ToList(),
                selectedIndex);
            enumValueField.RegisterValueChangedCallback(
                evt => HandleEnumValueChanged(entryIndex, evt.newValue));
            return enumValueField;
        }

        /// <summary>
        /// Updates one entry key and auto-normalizes blank or duplicated values.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="currentKey">Current key before the rename operation.</param>
        /// <param name="newKey">Requested key value.</param>
        private void HandleEntryKeyChanged(
            int entryIndex,
            string currentKey,
            string newKey)
        {
            ModifyEntries(
                "Rename Cutscene Blackboard Entry",
                entriesProperty =>
                {
                    SerializedProperty entryProperty =
                        entriesProperty.GetArrayElementAtIndex(entryIndex);
                    SerializedProperty keyProperty =
                        entryProperty.FindPropertyRelative(KeyPropertyName);

                    string sanitizedKey = SanitizeKey(newKey);
                    string resolvedKey =
                        MakeUniqueKey(entriesProperty, sanitizedKey, entryIndex);
                    keyProperty.stringValue = resolvedKey;
                    MigrateEntryExpansionState(currentKey, resolvedKey);
                });
        }

        /// <summary>
        /// Replaces one entry payload type with a fresh wrapper instance.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="typeDisplayName">Selected type label.</param>
        private void HandleEntryTypeChanged(
            int entryIndex,
            string typeDisplayName)
        {
            ModifyEntries(
                "Change Cutscene Blackboard Entry Type",
                entriesProperty =>
                {
                    SerializedProperty valueProperty =
                        GetValueProperty(entriesProperty, entryIndex);

                    if (valueProperty == null)
                    {
                        return;
                    }

                    valueProperty.managedReferenceValue =
                        CreateValueInstance(typeDisplayName);
                });
        }

        /// <summary>
        /// Reconfigures the concrete Unity object type accepted by one entry.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="objectType">Concrete Unity object type selected by the author.</param>
        private void HandleObjectTypeChanged(
            int entryIndex,
            Type objectType)
        {
            ModifyEntries(
                "Change Cutscene Blackboard Object Type",
                entriesProperty =>
                {
                    SerializedProperty valueProperty =
                        GetValueProperty(entriesProperty, entryIndex);
                    SerializedProperty objectTypeNameProperty =
                        valueProperty?.FindPropertyRelative(ObjectTypeNamePropertyName);
                    SerializedProperty boxedValueProperty =
                        valueProperty?.FindPropertyRelative(ValueFieldName);

                    if (objectTypeNameProperty == null || objectType == null)
                    {
                        return;
                    }

                    objectTypeNameProperty.stringValue =
                        objectType.AssemblyQualifiedName ?? string.Empty;

                    if (boxedValueProperty?.objectReferenceValue != null
                        && !objectType.IsInstanceOfType(boxedValueProperty.objectReferenceValue))
                    {
                        boxedValueProperty.objectReferenceValue = null;
                    }
                });
        }

        /// <summary>
        /// Reconfigures the concrete enum type stored by one entry.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="enumType">Concrete enum type selected by the author.</param>
        private void HandleEnumTypeChanged(
            int entryIndex,
            Type enumType)
        {
            ModifyEntries(
                "Change Cutscene Blackboard Enum Type",
                entriesProperty =>
                {
                    SerializedProperty valueProperty =
                        GetValueProperty(entriesProperty, entryIndex);
                    SerializedProperty enumTypeNameProperty =
                        valueProperty?.FindPropertyRelative(EnumTypeNamePropertyName);
                    SerializedProperty enumValueNameProperty =
                        valueProperty?.FindPropertyRelative(EnumValueNamePropertyName);

                    if (enumTypeNameProperty == null || enumValueNameProperty == null || enumType == null)
                    {
                        return;
                    }

                    enumTypeNameProperty.stringValue =
                        enumType.AssemblyQualifiedName ?? string.Empty;

                    string[] enumValueNames = Enum.GetNames(enumType);
                    enumValueNameProperty.stringValue =
                        enumValueNames.Length <= 0 ? string.Empty : enumValueNames[0];
                });
        }

        /// <summary>
        /// Updates the current named enum value stored by one entry.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="enumValueName">Selected enum value name.</param>
        private void HandleEnumValueChanged(
            int entryIndex,
            string enumValueName)
        {
            ModifyEntries(
                "Edit Cutscene Blackboard Enum Value",
                entriesProperty =>
                {
                    SerializedProperty valueProperty =
                        GetValueProperty(entriesProperty, entryIndex);
                    SerializedProperty enumValueNameProperty =
                        valueProperty?.FindPropertyRelative(EnumValueNamePropertyName);

                    if (enumValueNameProperty == null)
                    {
                        return;
                    }

                    enumValueNameProperty.stringValue = enumValueName ?? string.Empty;
                });
        }

        /// <summary>
        /// Removes one entry from the serialized list.
        /// </summary>
        /// <param name="entryIndex">Array index to remove.</param>
        /// <param name="entryKey">Current entry key used by the foldout state cache.</param>
        private void RemoveEntry(int entryIndex, string entryKey)
        {
            ModifyEntries(
                "Remove Cutscene Blackboard Entry",
                entriesProperty =>
                {
                    DeleteEntryAtIndex(entriesProperty, entryIndex);
                    _entryExpansionStates.Remove(entryKey);
                    _director?.RemoveBlackboardFoldoutState(entryKey);
                });
        }

        /// <summary>
        /// Updates one serialized child property inside the current entry payload.
        /// </summary>
        /// <param name="entryIndex">Array index being edited.</param>
        /// <param name="applyValue">Mutation applied to the payload child property.</param>
        private void SetEntryValue(
            int entryIndex,
            Action<SerializedProperty> applyValue)
        {
            ModifyEntries(
                "Edit Cutscene Blackboard Entry",
                entriesProperty =>
                {
                    SerializedProperty valueProperty =
                        GetValueProperty(entriesProperty, entryIndex);
                    SerializedProperty boxedValueProperty =
                        valueProperty?.FindPropertyRelative(ValueFieldName);

                    if (boxedValueProperty != null)
                    {
                        applyValue(boxedValueProperty);
                    }
                });
        }

        /// <summary>
        /// Executes one serialized list mutation and refreshes the view.
        /// </summary>
        /// <param name="undoLabel">Undo label recorded on the director.</param>
        /// <param name="mutateEntries">Mutation to apply to the entries list.</param>
        private void ModifyEntries(
            string undoLabel,
            Action<SerializedProperty> mutateEntries)
        {
            if (_director == null)
            {
                return;
            }

            SerializedObject serializedDirector = new(_director);
            serializedDirector.UpdateIfRequiredOrScript();
            SerializedProperty entriesProperty = FindEntriesProperty(serializedDirector);

            if (entriesProperty == null)
            {
                return;
            }

            CutsceneEditorUtility.RecordDirectorChange(_director, undoLabel);
            mutateEntries(entriesProperty);
            serializedDirector.ApplyModifiedProperties();
            Refresh();
            BlackboardChanged?.Invoke();
        }

        /// <summary>
        /// Displays one warning or error message above the entry list.
        /// </summary>
        /// <param name="message">Message shown in the help box.</param>
        /// <param name="messageType">Severity of the message.</param>
        private void ShowStateBox(string message, HelpBoxMessageType messageType)
        {
            _stateBox.text = message;
            _stateBox.messageType = messageType;
            _stateBox.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hides the state box when there is no warning or error to report.
        /// </summary>
        private void HideStateBox()
        {
            _stateBox.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Resolves the serialized blackboard entries list on the bound director.
        /// </summary>
        /// <param name="serializedDirector">Serialized director instance.</param>
        /// <returns>The entries property when it exists; otherwise null.</returns>
        private static SerializedProperty FindEntriesProperty(SerializedObject serializedDirector)
        {
            SerializedProperty graphProperty =
                serializedDirector.FindProperty(GraphPropertyPath);
            SerializedProperty blackboardProperty =
                graphProperty?.FindPropertyRelative(BlackboardPropertyName);
            return blackboardProperty?.FindPropertyRelative(EntriesPropertyName);
        }

        /// <summary>
        /// Resolves one entry payload property from the serialized entries array.
        /// </summary>
        /// <param name="entriesProperty">Serialized entries array.</param>
        /// <param name="entryIndex">Index of the entry being edited.</param>
        /// <returns>The payload property when available.</returns>
        private static SerializedProperty GetValueProperty(
            SerializedProperty entriesProperty,
            int entryIndex)
        {
            if (entriesProperty == null
                || entryIndex < 0
                || entryIndex >= entriesProperty.arraySize)
            {
                return null;
            }

            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(entryIndex);
            return entryProperty?.FindPropertyRelative(ValuePropertyName);
        }

        /// <summary>
        /// Applies the shared spacing used by help boxes in the blackboard editor.
        /// </summary>
        /// <param name="helpBox">Help box to style.</param>
        private static void ApplyInformativeBoxStyle(HelpBox helpBox)
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
        private static void ApplyEntryContainerStyle(VisualElement container)
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
        private static void ApplyFoldoutStyle(Foldout foldout)
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
        private static VisualElement CreateFieldSection(
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

            ApplyFieldControlStyle(field);
            section.Add(field);
            return section;
        }

        /// <summary>
        /// Applies one full-width layout to an editor field and hides inline labels.
        /// </summary>
        /// <param name="field">Field element to normalize.</param>
        private static void ApplyFieldControlStyle(VisualElement field)
        {
            if (field == null)
            {
                return;
            }

            field.style.alignSelf = Align.Stretch;
            field.style.flexGrow = 1f;
            field.style.flexShrink = 1f;
            field.style.minWidth = 0f;

            Label inlineLabel = field.Q<Label>(className: BaseFieldLabelClassName);

            if (inlineLabel != null)
            {
                inlineLabel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Updates the blackboard header with the current graph binding and authored entry count.
        /// </summary>
        /// <param name="entryCount">Current number of authored entries.</param>
        private void RefreshHeader(int entryCount)
        {
            _bindingLabel.text = _director == null
                ? "Graph (Unbound)"
                : $"Graph ({ResolveDirectorDisplayName(_director)})";

            _entryCountLabel.text = entryCount switch
            {
                <= 0 => "No entries",
                1 => "1 entry",
                _ => $"{entryCount} entries",
            };
        }

        /// <summary>
        /// Toggles the visibility of the blackboard editor content.
        /// </summary>
        private void ToggleExpanded()
        {
            _isExpanded = !_isExpanded;
            _contentContainer.style.display = _isExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _collapseButton.text = _isExpanded ? "v" : ">";
        }

        /// <summary>
        /// Resolves one compact display name for the currently bound director.
        /// </summary>
        /// <param name="director">Bound cutscene director.</param>
        /// <returns>The best available display name.</returns>
        private static string ResolveDirectorDisplayName(CutsceneDirector director)
        {
            if (director == null)
            {
                return "Unbound";
            }

            return string.IsNullOrWhiteSpace(director.Title)
                ? director.name
                : director.Title;
        }

        /// <summary>
        /// Normalizes blank keys to a stable default label.
        /// </summary>
        /// <param name="key">Requested key value.</param>
        /// <returns>The normalized key.</returns>
        private static string SanitizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? DefaultEntryKeyName
                : key.Trim();
        }

        /// <summary>
        /// Resolves the foldout title shown for one entry.
        /// </summary>
        /// <param name="keyProperty">Serialized key property.</param>
        /// <returns>The title rendered on the foldout header.</returns>
        private static string ResolveEntryTitle(SerializedProperty keyProperty)
        {
            return SanitizeKey(keyProperty?.stringValue);
        }

        /// <summary>
        /// Removes one serialized entry and retries when the first delete only clears the slot.
        /// </summary>
        /// <param name="entriesProperty">Serialized entries array.</param>
        /// <param name="entryIndex">Index that should be removed.</param>
        private static void DeleteEntryAtIndex(
            SerializedProperty entriesProperty,
            int entryIndex)
        {
            if (entriesProperty == null
                || entryIndex < 0
                || entryIndex >= entriesProperty.arraySize)
            {
                return;
            }

            int originalSize = entriesProperty.arraySize;
            entriesProperty.DeleteArrayElementAtIndex(entryIndex);

            if (entriesProperty.arraySize == originalSize
                && entryIndex < entriesProperty.arraySize)
            {
                entriesProperty.DeleteArrayElementAtIndex(entryIndex);
            }
        }

        /// <summary>
        /// Generates one unique key for the serialized entries list.
        /// </summary>
        /// <param name="entriesProperty">Target entries array.</param>
        /// <param name="requestedKey">Desired key value.</param>
        /// <param name="ignoredIndex">Array index that should be ignored during duplicate checks.</param>
        /// <returns>A key that does not collide with other entries.</returns>
        private static string MakeUniqueKey(
            SerializedProperty entriesProperty,
            string requestedKey,
            int ignoredIndex)
        {
            string candidate = SanitizeKey(requestedKey);

            if (!ContainsKey(entriesProperty, candidate, ignoredIndex))
            {
                return candidate;
            }

            int suffix = 1;

            while (ContainsKey(entriesProperty, $"{candidate}{suffix}", ignoredIndex))
            {
                suffix++;
            }

            return $"{candidate}{suffix}";
        }

        /// <summary>
        /// Determines whether the serialized entry list already contains one key.
        /// </summary>
        /// <param name="entriesProperty">Target entries array.</param>
        /// <param name="key">Key to search for.</param>
        /// <param name="ignoredIndex">Array index that should be ignored during duplicate checks.</param>
        /// <returns>True when another entry already uses the key.</returns>
        private static bool ContainsKey(
            SerializedProperty entriesProperty,
            string key,
            int ignoredIndex)
        {
            for (int index = 0; index < entriesProperty.arraySize; index++)
            {
                if (index == ignoredIndex)
                {
                    continue;
                }

                SerializedProperty entryProperty =
                    entriesProperty.GetArrayElementAtIndex(index);
                SerializedProperty keyProperty =
                    entryProperty.FindPropertyRelative(KeyPropertyName);

                if (string.Equals(keyProperty?.stringValue, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves the remembered expansion state for one entry.
        /// </summary>
        /// <param name="entryKey">Unique key used by the serialized entry.</param>
        /// <returns>The expansion state that should be applied to the foldout.</returns>
        private bool ResolveEntryExpansionState(string entryKey)
        {
            if (_entryExpansionStates.TryGetValue(entryKey, out bool cachedState))
            {
                return cachedState;
            }

            if (_director != null
                && _director.TryGetBlackboardFoldoutState(entryKey, out bool persistedState))
            {
                _entryExpansionStates[entryKey] = persistedState;
                return persistedState;
            }

            return true;
        }

        /// <summary>
        /// Carries one remembered expansion state across key renames.
        /// </summary>
        /// <param name="oldKey">Previous serialized key.</param>
        /// <param name="newKey">Resolved serialized key after rename normalization.</param>
        private void MigrateEntryExpansionState(string oldKey, string newKey)
        {
            if (string.Equals(oldKey, newKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool previousState = ResolveEntryExpansionState(oldKey);
            _entryExpansionStates.Remove(oldKey);
            _entryExpansionStates[newKey] = previousState;
            PersistDirectorExpansionStateRename(oldKey, newKey, previousState);
        }

        /// <summary>
        /// Persists one foldout expansion state on the currently bound director.
        /// </summary>
        /// <param name="entryKey">Unique blackboard entry key.</param>
        /// <param name="isExpanded">Expansion state that should be stored.</param>
        private void PersistEntryExpansionState(string entryKey, bool isExpanded)
        {
            _entryExpansionStates[entryKey] = isExpanded;

            if (_director == null
                || !_director.SetBlackboardFoldoutState(entryKey, isExpanded))
            {
                return;
            }

            CutsceneEditorUtility.MarkDirectorDirty(_director);
        }

        /// <summary>
        /// Persists one foldout state rename on the currently bound director.
        /// </summary>
        /// <param name="oldKey">Previous unique blackboard entry key.</param>
        /// <param name="newKey">New unique blackboard entry key.</param>
        /// <param name="isExpanded">Expansion state that should remain associated with the key.</param>
        private void PersistDirectorExpansionStateRename(
            string oldKey,
            string newKey,
            bool isExpanded)
        {
            if (_director == null)
            {
                return;
            }

            bool didRename = _director.RenameBlackboardFoldoutState(oldKey, newKey);
            bool didCreate = !didRename
                && _director.SetBlackboardFoldoutState(newKey, isExpanded);

            if (!didRename && !didCreate)
            {
                return;
            }

            CutsceneEditorUtility.MarkDirectorDirty(_director);
        }

        /// <summary>
        /// Resolves the current display label for one stored payload wrapper.
        /// </summary>
        /// <param name="valueProperty">Serialized payload property.</param>
        /// <returns>The display label used by the type picker.</returns>
        private static string GetEntryTypeDisplayName(SerializedProperty valueProperty)
        {
            if (valueProperty?.managedReferenceValue is not CutsceneGraphBlackboardValue value)
            {
                return DefaultEntryTypeDisplayName();
            }

            return CutsceneBlackboardValueRegistry.GetDisplayName(value);
        }

        /// <summary>
        /// Resolves the type labels currently available to the picker UI.
        /// </summary>
        /// <returns>The ordered picker labels.</returns>
        private static List<string> GetEntryTypeDisplayNames()
        {
            return CutsceneBlackboardValueRegistry.Descriptors
                .Where(descriptor => !descriptor.HiddenFromPicker)
                .Select(descriptor => descriptor.DisplayName)
                .ToList();
        }

        /// <summary>
        /// Creates one fresh wrapper instance for the requested picker label.
        /// </summary>
        /// <param name="displayName">Picker label selected by the author.</param>
        /// <returns>The created wrapper instance.</returns>
        private static CutsceneGraphBlackboardValue CreateValueInstance(string displayName)
        {
            if (CutsceneBlackboardValueRegistry.TryGetDescriptor(displayName, out var descriptor))
            {
                return descriptor.CreateValue(descriptor.RuntimeValueType);
            }

            return CreateDefaultValueInstance();
        }

        /// <summary>
        /// Creates the default payload used by newly added entries.
        /// </summary>
        /// <returns>The default string wrapper.</returns>
        private static CutsceneGraphBlackboardValue CreateDefaultValueInstance()
        {
            return CutsceneBlackboardValueRegistry.TryCreateValue(
                typeof(string),
                out CutsceneGraphBlackboardValue value)
                ? value
                : new CutsceneGraphBlackboardStringValue();
        }

        /// <summary>
        /// Resolves the default display label used by the type picker.
        /// </summary>
        /// <returns>The string wrapper display label when available.</returns>
        private static string DefaultEntryTypeDisplayName()
        {
            return CutsceneBlackboardValueRegistry.TryGetDescriptorForRuntimeType(
                typeof(string),
                out var descriptor)
                ? descriptor.DisplayName
                : "String";
        }

        /// <summary>
        /// Resolves the concrete Unity object type configured by one payload wrapper.
        /// </summary>
        /// <param name="valueProperty">Serialized payload property.</param>
        /// <returns>The resolved concrete object type.</returns>
        private static Type ResolveObjectType(SerializedProperty valueProperty)
        {
            if (valueProperty?.managedReferenceValue is CutsceneGraphBlackboardUnityObjectValue objectValue)
            {
                return objectValue.ResolveObjectType();
            }

            return typeof(UnityEngine.Object);
        }

        /// <summary>
        /// Resolves the concrete enum type configured by one payload wrapper.
        /// </summary>
        /// <param name="valueProperty">Serialized payload property.</param>
        /// <returns>The resolved concrete enum type when available.</returns>
        private static Type ResolveEnumType(SerializedProperty valueProperty)
        {
            return valueProperty?.managedReferenceValue is CutsceneGraphBlackboardEnumValue enumValue
                ? enumValue.ResolveEnumType()
                : null;
        }

        /// <summary>
        /// Resolves the current named enum value stored by the payload wrapper.
        /// </summary>
        /// <param name="valueProperty">Serialized payload property.</param>
        /// <param name="enumType">Concrete enum type currently selected.</param>
        /// <returns>The best available enum value name.</returns>
        private static string ResolveCurrentEnumValueName(
            SerializedProperty valueProperty,
            Type enumType)
        {
            if (enumType == null)
            {
                return string.Empty;
            }

            SerializedProperty enumValueNameProperty =
                valueProperty?.FindPropertyRelative(EnumValueNamePropertyName);
            string currentValueName = enumValueNameProperty?.stringValue ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(currentValueName)
                && Enum.IsDefined(enumType, currentValueName))
            {
                return currentValueName;
            }

            string[] enumValueNames = Enum.GetNames(enumType);
            return enumValueNames.Length <= 0 ? string.Empty : enumValueNames[0];
        }

        /// <summary>
        /// Displays the menu used to pick one concrete Unity object type.
        /// </summary>
        /// <param name="anchor">Visual element that anchors the menu.</param>
        /// <param name="entryIndex">Entry index being edited.</param>
        /// <param name="currentType">Currently selected object type.</param>
        private void ShowObjectTypeMenu(
            VisualElement anchor,
            int entryIndex,
            Type currentType)
        {
            GenericMenu menu = new();

            IReadOnlyList<Type> objectTypes = GetPickableObjectTypes();

            for (int index = 0; index < objectTypes.Count; index++)
            {
                Type candidateType = objectTypes[index];
                Type capturedType = candidateType;
                menu.AddItem(
                    new GUIContent(BuildObjectTypeMenuPath(candidateType)),
                    candidateType == currentType,
                    () => HandleObjectTypeChanged(entryIndex, capturedType));
            }

            menu.DropDown(anchor.worldBound);
        }

        /// <summary>
        /// Displays the menu used to pick one concrete enum type.
        /// </summary>
        /// <param name="anchor">Visual element that anchors the menu.</param>
        /// <param name="entryIndex">Entry index being edited.</param>
        /// <param name="currentType">Currently selected enum type.</param>
        private void ShowEnumTypeMenu(
            VisualElement anchor,
            int entryIndex,
            Type currentType)
        {
            GenericMenu menu = new();

            IReadOnlyList<Type> enumTypes = GetPickableEnumTypes();

            for (int index = 0; index < enumTypes.Count; index++)
            {
                Type candidateType = enumTypes[index];
                Type capturedType = candidateType;
                menu.AddItem(
                    new GUIContent(BuildEnumTypeMenuPath(candidateType)),
                    candidateType == currentType,
                    () => HandleEnumTypeChanged(entryIndex, capturedType));
            }

            menu.DropDown(anchor.worldBound);
        }

        /// <summary>
        /// Resolves the runtime-safe Unity object types available to the picker.
        /// </summary>
        /// <returns>The cached list of pickable object types.</returns>
        private static IReadOnlyList<Type> GetPickableObjectTypes()
        {
            if (s_pickableObjectTypes != null)
            {
                return s_pickableObjectTypes;
            }

            HashSet<Type> types = new()
            {
                typeof(UnityEngine.Object),
                typeof(GameObject),
                typeof(Component),
                typeof(Transform),
                typeof(MonoBehaviour),
                typeof(ScriptableObject),
                typeof(Material),
                typeof(AudioClip),
                typeof(Sprite),
                typeof(AnimationClip),
                typeof(RuntimeAnimatorController),
                typeof(Texture2D),
                typeof(Mesh),
            };

            foreach (Type candidateType in EnumerateRuntimeTypes())
            {
                if (!typeof(UnityEngine.Object).IsAssignableFrom(candidateType)
                    || candidateType.IsAbstract
                    || candidateType.ContainsGenericParameters
                    || IsEditorOnlyType(candidateType))
                {
                    continue;
                }

                types.Add(candidateType);
            }

            s_pickableObjectTypes = types
                .OrderBy(BuildObjectTypeMenuPath, StringComparer.Ordinal)
                .ToList();
            return s_pickableObjectTypes;
        }

        /// <summary>
        /// Resolves the runtime-safe enum types available to the picker.
        /// </summary>
        /// <returns>The cached list of pickable enum types.</returns>
        private static IReadOnlyList<Type> GetPickableEnumTypes()
        {
            if (s_pickableEnumTypes != null)
            {
                return s_pickableEnumTypes;
            }

            s_pickableEnumTypes = EnumerateRuntimeTypes()
                .Where(candidateType =>
                    candidateType.IsEnum
                    && !candidateType.ContainsGenericParameters
                    && !IsEditorOnlyType(candidateType))
                .OrderBy(BuildEnumTypeMenuPath, StringComparer.Ordinal)
                .ToList();
            return s_pickableEnumTypes;
        }

        /// <summary>
        /// Enumerates all currently loaded runtime types.
        /// </summary>
        /// <returns>The loaded runtime types available in the current domain.</returns>
        private static IEnumerable<Type> EnumerateRuntimeTypes()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Assembly assembly = assemblies[assemblyIndex];
                Type[] assemblyTypes = Array.Empty<Type>();

                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    assemblyTypes = exception.Types;
                }

                if (assemblyTypes == null)
                {
                    continue;
                }

                for (int typeIndex = 0; typeIndex < assemblyTypes.Length; typeIndex++)
                {
                    Type type = assemblyTypes[typeIndex];

                    if (type != null)
                    {
                        yield return type;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether one reflected type belongs to an editor-only assembly or namespace.
        /// </summary>
        /// <param name="type">Type being evaluated.</param>
        /// <returns>True when the type should be hidden from runtime-authored pickers.</returns>
        private static bool IsEditorOnlyType(Type type)
        {
            if (type == null)
            {
                return true;
            }

            string namespaceName = type.Namespace ?? string.Empty;

            if (namespaceName.StartsWith("UnityEditor", StringComparison.Ordinal))
            {
                return true;
            }

            string assemblyName = type.Assembly.GetName().Name ?? string.Empty;
            return assemblyName.StartsWith("UnityEditor", StringComparison.Ordinal)
                || assemblyName.EndsWith(".Editor", StringComparison.Ordinal)
                || assemblyName.IndexOf(".Editor.", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Builds the menu path used by one object type inside the picker menu.
        /// </summary>
        /// <param name="type">Object type represented by the menu entry.</param>
        /// <returns>The hierarchical menu path.</returns>
        private static string BuildObjectTypeMenuPath(Type type)
        {
            if (type == typeof(UnityEngine.Object)
                || type == typeof(GameObject)
                || type == typeof(Component)
                || type == typeof(Transform)
                || type == typeof(MonoBehaviour)
                || type == typeof(ScriptableObject))
            {
                return $"Common/{GetReadableTypeName(type)}";
            }

            string rootCategory = type.IsSubclassOf(typeof(MonoBehaviour))
                ? "MonoBehaviours"
                : type.IsSubclassOf(typeof(ScriptableObject))
                    ? "ScriptableObjects"
                    : type.IsSubclassOf(typeof(Component))
                        ? "Components"
                        : "UnityObjects";

            return $"{rootCategory}/{BuildTypeNamespacePath(type)}";
        }

        /// <summary>
        /// Builds the menu path used by one enum type inside the picker menu.
        /// </summary>
        /// <param name="type">Enum type represented by the menu entry.</param>
        /// <returns>The hierarchical menu path.</returns>
        private static string BuildEnumTypeMenuPath(Type type)
        {
            return $"Enums/{BuildTypeNamespacePath(type)}";
        }

        /// <summary>
        /// Builds one namespace-aware menu path segment for a reflected type.
        /// </summary>
        /// <param name="type">Reflected type.</param>
        /// <returns>The namespace-aware path segment.</returns>
        private static string BuildTypeNamespacePath(Type type)
        {
            string namespacePath = string.IsNullOrWhiteSpace(type.Namespace)
                ? "Global"
                : type.Namespace.Replace('.', '/');
            string typeName = GetReadableTypeName(type).Replace('+', '/');
            return $"{namespacePath}/{typeName}";
        }

        /// <summary>
        /// Resolves the short display name shown for one runtime type.
        /// </summary>
        /// <param name="type">Runtime type being displayed.</param>
        /// <returns>The short display name.</returns>
        private static string GetReadableTypeName(Type type)
        {
            if (type == null)
            {
                return "Unknown";
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            string genericTypeName = type.Name;
            int tickIndex = genericTypeName.IndexOf('`');

            if (tickIndex >= 0)
            {
                genericTypeName = genericTypeName.Substring(0, tickIndex);
            }

            Type[] genericArguments = type.GetGenericArguments();
            string argumentNames = string.Join(
                ", ",
                genericArguments.Select(GetReadableTypeName));
            return $"{genericTypeName}<{argumentNames}>";
        }
    }
}
