using System;
using System.Collections.Generic;
using System.IO;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Export
{
    /// <summary>
    /// Writes authored conversation tables to deterministic runtime JSON artifacts.
    /// </summary>
    public static class ConversationExporter
    {
        #region Constants

        private const string DefaultRootFolderName = "HandyTools/Conversations";
        private const string CatalogFileName = "catalog.json";
        private const string PayloadDirectoryName = "conversations";

        #endregion

        #region Public API

        /// <summary>
        /// Exports one authored table to the default Conversations StreamingAssets folder.
        /// </summary>
        /// <param name="table">Authored table that should be exported.</param>
        /// <returns>The export summary and generated catalog.</returns>
        public static ConversationExportResult Export(ConversationTable table)
        {
            return Export(new[] { table }, GetDefaultOutputRootPath());
        }

        /// <summary>
        /// Exports one authored table to the requested output folder.
        /// </summary>
        /// <param name="table">Authored table that should be exported.</param>
        /// <param name="outputRootPath">Absolute or project-relative output root.</param>
        /// <returns>The export summary and generated catalog.</returns>
        public static ConversationExportResult Export(
            ConversationTable table,
            string outputRootPath)
        {
            return Export(new[] { table }, outputRootPath);
        }

        /// <summary>
        /// Exports one set of authored tables to one shared runtime catalog and payload directory.
        /// </summary>
        /// <param name="tables">Authored tables that should contribute payloads.</param>
        /// <param name="outputRootPath">Absolute or project-relative output root.</param>
        /// <returns>The export summary and generated catalog.</returns>
        public static ConversationExportResult Export(
            IEnumerable<ConversationTable> tables,
            string outputRootPath)
        {
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            string resolvedOutputRootPath = ResolveOutputRootPath(outputRootPath);
            string payloadDirectoryPath = Path.Combine(
                resolvedOutputRootPath,
                PayloadDirectoryName);
            string catalogPath = Path.Combine(resolvedOutputRootPath, CatalogFileName);

            Directory.CreateDirectory(resolvedOutputRootPath);

            if (Directory.Exists(payloadDirectoryPath))
            {
                Directory.Delete(payloadDirectoryPath, recursive: true);
            }

            Directory.CreateDirectory(payloadDirectoryPath);

            List<ConversationRuntimeCatalog.Entry> catalogEntries = new();
            List<ConversationData> conversationDataSet = new();
            HashSet<SerializableGuid> exportedConversationIds = new();

            foreach (ConversationTable table in tables)
            {
                if (table == null)
                {
                    continue;
                }

                ConversationRuntimeCatalogBuilder.Build(
                    table,
                    out ConversationRuntimeCatalog tableCatalog,
                    out List<ConversationData> tableConversationDataSet,
                    PayloadDirectoryName);

                AppendExportData(
                    tableCatalog,
                    tableConversationDataSet,
                    catalogEntries,
                    conversationDataSet,
                    exportedConversationIds);
            }

            ConversationRuntimeCatalog catalog = new(1, catalogEntries);
            WriteCatalog(catalogPath, catalog);
            WritePayloads(resolvedOutputRootPath, conversationDataSet);
            RefreshProjectViewIfNeeded(resolvedOutputRootPath);

            return new ConversationExportResult(
                resolvedOutputRootPath,
                catalogPath,
                payloadDirectoryPath,
                catalog,
                conversationDataSet.Count);
        }

        /// <summary>
        /// Gets the default StreamingAssets export root used by the minimal exporter.
        /// </summary>
        /// <returns>The absolute default export root path.</returns>
        public static string GetDefaultOutputRootPath()
        {
            return Path.Combine(Application.streamingAssetsPath, DefaultRootFolderName);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Merges one table-local catalog and payload set into the shared export result.
        /// </summary>
        /// <param name="tableCatalog">Per-table runtime catalog.</param>
        /// <param name="tableConversationDataSet">Per-table runtime payloads.</param>
        /// <param name="catalogEntries">Merged shared catalog entries.</param>
        /// <param name="conversationDataSet">Merged shared runtime payloads.</param>
        /// <param name="exportedConversationIds">Set used to guard deterministic file collisions.</param>
        private static void AppendExportData(
            ConversationRuntimeCatalog tableCatalog,
            List<ConversationData> tableConversationDataSet,
            List<ConversationRuntimeCatalog.Entry> catalogEntries,
            List<ConversationData> conversationDataSet,
            HashSet<SerializableGuid> exportedConversationIds)
        {
            for (int index = 0; index < tableConversationDataSet.Count; index++)
            {
                ConversationData conversationData = tableConversationDataSet[index];

                if (!exportedConversationIds.Add(conversationData.ConversationId))
                {
                    throw new InvalidOperationException(
                        $"Conversation export cannot write duplicate conversation ids: "
                        + $"{conversationData.ConversationId}.");
                }

                conversationDataSet.Add(conversationData);
            }

            for (int index = 0; index < tableCatalog.Entries.Count; index++)
            {
                catalogEntries.Add(tableCatalog.Entries[index]);
            }
        }

        /// <summary>
        /// Writes the shared runtime catalog JSON file.
        /// </summary>
        /// <param name="catalogPath">Absolute catalog path.</param>
        /// <param name="catalog">Catalog payload to serialize.</param>
        private static void WriteCatalog(
            string catalogPath,
            ConversationRuntimeCatalog catalog)
        {
            File.WriteAllText(catalogPath, JsonUtility.ToJson(catalog, prettyPrint: true));
        }

        /// <summary>
        /// Writes the per-conversation runtime payload JSON files.
        /// </summary>
        /// <param name="outputRootPath">Absolute output root directory.</param>
        /// <param name="conversationDataSet">Payloads that should be serialized.</param>
        private static void WritePayloads(
            string outputRootPath,
            List<ConversationData> conversationDataSet)
        {
            for (int index = 0; index < conversationDataSet.Count; index++)
            {
                ConversationData conversationData = conversationDataSet[index];
                string payloadPath = Path.Combine(
                    outputRootPath,
                    PayloadDirectoryName,
                    $"{conversationData.ConversationId.ToHexString().ToLowerInvariant()}.json");

                File.WriteAllText(
                    payloadPath,
                    JsonUtility.ToJson(conversationData, prettyPrint: true));
            }
        }

        /// <summary>
        /// Resolves the output root to one absolute path with project-relative support.
        /// </summary>
        /// <param name="outputRootPath">Requested output root path.</param>
        /// <returns>The absolute export root path.</returns>
        private static string ResolveOutputRootPath(string outputRootPath)
        {
            if (string.IsNullOrWhiteSpace(outputRootPath))
            {
                return GetDefaultOutputRootPath();
            }

            string normalizedPath = outputRootPath.Trim();

            if (Path.IsPathRooted(normalizedPath))
            {
                return normalizedPath;
            }

            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName
                ?? Application.dataPath;

            return Path.GetFullPath(Path.Combine(projectRootPath, normalizedPath));
        }

        /// <summary>
        /// Refreshes the AssetDatabase when the export target is under the Unity project folder.
        /// </summary>
        /// <param name="resolvedOutputRootPath">Absolute export root path.</param>
        private static void RefreshProjectViewIfNeeded(string resolvedOutputRootPath)
        {
            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName
                ?? Application.dataPath;

            if (!resolvedOutputRootPath.StartsWith(
                    projectRootPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AssetDatabase.Refresh();
        }

        #endregion
    }

    /// <summary>
    /// Stores the output paths and summary metadata produced by one export operation.
    /// </summary>
    public sealed class ConversationExportResult
    {
        #region Constructor

        /// <summary>
        /// Initializes one export result.
        /// </summary>
        /// <param name="outputRootPath">Absolute export root path.</param>
        /// <param name="catalogPath">Absolute catalog file path.</param>
        /// <param name="payloadDirectoryPath">Absolute payload directory path.</param>
        /// <param name="catalog">Generated runtime catalog.</param>
        /// <param name="exportedConversationCount">Number of payloads written.</param>
        public ConversationExportResult(
            string outputRootPath,
            string catalogPath,
            string payloadDirectoryPath,
            ConversationRuntimeCatalog catalog,
            int exportedConversationCount)
        {
            OutputRootPath = outputRootPath ?? string.Empty;
            CatalogPath = catalogPath ?? string.Empty;
            PayloadDirectoryPath = payloadDirectoryPath ?? string.Empty;
            Catalog = catalog;
            ExportedConversationCount = exportedConversationCount;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the absolute export root path.
        /// </summary>
        public string OutputRootPath { get; }

        /// <summary>
        /// Gets the absolute catalog file path.
        /// </summary>
        public string CatalogPath { get; }

        /// <summary>
        /// Gets the absolute payload directory path.
        /// </summary>
        public string PayloadDirectoryPath { get; }

        /// <summary>
        /// Gets the generated runtime catalog.
        /// </summary>
        public ConversationRuntimeCatalog Catalog { get; }

        /// <summary>
        /// Gets the number of exported conversation payload files.
        /// </summary>
        public int ExportedConversationCount { get; }

        #endregion
    }
}