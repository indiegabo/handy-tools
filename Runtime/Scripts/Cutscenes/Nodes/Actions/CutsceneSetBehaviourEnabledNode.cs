using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Set Behaviour Enabled", "Set Behaviour Enabled")]
    public sealed class CutsceneSetBehaviourEnabledNode : CutsceneNodeBase
    {
        [SerializeField]
        [CutsceneValueSourceType(typeof(Behaviour))]
        private CutsceneValueSource _targetSource =
            CutsceneValueSource.CreateDirect<Behaviour>(null);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _enabledSource =
            CutsceneValueSource.CreateDirect(true);

        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            return $"{_targetSource.GetSummary()} -> {_enabledSource.GetSummary()}";
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_targetSource.TryGetValue(context, out Behaviour target))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set Behaviour Enabled node requires one valid behaviour source."));
                return;
            }

            if (!_enabledSource.TryGetValue(context, out bool isEnabled))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set Behaviour Enabled node requires one valid enabled-state source."));
                return;
            }

            target.enabled = isEnabled;
            context.TryComplete(CutsceneNodeResult.Success());
        }

        private void EnsureValueSourcesConfigured()
        {
            _targetSource ??= CutsceneValueSource.CreateDirect<Behaviour>(null);
            _enabledSource ??= CutsceneValueSource.CreateDirect(true);

            _targetSource.SetExpectedValueType(typeof(Behaviour));
            _enabledSource.SetExpectedValueType(typeof(bool));
        }
    }
}