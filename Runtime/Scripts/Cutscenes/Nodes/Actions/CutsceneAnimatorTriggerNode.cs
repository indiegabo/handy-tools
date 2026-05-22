using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Animator Trigger", "Animator Trigger")]
    public sealed class CutsceneAnimatorTriggerNode : CutsceneNodeBase
    {
        [SerializeField]
        [CutsceneValueSourceType(typeof(Animator))]
        private CutsceneValueSource _animatorSource =
            CutsceneValueSource.CreateDirect<Animator>(null);

        [SerializeField]
        [CutsceneValueSourceType(typeof(string))]
        private CutsceneValueSource _triggerNameSource =
            CutsceneValueSource.CreateDirect(string.Empty);

        [SerializeField]
        [CutsceneValueSourceType(typeof(string))]
        private CutsceneValueSource _resetTriggerNameSource =
            CutsceneValueSource.CreateDirect(string.Empty);

        /// <summary>
        /// Exposes the authored animator source for runtime migration.
        /// </summary>
        internal CutsceneValueSource AnimatorSource
        {
            get
            {
                EnsureValueSourcesConfigured();
                return _animatorSource;
            }
        }

        /// <summary>
        /// Exposes the authored trigger-name source for runtime migration.
        /// </summary>
        internal CutsceneValueSource TriggerNameSource
        {
            get
            {
                EnsureValueSourcesConfigured();
                return _triggerNameSource;
            }
        }

        /// <summary>
        /// Exposes the authored reset-trigger source for runtime migration.
        /// </summary>
        internal CutsceneValueSource ResetTriggerNameSource
        {
            get
            {
                EnsureValueSourcesConfigured();
                return _resetTriggerNameSource;
            }
        }

        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            return _triggerNameSource.GetSummary();
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_animatorSource.TryGetValue(context, out Animator animator))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Animator Trigger node requires one valid animator source."));
                return;
            }

            if (!_triggerNameSource.TryGetValue(context, out string triggerName)
                || string.IsNullOrWhiteSpace(triggerName))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Animator Trigger node requires one valid trigger source."));
                return;
            }

            string resetTriggerName = _resetTriggerNameSource.GetValueOrDefault(context, string.Empty);

            if (!string.IsNullOrWhiteSpace(resetTriggerName))
            {
                animator.ResetTrigger(resetTriggerName);
            }

            animator.SetTrigger(triggerName);
            context.TryComplete(CutsceneNodeResult.Success());
        }

        private void EnsureValueSourcesConfigured()
        {
            _animatorSource ??= CutsceneValueSource.CreateDirect<Animator>(null);
            _triggerNameSource ??= CutsceneValueSource.CreateDirect(string.Empty);
            _resetTriggerNameSource ??= CutsceneValueSource.CreateDirect(string.Empty);

            _animatorSource.SetExpectedValueType(typeof(Animator));
            _triggerNameSource.SetExpectedValueType(typeof(string));
            _resetTriggerNameSource.SetExpectedValueType(typeof(string));
        }
    }
}