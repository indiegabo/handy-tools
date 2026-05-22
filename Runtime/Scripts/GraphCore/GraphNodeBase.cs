using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Defines the shared authored data carried by one serialized graph node.
    /// </summary>
    [Serializable]
    public abstract class GraphNodeBase
    {
        [SerializeField] private SerializableGuid _id;
        [SerializeField] private string _title;
        [SerializeField] private Vector2 _position;

        /// <summary>
        /// Initializes one graph node and guarantees that it owns one stable id.
        /// </summary>
        protected GraphNodeBase()
        {
            EnsureId();
        }

        /// <summary>
        /// Gets the stable node identifier.
        /// </summary>
        public SerializableGuid Id => _id;

        /// <summary>
        /// Gets or sets the authored node title override.
        /// </summary>
        public string Title
        {
            get => _title;
            set => _title = value;
        }

        /// <summary>
        /// Gets the display title that should be shown in authoring surfaces.
        /// </summary>
        public string DisplayTitle => string.IsNullOrWhiteSpace(_title)
            ? DefaultTitle
            : _title;

        /// <summary>
        /// Gets the fallback title used when no custom title is authored.
        /// </summary>
        protected virtual string DefaultTitle => GetType().Name;

        /// <summary>
        /// Gets or sets the authored node position.
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// Gets whether authoring surfaces should render an input port.
        /// </summary>
        public virtual bool HasInputPort => true;

        /// <summary>
        /// Gets whether auto-arrange may reposition the node.
        /// </summary>
        public virtual bool ParticipatesInAutoArrange => true;

        /// <summary>
        /// Gets whether generic topology validation should include the node.
        /// </summary>
        public virtual bool ParticipatesInTopologyValidation => true;

        /// <summary>
        /// Gets whether runtime visualization may override authored styling.
        /// </summary>
        public virtual bool UsesRuntimeStateStyling => true;

        /// <summary>
        /// Gets whether the node requires one runtime tick loop.
        /// </summary>
        public virtual bool RequiresTick => false;

        /// <summary>
        /// Ensures that the node owns one non-empty stable identifier.
        /// </summary>
        public void EnsureId()
        {
            if (_id == SerializableGuid.Empty)
            {
                _id = SerializableGuid.NewGuid();
            }
        }

        /// <summary>
        /// Restores one authored identifier without generating a replacement.
        /// This is intended for migration, import, and validation adapter flows
        /// that must preserve the exact serialized identity, including invalid
        /// values that should be reported by validators.
        /// </summary>
        /// <param name="id">Identifier value that should be restored.</param>
        protected void RestoreId(SerializableGuid id)
        {
            _id = id;
        }

        /// <summary>
        /// Gets the output ports declared by the node.
        /// </summary>
        /// <returns>The declared output port definitions.</returns>
        public virtual IReadOnlyList<GraphPortDefinition> GetOutputPorts()
        {
            return GraphPortDefinition.NextOnly;
        }

        /// <summary>
        /// Gets one short authoring summary for inspectors and node previews.
        /// </summary>
        /// <returns>The authored summary string.</returns>
        public virtual string GetSummary()
        {
            return string.Empty;
        }
    }
}