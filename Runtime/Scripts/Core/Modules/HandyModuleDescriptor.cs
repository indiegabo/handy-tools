using System;

namespace IndieGabo.HandyTools.Modules
{
    /// <summary>
    /// Describes a HandyTools module exposed to the kernel.
    /// </summary>
    [Serializable]
    public sealed class HandyModuleDescriptor
    {
        /// <summary>
        /// Creates a module descriptor.
        /// </summary>
        /// <param name="id">Stable module identifier.</param>
        /// <param name="displayName">Human-readable module name.</param>
        /// <param name="description">Short module purpose.</param>
        /// <param name="activationMode">Whether the module is required or optional.</param>
        /// <param name="loadOrder">Ordering value used during runtime bootstrap.</param>
        /// <param name="isActiveByDefault">
        /// Whether an optional module should resolve as active when the project
        /// has not stored an explicit activation override yet.
        /// </param>
        public HandyModuleDescriptor(
            string id,
            string displayName,
            string description,
            HandyModuleActivationMode activationMode,
            int loadOrder,
            bool isActiveByDefault = false
        )
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            ActivationMode = activationMode;
            LoadOrder = loadOrder;
            IsActiveByDefault = isActiveByDefault;
        }

        /// <summary>
        /// Gets the stable module identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable module name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the module purpose.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets whether the module is mandatory or optional.
        /// </summary>
        public HandyModuleActivationMode ActivationMode { get; }

        /// <summary>
        /// Gets the runtime bootstrap ordering value.
        /// </summary>
        public int LoadOrder { get; }

        /// <summary>
        /// Gets whether an optional module should be treated as active before
        /// the project stores an explicit activation state.
        /// </summary>
        public bool IsActiveByDefault { get; }
    }
}