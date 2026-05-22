using System;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.Editor.GraphCore;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Renders one conversation-specific presentation override overlay next to the graph.
    /// </summary>
    public sealed class ConversationGraphPresentationOverrideView : GraphBlackboardOverlayView
    {
        private const string ConversationsPropertyName = "_conversations";
        private const string PresenterOverridePropertyName = "_presenterOverridePrefab";

        private ConversationTable _table;
        private ConversationDefinition _conversation;

        /// <summary>
        /// Raised after the conversation-specific presenter override changes.
        /// </summary>
        public event Action PresentationChanged;

        /// <summary>
        /// Creates the overlay hierarchy used by the presentation override panel.
        /// </summary>
        public ConversationGraphPresentationOverrideView()
            : base(
                "Presentation Override",
                "Reset",
                "Clear the conversation-specific presenter override and use the table default.")
        {
            Refresh();
        }

        /// <summary>
        /// Binds the overlay to one authored conversation.
        /// </summary>
        /// <param name="table">Authored table that owns the selected conversation.</param>
        /// <param name="conversation">Selected authored conversation.</param>
        public void BindConversation(
            ConversationTable table,
            ConversationDefinition conversation)
        {
            _table = table;
            _conversation = conversation;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the overlay from the currently bound table and conversation.
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
                UpdateOverlayHeader($"Presentation ({_table.DisplayName})", 0);
                ShowOverlayStateBox(
                    "Choose a conversation to override its presenter prefab.",
                    HelpBoxMessageType.Info);
                return;
            }

            SetOverlayContentEnabled(true);

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();
            SerializedProperty overrideProperty = FindOverrideProperty(serializedTable);

            if (overrideProperty == null)
            {
                UpdateOverlayHeader($"Presentation ({_conversation.Title})", 0);
                ShowOverlayStateBox(
                    "This conversation cannot show its presentation override right now.",
                    HelpBoxMessageType.Error);
                return;
            }

            GameObject overridePresenter = overrideProperty.objectReferenceValue as GameObject;
            GameObject effectivePresenter = _table.ResolvePresenterPrefab(_conversation);
            UpdateOverlayHeader(
                $"Presentation ({_conversation.Title})",
                overridePresenter == null ? 0 : 1);

            if (effectivePresenter == null)
            {
                ShowOverlayStateBox(
                    "No presenter prefab is configured yet. This conversation will only auto-spawn a presenter after the table default or this override is assigned.",
                    HelpBoxMessageType.Info);
            }
            else if (effectivePresenter.GetComponent<ConversationPresenterRoot>() == null)
            {
                ShowOverlayStateBox(
                    "The effective presenter prefab is missing ConversationPresenterRoot.",
                    HelpBoxMessageType.Warning);
            }
            else
            {
                HideOverlayStateBox();
            }

            AddOverlayEntry(CreateOverrideElement(overrideProperty, effectivePresenter));
        }

        /// <inheritdoc />
        protected override void HandleAddRequested()
        {
            if (_table == null || _conversation == null)
            {
                return;
            }

            ModifyOverride(
                "Reset Conversation Presenter Override",
                overrideProperty => overrideProperty.objectReferenceValue = null);
        }

        /// <summary>
        /// Creates the UI used to edit the conversation-specific presenter override.
        /// </summary>
        /// <param name="overrideProperty">Serialized presenter override property.</param>
        /// <param name="effectivePresenter">Effective presenter currently resolved.</param>
        /// <returns>The composed override element.</returns>
        private VisualElement CreateOverrideElement(
            SerializedProperty overrideProperty,
            GameObject effectivePresenter)
        {
            VisualElement container = new();
            ApplyOverlayEntryContainerStyle(container);

            Label titleLabel = new("Conversation Presenter");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 6f;
            container.Add(titleLabel);

            ObjectField overrideField = new()
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                value = overrideProperty.objectReferenceValue,
            };
            overrideField.RegisterValueChangedCallback(
                evt => HandleOverrideChanged(evt.newValue as GameObject));
            container.Add(CreateOverlayFieldSection("Override Prefab", overrideField));

            return container;
        }

        /// <summary>
        /// Stores one new presenter override selection.
        /// </summary>
        /// <param name="presenterPrefab">Presenter prefab that should override the table default.</param>
        private void HandleOverrideChanged(GameObject presenterPrefab)
        {
            ModifyOverride(
                "Set Conversation Presenter Override",
                overrideProperty => overrideProperty.objectReferenceValue = presenterPrefab);
        }

        /// <summary>
        /// Applies one override mutation through the serialized table.
        /// </summary>
        /// <param name="undoLabel">Undo label recorded for the mutation.</param>
        /// <param name="applyMutation">Mutation applied to the serialized override property.</param>
        private void ModifyOverride(
            string undoLabel,
            Action<SerializedProperty> applyMutation)
        {
            if (_table == null || _conversation == null || applyMutation == null)
            {
                return;
            }

            SerializedObject serializedTable = new(_table);
            serializedTable.UpdateIfRequiredOrScript();
            SerializedProperty overrideProperty = FindOverrideProperty(serializedTable);

            if (overrideProperty == null)
            {
                return;
            }

            Undo.RecordObject(_table, undoLabel);
            applyMutation(overrideProperty);
            serializedTable.ApplyModifiedProperties();
            EditorUtility.SetDirty(_table);
            Refresh();
            PresentationChanged?.Invoke();
        }

        /// <summary>
        /// Resolves the serialized presenter override property for the bound conversation.
        /// </summary>
        /// <param name="serializedTable">Serialized conversation table.</param>
        /// <returns>The serialized presenter override property when available.</returns>
        private SerializedProperty FindOverrideProperty(SerializedObject serializedTable)
        {
            if (_table == null
                || _conversation == null
                || !_table.TryGetConversationIndex(_conversation.ConversationId, out int conversationIndex))
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

            return conversationsProperty
                .GetArrayElementAtIndex(conversationIndex)
                .FindPropertyRelative(PresenterOverridePropertyName);
        }
    }
}