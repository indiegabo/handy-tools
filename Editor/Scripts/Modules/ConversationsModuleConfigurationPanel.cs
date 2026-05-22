using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.ConversationsModule.Loading;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Modules;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Exposes project-level Conversations configuration inside the shared HandyTools modules window.
    /// </summary>
    public sealed class ConversationsModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        #region Properties

        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => ConversationsModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            ConversationsModuleDefinition.Dependencies;

        #endregion

        #region Public API

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            _ = context;

            ConversationRuntimeSettings settings = ConversationRuntimeSettings.Instance;

            root.Add(CreateIntroLabel(
                "The Conversations module exports authored ConversationTable assets into lightweight runtime payloads and loads them through the selected runtime strategy."));
            root.Add(CreateInfoBox(
                "StreamingAssets and Addressables backends are available. Hybrid strategies retry the secondary backend when the primary one fails.",
                HelpBoxMessageType.Info));
            root.Add(CreateFallbackContinueActionField(settings));
            root.Add(CreateFallbackCancelActionField(settings));
            root.Add(CreateFallbackSkipActionField(settings));
            root.Add(CreateLoadingStrategyField(settings));
            root.Add(CreateCacheCapacityField(settings));
            root.Add(CreateStreamingRootField(settings));
            root.Add(CreateAlternateLocalizationRootField(settings));
            root.Add(CreateLocaleOverrideField(settings));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates the introductory label shown at the top of the module panel.
        /// </summary>
        /// <param name="text">Introductory text.</param>
        /// <returns>The created label.</returns>
        private static Label CreateIntroLabel(string text)
        {
            Label label = new(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 8f;
            return label;
        }

        /// <summary>
        /// Creates one styled help box used by the module panel.
        /// </summary>
        /// <param name="text">Help text.</param>
        /// <param name="messageType">Help-box message type.</param>
        /// <returns>The created help box.</returns>
        private static HelpBox CreateInfoBox(string text, HelpBoxMessageType messageType)
        {
            HelpBox helpBox = new(text, messageType);
            ApplyInformativeBoxStyle(helpBox);
            helpBox.style.marginBottom = 6f;
            return helpBox;
        }

        /// <summary>
        /// Creates the fallback continue-action selector.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created object field.</returns>
        private static ObjectField CreateFallbackContinueActionField(
            ConversationRuntimeSettings settings)
        {
            return CreateFallbackInputActionField(
                "Fallback Advance Action",
                "Used only when one ConversationTable leaves its own advance action empty.",
                settings.FallbackContinueAction,
                settings.SetFallbackContinueAction);
        }

        /// <summary>
        /// Creates the fallback cancel-action selector.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created object field.</returns>
        private static ObjectField CreateFallbackCancelActionField(
            ConversationRuntimeSettings settings)
        {
            return CreateFallbackInputActionField(
                "Fallback Cancel Action",
                "Used only when one ConversationTable leaves its own cancel action empty.",
                settings.FallbackCancelAction,
                settings.SetFallbackCancelAction);
        }

        /// <summary>
        /// Creates the fallback skip-action selector.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created object field.</returns>
        private static ObjectField CreateFallbackSkipActionField(
            ConversationRuntimeSettings settings)
        {
            return CreateFallbackInputActionField(
                "Fallback Skip Action",
                "Used only when one ConversationTable leaves its own skip action empty.",
                settings.FallbackSkipAction,
                settings.SetFallbackSkipAction);
        }

        /// <summary>
        /// Creates one fallback input-action selector.
        /// </summary>
        /// <param name="label">Field label.</param>
        /// <param name="tooltip">Field tooltip.</param>
        /// <param name="value">Current field value.</param>
        /// <param name="onChanged">Callback invoked after one value change.</param>
        /// <returns>The created object field.</returns>
        private static ObjectField CreateFallbackInputActionField(
            string label,
            string tooltip,
            InputActionReference value,
            Action<InputActionReference> onChanged)
        {
            ObjectField field = new(label)
            {
                objectType = typeof(InputActionReference),
                allowSceneObjects = false,
                value = value,
            };

            field.tooltip = tooltip;
            field.RegisterValueChangedCallback(evt =>
                onChanged(evt.newValue as InputActionReference));
            return field;
        }

        /// <summary>
        /// Creates the runtime loading-strategy selector.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created enum field.</returns>
        private static EnumField CreateLoadingStrategyField(ConversationRuntimeSettings settings)
        {
            EnumField field = new("Runtime Loading Strategy", settings.LoadingStrategy);
            field.tooltip =
                "Selects which backend the runtime loader should use for exported conversation payloads.";
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is ConversationLoadingStrategy loadingStrategy)
                {
                    settings.SetLoadingStrategy(loadingStrategy);
                }
            });

            return field;
        }

        /// <summary>
        /// Creates the cache-capacity field.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created integer field.</returns>
        private static IntegerField CreateCacheCapacityField(ConversationRuntimeSettings settings)
        {
            IntegerField field = new("Cache Capacity")
            {
                value = settings.CacheCapacity,
            };

            field.isDelayed = true;
            field.tooltip =
                "Maximum number of idle runtime payloads retained by the default Conversations cache.";
            field.RegisterValueChangedCallback(evt => settings.SetCacheCapacity(evt.newValue));
            return field;
        }

        /// <summary>
        /// Creates the optional StreamingAssets root override field.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created text field.</returns>
        private static TextField CreateStreamingRootField(ConversationRuntimeSettings settings)
        {
            TextField field = new("Streaming Root Override")
            {
                value = settings.StreamingAssetsRootOverride,
            };

            field.isDelayed = true;
            field.tooltip =
                "Optional absolute or StreamingAssets-relative override for the exported Conversations root folder.";
            field.RegisterValueChangedCallback(evt =>
                settings.SetStreamingAssetsRootOverride(evt.newValue));
            return field;
        }

        /// <summary>
        /// Creates the alternate-localization overlay root field.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created text field.</returns>
        private static TextField CreateAlternateLocalizationRootField(
            ConversationRuntimeSettings settings)
        {
            TextField field = new("Alternate Localization Root")
            {
                value = settings.AlternateLocalizationRootFolderName,
            };

            field.isDelayed = true;
            field.tooltip =
                "Optional StreamingAssets folder name used to load alternate conversation localizations, for example 'alternate-localizations'.";
            field.RegisterValueChangedCallback(evt =>
                settings.SetAlternateLocalizationRootFolderName(evt.newValue));
            return field;
        }

        /// <summary>
        /// Creates the optional locale-override field.
        /// </summary>
        /// <param name="settings">Project runtime settings asset.</param>
        /// <returns>The created text field.</returns>
        private static TextField CreateLocaleOverrideField(ConversationRuntimeSettings settings)
        {
            TextField field = new("Locale Override")
            {
                value = settings.LocaleOverride,
            };

            field.isDelayed = true;
            field.tooltip =
                "Optional locale override used by the alternate-localization overlay lookup. Leave empty to use CultureInfo.CurrentUICulture.";
            field.RegisterValueChangedCallback(evt => settings.SetLocaleOverride(evt.newValue));
            return field;
        }

        #endregion
    }
}