using System.Collections.Generic;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Modules;
using IndieGabo.HandyTools.ScreenShooter;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ScreenShooter
{
    /// <summary>
    /// UI Toolkit configuration panel for the ScreenShooter module.
    /// </summary>
    public sealed class ScreenShooterModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => ScreenShooterModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            ScreenShooterModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            ScreenShooterConfig.ReloadInstance();
            ScreenShooterConfig config = ScreenShooterConfig.Instance;

            HelpBox resolvedPathHelpBox = new(string.Empty, HelpBoxMessageType.None);
            resolvedPathHelpBox.style.marginBottom = 6f;

            root.Add(CreateIntroLabel(
                "The ScreenShooter module creates a persistent runtime capturer that saves screenshots to a configurable output directory."
            ));
            root.Add(CreateInputActionField(config));
            root.Add(CreateOutputDirectoryField(config, resolvedPathHelpBox));
            root.Add(resolvedPathHelpBox);

            RefreshResolvedPathHelpBox(config, resolvedPathHelpBox);
        }

        private static Label CreateIntroLabel(string text)
        {
            Label label = new(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8f;
            return label;
        }

        private static HelpBox CreateInfoBox(string text, HelpBoxMessageType messageType)
        {
            HelpBox helpBox = new(text, messageType);
            helpBox.style.marginBottom = 6f;
            return helpBox;
        }

        private static VisualElement CreateInputActionField(ScreenShooterConfig config)
        {
            SerializedObject serializedConfig = new(config);
            SerializedProperty shootInputActionProperty =
                serializedConfig.FindProperty("_shootInputAction");

            if (shootInputActionProperty == null)
            {
                return new HelpBox(
                    "The standalone screenshot InputAction could not be loaded.",
                    HelpBoxMessageType.Error
                );
            }

            IMGUIContainer field = new(() =>
            {
                serializedConfig.Update();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(
                    shootInputActionProperty,
                    new GUIContent(
                        "Screenshot Input Action",
                        "Configure a standalone embedded InputAction with direct bindings. No InputActionReference is used."
                    ),
                    true
                );

                if (!EditorGUI.EndChangeCheck())
                {
                    return;
                }

                serializedConfig.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            });
            field.style.marginBottom = 6f;
            return field;
        }

        private static TextField CreateOutputDirectoryField(
            ScreenShooterConfig config,
            HelpBox resolvedPathHelpBox
        )
        {
            TextField field = new("Output Directory Path")
            {
                value = config.OutputDirectoryPath,
            };
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent =>
            {
                config.OutputDirectoryPath = changeEvent.newValue;
                field.SetValueWithoutNotify(config.OutputDirectoryPath);
                AssetDatabase.SaveAssets();
                RefreshResolvedPathHelpBox(config, resolvedPathHelpBox);
            });
            return field;
        }

        private static void RefreshResolvedPathHelpBox(
            ScreenShooterConfig config,
            HelpBox resolvedPathHelpBox
        )
        {
            string configuredPath = config.OutputDirectoryPath;
            string resolvedPath = config.ResolvedOutputDirectoryPath;
            bool isAbsolutePath = System.IO.Path.IsPathRooted(configuredPath);

            resolvedPathHelpBox.text = isAbsolutePath
                ? $"Screenshots will be saved to the absolute directory: {resolvedPath}"
                : $"Screenshots will be saved relative to the project root: {resolvedPath}";
            resolvedPathHelpBox.messageType = HelpBoxMessageType.Info;
        }
    }
}