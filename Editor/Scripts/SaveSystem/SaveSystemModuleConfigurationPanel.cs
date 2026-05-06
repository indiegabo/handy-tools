using System.Collections.Generic;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Modules;
using IndieGabo.HandyTools.SaveSystemModule;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.SaveSystemModule
{
    /// <summary>
    /// UI Toolkit configuration panel for the SaveSystem module.
    /// </summary>
    public sealed class SaveSystemModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => SaveSystemModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            SaveSystemModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            SaveSystemConfig.ReloadInstance();
            SaveSystemConfig config = SaveSystemConfig.Instance;

            root.Add(CreateInfoLabel(
                "Configure automatic boot, slot strategy, storage behavior, and local save obfuscation settings."
            ));

            root.Add(CreateToggle(
                "Should Auto Boot",
                config.ShouldAutoBoot,
                value =>
                {
                    config.ShouldAutoBoot = value;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateEnumField(
                "Slot Strategy",
                config.SlotStrategy,
                value =>
                {
                    config.SlotStrategy = (SlotStrategy)value;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateIntegerField(
                "Max Indexed Slots",
                config.MaxIndexedSlots,
                value =>
                {
                    config.MaxIndexedSlots = value;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateToggle(
                "Ensure Indexed Slots",
                config.EnsureIndexedSlots,
                value =>
                {
                    config.EnsureIndexedSlots = value;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateTextField(
                "Save File Extension",
                config.SaveFileExtension,
                false,
                value =>
                {
                    config.SaveFileExtension = value;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateToggle(
                "Persist On Manager Destroy",
                config.PersistOnManagerDestroy,
                value =>
                {
                    config.PersistOnManagerDestroy = value;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateToggle(
                "Persist On Application Quit",
                config.PersistOnApplicationQuit,
                value =>
                {
                    config.PersistOnApplicationQuit = value;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateIntegerField(
                "Persistance Iteration Delta Factor",
                config.PersistanceIterationDeltaFactor,
                value =>
                {
                    config.PersistanceIterationDeltaFactor = value;
                    AssetDatabase.SaveAssets();
                }
            ));

            HelpBox encryptionHelpBox = new(
                string.Empty,
                HelpBoxMessageType.Info
            );
            ApplyInformativeBoxStyle(encryptionHelpBox);
            encryptionHelpBox.style.marginTop = 4f;
            encryptionHelpBox.style.marginBottom = 6f;

            TextField encryptionPasswordField = null;

            void RefreshEncryptionUi()
            {
                RefreshEncryptionControls(config, encryptionHelpBox, encryptionPasswordField);
            }

            EnumField encryptionModeField = CreateEnumField(
                "Encryption Mode",
                config.SaveEncryptionMode,
                value =>
                {
                    config.SaveEncryptionMode = (SaveEncryptionMode)value;
                    AssetDatabase.SaveAssets();
                    RefreshEncryptionUi();
                }
            );

            encryptionPasswordField = CreateTextField(
                "Encryption Password",
                config.SaveEncryptionPassword,
                true,
                value =>
                {
                    config.SaveEncryptionPassword = value;
                    AssetDatabase.SaveAssets();
                    RefreshEncryptionUi();
                }
            );

            root.Add(encryptionModeField);
            root.Add(encryptionHelpBox);
            root.Add(encryptionPasswordField);

            RefreshEncryptionUi();
        }

        private static Label CreateInfoLabel(string text)
        {
            Label label = new(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8f;
            return label;
        }

        private static Toggle CreateToggle(string label, bool value, System.Action<bool> onChanged)
        {
            Toggle field = new(label);
            field.SetValueWithoutNotify(value);
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent => onChanged(changeEvent.newValue));
            return field;
        }

        private static IntegerField CreateIntegerField(
            string label,
            int value,
            System.Action<int> onChanged
        )
        {
            IntegerField field = new(label)
            {
                value = value,
            };
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent => onChanged(changeEvent.newValue));
            return field;
        }

        private static TextField CreateTextField(
            string label,
            string value,
            bool isPassword,
            System.Action<string> onChanged
        )
        {
            TextField field = new(label)
            {
                value = value ?? string.Empty,
                isPasswordField = isPassword,
            };
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent => onChanged(changeEvent.newValue));
            return field;
        }

        private static EnumField CreateEnumField(
            string label,
            System.Enum value,
            System.Action<System.Enum> onChanged
        )
        {
            EnumField field = new(label, value);
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent => onChanged((System.Enum)changeEvent.newValue));
            return field;
        }

        private static void RefreshEncryptionControls(
            SaveSystemConfig config,
            HelpBox encryptionHelpBox,
            TextField encryptionPasswordField
        )
        {
            bool usesEncryption = config.UsesEncryption;
            encryptionPasswordField.style.display = usesEncryption
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!usesEncryption)
            {
                encryptionHelpBox.text =
                    "Encryption is disabled. Save files will be written without local obfuscation.";
                encryptionHelpBox.messageType = HelpBoxMessageType.Info;
                return;
            }

            if (string.IsNullOrWhiteSpace(config.SaveEncryptionPassword))
            {
                encryptionHelpBox.text =
                    "Provide a non-empty password before writing encrypted save files.";
                encryptionHelpBox.messageType = HelpBoxMessageType.Warning;
                return;
            }

            encryptionHelpBox.text =
                "This password is stored in the client configuration. Treat SaveSystem encryption as local obfuscation, not strong security.";
            encryptionHelpBox.messageType = HelpBoxMessageType.Info;
        }
    }
}