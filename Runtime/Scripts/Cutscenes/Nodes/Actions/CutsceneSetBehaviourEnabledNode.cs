using System.Reflection;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Set Behaviour Enabled", "Set Behaviour Enabled")]
    public sealed class CutsceneSetBehaviourEnabledNode : CutsceneNodeBase
    {
        [SerializeField]
        [CutsceneValueSourceType(typeof(Component))]
        private CutsceneValueSource _targetSource =
            CutsceneValueSource.CreateDirect<Component>(null);

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

            if (!_targetSource.TryGetValue(context, out Component target))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set Behaviour Enabled node requires one valid component source."));
                return;
            }

            if (!_enabledSource.TryGetValue(context, out bool isEnabled))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set Behaviour Enabled node requires one valid enabled-state source."));
                return;
            }

            if (!TrySetEnabledState(target, isEnabled))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set Behaviour Enabled node requires one component with a writable enabled state."));
                return;
            }

            context.TryComplete(CutsceneNodeResult.Success());
        }

        private void EnsureValueSourcesConfigured()
        {
            _targetSource ??= CutsceneValueSource.CreateDirect<Component>(null);
            _enabledSource ??= CutsceneValueSource.CreateDirect(true);

            _targetSource.SetExpectedValueType(typeof(Component));
            _enabledSource.SetExpectedValueType(typeof(bool));
        }

        private static bool TrySetEnabledState(Component target, bool isEnabled)
        {
            if (target == null)
            {
                return false;
            }

            PropertyInfo enabledProperty = target.GetType().GetProperty(
                "enabled",
                BindingFlags.Instance | BindingFlags.Public);

            if (enabledProperty == null
                || enabledProperty.PropertyType != typeof(bool)
                || !enabledProperty.CanWrite)
            {
                return false;
            }

            enabledProperty.SetValue(target, isEnabled);
            return true;
        }
    }
}