using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Runtime bootstrapper for the Command Pattern module.
    /// </summary>
    public sealed class CommandPatternModuleBootstrapper :
        IHandyModuleBootstrapper
    {
        #region Properties

        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor =>
            CommandPatternModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            CommandPatternModuleDefinition.Dependencies;

        #endregion

        #region Public API

        /// <inheritdoc />
        public void Bootstrap()
        {
            CommandServiceBootstrapper.Bootstrap();
        }

        #endregion
    }
}