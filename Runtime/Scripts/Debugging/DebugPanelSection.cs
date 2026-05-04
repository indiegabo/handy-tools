using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Debugging
{
    /// <summary>
    /// Base contract for all debug panel sections.
    /// Sections are instantiated by the panel bootstrapper and contribute a
    /// visual element to the panel body.
    /// </summary>
    public abstract class DebugPanelSection : MonoBehaviour
    {
        /// <summary>
        /// Gets the runtime panel host that owns the section.
        /// </summary>
        protected DebugPanel Panel { get; private set; }

        /// <summary>
        /// Gets whether this section may be created in the current context.
        /// </summary>
        public virtual bool IsAvailable => true;

        /// <summary>
        /// Initializes the section with its runtime host.
        /// </summary>
        /// <param name="panel">The owning debug panel.</param>
        public virtual void Initialize(DebugPanel panel) => Panel = panel;

        /// <summary>
        /// Gets the visual order for the section inside the panel.
        /// </summary>
        public abstract int OrderInPanel { get; }

        /// <summary>
        /// Builds the visual element that will be added to the panel.
        /// </summary>
        /// <returns>The section root visual element.</returns>
        public abstract VisualElement BuildSectionElement();
    }
}