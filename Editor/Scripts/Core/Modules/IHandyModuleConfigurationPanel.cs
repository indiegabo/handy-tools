using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Modules
{
    /// <summary>
    /// Defines a UI Toolkit configuration panel for one HandyTools module.
    /// </summary>
    public interface IHandyModuleConfigurationPanel
    {
        /// <summary>
        /// Gets the runtime descriptor represented by the editor panel.
        /// </summary>
        HandyModuleDescriptor Descriptor { get; }

        /// <summary>
        /// Gets dependency statuses used by the shared dependency gate.
        /// </summary>
        IReadOnlyList<HandyModuleDependencyStatus> Dependencies { get; }

        /// <summary>
        /// Creates the module configuration UI.
        /// </summary>
        /// <param name="context">Shared module editor context.</param>
        /// <returns>Root visual element for the module configuration panel.</returns>
        VisualElement CreatePanel(HandyModuleEditorContext context);
    }
}