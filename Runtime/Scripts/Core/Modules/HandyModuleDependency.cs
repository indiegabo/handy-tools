using System;

namespace IndieGabo.HandyTools.Modules
{
    /// <summary>
    /// Describes a dependency required by a HandyTools module.
    /// </summary>
    [Serializable]
    public readonly struct HandyModuleDependency
    {
        /// <summary>
        /// Creates a module dependency descriptor.
        /// </summary>
        /// <param name="id">Stable dependency identifier.</param>
        /// <param name="displayName">Human-readable dependency name.</param>
        /// <param name="description">Short dependency purpose or install hint.</param>
        public HandyModuleDependency(string id, string displayName, string description)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// Gets the stable dependency identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable dependency name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the dependency purpose or install hint.
        /// </summary>
        public string Description { get; }
    }
}