using UnityEngine;
using UnityEditor;
using System.IO;
using IndieGabo.HandyTools.LoggerModule;
using IndieGabo.HandyTools.Editor.InputModule;
using IndieGabo.HandyTools.Editor.SteamModule;

namespace IndieGabo.HandyTools.Editor.ProjectSetup
{
    /// <summary>
    /// Performs one-time HandyTools project setup tasks and keeps managed
    /// scripting defines in a valid state for the current project.
    /// </summary>
    [InitializeOnLoad]
    public static class HandyProjectSetup
    {
        static HandyProjectSetup()
        {
            HandyScriptingDefineUtility.SyncAvailabilityManagedDefines();

            string anchorFilePath = AnchorFilePath;
            if (File.Exists(anchorFilePath)) return;
            Setup();
        }

        /// <summary>
        /// Executes the default HandyTools project setup flow.
        /// </summary>
        [MenuItem("HandyTools/Complete Setup", false, 1000)]
        public static void Setup()
        {
            File.WriteAllText(AnchorFilePath, "");
            string steamStatusMessage = SteamModuleStarterSetup.Run();
            HandyLogger.Message(nameof(HandyProjectSetup), steamStatusMessage);
            HandyScriptingDefineUtility.ApplySetupDefaults();
            string statusMessage = InputModuleStarterSetup.Run(
                requestScriptReloadAfterCompletion: true
            );
            HandyLogger.Message(
                nameof(HandyProjectSetup),
                statusMessage
            );
        }

        /// <summary>
        /// Gets the anchor file that marks the project as already initialized.
        /// </summary>
        public static string AnchorFilePath =>
                Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", ".handy-anchor"
                ));
    }
}