using System.Collections.Generic;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.HandyInputSystem;
using IndieGabo.HandyTools.Modules;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Input
{
    /// <summary>
    /// UI Toolkit configuration panel for the Input module.
    /// </summary>
    public sealed class InputModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => InputModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            InputModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override bool SupportsStarterSetup => true;

        /// <inheritdoc />
        protected override string StarterSetupDescription =>
            "Import the default Input starter assets into Assets/_Project/Input and assign the Player Manager prefab to this module configuration.";

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            _ = context;

            ProjectInputConfig.ReloadInstance();
            bool hadExistingConfig = ProjectInputConfig.TryGetExisting(
                out ProjectInputConfig config
            );
            if (!hadExistingConfig)
            {
                config = ProjectInputConfig.GetOrCreateForEditor();
            }

            int sanitizedPlayerCount = Mathf.Clamp(config.MaxNumberOfPlayers, 1, 8);
            if (sanitizedPlayerCount != config.MaxNumberOfPlayers)
            {
                config.MaxNumberOfPlayers = sanitizedPlayerCount;
                AssetDatabase.SaveAssets();
            }

            Label modeLabel = CreateModeLabel(sanitizedPlayerCount);
            HelpBox prefabHelpBox = new(string.Empty, HelpBoxMessageType.None);
            ApplyInformativeBoxStyle(prefabHelpBox);
            prefabHelpBox.style.marginTop = 6f;
            prefabHelpBox.style.marginBottom = 2f;
            ObjectField playerManagerField = CreatePlayerManagerField(config, prefabHelpBox);
            SliderInt playerCountField = CreatePlayerCountField(config, modeLabel);

            root.Add(CreateIntroLabel());
            if (!hadExistingConfig)
            {
                root.Add(CreateNewConfigHelpBox());
            }

            root.Add(playerManagerField);
            root.Add(playerCountField);
            root.Add(modeLabel);
            root.Add(prefabHelpBox);

            RegisterProjectInputConfigSync(
                root,
                playerManagerField,
                playerCountField,
                modeLabel,
                prefabHelpBox
            );
            RefreshPrefabState(config, prefabHelpBox);
        }

        /// <inheritdoc />
        protected override string RunStarterSetup(HandyModuleEditorContext context)
        {
            _ = context;
            return InputModuleStarterSetup.Run();
        }

        private static Label CreateIntroLabel()
        {
            Label label = new(
                "Configure the shared player manager prefab and the number of local players bootstrapped by the Input module."
            );
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8f;
            return label;
        }

        private static ObjectField CreatePlayerManagerField(
            ProjectInputConfig config,
            HelpBox prefabHelpBox
        )
        {
            ObjectField field = new("Player Manager Prefab")
            {
                objectType = typeof(PlayerManager),
                allowSceneObjects = false,
                value = config.PlayerManagerPrefab,
            };
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent =>
            {
                PlayerManager playerManager = ResolvePlayerManagerReference(
                    changeEvent.newValue
                );
                if (playerManager != changeEvent.newValue)
                {
                    field.SetValueWithoutNotify(playerManager);
                }

                config.PlayerManagerPrefab = playerManager;
                AssetDatabase.SaveAssets();
                RefreshPrefabState(config, prefabHelpBox);
            });
            return field;
        }

        private static void RegisterProjectInputConfigSync(
            VisualElement root,
            ObjectField playerManagerField,
            SliderInt playerCountField,
            Label modeLabel,
            HelpBox prefabHelpBox
        )
        {
            void HandleProjectInputConfigUpdated()
            {
                ProjectInputConfig.ReloadInstance();
                if (!ProjectInputConfig.TryGetExisting(out ProjectInputConfig config))
                {
                    return;
                }

                int sanitizedPlayerCount = Mathf.Clamp(config.MaxNumberOfPlayers, 1, 8);
                playerManagerField.SetValueWithoutNotify(config.PlayerManagerPrefab);
                playerCountField.SetValueWithoutNotify(sanitizedPlayerCount);
                RefreshModeLabel(modeLabel, sanitizedPlayerCount);
                RefreshPrefabState(config, prefabHelpBox);
            }

            void HandleDetachFromPanel(DetachFromPanelEvent _)
            {
                InputModuleStarterSetup.ProjectInputConfigUpdated -=
                    HandleProjectInputConfigUpdated;
                root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            }

            InputModuleStarterSetup.ProjectInputConfigUpdated +=
                HandleProjectInputConfigUpdated;
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }

        private static SliderInt CreatePlayerCountField(
            ProjectInputConfig config,
            Label modeLabel
        )
        {
            SliderInt field = new("Max Number Of Players", 1, 8);
            field.showInputField = true;
            field.SetValueWithoutNotify(Mathf.Clamp(config.MaxNumberOfPlayers, 1, 8));
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(changeEvent =>
            {
                int sanitizedValue = Mathf.Clamp(changeEvent.newValue, 1, 8);
                if (sanitizedValue != changeEvent.newValue)
                {
                    field.SetValueWithoutNotify(sanitizedValue);
                }

                config.MaxNumberOfPlayers = sanitizedValue;
                AssetDatabase.SaveAssets();
                RefreshModeLabel(modeLabel, sanitizedValue);
            });
            return field;
        }

        private static Label CreateModeLabel(int maxNumberOfPlayers)
        {
            Label label = new();
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 6f;
            RefreshModeLabel(label, maxNumberOfPlayers);
            return label;
        }

        private static void RefreshModeLabel(Label label, int maxNumberOfPlayers)
        {
            label.text = maxNumberOfPlayers <= 1
                ? "Mode: Single Player"
                : "Mode: Multiplayer";
        }

        private static PlayerManager ResolvePlayerManagerReference(Object candidate)
        {
            if (candidate is PlayerManager playerManager)
            {
                return playerManager;
            }

            if (candidate is GameObject gameObject)
            {
                return gameObject.GetComponent<PlayerManager>();
            }

            if (candidate is Component component)
            {
                return component.GetComponent<PlayerManager>();
            }

            return null;
        }

        private static void RefreshPrefabState(ProjectInputConfig config, HelpBox prefabHelpBox)
        {
            if (config.PlayerManagerPrefab == null)
            {
                prefabHelpBox.text =
                    "Assign a PlayerManager prefab before enabling runtime input bootstrapping.";
                prefabHelpBox.messageType = HelpBoxMessageType.Warning;
                return;
            }

            prefabHelpBox.text =
                "The assigned PlayerManager prefab will be instantiated by the Input module bootstrapper.";
            prefabHelpBox.messageType = HelpBoxMessageType.Info;
        }

        private static HelpBox CreateNewConfigHelpBox()
        {
            HelpBox helpBox = new(
                "ProjectInputConfig.asset did not exist yet, so the editor created it in Assets/Resources/HandyTools. You can configure the module manually now, or run Starter Setup to import the default Input starter assets.",
                HelpBoxMessageType.Info
            );
            ApplyInformativeBoxStyle(helpBox);
            return helpBox;
        }
    }
}