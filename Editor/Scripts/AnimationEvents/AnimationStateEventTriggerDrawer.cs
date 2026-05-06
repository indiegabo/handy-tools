using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// UI Toolkit drawer for string-based state animation event triggers.
    /// </summary>
    [CustomPropertyDrawer(typeof(IndieGabo.HandyTools.AnimationEventsModule.AnimationStateEventTrigger))]
    public sealed class AnimationStateEventTriggerDrawer : PropertyDrawer
    {
        #region Inspector

        /// <summary>
        /// Creates the property UI for one state trigger entry.
        /// </summary>
        /// <param name="property">Serialized trigger property.</param>
        /// <returns>The root property visual element.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            string propertyPath = property.propertyPath;
            SerializedObject serializedObject = property.serializedObject;

            VisualElement root = CreateBox();

            PropertyField eventNameField = new(
                property.FindPropertyRelative("_eventName"),
                "Event Name"
            );
            eventNameField.style.marginBottom = 6f;
            root.Add(eventNameField);

            Slider triggerTimeSlider = new("Trigger Time", 0f, 1f)
            {
                showInputField = true,
            };
            triggerTimeSlider.SetValueWithoutNotify(
                property.FindPropertyRelative("_triggerTime").floatValue
            );
            triggerTimeSlider.RegisterValueChangedCallback(evt =>
            {
                serializedObject.Update();

                SerializedProperty triggerProperty = serializedObject
                    .FindProperty(propertyPath);
                SerializedProperty triggerTimeProperty = triggerProperty
                    .FindPropertyRelative("_triggerTime");

                triggerTimeProperty.floatValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                SyncAnimationWindowNeedle(serializedObject, evt.newValue);
            });
            root.Add(triggerTimeSlider);

            Button useNeedleTimeButton = new(() =>
                UseAnimationWindowNeedle(
                    serializedObject,
                    propertyPath,
                    triggerTimeSlider
                ))
            {
                text = "Use Needle Time"
            };
            useNeedleTimeButton.style.marginTop = 6f;
            root.Add(useNeedleTimeButton);

            EditorApplication.CallbackFunction refreshNeedleButton = () =>
                RefreshNeedleButtonState(useNeedleTimeButton, serializedObject);
            useNeedleTimeButton.RegisterCallback<AttachToPanelEvent>(
                _ => EditorApplication.update += refreshNeedleButton
            );
            useNeedleTimeButton.RegisterCallback<DetachFromPanelEvent>(
                _ => EditorApplication.update -= refreshNeedleButton
            );
            refreshNeedleButton();

            return root;
        }

        /// <summary>
        /// Draws an IMGUI fallback for hosts that do not support UI Toolkit
        /// property drawers.
        /// </summary>
        /// <param name="position">Draw area for the property.</param>
        /// <param name="property">Serialized trigger property.</param>
        /// <param name="label">Property label.</param>
        public override void OnGUI(
            UnityEngine.Rect position,
            SerializedProperty property,
            UnityEngine.GUIContent label
        )
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            UnityEngine.Rect contentRect = EditorGUI.IndentedRect(position);
            contentRect.height = lineHeight;

            SerializedProperty eventNameProperty = property
                .FindPropertyRelative("_eventName");
            SerializedProperty triggerTimeProperty = property
                .FindPropertyRelative("_triggerTime");

            EditorGUI.PropertyField(
                contentRect,
                eventNameProperty,
                new UnityEngine.GUIContent("Event Name")
            );

            contentRect.y += lineHeight + verticalSpacing;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(
                contentRect,
                triggerTimeProperty,
                new UnityEngine.GUIContent("Trigger Time")
            );

            if (EditorGUI.EndChangeCheck())
            {
                SyncAnimationWindowNeedle(
                    property.serializedObject,
                    triggerTimeProperty.floatValue
                );
            }

            contentRect.y += lineHeight + verticalSpacing;

            bool hasNeedle = TryGetAnimationWindowNeedle(
                property.serializedObject,
                out AnimationStateBehaviourPreviewSession.AnimationWindowNeedleContext needle,
                out _,
                out _
            );

            using (new EditorGUI.DisabledScope(!hasNeedle))
            {
                if (GUI.Button(contentRect, "Use Needle Time"))
                {
                    triggerTimeProperty.floatValue = needle.NormalizedTime;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets the IMGUI height required by the fallback drawer.
        /// </summary>
        /// <param name="property">Serialized trigger property.</param>
        /// <param name="label">Property label.</param>
        /// <returns>Height required to draw the property.</returns>
        public override float GetPropertyHeight(
            SerializedProperty property,
            UnityEngine.GUIContent label
        )
        {
            return (EditorGUIUtility.singleLineHeight * 3f)
                + (EditorGUIUtility.standardVerticalSpacing * 2f);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates the shared boxed layout used by list entries.
        /// </summary>
        /// <returns>Styled root visual element.</returns>
        private static VisualElement CreateBox()
        {
            VisualElement root = new();
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;
            return root;
        }

        /// <summary>
        /// Pushes the current trigger time into the Animation window needle
        /// when the owning state behaviour can be resolved.
        /// </summary>
        /// <param name="serializedObject">Owning serialized object.</param>
        /// <param name="normalizedTime">Normalized trigger time.</param>
        private static void SyncAnimationWindowNeedle(
            SerializedObject serializedObject,
            float normalizedTime
        )
        {
            if (serializedObject.targetObject is not StateMachineBehaviour behaviour)
            {
                return;
            }

            AnimationStateBehaviourPreviewSession.TrySyncAnimationWindowNeedle(
                behaviour,
                normalizedTime,
                out _,
                out _
            );
        }

        /// <summary>
        /// Copies the Animation window needle time into the trigger time.
        /// </summary>
        /// <param name="serializedObject">Owning serialized object.</param>
        /// <param name="propertyPath">Trigger property path.</param>
        /// <param name="triggerTimeSlider">Slider UI to update after copying.</param>
        private static void UseAnimationWindowNeedle(
            SerializedObject serializedObject,
            string propertyPath,
            Slider triggerTimeSlider
        )
        {
            if (!TryGetAnimationWindowNeedle(
                    serializedObject,
                    out AnimationStateBehaviourPreviewSession.AnimationWindowNeedleContext needle,
                    out _,
                    out _
                ))
            {
                return;
            }

            serializedObject.Update();

            SerializedProperty triggerProperty = serializedObject
                .FindProperty(propertyPath);
            SerializedProperty triggerTimeProperty = triggerProperty
                ?.FindPropertyRelative("_triggerTime");

            if (triggerTimeProperty == null)
            {
                return;
            }

            triggerTimeProperty.floatValue = needle.NormalizedTime;
            serializedObject.ApplyModifiedProperties();
            triggerTimeSlider?.SetValueWithoutNotify(needle.NormalizedTime);
        }

        /// <summary>
        /// Refreshes the needle button enabled state for the current host.
        /// </summary>
        /// <param name="button">Button to refresh.</param>
        /// <param name="serializedObject">Owning serialized object.</param>
        private static void RefreshNeedleButtonState(
            Button button,
            SerializedObject serializedObject
        )
        {
            bool hasNeedle = TryGetAnimationWindowNeedle(
                serializedObject,
                out AnimationStateBehaviourPreviewSession.AnimationWindowNeedleContext needle,
                out string message,
                out _
            );

            button.SetEnabled(hasNeedle);
            button.tooltip = hasNeedle
                ? $"Use the Animation Window needle time from {needle.AnimationClip.name}."
                : message;
        }

        /// <summary>
        /// Tries to resolve the Animation window needle for one drawer host.
        /// </summary>
        /// <param name="serializedObject">Owning serialized object.</param>
        /// <param name="needle">Resolved needle context.</param>
        /// <param name="message">User-facing status message.</param>
        /// <param name="messageType">Severity associated with the message.</param>
        /// <returns>True when the needle can be used.</returns>
        private static bool TryGetAnimationWindowNeedle(
            SerializedObject serializedObject,
            out AnimationStateBehaviourPreviewSession.AnimationWindowNeedleContext needle,
            out string message,
            out HelpBoxMessageType messageType
        )
        {
            if (serializedObject.targetObject is not StateMachineBehaviour behaviour)
            {
                needle = default;
                message = "The owning state behaviour could not be resolved.";
                messageType = HelpBoxMessageType.Warning;
                return false;
            }

            return AnimationStateBehaviourPreviewSession.TryGetAnimationWindowNeedle(
                behaviour,
                out needle,
                out message,
                out messageType
            );
        }

        #endregion
    }
}