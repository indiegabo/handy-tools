using System.Collections.Generic;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Gameplay;
using IndieGabo.HandyTools.Modules;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Gameplay
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
            root.Add(CreateIntroLabel(
                "The Gameplay module creates the global gameplay lifecycle service used to start, pause, resume, and stop the game flow."
            ));
            root.Add(CreateInfoBox(
                "No additional editor configuration is required. When the module is active, HandyTools bootstraps GameplayService before scene logic starts consuming it.",
                HelpBoxMessageType.Info
            ));
            root.Add(CreateInfoBox(
                "Gameplay manages its own time-scale transitions when starting, pausing, resuming, or stopping the simulation.",
                HelpBoxMessageType.Info
            ));
            root.Add(CreateInfoBox(
                "If SaveSystem has an active loaded slot, GameplayTimeRegisterer records elapsed gameplay time whenever gameplay pauses or stops.",
                HelpBoxMessageType.None
            ));
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
    }
}