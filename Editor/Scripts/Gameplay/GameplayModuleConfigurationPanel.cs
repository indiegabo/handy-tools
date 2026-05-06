using System.Collections.Generic;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.GameplayModule;
using IndieGabo.HandyTools.Modules;
using IndieGabo.HandyTools.SaveSystemModule;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.GameplayModule
{
    /// <summary>
    /// UI Toolkit configuration panel for the Gameplay module.
    /// </summary>
    public sealed class GameplayModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => GameplayModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            GameplayModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            _ = context;

            GameplayConfig.ReloadInstance();
            GameplayConfig config = GameplayConfig.Instance;

            if (!IsSaveSystemModuleActive()
                && config.TimePersistenceStrategy
                    == GameplayTimePersistenceStrategy.SaveSystem)
            {
                config.TimePersistenceStrategy =
                    GameplayTimePersistenceStrategy.LocalUserData;
                AssetDatabase.SaveAssets();
            }

            HelpBox persistenceHelpBox = CreateInfoBox(string.Empty, HelpBoxMessageType.Info);

            root.Add(CreateIntroLabel(
                "The Gameplay module creates the global gameplay lifecycle service used to start, interrupt, resume, and stop the game flow."
            ));
            root.Add(CreateInfoBox(
                "When the module is active, HandyTools bootstraps GameplayService before scene logic starts consuming it.",
                HelpBoxMessageType.Info
            ));
            root.Add(CreatePersistenceStrategyDropdown(config, persistenceHelpBox));
            root.Add(persistenceHelpBox);
            root.Add(CreateInfoBox(
                "Interrupting gameplay is always indefinite after the freeze transition completes. The simulation only returns when some system or player explicitly resumes gameplay.",
                HelpBoxMessageType.Info
            ));
            root.Add(CreateInfoBox(
                "Gameplay manages its own time-scale transitions when starting, interrupting, resuming, or stopping the simulation.",
                HelpBoxMessageType.Info
            ));
            root.Add(CreateInfoBox(
                "If the persistence strategy is Save System and there is an active loaded slot, GameplayTimeRegisterer records elapsed gameplay time into that slot whenever gameplay pauses or stops. Otherwise the strategy falls back to local user data.",
                HelpBoxMessageType.None
            ));

            RefreshPersistenceHelpBox(config, persistenceHelpBox);
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
            ApplyInformativeBoxStyle(helpBox);
            helpBox.style.marginBottom = 6f;
            return helpBox;
        }

        private static IMGUIContainer CreatePersistenceStrategyDropdown(
            GameplayConfig config,
            HelpBox persistenceHelpBox
        )
        {
            IMGUIContainer container = new(() =>
            {
                bool isSaveSystemActive = IsSaveSystemModuleActive();

                Rect totalRect = EditorGUILayout.GetControlRect();
                const float labelWidth = 170f;
                const float fieldSpacing = 8f;

                Rect labelRect = totalRect;
                labelRect.width = Mathf.Min(labelWidth, totalRect.width * 0.4f);

                Rect buttonRect = totalRect;
                buttonRect.xMin = labelRect.xMax + fieldSpacing;

                EditorGUI.LabelField(labelRect, "Time Persistence Strategy");

                if (EditorGUI.DropdownButton(
                    buttonRect,
                    new GUIContent(
                        GetPersistenceStrategyDisplayName(
                            config.TimePersistenceStrategy
                        )
                    ),
                    FocusType.Keyboard,
                    EditorStyles.popup
                ))
                {
                    GenericMenu menu = new();
                    AddPersistenceStrategyItem(
                        menu,
                        config,
                        GameplayTimePersistenceStrategy.LocalUserData,
                        "Local User Data",
                        persistenceHelpBox
                    );

                    if (isSaveSystemActive)
                    {
                        AddPersistenceStrategyItem(
                            menu,
                            config,
                            GameplayTimePersistenceStrategy.SaveSystem,
                            "Save System",
                            persistenceHelpBox
                        );
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Save System"));
                    }

                    menu.DropDown(buttonRect);
                }
            });

            container.style.marginBottom = 6f;
            return container;
        }

        private static void AddPersistenceStrategyItem(
            GenericMenu menu,
            GameplayConfig config,
            GameplayTimePersistenceStrategy strategy,
            string label,
            HelpBox persistenceHelpBox
        )
        {
            menu.AddItem(
                new GUIContent(label),
                config.TimePersistenceStrategy == strategy,
                () =>
                {
                    config.TimePersistenceStrategy = strategy;
                    AssetDatabase.SaveAssets();
                    RefreshPersistenceHelpBox(config, persistenceHelpBox);
                }
            );
        }

        private static void RefreshPersistenceHelpBox(
            GameplayConfig config,
            HelpBox persistenceHelpBox
        )
        {
            bool isSaveSystemActive = IsSaveSystemModuleActive();
            GameplayTimePersistenceStrategy strategy = config.TimePersistenceStrategy;

            if (strategy == GameplayTimePersistenceStrategy.SaveSystem
                && isSaveSystemActive)
            {
                persistenceHelpBox.text =
                    "Gameplay time will be written into the currently loaded Save System slot. If no slot is loaded, no slot-backed gameplay time is persisted for that interruption.";
                persistenceHelpBox.messageType = HelpBoxMessageType.Info;
                return;
            }

            persistenceHelpBox.text = isSaveSystemActive
                ? "Gameplay time will be stored in local user data on this machine. This strategy is independent from Save System slots."
                : "Gameplay time will be stored in local user data on this machine. Save System persistence becomes available after the Save System module is activated.";
            persistenceHelpBox.messageType = HelpBoxMessageType.Info;
        }

        private static string GetPersistenceStrategyDisplayName(
            GameplayTimePersistenceStrategy strategy
        )
        {
            return strategy switch
            {
                GameplayTimePersistenceStrategy.SaveSystem => "Save System",
                _ => "Local User Data",
            };
        }

        private static bool IsSaveSystemModuleActive()
        {
            return HandyModuleSettings.Instance.IsModuleActive(
                SaveSystemModuleDefinition.Descriptor
            );
        }
    }
}