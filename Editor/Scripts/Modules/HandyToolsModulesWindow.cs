using System;
using IndieGabo.HandyTools.Editor.DebuggingModule;
using IndieGabo.HandyTools.Editor.GameplayModule;
using IndieGabo.HandyTools.Editor.GlobalConfigModule;
using IndieGabo.HandyTools.Editor.InputModule;
using IndieGabo.HandyTools.Editor.LoggerModule;
using IndieGabo.HandyTools.Editor.SaveSystemModule;
using IndieGabo.HandyTools.Editor.ScreenShooterModule;
using IndieGabo.HandyTools.Editor.SteamModule;
using IndieGabo.HandyTools.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Modules
{
    /// <summary>
    /// Shared editor window that hosts all HandyTools module configuration
    /// panels in a single surface.
    /// </summary>
    public sealed class HandyToolsModulesWindow : EditorWindow
    {
        private const string _debuggingModuleId = "debugging";
        private const string _gameplayModuleId = "gameplay";
        private const string _globalConfigModuleId = "global-config";
        private const string _inputModuleId = "input";
        private const string _loggingModuleId = "logging";
        private const string _saveSystemModuleId = "save-system";
        private const string _screenShooterModuleId = "screen-shooter";
        private const string _steamModuleId = "steam";

        private static readonly ModulePanelRegistration[] _registrations =
        {
            new(_inputModuleId, "Input", new Vector2(900f, 560f), () => new InputModuleConfigurationPanel()),
            new(_gameplayModuleId, "Gameplay", new Vector2(860f, 520f), () => new GameplayModuleConfigurationPanel()),
            new(_saveSystemModuleId, "Save System", new Vector2(920f, 620f), () => new SaveSystemModuleConfigurationPanel()),
            new(_debuggingModuleId, "Debugging", new Vector2(900f, 560f), () => new DebuggingModuleConfigurationPanel()),
            new(_loggingModuleId, "Logging", new Vector2(860f, 520f), () => new LoggingModuleConfigurationPanel()),
            new(_globalConfigModuleId, "Globals", new Vector2(1040f, 680f), () => new GlobalConfigModuleConfigurationPanel()),
            new(_steamModuleId, "Steam", new Vector2(860f, 520f), () => new SteamModuleConfigurationPanel()),
            new(_screenShooterModuleId, "ScreenShooter", new Vector2(900f, 560f), () => new ScreenShooterModuleConfigurationPanel())
        };

        [SerializeField]
        private string _selectedModuleId = _inputModuleId;

        /// <summary>
        /// Opens the unified modules configuration window.
        /// </summary>
        [MenuItem(HandyToolsEditorMenuPaths.Modules, false, 1)]
        private static void OpenWindow()
        {
            HandyToolsModulesWindow window = GetWindow<HandyToolsModulesWindow>();
            window.Show();
        }

        /// <summary>
        /// Rebuilds the UI Toolkit tree for the modules window.
        /// </summary>
        public void CreateGUI()
        {
            EnsureSelection();
            RebuildUi();
        }

        /// <summary>
        /// Refreshes the current module panel when the shared window regains
        /// focus.
        /// </summary>
        public void OnFocus()
        {
            EnsureSelection();
            RebuildUi();
        }

        private static void OpenAndSelect(string moduleId)
        {
            HandyToolsModulesWindow window = GetWindow<HandyToolsModulesWindow>();
            window.SelectModule(moduleId);
            window.Show();
        }

        private void SelectModule(string moduleId)
        {
            ModulePanelRegistration registration = ResolveRegistration(moduleId);
            _selectedModuleId = registration.Id;
            ApplyWindowFrame(registration);

            if (rootVisualElement.panel != null)
            {
                RebuildUi();
            }
        }

        private void EnsureSelection()
        {
            ModulePanelRegistration registration = ResolveRegistration(_selectedModuleId);
            _selectedModuleId = registration.Id;
            ApplyWindowFrame(registration);
        }

        private void ApplyWindowFrame(ModulePanelRegistration registration)
        {
            titleContent = new GUIContent($"Modules - {registration.DisplayName}");
            minSize = registration.MinSize;
        }

        private void RebuildUi()
        {
            rootVisualElement.Clear();
            ApplyRootStyle(rootVisualElement);

            ModulePanelRegistration registration = ResolveRegistration(_selectedModuleId);

            rootVisualElement.Add(CreateHeader(registration));
            rootVisualElement.Add(CreateBody(registration));
        }

        private static void ApplyRootStyle(VisualElement root)
        {
            root.style.paddingLeft = 12f;
            root.style.paddingRight = 12f;
            root.style.paddingTop = 12f;
            root.style.paddingBottom = 12f;
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1f;
        }

        private static VisualElement CreateHeader(ModulePanelRegistration registration)
        {
            VisualElement container = new();
            container.style.marginBottom = 10f;

            Label title = new("HandyTools Module Configuration");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14f;
            title.style.marginBottom = 4f;
            container.Add(title);

            HelpBox helpBox = new(
                $"Use the left column to switch between modules. The active panel is currently {registration.DisplayName}.",
                HelpBoxMessageType.Info
            );
            container.Add(helpBox);

            return container;
        }

        private VisualElement CreateBody(ModulePanelRegistration registration)
        {
            VisualElement body = new();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.style.minHeight = 0f;

            body.Add(CreateSidebar());
            body.Add(CreateContentHost(registration));

            return body;
        }

        private VisualElement CreateSidebar()
        {
            ScrollView sidebar = new();
            sidebar.style.width = 180f;
            sidebar.style.marginRight = 12f;
            sidebar.style.flexShrink = 0f;
            sidebar.style.borderRightWidth = 1f;
            sidebar.style.borderRightColor = new Color(0.24f, 0.24f, 0.24f);
            sidebar.style.paddingRight = 10f;

            foreach (ModulePanelRegistration registration in _registrations)
            {
                Button button = new(() => SelectModule(registration.Id))
                {
                    text = registration.DisplayName
                };

                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.marginBottom = 6f;
                button.style.height = 28f;

                if (registration.Id == _selectedModuleId)
                {
                    button.style.unityFontStyleAndWeight = FontStyle.Bold;
                    button.style.backgroundColor = new Color(0.18f, 0.31f, 0.25f);
                }

                sidebar.Add(button);
            }

            return sidebar;
        }

        private static VisualElement CreateContentHost(ModulePanelRegistration registration)
        {
            ScrollView contentHost = new(ScrollViewMode.Vertical);
            contentHost.style.flexGrow = 1f;
            contentHost.style.minWidth = 320f;
            contentHost.style.minHeight = 0f;

            HandyModuleEditorContext context = new(HandyModuleSettings.Instance);
            IHandyModuleConfigurationPanel panel = registration.PanelFactory();
            VisualElement panelRoot = panel.CreatePanel(context);
            panelRoot.style.flexShrink = 0f;
            contentHost.Add(panelRoot);

            return contentHost;
        }

        private static ModulePanelRegistration ResolveRegistration(string moduleId)
        {
            if (!string.IsNullOrWhiteSpace(moduleId))
            {
                for (int index = 0; index < _registrations.Length; index++)
                {
                    if (string.Equals(
                        _registrations[index].Id,
                        moduleId,
                        StringComparison.Ordinal
                    ))
                    {
                        return _registrations[index];
                    }
                }
            }

            return _registrations[0];
        }

        private readonly struct ModulePanelRegistration
        {
            public ModulePanelRegistration(
                string id,
                string displayName,
                Vector2 minSize,
                Func<IHandyModuleConfigurationPanel> panelFactory
            )
            {
                Id = id;
                DisplayName = displayName;
                MinSize = minSize;
                PanelFactory = panelFactory;
            }

            public string Id { get; }

            public string DisplayName { get; }

            public Vector2 MinSize { get; }

            public Func<IHandyModuleConfigurationPanel> PanelFactory { get; }
        }
    }
}