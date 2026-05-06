using UnityEditor;
using UnityEditor.UIElements;
using IndieGabo.HandyTools.AnimationEventsModule;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// UI Toolkit drawer for typed HandyBus state animation event triggers.
    /// </summary>
    [CustomPropertyDrawer(typeof(IndieGabo.HandyTools.AnimationEventsModule.AnimationEventBusStateTrigger))]
    public sealed class AnimationEventBusStateTriggerDrawer : PropertyDrawer
    {
        #region Inspector

        /// <summary>
        /// Creates the property UI for one typed bus trigger entry.
        /// </summary>
        /// <param name="property">Serialized trigger property.</param>
        /// <returns>The root property visual element.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            string propertyPath = property.propertyPath;
            SerializedObject serializedObject = property.serializedObject;

            VisualElement root = CreateBox();

            Slider triggerTimeField = new("Trigger Time", 0f, 1f)
            {
                showInputField = true,
            };
            triggerTimeField.SetValueWithoutNotify(
                property.FindPropertyRelative("_triggerTime").floatValue
            );
            triggerTimeField.RegisterValueChangedCallback(evt =>
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
            triggerTimeField.style.marginBottom = 6f;
            root.Add(triggerTimeField);

            Button useNeedleTimeButton = new(() =>
                UseAnimationWindowNeedle(
                    serializedObject,
                    propertyPath,
                    triggerTimeField
                ))
            {
                text = "Use Needle Time"
            };
            useNeedleTimeButton.style.marginBottom = 6f;
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

            Label eventLabel = new("Event");
            eventLabel.style.marginBottom = 4f;
            root.Add(eventLabel);

            ToolbarMenu eventMenu = new();
            eventMenu.style.marginBottom = 6f;
            root.Add(eventMenu);

            HelpBox descriptionBox = new(string.Empty, HelpBoxMessageType.Info);
            descriptionBox.style.marginBottom = 6f;
            root.Add(descriptionBox);

            VisualElement payloadContainer = new();
            payloadContainer.style.flexDirection = FlexDirection.Column;
            root.Add(payloadContainer);

            PopulateMenu(eventMenu, propertyPath, serializedObject, RefreshContent);
            RefreshContent();

            return root;

            /// <summary>
            /// Refreshes the selected metadata summary and the payload editor.
            /// </summary>
            void RefreshContent()
            {
                serializedObject.Update();

                SerializedProperty triggerProperty = serializedObject
                    .FindProperty(propertyPath);

                AnimatorBusEventMetadata metadata =
                    AnimatorBusEventEditorUtility.GetSelectedMetadata(
                        triggerProperty
                    );

                eventMenu.text = metadata != null
                    ? metadata.Path
                    : "Select Event";

                if (metadata == null)
                {
                    descriptionBox.text =
                        "Select an attributed AnimatorBusEvent type to author "
                        + "its payload inline.";
                    descriptionBox.style.display = DisplayStyle.Flex;
                }
                else if (string.IsNullOrWhiteSpace(metadata.Description))
                {
                    descriptionBox.text = metadata.DisplayName;
                    descriptionBox.style.display = DisplayStyle.Flex;
                }
                else
                {
                    descriptionBox.text =
                        $"{metadata.DisplayName}: {metadata.Description}";
                    descriptionBox.style.display = DisplayStyle.Flex;
                }

                payloadContainer.Clear();

                SerializedProperty payloadProperty = triggerProperty
                    .FindPropertyRelative(
                        AnimatorBusEventEditorUtility.EventPayloadFieldName
                    );

                if (metadata == null
                    || payloadProperty == null
                    || payloadProperty.managedReferenceValue == null)
                {
                    payloadContainer.Add(
                        new HelpBox(
                            "No event payload is currently selected.",
                            HelpBoxMessageType.Info
                        )
                    );
                    return;
                }

                payloadContainer.Add(
                    CreatePayloadInspector(propertyPath, serializedObject)
                );
            }
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

            SerializedProperty triggerTimeProperty = property
                .FindPropertyRelative("_triggerTime");
            SerializedProperty payloadProperty = property
                .FindPropertyRelative(
                    AnimatorBusEventEditorUtility.EventPayloadFieldName
                );
            AnimatorBusEventMetadata metadata =
                AnimatorBusEventEditorUtility.GetSelectedMetadata(property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            UnityEngine.Rect currentRect = EditorGUI.IndentedRect(position);
            currentRect.height = lineHeight;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(
                currentRect,
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

            currentRect.y += lineHeight + verticalSpacing;

            bool hasNeedle = TryGetAnimationWindowNeedle(
                property.serializedObject,
                out AnimationStateBehaviourPreviewSession.AnimationWindowNeedleContext needle,
                out _,
                out _
            );

            using (new EditorGUI.DisabledScope(!hasNeedle))
            {
                if (GUI.Button(currentRect, "Use Needle Time"))
                {
                    triggerTimeProperty.floatValue = needle.NormalizedTime;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            currentRect.y += lineHeight + verticalSpacing;

            UnityEngine.Rect eventLabelRect = currentRect;
            eventLabelRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(eventLabelRect, "Event");

            UnityEngine.Rect eventButtonRect = currentRect;
            eventButtonRect.xMin += EditorGUIUtility.labelWidth;

            string buttonText = metadata != null ? metadata.Path : "Select Event";
            if (EditorGUI.DropdownButton(
                    eventButtonRect,
                    new UnityEngine.GUIContent(buttonText),
                    UnityEngine.FocusType.Passive
                ))
            {
                ShowEventMenu(property.propertyPath, property.serializedObject);
            }

            currentRect.y += lineHeight + verticalSpacing;

            string descriptionText = metadata == null
                ? "Select an attributed AnimatorBusEvent type to author its payload inline."
                : string.IsNullOrWhiteSpace(metadata.Description)
                    ? metadata.DisplayName
                    : $"{metadata.DisplayName}: {metadata.Description}";
            float descriptionHeight = GetHelpBoxHeight(descriptionText);
            UnityEngine.Rect descriptionRect = currentRect;
            descriptionRect.height = descriptionHeight;
            EditorGUI.HelpBox(descriptionRect, descriptionText, MessageType.Info);

            currentRect.y += descriptionHeight + verticalSpacing;

            if (metadata == null
                || payloadProperty == null
                || payloadProperty.managedReferenceValue == null)
            {
                float infoHeight = GetHelpBoxHeight(
                    "No event payload is currently selected."
                );
                UnityEngine.Rect infoRect = currentRect;
                infoRect.height = infoHeight;
                EditorGUI.HelpBox(
                    infoRect,
                    "No event payload is currently selected.",
                    MessageType.Info
                );
                EditorGUI.EndProperty();
                return;
            }

            float payloadHeight = GetPayloadFieldsHeight(payloadProperty);
            UnityEngine.Rect payloadRect = currentRect;
            payloadRect.height = payloadHeight;
            DrawPayloadFields(payloadRect, payloadProperty);

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
            SerializedProperty payloadProperty = property
                .FindPropertyRelative(
                    AnimatorBusEventEditorUtility.EventPayloadFieldName
                );
            AnimatorBusEventMetadata metadata =
                AnimatorBusEventEditorUtility.GetSelectedMetadata(property);

            float height = (EditorGUIUtility.singleLineHeight * 3f)
                + (EditorGUIUtility.standardVerticalSpacing * 4f);

            string descriptionText = metadata == null
                ? "Select an attributed AnimatorBusEvent type to author its payload inline."
                : string.IsNullOrWhiteSpace(metadata.Description)
                    ? metadata.DisplayName
                    : $"{metadata.DisplayName}: {metadata.Description}";
            height += GetHelpBoxHeight(descriptionText);

            if (metadata == null
                || payloadProperty == null
                || payloadProperty.managedReferenceValue == null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += GetHelpBoxHeight("No event payload is currently selected.");
                return height;
            }

            height += EditorGUIUtility.standardVerticalSpacing;
            height += GetPayloadFieldsHeight(payloadProperty);
            return height;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Populates the event selection menu with every discovered bus event.
        /// </summary>
        /// <param name="eventMenu">Toolbar menu used for selection.</param>
        /// <param name="propertyPath">Trigger property path.</param>
        /// <param name="serializedObject">Owning serialized object.</param>
        /// <param name="refreshContent">Refresh callback after selection.</param>
        private static void PopulateMenu(
            ToolbarMenu eventMenu,
            string propertyPath,
            SerializedObject serializedObject,
            System.Action refreshContent
        )
        {
            eventMenu.menu.AppendAction(
                "None",
                _ =>
                {
                    SerializedProperty triggerProperty = serializedObject
                        .FindProperty(propertyPath);

                    AnimatorBusEventEditorUtility.ApplySelection(
                        triggerProperty,
                        metadata: null
                    );

                    refreshContent();
                },
                action =>
                {
                    SerializedProperty triggerProperty = serializedObject
                        .FindProperty(propertyPath);

                    return AnimatorBusEventEditorUtility.GetSelectedMetadata(
                        triggerProperty
                    ) == null
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal;
                }
            );

            foreach (AnimatorBusEventMetadata metadata in
                AnimatorBusEventRegistry.GetEventMetadata())
            {
                eventMenu.menu.AppendAction(
                    AnimatorBusEventEditorUtility.GetMenuPath(metadata),
                    _ =>
                    {
                        SerializedProperty triggerProperty = serializedObject
                            .FindProperty(propertyPath);

                        AnimatorBusEventEditorUtility.ApplySelection(
                            triggerProperty,
                            metadata
                        );

                        refreshContent();
                    },
                    action =>
                    {
                        SerializedProperty triggerProperty = serializedObject
                            .FindProperty(propertyPath);

                        AnimatorBusEventMetadata selectedMetadata =
                            AnimatorBusEventEditorUtility.GetSelectedMetadata(
                                triggerProperty
                            );

                        return selectedMetadata != null
                            && selectedMetadata.Path == metadata.Path
                                ? DropdownMenuAction.Status.Checked
                                : DropdownMenuAction.Status.Normal;
                    }
                );
            }
        }

        /// <summary>
        /// Shows the IMGUI event picker menu.
        /// </summary>
        /// <param name="propertyPath">Trigger property path.</param>
        /// <param name="serializedObject">Owning serialized object.</param>
        private static void ShowEventMenu(
            string propertyPath,
            SerializedObject serializedObject
        )
        {
            GenericMenu menu = new();

            SerializedProperty selectedTriggerProperty = serializedObject
                .FindProperty(propertyPath);
            AnimatorBusEventMetadata selectedMetadata =
                AnimatorBusEventEditorUtility.GetSelectedMetadata(
                    selectedTriggerProperty
                );

            menu.AddItem(
                new UnityEngine.GUIContent("None"),
                selectedMetadata == null,
                () =>
                {
                    SerializedProperty triggerProperty = serializedObject
                        .FindProperty(propertyPath);
                    AnimatorBusEventEditorUtility.ApplySelection(
                        triggerProperty,
                        metadata: null
                    );
                }
            );

            foreach (AnimatorBusEventMetadata metadata in
                AnimatorBusEventRegistry.GetEventMetadata())
            {
                menu.AddItem(
                    new UnityEngine.GUIContent(
                        AnimatorBusEventEditorUtility.GetMenuPath(metadata)
                    ),
                    selectedMetadata != null
                        && selectedMetadata.Path == metadata.Path,
                    () =>
                    {
                        SerializedProperty triggerProperty = serializedObject
                            .FindProperty(propertyPath);
                        AnimatorBusEventEditorUtility.ApplySelection(
                            triggerProperty,
                            metadata
                        );
                    }
                );
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Calculates the help-box height used by the IMGUI fallback.
        /// </summary>
        /// <param name="text">Help-box text content.</param>
        /// <returns>Calculated IMGUI help-box height.</returns>
        private static float GetHelpBoxHeight(string text)
        {
            return EditorStyles.helpBox.CalcHeight(
                new UnityEngine.GUIContent(text),
                EditorGUIUtility.currentViewWidth - 48f
            );
        }

        /// <summary>
        /// Creates the inline payload inspector used inside the UI Toolkit
        /// drawer host.
        /// </summary>
        /// <param name="propertyPath">Trigger property path.</param>
        /// <param name="serializedObject">Owning serialized object.</param>
        /// <returns>IMGUI payload inspector container.</returns>
        private static IMGUIContainer CreatePayloadInspector(
            string propertyPath,
            SerializedObject serializedObject
        )
        {
            IMGUIContainer payloadInspector = new(() =>
            {
                serializedObject.Update();

                SerializedProperty triggerProperty = serializedObject
                    .FindProperty(propertyPath);
                SerializedProperty payloadProperty = triggerProperty
                    ?.FindPropertyRelative(
                        AnimatorBusEventEditorUtility.EventPayloadFieldName
                    );

                if (payloadProperty == null
                    || payloadProperty.managedReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(
                        "No event payload is currently selected.",
                        MessageType.Info
                    );
                    return;
                }

                EditorGUI.BeginChangeCheck();
                DrawPayloadFieldsLayout(payloadProperty);

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            });

            return payloadInspector;
        }

        /// <summary>
        /// Draws the payload section in layout mode without a foldout header.
        /// </summary>
        /// <param name="payloadProperty">Managed-reference payload property.</param>
        private static void DrawPayloadFieldsLayout(
            SerializedProperty payloadProperty
        )
        {
            EditorGUILayout.LabelField("Payload", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty iterator = payloadProperty.Copy();
                SerializedProperty endProperty = payloadProperty.GetEndProperty();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren)
                    && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    EditorGUILayout.PropertyField(
                        iterator,
                        includeChildren: true
                    );
                    enterChildren = false;
                }
            }
        }

        /// <summary>
        /// Draws the payload section in rect mode without a foldout header.
        /// </summary>
        /// <param name="position">Drawing area.</param>
        /// <param name="payloadProperty">Managed-reference payload property.</param>
        private static void DrawPayloadFields(
            UnityEngine.Rect position,
            SerializedProperty payloadProperty
        )
        {
            UnityEngine.Rect currentRect = position;
            currentRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(
                currentRect,
                "Payload",
                EditorStyles.boldLabel
            );

            currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty iterator = payloadProperty.Copy();
                SerializedProperty endProperty = payloadProperty.GetEndProperty();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren)
                    && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    float propertyHeight = EditorGUI.GetPropertyHeight(
                        iterator,
                        includeChildren: true
                    );

                    UnityEngine.Rect propertyRect = currentRect;
                    propertyRect.height = propertyHeight;

                    EditorGUI.PropertyField(
                        propertyRect,
                        iterator,
                        includeChildren: true
                    );

                    currentRect.y += propertyHeight
                        + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }
            }
        }

        /// <summary>
        /// Calculates the height of the always-expanded payload section.
        /// </summary>
        /// <param name="payloadProperty">Managed-reference payload property.</param>
        /// <returns>Height required to draw the payload label and fields.</returns>
        private static float GetPayloadFieldsHeight(
            SerializedProperty payloadProperty
        )
        {
            float height = EditorGUIUtility.singleLineHeight;

            SerializedProperty iterator = payloadProperty.Copy();
            SerializedProperty endProperty = payloadProperty.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren)
                && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(
                    iterator,
                    includeChildren: true
                );
                enterChildren = false;
            }

            return height;
        }

        /// <summary>
        /// Copies the Animation window needle time into the trigger time.
        /// </summary>
        /// <param name="serializedObject">Owning serialized object.</param>
        /// <param name="propertyPath">Trigger property path.</param>
        /// <param name="triggerTimeField">Slider UI to update after copying.</param>
        private static void UseAnimationWindowNeedle(
            SerializedObject serializedObject,
            string propertyPath,
            Slider triggerTimeField
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
            triggerTimeField?.SetValueWithoutNotify(needle.NormalizedTime);
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

        #endregion
    }
}