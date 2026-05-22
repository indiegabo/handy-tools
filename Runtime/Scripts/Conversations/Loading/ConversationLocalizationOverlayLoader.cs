using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Loads optional alternate-localization overlay payloads for conversation text ids.
    /// </summary>
    public static class ConversationLocalizationOverlayLoader
    {
        private const string ConversationsFolderName = "conversations";

        /// <summary>
        /// Loads the localized text map for one conversation and the active locale.
        /// </summary>
        /// <param name="settings">Runtime settings that define locale and overlay root options.</param>
        /// <param name="conversationId">Conversation whose overlay should be loaded.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The localized text lookup when an overlay exists; otherwise null.</returns>
        public static async Task<IReadOnlyDictionary<string, string>> LoadTextMapAsync(
            ConversationRuntimeSettings settings,
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default)
        {
            settings ??= ConversationRuntimeSettings.Instance;

            if (conversationId == SerializableGuid.Empty)
            {
                return null;
            }

            string overlayRootFolderName =
                settings.AlternateLocalizationRootFolderName?.Trim() ?? string.Empty;
            string activeLocale = ResolveActiveLocale(settings);

            if (string.IsNullOrWhiteSpace(overlayRootFolderName)
                || string.IsNullOrWhiteSpace(activeLocale))
            {
                return null;
            }

            string overlayPath = BuildOverlayPath(
                overlayRootFolderName,
                activeLocale,
                conversationId);

            try
            {
                string overlayJson = await ConversationStreamingJsonIO.ReadTextAsync(
                    overlayPath,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(overlayJson))
                {
                    return null;
                }

                ConversationLocalizationData overlayData =
                    JsonUtility.FromJson<ConversationLocalizationData>(overlayJson);

                if (overlayData == null)
                {
                    Debug.LogWarning(
                        $"Conversation localization overlay at '{overlayPath}' could not be deserialized.");
                    return null;
                }

                if (overlayData.ConversationId != SerializableGuid.Empty
                    && overlayData.ConversationId != conversationId)
                {
                    Debug.LogWarning(
                        $"Conversation localization overlay at '{overlayPath}' targets conversation '{overlayData.ConversationId}' instead of '{conversationId}'.");
                    return null;
                }

                return BuildLookup(overlayData);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Conversation localization overlay for '{conversationId}' failed to load: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolves the active locale that should be used for overlay lookup.
        /// </summary>
        /// <param name="settings">Runtime settings that may define one explicit locale override.</param>
        /// <returns>The resolved active locale.</returns>
        public static string ResolveActiveLocale(ConversationRuntimeSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings?.LocaleOverride))
            {
                return settings.LocaleOverride.Trim();
            }

            string currentLocale = CultureInfo.CurrentUICulture?.Name;

            if (!string.IsNullOrWhiteSpace(currentLocale))
            {
                return currentLocale;
            }

            return CultureInfo.CurrentCulture?.Name ?? string.Empty;
        }

        /// <summary>
        /// Builds the absolute path of one conversation localization overlay file.
        /// </summary>
        /// <param name="overlayRootFolderName">Configured root folder name for alternate localizations.</param>
        /// <param name="locale">Active locale that should be loaded.</param>
        /// <param name="conversationId">Conversation whose overlay should be loaded.</param>
        /// <returns>The absolute overlay file path.</returns>
        private static string BuildOverlayPath(
            string overlayRootFolderName,
            string locale,
            SerializableGuid conversationId)
        {
            string conversationFileName =
                $"{conversationId.ToHexString().ToLowerInvariant()}.json";
            string normalizedRootFolderName = overlayRootFolderName.Replace(
                '/',
                Path.DirectorySeparatorChar);

            return Path.Combine(
                Application.streamingAssetsPath,
                normalizedRootFolderName,
                locale,
                ConversationsFolderName,
                conversationFileName);
        }

        /// <summary>
        /// Builds one localized-text lookup from the overlay DTO.
        /// </summary>
        /// <param name="overlayData">Overlay DTO that should be indexed.</param>
        /// <returns>The indexed lookup when at least one valid entry exists; otherwise null.</returns>
        private static IReadOnlyDictionary<string, string> BuildLookup(
            ConversationLocalizationData overlayData)
        {
            if (overlayData?.Entries == null || overlayData.Entries.Count == 0)
            {
                return null;
            }

            Dictionary<string, string> lookup = new(StringComparer.Ordinal);

            for (int index = 0; index < overlayData.Entries.Count; index++)
            {
                ConversationLocalizedTextEntryData entry = overlayData.Entries[index];

                if (entry == null || string.IsNullOrWhiteSpace(entry.TextId))
                {
                    continue;
                }

                string textId = entry.TextId.Trim();

                if (!lookup.ContainsKey(textId))
                {
                    lookup.Add(textId, entry.Text ?? string.Empty);
                }
            }

            return lookup.Count > 0 ? lookup : null;
        }
    }
}