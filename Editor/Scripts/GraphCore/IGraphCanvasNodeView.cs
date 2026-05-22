using System;
using IndieGabo.HandyTools.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Exposes the minimum node-view contract required by the reusable graph canvas shell.
    /// </summary>
    public interface IGraphCanvasNodeView
    {
        /// <summary>
        /// Gets the stable authored node identifier represented by the view.
        /// </summary>
        SerializableGuid NodeId { get; }

        /// <summary>
        /// Gets the rendered input port when the node exposes one.
        /// </summary>
        Port InputPort { get; }

        /// <summary>
        /// Gets the authored graph position represented by the view.
        /// </summary>
        Vector2 AuthoredPosition { get; }

        /// <summary>
        /// Raised when the GraphView selection state for the node changes.
        /// </summary>
        event Action SelectionStateChanged;
    }
}