using System.Collections.Generic;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Debugging;
using IndieGabo.HandyTools.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Debugging
{
    /// <summary>
    /// UI Toolkit configuration panel for the Debugging module.
    /// </summary>
    public sealed class DebuggingModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        private static readonly List<string> FpsChoices = new()
        {
            "60 FPS",
            "30 FPS",
            "No Constraints",
        };

        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => DebuggingModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            DebuggingModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            DebugPanelConfig.ReloadInstance();
            DebugSettingsConfig.ReloadInstance();

            DebugPanelConfig panelConfig = DebugPanelConfig.Instance;
            DebugSettingsConfig settingsConfig = DebugSettingsConfig.Instance;

            root.Add(CreateIntroLabel());
            root.Add(CreateBoolToggle(
                "Enable Debug Panel",
                panelConfig.IsEnabled,
                value =>
                {
                    panelConfig.IsEnabled = value;
                    AssetDatabase.SaveAssets();
                }
            ));
            root.Add(CreateBoolToggle(
                "Pause Gameplay When Open",
                panelConfig.PauseGameplayWhenOpen,
                value =>
                {
                    panelConfig.PauseGameplayWhenOpen = value;
                    AssetDatabase.SaveAssets();
                }
            ));
            root.Add(CreateBoolToggle(
                "Unlock Cursor When Open",
                panelConfig.UnlockCursorWhenOpen,
                value =>
                {
                    panelConfig.UnlockCursorWhenOpen = value;
                    AssetDatabase.SaveAssets();
                }
            ));
            root.Add(CreateInputActionField(panelConfig));
            root.Add(CreateBoolToggle(
                "Vertical Sync",
                settingsConfig.VSyncOn,
                value =>
                {
                    settingsConfig.VSyncOn = value;
                    AssetDatabase.SaveAssets();
                }
            ));
            root.Add(CreateFpsLimitField(settingsConfig));
            root.Add(CreateBuildAvailabilityLabel());
        }

        private static Label CreateIntroLabel()
        {
            Label label = new(
                "Configure how the runtime debug panel behaves when it is available in editor, development, or debug builds."
            );
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8f;
            return label;
        }

        private static Toggle CreateBoolToggle(string label, bool value, System.Action<bool> onChanged)
        {
            Toggle toggle = new(label);
            toggle.SetValueWithoutNotify(value);
            toggle.style.marginBottom = 6f;
            toggle.RegisterValueChangedCallback(changeEvent => onChanged(changeEvent.newValue));
            return toggle;
        }

        private static VisualElement CreateInputActionField(DebugPanelConfig panelConfig)
        {
            SerializedObject serializedPanelConfig = new(panelConfig);
            SerializedProperty openCloseInputActionProperty =
                serializedPanelConfig.FindProperty("_openCloseInputAction");

            if (openCloseInputActionProperty == null)
            {
                return new HelpBox(
                    "The standalone debug panel InputAction could not be loaded.",
                    HelpBoxMessageType.Error
                );
            }

            IMGUIContainer field = new(() =>
            {
                serializedPanelConfig.Update();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(
                    openCloseInputActionProperty,
                    new GUIContent(
                        "Open / Close Input Action",
                        "Configure a standalone embedded InputAction with direct bindings. No InputActionReference is used."
                    ),
                    true
                );

                if (!EditorGUI.EndChangeCheck())
                {
                    return;
                }

                serializedPanelConfig.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            });
            field.style.marginBottom = 6f;
            return field;
        }

        private static DropdownField CreateFpsLimitField(DebugSettingsConfig settingsConfig)
        {
            List<string> choices = new(FpsChoices);
            if (!choices.Contains(settingsConfig.FpsLimit))
            {
                choices.Add(settingsConfig.FpsLimit);
            }

            DropdownField field = new("FPS Limit", choices, settingsConfig.FpsLimit);
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent =>
            {
                settingsConfig.FpsLimit = changeEvent.newValue;
                AssetDatabase.SaveAssets();
            });
            return field;
        }

        private static HelpBox CreateBuildAvailabilityLabel()
        {
            return new HelpBox(
                "The runtime debug panel is available in the editor and development or debug builds.",
                HelpBoxMessageType.Info
            );
        }
    }
}