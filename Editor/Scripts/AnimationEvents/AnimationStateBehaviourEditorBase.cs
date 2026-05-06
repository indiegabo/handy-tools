using IndieGabo.HandyTools.AnimationEventsModule;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// Shared inspector base for animation-event state behaviours.
    /// </summary>
    public abstract class AnimationStateBehaviourEditorBase : UnityEditor.Editor
    {
        #region State

        private SerializedProperty _eventProperty;
        private HelpBox _configurationBox;

        #endregion

        #region Abstract Members

        /// <summary>
        /// Gets the label shown for the serialized event field.
        /// </summary>
        protected virtual string EventFieldLabel => "Event";

        /// <summary>
        /// Resolves the configuration validation message for the concrete
        /// behaviour.
        /// </summary>
        /// <param name="eventProperty">Serialized event trigger property.</param>
        /// <param name="message">Resolved validation message.</param>
        /// <param name="messageType">Severity associated with the message.</param>
        /// <returns>True when a validation message should be shown.</returns>
        protected abstract bool TryGetConfigurationStatus(
            SerializedProperty eventProperty,
            out string message,
            out HelpBoxMessageType messageType
        );

        #endregion

        #region Editor Lifecycle

        /// <summary>
        /// Releases editor callbacks when the inspector is disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        #endregion

        #region Inspector

        /// <summary>
        /// Creates the custom inspector UI for the state behaviour.
        /// </summary>
        /// <returns>The root inspector visual element.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            EnsureEditorCallbacks();
            EnsureSerializedProperties();

            VisualElement root = CreateRoot();

            PropertyField eventField = new(_eventProperty, EventFieldLabel);
            eventField.style.marginBottom = 8f;
            root.Add(eventField);

            _configurationBox = new(string.Empty, HelpBoxMessageType.None);
            _configurationBox.style.marginBottom = 8f;
            root.Add(_configurationBox);

            root.Bind(serializedObject);
            RefreshUi();
            return root;
        }

        /// <summary>
        /// Draws an IMGUI fallback for hosts that do not support UI Toolkit
        /// custom editors for state machine behaviours.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EnsureSerializedProperties();
            serializedObject.Update();

            EditorGUILayout.PropertyField(
                _eventProperty,
                new GUIContent(EventFieldLabel),
                includeChildren: true
            );

            if (TryGetConfigurationStatus(
                    _eventProperty,
                    out string configurationMessage,
                    out HelpBoxMessageType configurationMessageType
                ))
            {
                EditorGUILayout.HelpBox(
                    configurationMessage,
                    ToImGuiMessageType(configurationMessageType)
                );
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        /// <summary>
        /// Refreshes validation after editor changes.
        /// </summary>
        private void RefreshUi()
        {
            if (_eventProperty == null || _configurationBox == null || target == null)
            {
                return;
            }

            serializedObject.Update();

            if (TryGetConfigurationStatus(
                    _eventProperty,
                    out string configurationMessage,
                    out HelpBoxMessageType configurationMessageType
                ))
            {
                _configurationBox.text = configurationMessage;
                _configurationBox.messageType = configurationMessageType;
                _configurationBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                _configurationBox.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Handles selection changes so configuration status stays current.
        /// </summary>
        private void OnSelectionChanged()
        {
            Repaint();
            RefreshUi();
        }

        #region Helpers

        /// <summary>
        /// Ensures cached serialized properties are available in both UI Toolkit
        /// and IMGUI hosts.
        /// </summary>
        private void EnsureSerializedProperties()
        {
            _eventProperty ??= serializedObject.FindProperty("_event");
        }

        /// <summary>
        /// Ensures editor callbacks are subscribed exactly once.
        /// </summary>
        private void EnsureEditorCallbacks()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        /// <summary>
        /// Converts one UI Toolkit help-box message type into its IMGUI
        /// equivalent.
        /// </summary>
        /// <param name="messageType">UI Toolkit message type.</param>
        /// <returns>Equivalent IMGUI message type.</returns>
        private static MessageType ToImGuiMessageType(
            HelpBoxMessageType messageType
        )
        {
            return messageType switch
            {
                HelpBoxMessageType.Error => MessageType.Error,
                HelpBoxMessageType.Warning => MessageType.Warning,
                HelpBoxMessageType.None => MessageType.None,
                _ => MessageType.Info,
            };
        }

        /// <summary>
        /// Creates the shared root container style for the inspector.
        /// </summary>
        /// <returns>Styled root visual element.</returns>
        private static VisualElement CreateRoot()
        {
            VisualElement root = new();
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 4f;
            return root;
        }

        #endregion
    }
}