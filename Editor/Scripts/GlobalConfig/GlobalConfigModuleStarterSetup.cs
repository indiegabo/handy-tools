#if UNITY_EDITOR
using IndieGabo.HandyTools.GlobalConfigModule;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor.GlobalConfigModule
{
    /// <summary>
    /// Performs the project-side starter setup flow for the GlobalConfig
    /// module.
    /// </summary>
    public static class GlobalConfigModuleStarterSetup
    {
        /// <summary>
        /// Creates the default globals.json file in Assets/Resources when it
        /// is not present yet.
        /// </summary>
        /// <returns>
        /// A user-facing status message describing the outcome.
        /// </returns>
        public static string Run()
        {
            if (GlobalsFileUtility.DoesGlobalsFileExist())
            {
                Globals.LoadFromGlobals();
                return "globals.json is already present in Assets/Resources.";
            }

            GlobalsFileUtility.EnsureGlobalsFileExists();
            AssetDatabase.Refresh();
            Globals.LoadFromGlobals();
            return "Created Assets/Resources/globals.json for the Globals module.";
        }
    }
}
#endif