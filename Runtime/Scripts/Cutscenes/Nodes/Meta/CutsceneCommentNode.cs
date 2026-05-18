using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Meta
{
    /// <summary>
    /// Provides one visual-only annotation node that never participates in
    /// execution flow, auto-arrange, or topology validation.
    /// </summary>
    [Serializable]
    [CutsceneNodeMenu("Meta/Comment", "Comment")]
    public sealed class CutsceneCommentNode : CutsceneNodeBase
    {
        #region Inspector

        [SerializeField]
        [TextArea(3, 8)]
        private string _body = "Add one authoring note.";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the authored comment body shown in the graph node.
        /// </summary>
        public string Body => _body ?? string.Empty;

        /// <summary>
        /// Gets whether the graph editor should render one input port.
        /// </summary>
        public override bool HasInputPort => false;

        /// <summary>
        /// Gets whether editor auto-arrange should reposition this node.
        /// </summary>
        public override bool ParticipatesInAutoArrange => false;

        /// <summary>
        /// Gets whether topology validation should include this node.
        /// </summary>
        public override bool ParticipatesInTopologyValidation => false;

        /// <summary>
        /// Gets whether runtime visualization may override the authored palette.
        /// </summary>
        public override bool UsesRuntimeStateStyling => false;

        #endregion

        #region Public API

        /// <summary>
        /// Replaces the authored comment body.
        /// </summary>
        /// <param name="body">Comment body displayed on the node.</param>
        public void SetBody(string body)
        {
            _body = body ?? string.Empty;
        }

        /// <summary>
        /// Returns the output ports exposed by this node.
        /// </summary>
        /// <returns>An empty list because the node is visual-only.</returns>
        public override IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            return CutsceneNodePort.None;
        }

        /// <summary>
        /// Returns the annotation body shown in the graph node body.
        /// </summary>
        /// <returns>The authored comment body.</returns>
        public override string GetSummary()
        {
            return string.IsNullOrWhiteSpace(Body)
                ? "Comment node."
                : Body;
        }

        #endregion
    }
}