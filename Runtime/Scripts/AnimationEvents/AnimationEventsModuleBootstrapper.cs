using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Runtime bootstrapper for the Animation Events module.
    /// </summary>
    public sealed class AnimationEventsModuleBootstrapper :
        IHandyModuleBootstrapper
    {
        #region Properties

        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor =>
            AnimationEventsModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            AnimationEventsModuleDefinition.Dependencies;

        #endregion

        #region Public API

        /// <inheritdoc />
        public void Bootstrap()
        {
            AnimatorBusEventRegistry.Refresh();
        }

        #endregion
    }
}