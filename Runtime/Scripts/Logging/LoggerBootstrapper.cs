using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;
using UnityEngine;

namespace IndieGabo.HandyTools.Logger
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the Logging module.
    /// </summary>
    public sealed class LoggerBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => LoggingModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            LoggingModuleDefinition.Dependencies;

        /// <inheritdoc />
#if UNITY_EDITOR || HANDY_DEBUG
        public void Bootstrap()
        {
            GameObject loggerOBJ = new("[HandyTools] Logger");
            loggerOBJ.AddComponent<HandyLogger>();
            Object.DontDestroyOnLoad(loggerOBJ);
        }
#else
        public void Bootstrap()
        {
        }
#endif
    }
}