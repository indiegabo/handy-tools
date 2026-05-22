using System;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Represents one directed edge between two nodes in one graph.
    /// </summary>
    [Serializable]
    public class GraphConnection
    {
        [SerializeField] private SerializableGuid _fromNodeId;
        [SerializeField] private string _outputKey = GraphPortKeys.Next;
        [SerializeField] private SerializableGuid _toNodeId;
        [SerializeField] private bool _hasCustomColor;
        [SerializeField] private Color _customColor = new(0.45f, 0.45f, 0.45f, 1f);

        /// <summary>
        /// Initializes one directed graph connection.
        /// </summary>
        /// <param name="fromNodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        /// <param name="toNodeId">Target node identifier.</param>
        public GraphConnection(
            SerializableGuid fromNodeId,
            string outputKey,
            SerializableGuid toNodeId)
        {
            _fromNodeId = fromNodeId;
            _outputKey = outputKey;
            _toNodeId = toNodeId;
        }

        /// <summary>
        /// Gets the origin node identifier.
        /// </summary>
        public SerializableGuid FromNodeId => _fromNodeId;

        /// <summary>
        /// Gets the origin output key.
        /// </summary>
        public string OutputKey => _outputKey;

        /// <summary>
        /// Gets the target node identifier.
        /// </summary>
        public SerializableGuid ToNodeId => _toNodeId;

        /// <summary>
        /// Gets whether the connection declares one custom authoring color.
        /// </summary>
        public bool HasCustomColor => _hasCustomColor;

        /// <summary>
        /// Gets the custom authoring color.
        /// </summary>
        public Color CustomColor => _customColor;

        /// <summary>
        /// Replaces the target node identifier.
        /// </summary>
        /// <param name="toNodeId">Replacement target node identifier.</param>
        public void SetTarget(SerializableGuid toNodeId)
        {
            _toNodeId = toNodeId;
        }

        /// <summary>
        /// Applies one custom authoring color to the connection.
        /// </summary>
        /// <param name="color">Color to store on the connection.</param>
        public void SetCustomColor(Color color)
        {
            _hasCustomColor = true;
            _customColor = color;
        }

        /// <summary>
        /// Removes the custom authoring color flag.
        /// </summary>
        public void ClearCustomColor()
        {
            _hasCustomColor = false;
        }
    }
}