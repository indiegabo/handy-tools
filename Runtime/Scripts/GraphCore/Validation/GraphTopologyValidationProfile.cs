using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.GraphCore.Validation
{
    /// <summary>
    /// Configures one topology validation pass for one graph family.
    /// </summary>
    public sealed class GraphTopologyValidationProfile
    {
        /// <summary>
        /// Gets or sets the graph label used in generic issue messages.
        /// </summary>
        public string GraphDisplayName { get; set; } = "graph";

        /// <summary>
        /// Gets or sets the root-node label used in generic issue messages.
        /// </summary>
        public string RootNodeDisplayName { get; set; } = "root";

        /// <summary>
        /// Gets or sets whether the graph must contain at least one root node.
        /// </summary>
        public bool RequireRootNode { get; set; }

        /// <summary>
        /// Gets or sets whether more than one root node is allowed.
        /// </summary>
        public bool AllowMultipleRootNodes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether missing source or target nodes should be reported.
        /// </summary>
        public bool DetectMissingNodes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether undeclared connection output keys should be reported.
        /// </summary>
        public bool DetectUndeclaredOutputKeys { get; set; } = true;

        /// <summary>
        /// Gets or sets whether multiple connections for the same output key should be reported.
        /// </summary>
        public bool DetectConnectionMultiplicity { get; set; } = true;

        /// <summary>
        /// Gets or sets whether mandatory node outputs should be enforced.
        /// </summary>
        public bool DetectMissingMandatoryOutputs { get; set; } = true;

        /// <summary>
        /// Gets or sets whether nodes unreachable from the root set should be reported.
        /// </summary>
        public bool DetectUnreachableNodes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether orphan nodes with no incoming connections should be reported.
        /// </summary>
        public bool DetectOrphanNodes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether family mismatches should be reported when family data is available.
        /// </summary>
        public bool DetectFamilyMismatch { get; set; } = true;

        /// <summary>
        /// Gets or sets the predicate that determines whether one node participates in topology validation.
        /// </summary>
        public Func<GraphNodeBase, bool> ShouldValidateNode { get; set; } =
            node => node != null && node.ParticipatesInTopologyValidation;

        /// <summary>
        /// Gets or sets the predicate that identifies root nodes.
        /// When unset, nodes without input ports are treated as roots.
        /// </summary>
        public Func<GraphNodeBase, bool> IsRootNode { get; set; }

        /// <summary>
        /// Gets or sets the resolver used to expose one authored family id per node.
        /// </summary>
        public Func<GraphNodeBase, string> ResolveNodeFamilyId { get; set; }

        /// <summary>
        /// Gets or sets the expected family id for the current graph.
        /// </summary>
        public string ExpectedFamilyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets one optional callback used to append family-specific semantic issues.
        /// </summary>
        public Action<
            GraphDefinition,
            IReadOnlyList<GraphNodeBase>,
            ICollection<GraphValidationIssue>> AppendSemanticIssues
        { get; set; }
    }
}