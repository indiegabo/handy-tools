using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.GraphCore;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Discovers attributed graph node types and instantiates them for family-scoped catalogs.
    /// </summary>
    public static class GraphAttributedNodeCatalogUtility
    {
        /// <summary>
        /// Describes one node type discovered from one menu-metadata attribute.
        /// </summary>
        /// <typeparam name="TAttribute">Attribute type that carries the menu metadata.</typeparam>
        public readonly struct Descriptor<TAttribute>
            where TAttribute : Attribute, IGraphNodeMenuMetadata
        {
            /// <summary>
            /// Initializes one attributed node descriptor.
            /// </summary>
            /// <param name="nodeType">Concrete node type exposed to creation UIs.</param>
            /// <param name="attribute">Resolved menu metadata attribute.</param>
            public Descriptor(Type nodeType, TAttribute attribute)
            {
                NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
                Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            }

            /// <summary>
            /// Gets the concrete node type exposed to creation UIs.
            /// </summary>
            public Type NodeType { get; }

            /// <summary>
            /// Gets the menu metadata declared by the node attribute.
            /// </summary>
            public TAttribute Attribute { get; }

            /// <summary>
            /// Gets the menu path exposed to creation UIs.
            /// </summary>
            public string MenuPath => Attribute.MenuPath;

            /// <summary>
            /// Gets the short display name resolved from the menu path.
            /// </summary>
            public string DisplayName => MenuPath.Split('/').LastOrDefault() ?? NodeType.Name;
        }

        /// <summary>
        /// Discovers all non-abstract node types derived from one base node type and decorated with one menu attribute.
        /// </summary>
        /// <typeparam name="TNodeBase">Base node type exposed by the graph family.</typeparam>
        /// <typeparam name="TAttribute">Attribute type that carries the menu metadata.</typeparam>
        /// <returns>The discovered node descriptors sorted by menu path.</returns>
        public static IReadOnlyList<Descriptor<TAttribute>> Discover<TNodeBase, TAttribute>()
            where TNodeBase : GraphNodeBase
            where TAttribute : Attribute, IGraphNodeMenuMetadata
        {
            return TypeCache.GetTypesWithAttribute<TAttribute>()
                .Where(type => type != null
                    && !type.IsAbstract
                    && typeof(TNodeBase).IsAssignableFrom(type))
                .Select(type =>
                {
                    TAttribute attribute = type.GetCustomAttributes(typeof(TAttribute), false)
                        .OfType<TAttribute>()
                        .First();

                    return new Descriptor<TAttribute>(type, attribute);
                })
                .OrderBy(descriptor => descriptor.MenuPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Creates one authored node instance for the requested node type.
        /// </summary>
        /// <typeparam name="TNodeBase">Base node type expected by the caller.</typeparam>
        /// <param name="nodeType">Concrete node type that should be instantiated.</param>
        /// <returns>The created node instance when the type is valid.</returns>
        public static TNodeBase CreateNode<TNodeBase>(Type nodeType)
            where TNodeBase : GraphNodeBase
        {
            if (nodeType == null || !typeof(TNodeBase).IsAssignableFrom(nodeType))
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(nodeType, true) as TNodeBase;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }
    }
}