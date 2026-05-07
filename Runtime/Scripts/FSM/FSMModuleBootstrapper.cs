using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Registers the FSM module with the HandyTools kernel.
    /// </summary>
    public sealed class FSMModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => FSMModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            FSMModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
        }
    }
}