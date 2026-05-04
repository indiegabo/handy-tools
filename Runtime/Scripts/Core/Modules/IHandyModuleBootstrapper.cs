using System.Collections.Generic;

namespace IndieGabo.HandyTools.Modules
{
    /// <summary>
    /// Contract implemented by runtime modules that can be loaded by the
    /// HandyTools kernel.
    /// </summary>
    public interface IHandyModuleBootstrapper
    {
        /// <summary>
        /// Gets the metadata used by the kernel to classify and order the module.
        /// </summary>
        HandyModuleDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the current dependency status list for the module.
        /// </summary>
        IReadOnlyList<HandyModuleDependencyStatus> Dependencies { get; }

        /// <summary>
        /// Loads the module runtime services and resources.
        /// </summary>
        void Bootstrap();
    }
}