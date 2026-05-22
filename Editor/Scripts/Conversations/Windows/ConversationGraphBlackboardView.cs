using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.GraphCore;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Renders one minimal blackboard overlay for Conversations asset hosts.
    /// </summary>
    public sealed class ConversationGraphBlackboardView : GraphBlackboardOverlayView
    {
        private const string ConversationsPropertyName = "_conversations";
        private const string GraphPropertyName = "_graph";
        private const string BlackboardPropertyName = "_blackboard";
        private const string EntriesPropertyName = "_entries";
        private const string KeyPropertyName = "_key";
        private const string ValuePropertyName = "_value";
        private const string DefaultEntryKeyName = "Entry";

        private readonly Dictionary<string, bool> _entryExpansionStates =
            new(StringComparer.OrdinalIgnoreCase);

        private ConversationTable _table;
        private ConversationDefinition _conversation;

        /// <summary>
        /// Raised after one blackboard mutation is committed.
        /// </summary>
        public event Action BlackboardChanged;

        /// <summary>
        /// Creates the UI Toolkit hierarchy used by the overlay.
        /// </summary>
        public ConversationGraphBlackboardView()
            : base("Blackboard")
        {
            Refresh();
        }

        /// <summary>
        /// Binds the overlay to one conversation table.
        /// </summary>
        /// <param name="table">Authored table that owns the selected conversation.</param>
        /// <param name="conversation">Selected authored conversation.</param>
        public void BindConversation(
            ConversationTable table,
            ConversationDefinition conversation)
        {
            _entryExpansionStates.Clear();
            _table = table;
            _conversation = conversation;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the overlay from the currently bound table.
        /// </summary>
        public void Refresh()
        {
            ClearOverlayEntries();

            if (_table == null)
            {
                SetOverlayContentEnabled(false);
                UpdateOverlayHeader("Conversation (Unbound)", 0);
                HideOverlayStateBox();
                return;
            }

            if (_conversation == null)
            {
                SetOverlayContentEnabled(false);
                UpdateOverlayHeader($"Variables ({_table.DisplayName})", 0);
                ShowOverlayStateBox(
                    "Choose a conversation to edit the variables used by its graph.",
                    HelpBoxMessageType.Info);
                return;
            }

            SetOverlayContentEnabled(true);

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();
            SerializedProperty entriesProperty = FindEntriesProperty(serializedTable);

            if (entriesProperty == null)
            {
                UpdateOverlayHeader($"Variables ({_conversation.Title})", 0);
                ShowOverlayStateBox(
                    "This conversation cannot show its variables right now.",
                    HelpBoxMessageType.Error);
                return;
            }

            UpdateOverlayHeader(
                $"Variables ({_conversation.Title})",
                entriesProperty.arraySize);
            HideOverlayStateBox();

            for (int index = 0; index < entriesProperty.arraySize; index++)
            {
                SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);

                if (entryProperty == null)
                {
                    continue;
                }

                AddOverlayEntry(CreateEntryElement(entryProperty, index));
            }
        }

        /// <inheritdoc />
        protected override void HandleAddRequested()
        {
            if (_table == null)
            {
                return;
            }

            ModifyEntries(
                "Add Conversation Blackboard Entry",
                entriesProperty =>
                {
                    int entryIndex = entriesProperty.arraySize;
                    entriesProperty.InsertArrayElementAtIndex(entryIndex);

                    SerializedProperty entryProperty =
                        entriesProperty.GetArrayElementAtIndex(entryIndex);
                    SerializedProperty keyProperty = entryProperty.FindPropertyRelative(
                        KeyPropertyName);
                    SerializedProperty valueProperty = entryProperty.FindPropertyRelative(
                        ValuePropertyName);

                    keyProperty.stringValue = MakeUniqueKey(
                        entriesProperty,
                        DefaultEntryKeyName,
                        entryIndex);
                    valueProperty.managedReferenceValue = CreateDefaultValueInstance();
                });
        }

        private VisualElement CreateEntryElement(
            SerializedProperty entryProperty,
            int entryIndex)
        {
            SerializedProperty keyProperty = entryProperty.FindPropertyRelative(KeyPropertyName);
            SerializedProperty valueProperty = entryProperty.FindPropertyRelative(ValuePropertyName);
            string entryKey = ResolveEntryTitle(keyProperty);

            VisualElement container = new();
            ApplyOverlayEntryContainerStyle(container);

            Foldout foldout = new()
            {
                text = entryKey,
                value = ResolveEntryExpansionState(entryKey),
            };
            ApplyOverlayFoldoutStyle(foldout);
            foldout.RegisterValueChangedCallback(
                evt => PersistEntryExpansionState(entryKey, evt.newValue));
            container.Add(foldout);

            RegisterEntryDrag(foldout, entryIndex);

            TextField keyField = new()
            {
                value = keyProperty?.stringValue ?? string.Empty,
                isDelayed = true,
            };
            keyField.RegisterValueChangedCallback(
                evt => HandleEntryKeyChanged(entryIndex, entryKey, evt.newValue));
            foldout.Add(CreateOverlayFieldSection("Name", keyField));

            List<string> typeNames = GetEntryTypeDisplayNames();
            string currentTypeName = GetEntryTypeDisplayName(valueProperty);
            int selectedIndex = Mathf.Max(0, typeNames.IndexOf(currentTypeName));
            DropdownField typeField = new(typeNames, selectedIndex);
            typeField.RegisterValueChangedCallback(
                evt => HandleEntryTypeChanged(entryIndex, evt.newValue));
            foldout.Add(CreateOverlayFieldSection("Type", typeField));

            PropertyField valueField = new(valueProperty.Copy(), "Value");
            valueField.RegisterCallback<SerializedPropertyChangeEvent>(
                evt => HandleEntryValueChanged());
            foldout.Add(CreateOverlayFieldSection("Value", valueField));

            Button removeButton = new(() => RemoveEntry(entryIndex, entryKey))
            {
                text = "Remove",
            };
            removeButton.style.alignSelf = Align.FlexEnd;
            foldout.Add(removeButton);
            return container;
        }

        private void RegisterEntryDrag(Foldout entryFoldout, int entryIndex)
        {
            Toggle headerToggle = entryFoldout.Q<Toggle>();

            if (headerToggle == null)
            {
                return;
            }

            headerToggle.tooltip =
                "Drag this variable onto a node field to use it there.";
            GraphBlackboardDragSourceUtility.ApplyDragSourceHintStyle(headerToggle);
            GraphBlackboardDragSourceUtility.RegisterDragSource(
                headerToggle,
                () => GraphBlackboardDragSession.HasActiveDrag,
                () => TryBeginEntryDrag(entryIndex));
        }

        private bool TryBeginEntryDrag(int entryIndex)
        {
            if (!TryGetEntry(entryIndex, out GraphBlackboardEntry entry))
            {
                return false;
            }

            entry.EnsureId();
            GraphBlackboardDragSession.BeginDrag(
                _conversation,
                entry.Id,
                entry.Key ?? string.Empty);
            return true;
        }

        private void HandleEntryKeyChanged(
            int entryIndex,
            string previousKey,
            string newKey)
        {
            ModifyEntries(
                "Rename Conversation Blackboard Entry",
                entriesProperty =>
                {
                    if (entryIndex < 0 || entryIndex >= entriesProperty.arraySize)
                    {
                        return;
                    }

                    SerializedProperty entryProperty =
                        entriesProperty.GetArrayElementAtIndex(entryIndex);
                    SerializedProperty keyProperty = entryProperty.FindPropertyRelative(
                        KeyPropertyName);
                    keyProperty.stringValue = string.IsNullOrWhiteSpace(newKey)
                        ? MakeUniqueKey(entriesProperty, DefaultEntryKeyName, entryIndex)
                        : newKey.Trim();
                });

            if (_entryExpansionStates.TryGetValue(previousKey, out bool wasExpanded))
            {
                _entryExpansionStates.Remove(previousKey);
                _entryExpansionStates[newKey ?? string.Empty] = wasExpanded;
            }
        }

        private void HandleEntryTypeChanged(int entryIndex, string displayName)
        {
            ModifyEntries(
                "Change Conversation Blackboard Entry Type",
                entriesProperty =>
                {
                    if (entryIndex < 0 || entryIndex >= entriesProperty.arraySize)
                    {
                        return;
                    }

                    SerializedProperty valueProperty = entriesProperty
                        .GetArrayElementAtIndex(entryIndex)
                        .FindPropertyRelative(ValuePropertyName);
                    valueProperty.managedReferenceValue = CreateValueInstance(displayName);
                });
        }

        private void HandleEntryValueChanged()
        {
            if (_table == null)
            {
                return;
            }

            EditorUtility.SetDirty(_table);
            BlackboardChanged?.Invoke();
        }

        private void RemoveEntry(int entryIndex, string entryKey)
        {
            ModifyEntries(
                "Remove Conversation Blackboard Entry",
                entriesProperty =>
                {
                    if (entryIndex < 0 || entryIndex >= entriesProperty.arraySize)
                    {
                        return;
                    }

                    entriesProperty.DeleteArrayElementAtIndex(entryIndex);
                });

            _entryExpansionStates.Remove(entryKey);
        }

        private void ModifyEntries(
            string undoLabel,
            Action<SerializedProperty> mutation)
        {
            if (_table == null || mutation == null)
            {
                return;
            }

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();
            SerializedProperty entriesProperty = FindEntriesProperty(serializedTable);

            if (entriesProperty == null)
            {
                return;
            }

            Undo.RecordObject(_table, undoLabel);
            mutation(entriesProperty);
            serializedTable.ApplyModifiedProperties();
            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);
            Refresh();
            BlackboardChanged?.Invoke();
        }

        private SerializedProperty FindEntriesProperty(SerializedObject serializedTable)
        {
            SerializedProperty conversationProperty = FindSelectedConversationProperty(
                serializedTable);
            SerializedProperty graphProperty = conversationProperty?.FindPropertyRelative(
                GraphPropertyName);
            SerializedProperty blackboardProperty = graphProperty?.FindPropertyRelative(
                BlackboardPropertyName);
            return blackboardProperty?.FindPropertyRelative(EntriesPropertyName);
        }

        private bool TryGetEntry(int entryIndex, out GraphBlackboardEntry entry)
        {
            entry = null;

            IReadOnlyList<GraphBlackboardEntry> entries = _conversation?.Graph?.Blackboard?.Entries;

            if (entries == null || entryIndex < 0 || entryIndex >= entries.Count)
            {
                return false;
            }

            entry = entries[entryIndex];
            return entry != null;
        }

        private static string ResolveEntryTitle(SerializedProperty keyProperty)
        {
            return string.IsNullOrWhiteSpace(keyProperty?.stringValue)
                ? DefaultEntryKeyName
                : keyProperty.stringValue.Trim();
        }

        private bool ResolveEntryExpansionState(string entryKey)
        {
            return string.IsNullOrWhiteSpace(entryKey)
                || !_entryExpansionStates.TryGetValue(entryKey, out bool isExpanded)
                || isExpanded;
        }

        private void PersistEntryExpansionState(string entryKey, bool isExpanded)
        {
            if (string.IsNullOrWhiteSpace(entryKey))
            {
                return;
            }

            _entryExpansionStates[entryKey] = isExpanded;
        }

        private List<string> GetEntryTypeDisplayNames()
        {
            return GraphBlackboardValueRegistry.GetDescriptors(ConversationGraphFamily.Id)
                .Where(descriptor => !descriptor.HiddenFromPicker)
                .Select(descriptor => descriptor.DisplayName)
                .ToList();
        }

        private string GetEntryTypeDisplayName(SerializedProperty valueProperty)
        {
            if (valueProperty?.managedReferenceValue is not GraphBlackboardValue value)
            {
                return DefaultEntryTypeDisplayName();
            }

            return GraphBlackboardValueRegistry.GetDisplayName(value);
        }

        private GraphBlackboardValue CreateValueInstance(string displayName)
        {
            if (GraphBlackboardValueRegistry.TryGetDescriptor(
                    displayName,
                    ConversationGraphFamily.Id,
                    out GraphBlackboardValueRegistry.Descriptor descriptor))
            {
                return descriptor.CreateValue(descriptor.RuntimeValueType);
            }

            return CreateDefaultValueInstance();
        }

        private GraphBlackboardValue CreateDefaultValueInstance()
        {
            return GraphBlackboardValueRegistry.TryCreateValue(
                typeof(string),
                ConversationGraphFamily.Id,
                out GraphBlackboardValue value)
                ? value
                : new GraphBlackboardStringValue();
        }

        private string DefaultEntryTypeDisplayName()
        {
            return GraphBlackboardValueRegistry.TryGetDescriptorForRuntimeType(
                typeof(string),
                ConversationGraphFamily.Id,
                out GraphBlackboardValueRegistry.Descriptor descriptor)
                ? descriptor.DisplayName
                : "String";
        }

        private SerializedProperty FindSelectedConversationProperty(SerializedObject serializedTable)
        {
            if (_table == null
                || _conversation == null
                || serializedTable == null
                || !_table.TryGetConversationIndex(
                    _conversation.ConversationId,
                    out int conversationIndex))
            {
                return null;
            }

            SerializedProperty conversationsProperty = serializedTable.FindProperty(
                ConversationsPropertyName);

            if (conversationsProperty == null
                || conversationIndex < 0
                || conversationIndex >= conversationsProperty.arraySize)
            {
                return null;
            }

            return conversationsProperty.GetArrayElementAtIndex(conversationIndex);
        }

        private static string MakeUniqueKey(
            SerializedProperty entriesProperty,
            string baseKey,
            int seed)
        {
            string resolvedBaseKey = string.IsNullOrWhiteSpace(baseKey)
                ? DefaultEntryKeyName
                : baseKey.Trim();
            HashSet<string> existingKeys = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < entriesProperty.arraySize; index++)
            {
                SerializedProperty keyProperty = entriesProperty
                    .GetArrayElementAtIndex(index)
                    ?.FindPropertyRelative(KeyPropertyName);

                if (!string.IsNullOrWhiteSpace(keyProperty?.stringValue))
                {
                    existingKeys.Add(keyProperty.stringValue.Trim());
                }
            }

            if (!existingKeys.Contains(resolvedBaseKey))
            {
                return resolvedBaseKey;
            }

            int suffix = Math.Max(1, seed + 1);

            while (existingKeys.Contains($"{resolvedBaseKey} {suffix}"))
            {
                suffix++;
            }

            return $"{resolvedBaseKey} {suffix}";
        }
    }
}