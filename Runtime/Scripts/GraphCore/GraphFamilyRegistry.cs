using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Stores the graph families registered in the current application domain.
    /// </summary>
    public static class GraphFamilyRegistry
    {
        private static readonly Dictionary<string, GraphFamilyDefinition> _definitions =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the currently registered graph families.
        /// </summary>
        public static IReadOnlyCollection<GraphFamilyDefinition> Definitions =>
            _definitions.Values.ToArray();

        /// <summary>
        /// Registers one graph family definition.
        /// Re-registering the same semantic definition is treated as idempotent.
        /// Registering the same id with different metadata throws.
        /// </summary>
        /// <param name="definition">Definition to register.</param>
        /// <returns>The registered family definition.</returns>
        public static GraphFamilyDefinition Register(GraphFamilyDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (_definitions.TryGetValue(definition.Id, out GraphFamilyDefinition existing))
            {
                if (existing.Equals(definition))
                {
                    return existing;
                }

                throw new InvalidOperationException(
                    $"Graph family '{definition.Id}' is already registered with different metadata.");
            }

            _definitions.Add(definition.Id, definition);
            return definition;
        }

        /// <summary>
        /// Gets whether one graph family id is already registered.
        /// </summary>
        /// <param name="familyId">Stable family identifier.</param>
        /// <returns>True when the registry contains the family id.</returns>
        public static bool IsRegistered(string familyId)
        {
            return !string.IsNullOrWhiteSpace(familyId)
                && _definitions.ContainsKey(familyId.Trim());
        }

        /// <summary>
        /// Attempts to resolve one registered graph family definition.
        /// </summary>
        /// <param name="familyId">Stable family identifier.</param>
        /// <param name="definition">Resolved definition when the id exists.</param>
        /// <returns>True when the family id exists in the registry.</returns>
        public static bool TryGetDefinition(
            string familyId,
            out GraphFamilyDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(familyId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(familyId.Trim(), out definition);
        }

        /// <summary>
        /// Clears registered graph families.
        /// This is intended for controlled test or domain-reset scenarios.
        /// </summary>
        internal static void Clear()
        {
            _definitions.Clear();
        }
    }
}