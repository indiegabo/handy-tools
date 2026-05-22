using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Loading;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Export
{
    /// <summary>
    /// Discovers build-referenced conversation tables and stages temporary export artifacts for player builds.
    /// </summary>
    public static class ConversationBuildReferenceDiscovery
    {
        #region Constants

        /// <summary>
        /// Temporary root folder used to stage build-only conversation exports.
        /// </summary>
        public const string BuildExportRootFolderPath =
            "Assets/__HandyToolsGenerated/ConversationsBuild";

        /// <summary>
        /// Temporary backup root used to preserve any project-authored Conversations StreamingAssets content
        /// while build-only exports occupy the runtime folder.
        /// </summary>
        public const string BuildExportBackupRootFolderPath =
            BuildExportRootFolderPath + "/Backup";

        /// <summary>
        /// Project-relative output root consumed by the build as StreamingAssets content.
        /// </summary>
        public const string BuildExportOutputRootPath =
            "Assets/StreamingAssets/HandyTools/Conversations";

        /// <summary>
        /// Project-relative backup root used to restore pre-existing Conversations StreamingAssets content after builds.
        /// </summary>
        public const string BuildExportBackupOutputRootPath =
            BuildExportBackupRootFolderPath + "/HandyTools/Conversations";

        /// <summary>
        /// Project-relative output root consumed by Addressables as generated text-asset content.
        /// </summary>
        public const string BuildExportAddressablesOutputRootPath =
            BuildExportRootFolderPath + "/Addressables/HandyTools/Conversations";

        private const string BuildExportStateFilePath =
            BuildExportRootFolderPath + "/build-state.json";

        #endregion

        #region Public API

        /// <summary>
        /// Finds all conversation tables referenced by enabled build scenes through their serialized dependency graph.
        /// </summary>
        /// <returns>The referenced conversation tables sorted deterministically by asset path.</returns>
        public static IReadOnlyList<ConversationTable> FindReferencedTablesInEnabledBuildScenes()
        {
            List<ConversationTable> referencedTables = new();
            string[] enabledScenePaths = GetEnabledBuildScenePaths();

            if (enabledScenePaths.Length == 0)
            {
                return referencedTables;
            }

            string[] dependencyPaths = AssetDatabase.GetDependencies(enabledScenePaths, recursive: true);
            SortedDictionary<string, ConversationTable> uniqueTablesByPath = new(
                StringComparer.Ordinal);

            for (int index = 0; index < dependencyPaths.Length; index++)
            {
                string dependencyPath = dependencyPaths[index];

                if (string.IsNullOrWhiteSpace(dependencyPath)
                    || !dependencyPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ConversationTable table = AssetDatabase.LoadAssetAtPath<ConversationTable>(
                    dependencyPath);

                if (table == null)
                {
                    continue;
                }

                uniqueTablesByPath[dependencyPath] = table;
            }

            foreach (KeyValuePair<string, ConversationTable> pair in uniqueTablesByPath)
            {
                referencedTables.Add(pair.Value);
            }

            return referencedTables;
        }

        /// <summary>
        /// Stages temporary conversation export artifacts for the current build-scene set.
        /// </summary>
        /// <returns>True when the staging step completed successfully.</returns>
        public static bool PrepareBuildExportArtifacts()
        {
            CleanupBuildExportArtifacts();

            IReadOnlyList<ConversationTable> referencedTables =
                FindReferencedTablesInEnabledBuildScenes();

            Debug.Log(
                "[HandyTools][Conversations][Build] Discovered "
                + referencedTables.Count
                + " ConversationTable asset(s) referenced by enabled build scenes.");

            if (referencedTables.Count == 0)
            {
                Debug.Log(
                    "[HandyTools][Conversations][Build] No Conversations export artifacts were staged. "
                    + "Expected StreamingAssets root: '"
                    + BuildExportOutputRootPath
                    + "'.");
                return true;
            }

            bool hadExistingStreamingAssetsExport =
                AssetDatabase.IsValidFolder(BuildExportOutputRootPath);

            EnsureFolder(BuildExportRootFolderPath);
            WriteBuildExportState(hadExistingStreamingAssetsExport);

            if (hadExistingStreamingAssetsExport
                && !BackupExistingStreamingAssetsExport())
            {
                CleanupBuildExportArtifacts();
                return false;
            }

            ConversationExportResult result = ConversationExporter.Export(
                referencedTables,
                BuildExportOutputRootPath);

            if (result?.Catalog == null)
            {
                CleanupBuildExportArtifacts();
                return false;
            }

            Debug.Log(
                "[HandyTools][Conversations][Build] Staged "
                + result.ExportedConversationCount
                + " conversation payload(s) from "
                + referencedTables.Count
                + " ConversationTable asset(s). Output root: '"
                + result.OutputRootPath
                + "'. Catalog: '"
                + result.CatalogPath
                + "'. Payload directory: '"
                + result.PayloadDirectoryPath
                + "'.");

            if (!UsesAddressablesBackend(ConversationRuntimeSettings.Instance.LoadingStrategy))
            {
                return true;
            }

            if (ConversationAddressablesBuildSupport.PrepareBuildExportArtifacts(
                    referencedTables))
            {
                Debug.Log(
                    "[HandyTools][Conversations][Build] Addressables staging root: '"
                    + BuildExportAddressablesOutputRootPath
                    + "'.");
                return true;
            }

            CleanupBuildExportArtifacts();
            return false;
        }

        /// <summary>
        /// Removes any temporary build-only export artifacts left behind by a previous build preparation pass.
        /// </summary>
        public static void CleanupBuildExportArtifacts()
        {
            ConversationAddressablesBuildSupport.CleanupBuildExportArtifacts();

            BuildExportState state = ReadBuildExportState();

            if (state != null)
            {
                if (state.HadExistingStreamingAssetsExport)
                {
                    DeleteStagedStreamingAssetsExport();
                    RestoreBackedUpStreamingAssetsExport();
                }
                else
                {
                    DeleteStagedStreamingAssetsExport();
                }
            }

            if (AssetDatabase.IsValidFolder(BuildExportRootFolderPath))
            {
                AssetDatabase.DeleteAsset(BuildExportRootFolderPath);
            }

            CleanupEmptyStreamingAssetsParents();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Cleans stale staged exports when the editor domain reloads after an interrupted build.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void CleanupStaleBuildExportOnDomainLoad()
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            CleanupBuildExportArtifacts();
        }

        /// <summary>
        /// Resolves the enabled build-scene paths in deterministic order.
        /// </summary>
        /// <returns>The enabled build-scene paths.</returns>
        private static string[] GetEnabledBuildScenePaths()
        {
            List<string> enabledScenePaths = new();
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

            for (int index = 0; index < buildScenes.Length; index++)
            {
                EditorBuildSettingsScene buildScene = buildScenes[index];

                if (buildScene == null
                    || !buildScene.enabled
                    || string.IsNullOrWhiteSpace(buildScene.path)
                    || !File.Exists(buildScene.path))
                {
                    continue;
                }

                enabledScenePaths.Add(buildScene.path);
            }

            enabledScenePaths.Sort(StringComparer.Ordinal);
            return enabledScenePaths.ToArray();
        }

        /// <summary>
        /// Gets whether the configured runtime strategy requires generated Addressables artifacts.
        /// </summary>
        /// <param name="loadingStrategy">Runtime loading strategy.</param>
        /// <returns>True when the strategy includes an Addressables backend.</returns>
        private static bool UsesAddressablesBackend(
            ConversationLoadingStrategy loadingStrategy)
        {
            return loadingStrategy != ConversationLoadingStrategy.StreamingAssetsOnly;
        }

        /// <summary>
        /// Preserves an existing Conversations StreamingAssets export by moving it under the generated build root.
        /// </summary>
        /// <returns>True when the backup step completed successfully.</returns>
        private static bool BackupExistingStreamingAssetsExport()
        {
            if (!AssetDatabase.IsValidFolder(BuildExportOutputRootPath))
            {
                return true;
            }

            EnsureFolder(BuildExportBackupRootFolderPath);
            EnsureFolder(BuildExportBackupRootFolderPath + "/HandyTools");

            string error = AssetDatabase.MoveAsset(
                BuildExportOutputRootPath,
                BuildExportBackupOutputRootPath);

            if (string.IsNullOrEmpty(error))
            {
                return true;
            }

            Debug.LogError(
                "Could not back up the existing Conversations StreamingAssets export before the build: "
                + error);
            return false;
        }

        /// <summary>
        /// Restores the Conversations StreamingAssets export that existed before the build staging pass.
        /// </summary>
        /// <returns>True when the restore step completed successfully.</returns>
        private static bool RestoreBackedUpStreamingAssetsExport()
        {
            if (!AssetDatabase.IsValidFolder(BuildExportBackupOutputRootPath))
            {
                return true;
            }

            EnsureFolder("Assets/StreamingAssets");
            EnsureFolder("Assets/StreamingAssets/HandyTools");

            string error = AssetDatabase.MoveAsset(
                BuildExportBackupOutputRootPath,
                BuildExportOutputRootPath);

            if (string.IsNullOrEmpty(error))
            {
                return true;
            }

            Debug.LogError(
                "Could not restore the pre-build Conversations StreamingAssets export after the build: "
                + error);
            return false;
        }

        /// <summary>
        /// Deletes the currently staged Conversations StreamingAssets export when the build owns it.
        /// </summary>
        private static void DeleteStagedStreamingAssetsExport()
        {
            if (AssetDatabase.IsValidFolder(BuildExportOutputRootPath))
            {
                AssetDatabase.DeleteAsset(BuildExportOutputRootPath);
            }
        }

        /// <summary>
        /// Persists one small state file so stale cleanup can distinguish build-owned staging from user-authored exports.
        /// </summary>
        /// <param name="hadExistingStreamingAssetsExport">Whether the build replaced a pre-existing export folder.</param>
        private static void WriteBuildExportState(bool hadExistingStreamingAssetsExport)
        {
            string absolutePath = ToAbsoluteProjectPath(BuildExportStateFilePath);
            string parentDirectoryPath = Path.GetDirectoryName(absolutePath);

            if (!string.IsNullOrEmpty(parentDirectoryPath))
            {
                Directory.CreateDirectory(parentDirectoryPath);
            }

            BuildExportState state = new()
            {
                HadExistingStreamingAssetsExport = hadExistingStreamingAssetsExport,
            };

            File.WriteAllText(absolutePath, JsonUtility.ToJson(state, prettyPrint: false));
        }

        /// <summary>
        /// Reads the persisted build-export state file when one exists.
        /// </summary>
        /// <returns>The parsed state payload, or null when no state was persisted.</returns>
        private static BuildExportState ReadBuildExportState()
        {
            string absolutePath = ToAbsoluteProjectPath(BuildExportStateFilePath);

            if (!File.Exists(absolutePath))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<BuildExportState>(
                    File.ReadAllText(absolutePath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Could not read the Conversations build-export state file during cleanup. "
                    + exception.Message);
                return null;
            }
        }

        /// <summary>
        /// Resolves one project-relative asset path to an absolute filesystem path.
        /// </summary>
        /// <param name="projectRelativePath">Project-relative asset path.</param>
        /// <returns>The absolute filesystem path.</returns>
        private static string ToAbsoluteProjectPath(string projectRelativePath)
        {
            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName
                ?? Application.dataPath;

            return Path.GetFullPath(Path.Combine(projectRootPath, projectRelativePath));
        }

        /// <summary>
        /// Ensures one project folder exists, creating all missing parents recursively.
        /// </summary>
        /// <param name="folderPath">Project-relative folder path.</param>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int separatorIndex = folderPath.LastIndexOf('/');

            if (separatorIndex <= 0)
            {
                return;
            }

            string parentFolderPath = folderPath.Substring(0, separatorIndex);
            string folderName = folderPath[(separatorIndex + 1)..];

            EnsureFolder(parentFolderPath);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentFolderPath, folderName);
            }
        }

        /// <summary>
        /// Removes empty parent folders left behind after build-only StreamingAssets staging is cleaned up.
        /// </summary>
        private static void CleanupEmptyStreamingAssetsParents()
        {
            DeleteFolderIfEmpty("Assets/StreamingAssets/HandyTools");
            DeleteFolderIfEmpty("Assets/StreamingAssets");
            DeleteFolderIfEmpty("Assets/__HandyToolsGenerated");
        }

        /// <summary>
        /// Deletes the provided folder only when it has no subfolders and no non-meta files.
        /// </summary>
        /// <param name="folderPath">Project-relative folder path.</param>
        private static void DeleteFolderIfEmpty(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            if (AssetDatabase.GetSubFolders(folderPath).Length > 0)
            {
                return;
            }

            string absoluteFolderPath = ToAbsoluteProjectPath(folderPath);

            if (Directory.Exists(absoluteFolderPath))
            {
                string[] files = Directory.GetFiles(absoluteFolderPath, "*", SearchOption.TopDirectoryOnly);

                for (int index = 0; index < files.Length; index++)
                {
                    if (!files[index].EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            AssetDatabase.DeleteAsset(folderPath);
        }

        #endregion

        #region Nested Types

        [Serializable]
        private sealed class BuildExportState
        {
            public bool HadExistingStreamingAssetsExport;
        }

        #endregion
    }

    /// <summary>
    /// Stages generated conversation payloads as Addressables-managed text assets without taking a hard
    /// compile-time dependency on the Addressables editor assembly.
    /// </summary>
    internal static class ConversationAddressablesBuildSupport
    {
        #region Constants

        private const string ManagedGroupName = "HandyTools Conversations Generated";

        private const string CatalogAddress = "conversations/catalog";

        #endregion

        #region Static Fields

        private static readonly Type AddressableAssetSettingsDefaultObjectType = Type.GetType(
            "UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");

        private static readonly Type AddressableAssetSettingsType = Type.GetType(
            "UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, Unity.Addressables.Editor");

        private static readonly Type AddressableAssetGroupType = Type.GetType(
            "UnityEditor.AddressableAssets.Settings.AddressableAssetGroup, Unity.Addressables.Editor");

        private static readonly Type AddressableAssetGroupSchemaType = Type.GetType(
            "UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema, Unity.Addressables.Editor");

        private static readonly Type ContentUpdateGroupSchemaType = Type.GetType(
            "UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema, Unity.Addressables.Editor");

        private static readonly Type BundledAssetGroupSchemaType = Type.GetType(
            "UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema, Unity.Addressables.Editor");

        #endregion

        #region Public API

        /// <summary>
        /// Exports generated Conversations JSON assets into one managed Addressables group.
        /// </summary>
        /// <param name="referencedTables">Conversation tables that should be exported.</param>
        /// <returns>True when the Addressables staging step completed successfully.</returns>
        public static bool PrepareBuildExportArtifacts(
            IReadOnlyList<ConversationTable> referencedTables)
        {
            EnsureAvailable();

            if (referencedTables == null || referencedTables.Count == 0)
            {
                return true;
            }

            ConversationExportResult result = ConversationExporter.Export(
                referencedTables,
                ConversationBuildReferenceDiscovery.BuildExportAddressablesOutputRootPath);

            if (result?.Catalog == null)
            {
                return false;
            }

            object settings = GetSettings(create: true);
            object group = GetOrCreateManagedGroup(settings);

            RegisterAsset(
                settings,
                group,
                ToProjectAssetPath(result.CatalogPath),
                CatalogAddress);

            for (int index = 0; index < result.Catalog.Entries.Count; index++)
            {
                var catalogEntry = result.Catalog.Entries[index];

                if (catalogEntry == null || string.IsNullOrWhiteSpace(catalogEntry.PayloadKey))
                {
                    continue;
                }

                string payloadAbsolutePath = Path.Combine(
                    result.OutputRootPath,
                    catalogEntry.PayloadPath.Replace('/', Path.DirectorySeparatorChar));

                RegisterAsset(
                    settings,
                    group,
                    ToProjectAssetPath(payloadAbsolutePath),
                    catalogEntry.PayloadKey);
            }

            SaveSettings(settings);
            return true;
        }

        /// <summary>
        /// Removes all managed Addressables entries created for build-staged Conversations exports.
        /// </summary>
        public static void CleanupBuildExportArtifacts()
        {
            if (!IsAvailable)
            {
                return;
            }

            object settings = GetSettings(create: false);

            if (settings == null)
            {
                return;
            }

            object group = FindManagedGroup(settings);

            if (group == null)
            {
                return;
            }

            List<string> entryGuids = GetGroupEntryGuids(group);
            bool removedAnyEntry = false;

            if (entryGuids.Count == 0)
            {
                return;
            }

            for (int index = 0; index < entryGuids.Count; index++)
            {
                removedAnyEntry |= RemoveAssetEntry(settings, entryGuids[index]);
            }

            if (removedAnyEntry)
            {
                SaveSettings(settings);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets whether the reflected Addressables editor APIs are available in the current editor domain.
        /// </summary>
        private static bool IsAvailable => AddressableAssetSettingsDefaultObjectType != null
            && AddressableAssetSettingsType != null
            && AddressableAssetGroupType != null
            && AddressableAssetGroupSchemaType != null
            && ContentUpdateGroupSchemaType != null
            && BundledAssetGroupSchemaType != null;

        /// <summary>
        /// Throws one explicit exception when the Addressables editor APIs are unavailable.
        /// </summary>
        private static void EnsureAvailable()
        {
            if (IsAvailable)
            {
                return;
            }

            throw new InvalidOperationException(
                "Conversation Addressables build staging requires the Unity Addressables editor package to be available.");
        }

        /// <summary>
        /// Gets the project default Addressables settings object through reflection.
        /// </summary>
        /// <param name="create">Whether the settings asset should be created when missing.</param>
        /// <returns>The reflected Addressables settings object when available.</returns>
        private static object GetSettings(bool create)
        {
            var getSettingsMethod = AddressableAssetSettingsDefaultObjectType?.GetMethod(
                "GetSettings",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            return getSettingsMethod?.Invoke(null, new object[] { create });
        }

        /// <summary>
        /// Finds the dedicated managed group used for generated Conversations payload assets.
        /// </summary>
        /// <param name="settings">Reflected Addressables settings object.</param>
        /// <returns>The reflected managed group when present.</returns>
        private static object FindManagedGroup(object settings)
        {
            var findGroupMethod = AddressableAssetSettingsType?.GetMethod(
                "FindGroup",
                new[] { typeof(string) });

            return findGroupMethod?.Invoke(settings, new object[] { ManagedGroupName });
        }

        /// <summary>
        /// Finds the managed group or creates it when it does not exist yet.
        /// </summary>
        /// <param name="settings">Reflected Addressables settings object.</param>
        /// <returns>The reflected managed group.</returns>
        private static object GetOrCreateManagedGroup(object settings)
        {
            object existingGroup = FindManagedGroup(settings);

            if (existingGroup != null)
            {
                return existingGroup;
            }

            Type groupSchemaListType = typeof(List<>).MakeGenericType(
                AddressableAssetGroupSchemaType);
            var createGroupMethod = AddressableAssetSettingsType?.GetMethod(
                "CreateGroup",
                new[]
                {
                    typeof(string),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    groupSchemaListType,
                    typeof(Type[]),
                });

            return createGroupMethod?.Invoke(
                settings,
                new object[]
                {
                    ManagedGroupName,
                    false,
                    false,
                    false,
                    null,
                    new[]
                    {
                        ContentUpdateGroupSchemaType,
                        BundledAssetGroupSchemaType,
                    },
                });
        }

        /// <summary>
        /// Creates or moves one generated asset entry into the managed group and assigns its address.
        /// </summary>
        /// <param name="settings">Reflected Addressables settings object.</param>
        /// <param name="group">Reflected managed group.</param>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <param name="address">Address assigned to the entry.</param>
        private static void RegisterAsset(
            object settings,
            object group,
            string assetPath,
            string address)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrWhiteSpace(guid))
            {
                throw new InvalidOperationException(
                    $"Could not resolve an Addressables guid for generated asset '{assetPath}'.");
            }

            var createOrMoveEntryMethod = AddressableAssetSettingsType?.GetMethod(
                "CreateOrMoveEntry",
                new[]
                {
                    typeof(string),
                    AddressableAssetGroupType,
                    typeof(bool),
                    typeof(bool),
                });

            object entry = createOrMoveEntryMethod?.Invoke(
                settings,
                new object[]
                {
                    guid,
                    group,
                    false,
                    false,
                });

            if (entry == null)
            {
                throw new InvalidOperationException(
                    $"Could not create one Addressables entry for generated asset '{assetPath}'.");
            }

            entry.GetType().GetProperty("address")?.SetValue(entry, address);
        }

        /// <summary>
        /// Enumerates the guids of every entry in the managed Addressables group.
        /// </summary>
        /// <param name="group">Reflected managed group.</param>
        /// <returns>The entry guids.</returns>
        private static List<string> GetGroupEntryGuids(object group)
        {
            List<string> entryGuids = new();
            var entriesProperty = AddressableAssetGroupType?.GetProperty("entries");
            IEnumerable entries = entriesProperty?.GetValue(group) as IEnumerable;

            if (entries == null)
            {
                return entryGuids;
            }

            foreach (object entry in entries)
            {
                string guid = entry?.GetType().GetProperty("guid")?.GetValue(entry) as string;

                if (!string.IsNullOrWhiteSpace(guid))
                {
                    entryGuids.Add(guid);
                }
            }

            return entryGuids;
        }

        /// <summary>
        /// Removes one asset entry from the Addressables settings by guid.
        /// </summary>
        /// <param name="settings">Reflected Addressables settings object.</param>
        /// <param name="guid">Entry guid that should be removed.</param>
        private static bool RemoveAssetEntry(object settings, string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return false;
            }

            var removeAssetEntryMethod = AddressableAssetSettingsType?.GetMethod(
                "RemoveAssetEntry",
                new[] { typeof(string), typeof(bool) });

            object result = removeAssetEntryMethod?.Invoke(settings, new object[] { guid, false });
            return result is bool removed
                ? removed
                : removeAssetEntryMethod != null;
        }

        /// <summary>
        /// Converts one generated absolute path under the Unity project root into one project-relative asset path.
        /// </summary>
        /// <param name="absolutePath">Absolute path to convert.</param>
        /// <returns>The project-relative asset path.</returns>
        private static string ToProjectAssetPath(string absolutePath)
        {
            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName
                ?? Application.dataPath;
            string normalizedProjectRootPath = projectRootPath.Replace('\\', '/');
            string normalizedAbsolutePath = Path.GetFullPath(absolutePath).Replace('\\', '/');

            if (!normalizedAbsolutePath.StartsWith(
                    normalizedProjectRootPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Generated Addressables asset path '{absolutePath}' is outside the Unity project root.");
            }

            return normalizedAbsolutePath[(normalizedProjectRootPath.Length + 1)..];
        }

        /// <summary>
        /// Marks the Addressables settings asset dirty and saves pending asset changes.
        /// </summary>
        /// <param name="settings">Reflected Addressables settings object.</param>
        private static void SaveSettings(object settings)
        {
            if (settings is UnityEngine.Object settingsObject)
            {
                EditorUtility.SetDirty(settingsObject);
            }

            AssetDatabase.SaveAssets();
        }

        #endregion
    }
}