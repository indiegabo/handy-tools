using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Flow
{
    /// <summary>
    /// Routes execution to one dynamic output whose configured match value
    /// equals the current branch value.
    /// </summary>
    [Serializable]
    [CutsceneNodeMenu("Flow/Branch", "Branch")]
    public sealed class CutsceneValueBranchNode : CutsceneNodeBase
    {
        [Serializable]
        public sealed class BranchOption
        {
            [SerializeField, HideInInspector]
            private SerializableGuid _id;

            [SerializeField]
            private string _matchValue = string.Empty;

            [SerializeField]
            private string _displayName = "Branch";

            /// <summary>
            /// Gets the stable connection key used by graph edges for this branch.
            /// </summary>
            public string OutputKey => _id.ToHexString();

            /// <summary>
            /// Gets the configured value that selects this branch.
            /// </summary>
            public string MatchValue => _matchValue ?? string.Empty;

            /// <summary>
            /// Gets the label shown by the output port in the graph.
            /// </summary>
            public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
                ? ResolveFallbackDisplayName(_matchValue)
                : _displayName;

            /// <summary>
            /// Configures the match value and graph label for this branch.
            /// </summary>
            /// <param name="matchValue">Value that selects this branch.</param>
            /// <param name="displayName">Label shown by the output port.</param>
            public void Configure(string matchValue, string displayName = null)
            {
                _matchValue = matchValue ?? string.Empty;
                _displayName = string.IsNullOrWhiteSpace(displayName)
                    ? ResolveFallbackDisplayName(_matchValue)
                    : displayName;
            }

            /// <summary>
            /// Ensures the branch owns one stable serialized identifier.
            /// </summary>
            public void EnsureId()
            {
                if (_id == SerializableGuid.Empty)
                {
                    _id = SerializableGuid.NewGuid();
                }
            }

            private static string ResolveFallbackDisplayName(string matchValue)
            {
                return string.IsNullOrWhiteSpace(matchValue)
                    ? "Empty"
                    : matchValue;
            }
        }

        [SerializeField]
        [CutsceneValueSourceType(typeof(string))]
        private CutsceneValueSource _valueSource =
            CutsceneValueSource.CreateDirect(string.Empty);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _ignoreCaseSource =
            CutsceneValueSource.CreateDirect(true);

        [SerializeField]
        private List<BranchOption> _branches = new()
        {
            CreateDefaultBranch("A"),
            CreateDefaultBranch("B"),
        };

        /// <summary>
        /// Gets the configured branch options for validation and tests.
        /// </summary>
        public IReadOnlyList<BranchOption> Branches => _branches;

        /// <summary>
        /// Gets whether value comparisons ignore character casing.
        /// </summary>
        public bool IgnoreCase => _ignoreCaseSource?.Mode == CutsceneValueSourceMode.Direct
            && _ignoreCaseSource.DirectValue?.GetBoxedValue() is bool directIgnoreCase
            ? directIgnoreCase
            : true;

        /// <summary>
        /// Sets the current value and comparison mode used during execution.
        /// </summary>
        /// <param name="value">Current value that determines the selected output.</param>
        /// <param name="ignoreCase">Whether comparisons should ignore casing.</param>
        public void Configure(string value, bool ignoreCase = true)
        {
            EnsureValueSourcesConfigured();
            _valueSource.SetDirectValue(value ?? string.Empty);
            _ignoreCaseSource.SetDirectValue(ignoreCase);
        }

        /// <summary>
        /// Replaces the current branch list with the provided options.
        /// </summary>
        /// <param name="branches">Branch options to serialize on this node.</param>
        public void SetBranches(params BranchOption[] branches)
        {
            _branches = branches == null
                ? new List<BranchOption>()
                : branches.Where(branch => branch != null).ToList();

            EnsureBranchIds();
        }

        /// <summary>
        /// Adds one new branch option that selects on the provided value.
        /// </summary>
        /// <param name="matchValue">Value that chooses this branch.</param>
        /// <param name="displayName">Label shown by the branch output port.</param>
        public void AddBranch(string matchValue, string displayName = null)
        {
            BranchOption branch = new();
            branch.Configure(matchValue, displayName);
            branch.EnsureId();

            _branches ??= new List<BranchOption>();
            _branches.Add(branch);
        }

        /// <summary>
        /// Removes every configured branch option from this node.
        /// </summary>
        public void ClearBranches()
        {
            _branches?.Clear();
        }

        /// <summary>
        /// Returns the dynamic output ports configured for this branch node.
        /// </summary>
        /// <returns>The runtime-created output ports for all branch options.</returns>
        public override IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            EnsureBranchIds();

            if (_branches == null || _branches.Count == 0)
            {
                return CutsceneNodePort.None;
            }

            List<CutsceneNodePort> ports = new(_branches.Count);

            for (int index = 0; index < _branches.Count; index++)
            {
                BranchOption branch = _branches[index];

                if (branch == null)
                {
                    continue;
                }

                ports.Add(new CutsceneNodePort(branch.OutputKey, branch.DisplayName));
            }

            return ports;
        }

        /// <summary>
        /// Returns a concise summary of the current branch value and output count.
        /// </summary>
        /// <returns>One summary string shown in the graph node body.</returns>
        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            int branchCount = _branches?.Count ?? 0;
            return $"Value: {_valueSource.GetSummary()} | Outputs: {branchCount} | Ignore Case: {_ignoreCaseSource.GetSummary()}";
        }

        /// <summary>
        /// Selects the first branch whose configured value matches the current value.
        /// </summary>
        /// <param name="context">Execution context for the active cutscene run.</param>
        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();
            EnsureBranchIds();

            if (_branches == null || _branches.Count == 0)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Branch node requires at least one configured output."));
                return;
            }

            if (!_valueSource.TryGetValue(context, out string currentValue)
                || !_ignoreCaseSource.TryGetValue(context, out bool ignoreCase))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Branch node requires valid value and ignore-case sources."));
                return;
            }

            StringComparison comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            for (int index = 0; index < _branches.Count; index++)
            {
                BranchOption branch = _branches[index];

                if (branch == null)
                {
                    continue;
                }

                if (string.Equals(branch.MatchValue, currentValue, comparison))
                {
                    context.TryComplete(CutsceneNodeResult.Success(branch.OutputKey));
                    return;
                }
            }

            context.TryComplete(CutsceneNodeResult.Failure(
                $"Branch node '{DisplayTitle}' has no output configured for value '{currentValue}'."));
        }

        private void EnsureValueSourcesConfigured()
        {
            _valueSource ??= CutsceneValueSource.CreateDirect(string.Empty);
            _ignoreCaseSource ??= CutsceneValueSource.CreateDirect(true);

            _valueSource.SetExpectedValueType(typeof(string));
            _ignoreCaseSource.SetExpectedValueType(typeof(bool));
        }

        private void EnsureBranchIds()
        {
            if (_branches == null)
            {
                _branches = new List<BranchOption>();
                return;
            }

            for (int index = 0; index < _branches.Count; index++)
            {
                _branches[index]?.EnsureId();
            }
        }

        private static BranchOption CreateDefaultBranch(string label)
        {
            BranchOption branch = new();
            branch.Configure(label, label);
            branch.EnsureId();
            return branch;
        }
    }
}