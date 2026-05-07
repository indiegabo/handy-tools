using Unity.CodeEditor;
using UnityEditor;

namespace HandyTools.Editor.Utils
{
    /// <summary>
    /// Provides an editor entry point for synchronizing Unity-generated
    /// solution and project files.
    /// </summary>
    public static class HandyProjectFilesSyncUtility
    {
        #region Menu

        /// <summary>
        /// Regenerates the current external-code-editor project files and then
        /// refreshes the AssetDatabase.
        /// </summary>
        [MenuItem("HandyTools/Internal/Sync Project Files")]
        public static void SyncProjectFiles()
        {
            CodeEditor.Editor?.CurrentCodeEditor?.SyncAll();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        #endregion
    }
}