using System;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Describes one logical graph family that can own its own node catalog,
    /// validators, and blackboard type contributions.
    /// </summary>
    public sealed class GraphFamilyDefinition : IEquatable<GraphFamilyDefinition>
    {
        /// <summary>
        /// Initializes one graph family descriptor.
        /// </summary>
        /// <param name="id">Stable unique family identifier.</param>
        /// <param name="displayName">Human-readable family name.</param>
        /// <param name="description">Optional documentation summary.</param>
        public GraphFamilyDefinition(
            string id,
            string displayName,
            string description = "")
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Graph family id cannot be null or whitespace.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Graph family display name cannot be null or whitespace.",
                    nameof(displayName));
            }

            Id = id.Trim();
            DisplayName = displayName.Trim();
            Description = description?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Gets the stable unique family identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable family name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets one optional documentation summary for the family.
        /// </summary>
        public string Description { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as GraphFamilyDefinition);
        }

        /// <inheritdoc />
        public bool Equals(GraphFamilyDefinition other)
        {
            return other != null
                && string.Equals(Id, other.Id, StringComparison.Ordinal)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Description, other.Description, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, DisplayName, Description);
        }

        /// <summary>
        /// Returns the display name for diagnostics and editor labels.
        /// </summary>
        /// <returns>The graph family display name.</returns>
        public override string ToString()
        {
            return DisplayName;
        }
    }
}