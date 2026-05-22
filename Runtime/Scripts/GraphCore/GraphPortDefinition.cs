using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Describes one output port that can be declared by one graph node.
    /// </summary>
    [Serializable]
    public sealed class GraphPortDefinition
    {
        [SerializeField] private string _key;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isMandatory = true;

        /// <summary>
        /// Initializes one empty port definition for Unity serialization.
        /// </summary>
        public GraphPortDefinition()
        {
            _key = string.Empty;
            _displayName = string.Empty;
            _isMandatory = true;
        }

        /// <summary>
        /// Initializes one node output port definition.
        /// </summary>
        /// <param name="key">Unique key used to identify the output.</param>
        /// <param name="displayName">Human-readable name shown in the editor.</param>
        /// <param name="isMandatory">Whether the output must be connected.</param>
        public GraphPortDefinition(
            string key,
            string displayName,
            bool isMandatory = true)
        {
            _key = key;
            _displayName = displayName;
            _isMandatory = isMandatory;
        }

        /// <summary>
        /// Gets the runtime key used to identify the output.
        /// </summary>
        public string Key => _key;

        /// <summary>
        /// Gets the human-readable label shown in graph authoring surfaces.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Gets whether topology validation should require a connection.
        /// </summary>
        public bool IsMandatory => _isMandatory;

        /// <summary>
        /// Gets one built-in single-next output definition list.
        /// </summary>
        public static IReadOnlyList<GraphPortDefinition> NextOnly { get; } = new[]
        {
            new GraphPortDefinition(GraphPortKeys.Next, GraphPortKeys.Next),
        };

        /// <summary>
        /// Gets one shared empty output definition list.
        /// </summary>
        public static IReadOnlyList<GraphPortDefinition> None { get; } =
            Array.Empty<GraphPortDefinition>();
    }
}