using System.Collections.Generic;
using System.IO;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Modules;
using IndieGabo.HandyTools.Steam;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Steam
{
    /// <summary>
    /// UI Toolkit configuration panel for the Steam module.
    /// </summary>
    public sealed class SteamModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => SteamModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            SteamModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override bool SupportsStarterSetup => true;

        /// <inheritdoc />
        protected override string StarterSetupDescription =>
            "Create steam_appid.txt at the project root so local startup and debugging work outside the Steam client.";

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            root.Add(CreateIntroLabel(
                "The Steam module boots a persistent Steamworks.NET manager and runs Steam callbacks while the game is active."
            ));
            root.Add(CreateInfoBox(
                ResolveAppIdMessage(),
                ResolveAppIdMessageType()
            ));
            root.Add(CreateInfoBox(
                ResolveSteamAppIdFileMessage(),
                ResolveSteamAppIdFileMessageType()
            ));
            root.Add(CreateInfoBox(
                "When this module is active, HandyTools creates the SteamManager object before scene logic consumes Steamworks APIs.",
                HelpBoxMessageType.Info
            ));
        }

        /// <inheritdoc />
        protected override string RunStarterSetup(HandyModuleEditorContext context)
        {
            _ = context;
            return SteamModuleStarterSetup.Run();
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

        private static string ResolveAppIdMessage()
        {
            return HandySteamManager.ConfiguredAppId == "480"
                ? "The Steam module is still using App ID 480. Replace it with your real Steam App ID before shipping."
                : $"Configured Steam App ID: {HandySteamManager.ConfiguredAppId}.";
        }

        private static HelpBoxMessageType ResolveAppIdMessageType()
        {
            return HandySteamManager.ConfiguredAppId == "480"
                ? HelpBoxMessageType.Warning
                : HelpBoxMessageType.Info;
        }

        private static string ResolveSteamAppIdFileMessage()
        {
            return File.Exists(GetSteamAppIdFilePath())
                ? "steam_appid.txt is present in the project root for local startup and debugging flows."
                : "steam_appid.txt was not found in the project root. SteamAPI_Init may fail when launching outside the Steam client.";
        }

        private static HelpBoxMessageType ResolveSteamAppIdFileMessageType()
        {
            return File.Exists(GetSteamAppIdFilePath())
                ? HelpBoxMessageType.Info
                : HelpBoxMessageType.Warning;
        }

        private static string GetSteamAppIdFilePath()
        {
            return SteamModuleStarterSetup.GetSteamAppIdFilePath();
        }
    }
}