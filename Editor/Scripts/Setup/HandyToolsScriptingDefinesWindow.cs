using IndieGabo.HandyTools.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ProjectSetup
{
    /// <summary>
    /// General HandyTools configuration window for managed scripting defines.
    /// </summary>
    public sealed class HandyToolsScriptingDefinesWindow : EditorWindow
    {
        /// <summary>
        /// Opens the scripting define configuration window.
        /// </summary>
        [MenuItem(HandyToolsEditorMenuPaths.ScriptingDefines, false, 50)]
        private static void OpenWindow()
        {
            HandyToolsScriptingDefinesWindow window =
                GetWindow<HandyToolsScriptingDefinesWindow>();
            window.titleContent = new GUIContent("HandyTools Defines");
            window.minSize = new Vector2(460f, 360f);
            window.Show();
        }

        /// <summary>
        /// Rebuilds the UI Toolkit tree for the configuration window.
        /// </summary>
        public void CreateGUI()
        {
            RebuildUi();
        }

        /// <summary>
        /// Refreshes the window content whenever the window regains focus.
        /// </summary>
        public void OnFocus()
        {
            RebuildUi();
        }

        private void RebuildUi()
        {
            rootVisualElement.Clear();
            ApplyRootStyle(rootVisualElement);

            HandyScriptingDefineUtility.RemoveUnavailableDefines();

            rootVisualElement.Add(CreateHeaderLabel());
            rootVisualElement.Add(CreateTargetInfoBox());
            rootVisualElement.Add(CreateSummaryBox());
            rootVisualElement.Add(CreateDefinitionsList());
        }

        private static void ApplyRootStyle(VisualElement root)
        {
            root.style.paddingLeft = 12f;
            root.style.paddingRight = 12f;
            root.style.paddingTop = 12f;
            root.style.paddingBottom = 12f;
        }

        private static Label CreateHeaderLabel()
        {
            Label label = new(
                "Configure the HandyTools scripting defines that should be present in the selected build target."
            );
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 13f;
            label.style.marginBottom = 8f;
            return label;
        }

        private static HelpBox CreateTargetInfoBox()
        {
            return new HelpBox(
                $"Selected build target: {EditorUserBuildSettings.selectedBuildTargetGroup}",
                HelpBoxMessageType.Info
            );
        }

        private static HelpBox CreateSummaryBox()
        {
            HelpBox helpBox = new(
                "Unavailable defines are automatically removed on editor load so the project does not keep invalid compile states.",
                HelpBoxMessageType.None
            );
            helpBox.style.marginTop = 6f;
            helpBox.style.marginBottom = 10f;
            return helpBox;
        }

        private VisualElement CreateDefinitionsList()
        {
            ScrollView scrollView = new();
            scrollView.style.flexGrow = 1f;

            foreach (HandyScriptingDefineDefinition definition
                in HandyScriptingDefineRegistry.Definitions)
            {
                scrollView.Add(CreateDefinitionCard(definition));
            }

            return scrollView;
        }

        private VisualElement CreateDefinitionCard(
            HandyScriptingDefineDefinition definition
        )
        {
            VisualElement card = new();
            card.style.paddingLeft = 12f;
            card.style.paddingRight = 12f;
            card.style.paddingTop = 10f;
            card.style.paddingBottom = 10f;
            card.style.marginBottom = 10f;
            card.style.borderTopLeftRadius = 6f;
            card.style.borderTopRightRadius = 6f;
            card.style.borderBottomLeftRadius = 6f;
            card.style.borderBottomRightRadius = 6f;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;
            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftColor = new Color(0.24f, 0.24f, 0.24f);
            card.style.borderRightColor = new Color(0.24f, 0.24f, 0.24f);
            card.style.borderTopColor = new Color(0.24f, 0.24f, 0.24f);
            card.style.borderBottomColor = new Color(0.24f, 0.24f, 0.24f);
            card.style.backgroundColor = new Color(0.17f, 0.17f, 0.17f);

            bool isEnabled = HandyScriptingDefineUtility.IsEnabled(definition);
            bool isAvailable = definition.IsAvailable;

            Toggle toggle = new(definition.DisplayName);
            toggle.SetValueWithoutNotify(isEnabled);
            toggle.SetEnabled(isAvailable || isEnabled);
            toggle.style.marginBottom = 6f;
            toggle.RegisterValueChangedCallback(_ =>
            {
                HandyScriptingDefineUtility.SetEnabled(
                    definition,
                    toggle.value
                );
                RebuildUi();
            });
            card.Add(toggle);

            Label symbolLabel = new($"Symbol: {definition.Symbol}");
            symbolLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            symbolLabel.style.marginBottom = 4f;
            card.Add(symbolLabel);

            Label descriptionLabel = new(definition.Description);
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            descriptionLabel.style.marginBottom = 6f;
            card.Add(descriptionLabel);

            HelpBox stateBox = new(
                isEnabled
                    ? "This define is currently enabled for the selected build target."
                    : "This define is currently disabled for the selected build target.",
                isEnabled ? HelpBoxMessageType.Info : HelpBoxMessageType.None
            );
            stateBox.style.marginBottom = 6f;
            card.Add(stateBox);

            if (!isAvailable)
            {
                HelpBox unavailableBox = new(
                    definition.UnavailableReason,
                    HelpBoxMessageType.Warning
                );
                card.Add(unavailableBox);
            }

            return card;
        }
    }
}