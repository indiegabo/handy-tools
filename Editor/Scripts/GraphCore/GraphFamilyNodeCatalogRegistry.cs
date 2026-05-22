using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Stores family-scoped node creation descriptors used by host-specific
    /// graph windows and search surfaces.
    /// </summary>
    public static class GraphFamilyNodeCatalogRegistry
    {
        /// <summary>
        /// Describes one createable node entry exposed by one graph family.
        /// </summary>
        public readonly struct Descriptor : IEquatable<Descriptor>
        {
            /// <summary>
            /// Initializes one family-scoped node descriptor.
            /// </summary>
            /// <param name="familyId">Owning graph family identifier.</param>
            /// <param name="nodeType">Concrete node implementation type.</param>
            /// <param name="menuPath">Menu path shown in creation UIs.</param>
            public Descriptor(string familyId, Type nodeType, string menuPath)
            {
                FamilyId = string.IsNullOrWhiteSpace(familyId)
                    ? throw new ArgumentException(
                        "Graph family id cannot be null or whitespace.",
                        nameof(familyId))
                    : familyId.Trim();
                NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
                MenuPath = string.IsNullOrWhiteSpace(menuPath)
                    ? throw new ArgumentException(
                        "Node menu path cannot be null or whitespace.",
                        nameof(menuPath))
                    : menuPath.Trim();
            }

            /// <summary>
            /// Gets the owning graph family identifier.
            /// </summary>
            public string FamilyId { get; }

            /// <summary>
            /// Gets the concrete node implementation type.
            /// </summary>
            public Type NodeType { get; }

            /// <summary>
            /// Gets the menu path shown in creation UIs.
            /// </summary>
            public string MenuPath { get; }

            /// <summary>
            /// Gets the short display name resolved from the menu path.
            /// </summary>
            public string DisplayName => MenuPath.Split('/').LastOrDefault() ?? NodeType.Name;

            /// <inheritdoc />
            public bool Equals(Descriptor other)
            {
                return string.Equals(FamilyId, other.FamilyId, StringComparison.Ordinal)
                    && NodeType == other.NodeType
                    && string.Equals(MenuPath, other.MenuPath, StringComparison.Ordinal);
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is Descriptor other && Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return HashCode.Combine(FamilyId, NodeType, MenuPath);
            }
        }

        private static readonly Dictionary<string, Dictionary<Type, Descriptor>> _descriptorsByFamily =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Registers one family-scoped node descriptor.
        /// Re-registering the same semantic descriptor is treated as idempotent.
        /// Registering the same node type under the same family with a different
        /// menu path throws.
        /// </summary>
        /// <param name="descriptor">Descriptor to register.</param>
        /// <returns>The registered descriptor.</returns>
        public static Descriptor Register(Descriptor descriptor)
        {
            if (!GraphFamilyRegistry.IsRegistered(descriptor.FamilyId))
            {
                throw new InvalidOperationException(
                    $"Graph family '{descriptor.FamilyId}' must be registered before node descriptors can be added.");
            }

            if (!_descriptorsByFamily.TryGetValue(
                    descriptor.FamilyId,
                    out Dictionary<Type, Descriptor> familyDescriptors))
            {
                familyDescriptors = new Dictionary<Type, Descriptor>();
                _descriptorsByFamily.Add(descriptor.FamilyId, familyDescriptors);
            }

            if (familyDescriptors.TryGetValue(descriptor.NodeType, out Descriptor existing))
            {
                if (existing.Equals(descriptor))
                {
                    return existing;
                }

                throw new InvalidOperationException(
                    $"Node type '{descriptor.NodeType.FullName}' is already registered for graph family '{descriptor.FamilyId}' with different metadata.");
            }

            familyDescriptors.Add(descriptor.NodeType, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Registers multiple family-scoped node descriptors.
        /// </summary>
        /// <param name="descriptors">Descriptors that should be registered.</param>
        /// <returns>The registered descriptors in registration order.</returns>
        public static IReadOnlyList<Descriptor> RegisterRange(IEnumerable<Descriptor> descriptors)
        {
            if (descriptors == null)
            {
                return Array.Empty<Descriptor>();
            }

            List<Descriptor> registered = new();

            foreach (Descriptor descriptor in descriptors)
            {
                registered.Add(Register(descriptor));
            }

            return registered;
        }

        /// <summary>
        /// Gets the descriptors registered for one graph family.
        /// </summary>
        /// <param name="familyId">Owning graph family identifier.</param>
        /// <returns>The registered descriptors sorted by menu path.</returns>
        public static IReadOnlyList<Descriptor> GetDescriptors(string familyId)
        {
            if (string.IsNullOrWhiteSpace(familyId)
                || !_descriptorsByFamily.TryGetValue(
                    familyId.Trim(),
                    out Dictionary<Type, Descriptor> familyDescriptors))
            {
                return Array.Empty<Descriptor>();
            }

            return familyDescriptors.Values
                .OrderBy(descriptor => descriptor.MenuPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Attempts to resolve one descriptor for one node type inside one graph family.
        /// </summary>
        /// <param name="familyId">Owning graph family identifier.</param>
        /// <param name="nodeType">Concrete node implementation type.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the family exposes the node type.</returns>
        public static bool TryGetDescriptor(
            string familyId,
            Type nodeType,
            out Descriptor descriptor)
        {
            descriptor = default;

            return !string.IsNullOrWhiteSpace(familyId)
                && nodeType != null
                && _descriptorsByFamily.TryGetValue(
                    familyId.Trim(),
                    out Dictionary<Type, Descriptor> familyDescriptors)
                && familyDescriptors.TryGetValue(nodeType, out descriptor);
        }

        /// <summary>
        /// Clears all registered family-scoped node descriptors.
        /// This is intended for controlled test or domain-reset scenarios.
        /// </summary>
        internal static void Clear()
        {
            _descriptorsByFamily.Clear();
        }
    }
}