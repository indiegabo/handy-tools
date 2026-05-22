using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Scenes;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Scenes.Authoring
{
    /// <summary>
    /// Discovers and sorts the concrete SceneExtender types available in the
    /// current editor domain.
    /// </summary>
    public static class HandySceneSectionTypeCache
    {
        #region Types

        /// <summary>
        /// Describes one concrete SceneExtender type and its effective section
        /// metadata.
        /// </summary>
        public readonly struct SectionDescriptor
        {
            #region Constructors

            /// <summary>
            /// Initializes one section descriptor.
            /// </summary>
            /// <param name="type">Concrete SceneExtender type.</param>
            /// <param name="sectionId">Stable section identifier.</param>
            /// <param name="displayName">Inspector display name.</param>
            /// <param name="order">Relative sort order.</param>
            public SectionDescriptor(
                Type type,
                string sectionId,
                string displayName,
                int order)
            {
                Type = type;
                SectionId = sectionId;
                DisplayName = displayName;
                Order = order;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets the concrete runtime type for the section.
            /// </summary>
            public Type Type { get; }

            /// <summary>
            /// Gets the stable identifier used to persist the section.
            /// </summary>
            public string SectionId { get; }

            /// <summary>
            /// Gets the display name shown in the inspector.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Gets the relative order used when sorting sections.
            /// </summary>
            public int Order { get; }

            /// <summary>
            /// Gets the serialized runtime type name for persistence.
            /// </summary>
            public string SerializedTypeName =>
                Type.AssemblyQualifiedName ?? Type.FullName ?? Type.Name;

            #endregion
        }

        #endregion

        #region Fields

        private static readonly Lazy<IReadOnlyList<SectionDescriptor>> CachedDescriptors =
            new(() => BuildDescriptors(includeTestAssemblies: false));

        private static readonly Lazy<IReadOnlyList<SectionDescriptor>> CachedTestDescriptors =
            new(() => BuildDescriptors(includeTestAssemblies: true));

        #endregion

        #region Public API

        /// <summary>
        /// Gets all discovered section descriptors sorted for inspector use.
        /// </summary>
        /// <param name="sceneAssetPath">
        /// Optional scene path used to decide whether test-only section
        /// assemblies should participate in discovery.
        /// </param>
        /// <returns>The discovered section descriptors.</returns>
        public static IReadOnlyList<SectionDescriptor> GetDescriptors(
            string sceneAssetPath = null)
        {
            return ShouldIncludeTestAssemblies(sceneAssetPath)
                ? CachedTestDescriptors.Value
                : CachedDescriptors.Value;
        }

        /// <summary>
        /// Attempts to resolve one descriptor by section identifier.
        /// </summary>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <param name="descriptor">Resolved descriptor.</param>
        /// <param name="sceneAssetPath">
        /// Optional scene path used to decide whether test-only sections are
        /// visible in the current authoring context.
        /// </param>
        /// <returns>True when one descriptor was resolved.</returns>
        public static bool TryGetDescriptor(
            string sectionId,
            out SectionDescriptor descriptor,
            string sceneAssetPath = null)
        {
            IReadOnlyList<SectionDescriptor> descriptors = GetDescriptors(sceneAssetPath);

            for (int index = 0; index < descriptors.Count; index++)
            {
                SectionDescriptor candidate = descriptors[index];
                if (string.Equals(candidate.SectionId, sectionId, StringComparison.Ordinal))
                {
                    descriptor = candidate;
                    return true;
                }
            }

            descriptor = default;
            return false;
        }

        #endregion

        #region Helpers

        private static IReadOnlyList<SectionDescriptor> BuildDescriptors(
            bool includeTestAssemblies)
        {
            List<SectionDescriptor> descriptors = new();
            HashSet<string> seenSectionIds = new(StringComparer.Ordinal);

            foreach (Type type in TypeCache.GetTypesDerivedFrom<SceneExtender>())
            {
                if (type == null
                    || type.IsAbstract
                    || type.IsGenericTypeDefinition
                    || IsExcludedFromDiscovery(type, includeTestAssemblies))
                {
                    continue;
                }

                HandySceneSectionAttribute attribute =
                    Attribute.GetCustomAttribute(
                        type,
                        typeof(HandySceneSectionAttribute),
                        false) as HandySceneSectionAttribute;

                string sectionId = string.IsNullOrWhiteSpace(attribute?.SectionId)
                    ? type.FullName ?? type.Name
                    : attribute.SectionId;

                if (!seenSectionIds.Add(sectionId))
                {
                    Debug.LogWarning(
                        $"Duplicate HandyScene section id '{sectionId}' found on " +
                        $"type '{type.FullName}'. The later type will be ignored.");
                    continue;
                }

                string displayName = string.IsNullOrWhiteSpace(attribute?.DisplayName)
                    ? ObjectNames.NicifyVariableName(type.Name)
                    : attribute.DisplayName;

                descriptors.Add(new SectionDescriptor(
                    type,
                    sectionId,
                    displayName,
                    attribute?.Order ?? 0));
            }

            descriptors.Sort(CompareDescriptors);
            return descriptors;
        }

        private static bool IsExcludedFromDiscovery(
            Type type,
            bool includeTestAssemblies)
        {
            string assemblyName = type.Assembly.GetName().Name ?? string.Empty;
            return !includeTestAssemblies
                && assemblyName.Contains(".Tests", StringComparison.Ordinal);
        }

        private static bool ShouldIncludeTestAssemblies(string sceneAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return false;
            }

            string normalizedPath = sceneAssetPath.Replace('\\', '/');
            return normalizedPath.StartsWith("Assets/Tests/", StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareDescriptors(
            SectionDescriptor left,
            SectionDescriptor right)
        {
            int orderComparison = left.Order.CompareTo(right.Order);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            int nameComparison = string.CompareOrdinal(
                left.DisplayName,
                right.DisplayName);

            if (nameComparison != 0)
            {
                return nameComparison;
            }

            return string.CompareOrdinal(
                left.Type.FullName,
                right.Type.FullName);
        }

        #endregion
    }
}