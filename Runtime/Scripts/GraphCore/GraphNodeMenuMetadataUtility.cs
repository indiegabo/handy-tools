using System;
using System.Collections.Generic;
using System.Reflection;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Resolves shared default-title behavior from graph node menu metadata.
    /// </summary>
    public static class GraphNodeMenuMetadataUtility
    {
        /// <summary>
        /// Resolves and caches the default title declared by one node-menu attribute.
        /// </summary>
        /// <typeparam name="TAttribute">Attribute type that carries the menu metadata.</typeparam>
        /// <param name="nodeType">Concrete node type whose metadata should be inspected.</param>
        /// <param name="cache">Mutable title cache keyed by node type.</param>
        /// <returns>The resolved fallback title for the node type.</returns>
        public static string ResolveDefaultTitle<TAttribute>(
            Type nodeType,
            IDictionary<Type, string> cache)
            where TAttribute : Attribute, IGraphNodeMenuMetadata
        {
            if (nodeType == null)
            {
                throw new ArgumentNullException(nameof(nodeType));
            }

            if (cache != null && cache.TryGetValue(nodeType, out string cachedTitle))
            {
                return cachedTitle;
            }

            TAttribute menuAttribute = nodeType.GetCustomAttribute<TAttribute>(false);
            string resolvedTitle = ResolveDefaultTitle(menuAttribute, nodeType.Name);

            cache?.Add(nodeType, resolvedTitle);
            return resolvedTitle;
        }

        /// <summary>
        /// Resolves one fallback title from menu metadata and one type-name fallback.
        /// </summary>
        /// <param name="metadata">Menu metadata declared by the node type.</param>
        /// <param name="typeNameFallback">Fallback type name used when metadata is incomplete.</param>
        /// <returns>The resolved fallback node title.</returns>
        public static string ResolveDefaultTitle(
            IGraphNodeMenuMetadata metadata,
            string typeNameFallback)
        {
            if (metadata != null)
            {
                if (!string.IsNullOrWhiteSpace(metadata.DefaultTitle))
                {
                    return metadata.DefaultTitle;
                }

                if (!string.IsNullOrWhiteSpace(metadata.MenuPath))
                {
                    int slashIndex = metadata.MenuPath.LastIndexOf('/');
                    string pathTitle = slashIndex >= 0
                        ? metadata.MenuPath[(slashIndex + 1)..]
                        : metadata.MenuPath;

                    if (!string.IsNullOrWhiteSpace(pathTitle))
                    {
                        return pathTitle;
                    }
                }
            }

            return string.IsNullOrWhiteSpace(typeNameFallback)
                ? string.Empty
                : typeNameFallback;
        }
    }
}