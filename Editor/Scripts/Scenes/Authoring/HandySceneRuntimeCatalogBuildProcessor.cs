using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace IndieGabo.HandyTools.Editor.Scenes.Authoring
{
    /// <summary>
    /// Ensures the generated HandyScene runtime catalog is refreshed before a
    /// player build starts so unloaded HandyScene metadata is available in
    /// builds.
    /// </summary>
    public sealed class HandySceneRuntimeCatalogBuildProcessor :
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
        /// Rebuilds the HandyScene runtime catalog before the player build
        /// consumes Resources assets.
        /// </summary>
        /// <param name="report">Build report describing the outgoing build.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!HandySceneRuntimeCatalogBuilder.PrepareBuildCatalogAsset())
            {
                throw new BuildFailedException(
                    "Could not prepare the HandyScene runtime catalog before the build.");
            }
        }

        /// <summary>
        /// Removes the temporary build-only runtime catalog once the player
        /// build has finished consuming the generated Resources asset.
        /// </summary>
        /// <param name="report">Build report describing the completed build.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            HandySceneRuntimeCatalogBuilder.CleanupBuildCatalogAsset(logResult: true);
        }

        #endregion
    }
}