using IndieGabo.HandyTools.ConversationsModule.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Inspectors
{
    /// <summary>
    /// Provides the primary authoring entry point for one conversation table asset.
    /// </summary>
    [CustomEditor(typeof(ConversationTable), true)]
    public sealed class ConversationTableInspector : UnityEditor.Editor
    {
        private const float PrimaryButtonHeight = 36f;

        private SerializedProperty _conversationsProperty;
        private SerializedProperty _actorsProperty;

        /// <summary>
        /// Caches serialized property handles used by the inspector.
        /// </summary>
        private void OnEnable()
        {
            _conversationsProperty = serializedObject.FindProperty("_conversations");
            _actorsProperty = serializedObject.FindProperty("_actors");
        }

        /// <summary>
        /// Creates the custom inspector UI for one conversation table asset.
        /// </summary>
        /// <returns>The configured inspector root.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            ConversationTable table = target as ConversationTable;

            if (table == null)
            {
                return new VisualElement();
            }

            VisualElement root = new();
            root.style.marginTop = 2f;

            root.Add(CreateScriptReferenceField());
            root.Add(CreateOpenGraphButton(table));
            root.Add(new HelpBox(
                "Use the Conversations window to manage shared conversants, author conversations, configure presentation defaults and per-conversation overrides, edit one selected graph, and configure table-level input.",
                HelpBoxMessageType.Info));

            Foldout graphFoldout = new()
            {
                text = "Serialized Table",
                value = false,
            };

            graphFoldout.style.marginTop = 6f;

            if (_conversationsProperty != null)
            {
                graphFoldout.Add(new PropertyField(_conversationsProperty));
            }

            if (_actorsProperty != null)
            {
                graphFoldout.Add(new PropertyField(_actorsProperty));
            }

            root.Add(graphFoldout);

            root.Bind(serializedObject);
            return root;
        }

        /// <summary>
        /// Creates the disabled script reference field.
        /// </summary>
        /// <returns>The configured property field.</returns>
        private PropertyField CreateScriptReferenceField()
        {
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            PropertyField field = new(scriptProperty);
            field.SetEnabled(false);
            field.style.marginBottom = 6f;
            return field;
        }

        /// <summary>
        /// Creates the primary action button that opens the graph window.
        /// </summary>
        /// <param name="table">Inspected conversation table.</param>
        /// <returns>The configured button.</returns>
        private Button CreateOpenGraphButton(ConversationTable table)
        {
            Button button = new(() =>
            {
                serializedObject.ApplyModifiedProperties();
                ConversationGraphWindow.Open(table);
            })
            {
                text = "Open Conversations Window"
            };

            button.style.height = PrimaryButtonHeight;
            button.style.marginBottom = 6f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;

            return button;
        }
    }
}