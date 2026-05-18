using IndieGabo.HandyTools.CutscenesModule.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Wait", "Wait")]
    public sealed class CutsceneWaitNode : CutsceneNodeBase
    {
        private const string ElapsedStateKey = "Elapsed";
        private const string DurationStateKey = "Duration";
        private const string TimeModeStateKey = "TimeMode";

        [SerializeField]
        [CutsceneValueSourceType(typeof(float))]
        private CutsceneValueSource _durationSource =
            CutsceneValueSource.CreateDirect(1f);

        [SerializeField]
        [CutsceneValueSourceType(typeof(CutsceneTimeMode))]
        private CutsceneValueSource _timeModeSource =
            CutsceneValueSource.CreateDirect(CutsceneTimeMode.Scaled);

        public override bool RequiresTick => true;

        public void Configure(float duration, CutsceneTimeMode timeMode)
        {
            EnsureValueSourcesConfigured();
            _durationSource.SetDirectValue(duration);
            _timeModeSource.SetDirectValue(timeMode);
        }

        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            return $"{_durationSource.GetSummary()}s ({_timeModeSource.GetSummary()})";
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_durationSource.TryGetValue(context, out float duration))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Wait node requires one valid duration source."));
                return;
            }

            if (!_timeModeSource.TryGetValue(context, out CutsceneTimeMode timeMode))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Wait node requires one valid time-mode source."));
                return;
            }

            duration = Mathf.Max(0f, duration);
            context.SetNodeState(ElapsedStateKey, 0f);
            context.SetNodeState(DurationStateKey, duration);
            context.SetNodeState(TimeModeStateKey, timeMode);

            if (duration <= 0f)
            {
                context.TryComplete(CutsceneNodeResult.Success());
            }
        }

        public override void Tick(CutsceneExecutionContext context)
        {
            context.TryGetNodeState(ElapsedStateKey, out float elapsed);

            if (!context.TryGetNodeState(DurationStateKey, out float duration)
                || !context.TryGetNodeState(TimeModeStateKey, out CutsceneTimeMode timeMode))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Wait node lost its resolved runtime state."));
                return;
            }

            elapsed += context.GetDeltaTime(timeMode);
            context.SetNodeState(ElapsedStateKey, elapsed);

            if (elapsed >= duration)
            {
                context.TryComplete(CutsceneNodeResult.Success());
            }
        }

        private void EnsureValueSourcesConfigured()
        {
            _durationSource ??= CutsceneValueSource.CreateDirect(1f);
            _timeModeSource ??= CutsceneValueSource.CreateDirect(CutsceneTimeMode.Scaled);

            _durationSource.SetExpectedValueType(typeof(float));
            _timeModeSource.SetExpectedValueType(typeof(CutsceneTimeMode));
        }
    }
}