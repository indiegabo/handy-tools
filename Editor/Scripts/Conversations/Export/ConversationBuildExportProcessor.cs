using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using IndieGabo.HandyTools.ConversationsModule;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Export
{
    /// <summary>
    /// Stages build-only conversation export artifacts before player builds and removes them afterward.
    /// </summary>
    public sealed class ConversationBuildExportProcessor :
        IPreprocessBuildWithReport,
        IPostprocessBuildWithReport
    {
        #region Properties

        /// <summary>
        /// Gets the callback order used by the build pipeline.
        /// </summary>
        public int callbackOrder => 0;

        #endregion

        #region Public API

        /// <summary>
        /// Rebuilds temporary conversation export artifacts before the player build consumes StreamingAssets content.
        /// </summary>
        /// <param name="report">Build report describing the outgoing build.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!ConversationsModuleDefinition.IsActive)
            {
                Debug.Log(
                    "[HandyTools][Conversations][Build] Conversations module is inactive. "
                    + "Skipping build export staging.");
                ConversationBuildReferenceDiscovery.CleanupBuildExportArtifacts();
                return;
            }

            if (!ConversationBuildReferenceDiscovery.PrepareBuildExportArtifacts())
            {
                throw new BuildFailedException(
                    "Could not prepare the Conversations runtime export before the build.");
            }
        }

        /// <summary>
        /// Removes temporary build-only conversation export artifacts after the player build completes.
        /// </summary>
        /// <param name="report">Build report describing the completed build.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            ConversationBuildReferenceDiscovery.CleanupBuildExportArtifacts();
        }

        #endregion
    }
}