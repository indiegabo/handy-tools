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

            SerializedObject serializedConfig = new(config);
            HelpBox prefabHelpBox = new(string.Empty, HelpBoxMessageType.None);
            ApplyInformativeBoxStyle(prefabHelpBox);
            prefabHelpBox.style.marginTop = 6f;
            prefabHelpBox.style.marginBottom = 2f;

            root.Add(CreateIntroLabel());
            if (!hadExistingConfig)
            {
                root.Add(CreateNewConfigHelpBox());
            }

            VisualElement templateRoot = CreateConfigFieldsRoot(
                config,
                serializedConfig,
                prefabHelpBox,
                sanitizedPlayerCount,
                out IMGUIContainer playerManagerField,
                out SliderInt playerCountField,
                out Label modeLabel
            );

            root.Add(templateRoot);
            root.Add(prefabHelpBox);

            if (playerManagerField == null
                || playerCountField == null
                || modeLabel == null)
            {
                return;
            }

            RegisterProjectInputConfigSync(
                root,
                serializedConfig,
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

        private static VisualElement CreateConfigFieldsRoot(
            ProjectInputConfig config,
            SerializedObject serializedConfig,
            HelpBox prefabHelpBox,
            int sanitizedPlayerCount,
            out IMGUIContainer playerManagerField,
            out SliderInt playerCountField,
            out Label modeLabel
        )
        {
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(
                "UI Toolkit/Input/ProjectInputConfigWindow_Template"
            );

            if (visualTreeAsset == null)
            {
                playerManagerField = null;
                playerCountField = null;
                modeLabel = null;

                HelpBox helpBox = new(
                    "The Input UI template could not be loaded from Resources/UI Toolkit/Input/ProjectInputConfigWindow_Template.",
                    HelpBoxMessageType.Error
                );
                ApplyInformativeBoxStyle(helpBox);
                return helpBox;
            }

            VisualElement templateRoot = new();
            visualTreeAsset.CloneTree(templateRoot);

            VisualElement playerManagerContainer = templateRoot.Q<VisualElement>(
                "container-player-manager"
            );
            VisualElement playerManagerFieldHost = templateRoot.Q<VisualElement>(
                "field-player-manager-prefab-host"
            );
            VisualElement playerCountContainer = templateRoot.Q<VisualElement>(
                "container-number-of-players"
            );
            playerCountField = templateRoot.Q<SliderInt>(
                "field-max-number-of-players"
            );
            modeLabel = templateRoot.Q<Label>("label-type-hint");

            if (playerManagerContainer == null
                || playerManagerFieldHost == null
                || playerCountContainer == null
                || playerCountField == null
                || modeLabel == null)
            {
                playerManagerField = null;
                playerCountField = null;
                modeLabel = null;

                HelpBox helpBox = new(
                    "The Input UI template is missing one or more required fields.",
                    HelpBoxMessageType.Error
                );
                ApplyInformativeBoxStyle(helpBox);
                return helpBox;
            }

            HandyModuleConfigurationPanelBase.ApplyConfigurableValueContainerStyle(
                playerManagerContainer
            );
            HandyModuleConfigurationPanelBase.ApplyConfigurableValueContainerStyle(
                playerCountContainer
            );

            SerializedProperty playerManagerProperty = serializedConfig.FindProperty(
                "_playerManagerPrefab"
            );

            if (playerManagerProperty == null)
            {
                playerManagerField = null;
                playerCountField = null;
                modeLabel = null;

                HelpBox helpBox = new(
                    "The Input config is missing the serialized PlayerManager property.",
                    HelpBoxMessageType.Error
                );
                ApplyInformativeBoxStyle(helpBox);
                return helpBox;
            }

            playerManagerField = new IMGUIContainer(() =>
            {
                serializedConfig.Update();

                PlayerManager currentPlayerManager =
                    playerManagerProperty.objectReferenceValue as PlayerManager;

                EditorGUI.BeginChangeCheck();
                PlayerManager selectedPlayerManager = EditorGUILayout.ObjectField(
                    GUIContent.none,
                    currentPlayerManager,
                    typeof(PlayerManager),
                    false
                ) as PlayerManager;

                if (!EditorGUI.EndChangeCheck())
                {
                    return;
                }

                playerManagerProperty.objectReferenceValue = selectedPlayerManager;
                serializedConfig.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                RefreshPrefabState(
                    serializedConfig.targetObject as ProjectInputConfig,
                    prefabHelpBox
                );
            });
            playerManagerField.style.marginBottom = 0f;
            playerManagerFieldHost.Add(playerManagerField);

            SliderInt resolvedPlayerCountField = playerCountField;
            Label resolvedModeLabel = modeLabel;

            playerCountField.SetValueWithoutNotify(sanitizedPlayerCount);
            playerCountField.RegisterValueChangedCallback(changeEvent =>
            {
                int sanitizedValue = Mathf.Clamp(changeEvent.newValue, 1, 8);
                if (sanitizedValue != changeEvent.newValue)
                {
                    resolvedPlayerCountField.SetValueWithoutNotify(sanitizedValue);
                }

                config.MaxNumberOfPlayers = sanitizedValue;
                AssetDatabase.SaveAssets();
                RefreshModeLabel(resolvedModeLabel, sanitizedValue);
            });

            RefreshModeLabel(resolvedModeLabel, sanitizedPlayerCount);
            return templateRoot;
        }

        private static void RegisterProjectInputConfigSync(
            VisualElement root,
            SerializedObject serializedConfig,
            IMGUIContainer playerManagerField,
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
                serializedConfig.Update();
                playerManagerField.MarkDirtyRepaint();
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

        private static void RefreshModeLabel(Label label, int maxNumberOfPlayers)
        {
            label.text = maxNumberOfPlayers <= 1
                ? "Mode: Single Player"
                : "Mode: Multiplayer";
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