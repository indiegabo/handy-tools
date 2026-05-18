using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Modules;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    public sealed class CutscenesModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        public override HandyModuleDescriptor Descriptor => CutscenesModuleDefinition.Descriptor;

        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            CutscenesModuleDefinition.Dependencies;

        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            _ = context;

            bool isDialogueSystemAvailable = DialogueSystemIntegrationAvailability.IsAvailable();

            root.Add(CreateIntroLabel(
                "The Cutscenes module owns scene-authored cutscene graphs through CutsceneDirector components placed directly in the scene."));
            root.Add(CreateInfoBox(
                "Use the director inspector or the graph window to author nodes, connections, and runtime policies for one scene-local cutscene graph.",
                HelpBoxMessageType.Info));
            root.Add(CreateInfoBox(
                "Runtime state stays inside CutsceneRun and runtime state containers. Serialized node definitions remain authoring data only.",
                HelpBoxMessageType.Info));
            root.Add(CreateOptionalIntegrationBox(isDialogueSystemAvailable));
            root.Add(CreateOpenSelectedDirectorButton());
            root.Add(CreateInfoBox(
                "If no CutsceneDirector is currently selected, the graph window still opens and lets you bind one from the window toolbar.",
                HelpBoxMessageType.None));
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

        private static HelpBox CreateOptionalIntegrationBox(bool isDialogueSystemAvailable)
        {
            return CreateInfoBox(
                isDialogueSystemAvailable
                    ? "Optional Integrations: Dialogue System is available. Dialogue conversation nodes can be authored and executed through the Cutscenes bridge."
                    : "Optional Integrations: Dialogue System is not available. Base cutscene authoring remains fully usable, while dialogue node creation stays hidden until the dependency is installed.",
                isDialogueSystemAvailable
                    ? HelpBoxMessageType.Info
                    : HelpBoxMessageType.Warning);
        }

        private static Button CreateOpenSelectedDirectorButton()
        {
            Button button = new(() =>
            {
                CutsceneDirector director = CutsceneEditorUtility.GetSelectedDirector();
                CutsceneGraphWindow.Open(director);
            })
            {
                text = "Open Selected Director Graph",
            };

            button.style.alignSelf = Align.FlexStart;
            button.style.marginTop = 6f;
            button.style.marginBottom = 8f;
            button.style.height = 28f;
            return button;
        }
    }
}