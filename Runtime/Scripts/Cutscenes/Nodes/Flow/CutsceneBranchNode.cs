using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Flow
{
    [System.Serializable]
    [CutsceneNodeMenu("Flow/Boolean Branch", "Boolean Branch")]
    public sealed class CutsceneBranchNode : CutsceneNodeBase
    {
        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _branchValueSource =
            CutsceneValueSource.CreateDirect(true);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _invertSource =
            CutsceneValueSource.CreateDirect(false);

        public void Configure(bool branchValue, bool invert = false)
        {
            EnsureValueSourcesConfigured();
            _branchValueSource.SetDirectValue(branchValue);
            _invertSource.SetDirectValue(invert);
        }

        public override IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            return new[]
            {
                new CutsceneNodePort(CutsceneNodePorts.True, "True"),
                new CutsceneNodePort(CutsceneNodePorts.False, "False"),
            };
        }

        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            return $"Value: {_branchValueSource.GetSummary()} | Invert: {_invertSource.GetSummary()}";
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_branchValueSource.TryGetValue(context, out bool branchValue)
                || !_invertSource.TryGetValue(context, out bool invert))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Boolean Branch node requires valid bool sources."));
                return;
            }

            bool result = invert ? !branchValue : branchValue;
            context.TryComplete(CutsceneNodeResult.Success(result ? CutsceneNodePorts.True : CutsceneNodePorts.False));
        }

        private void EnsureValueSourcesConfigured()
        {
            _branchValueSource ??= CutsceneValueSource.CreateDirect(true);
            _invertSource ??= CutsceneValueSource.CreateDirect(false);

            _branchValueSource.SetExpectedValueType(typeof(bool));
            _invertSource.SetExpectedValueType(typeof(bool));
        }
    }
}