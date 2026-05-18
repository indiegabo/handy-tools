using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Set GameObject Active", "Set GameObject Active")]
    public sealed class CutsceneSetGameObjectActiveNode : CutsceneNodeBase
    {
        [SerializeField]
        [CutsceneValueSourceType(typeof(GameObject))]
        private CutsceneValueSource _targetSource =
            CutsceneValueSource.CreateDirect<GameObject>(null);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _activeSource =
            CutsceneValueSource.CreateDirect(true);

        public void Configure(GameObject target, bool isActive)
        {
            EnsureValueSourcesConfigured();
            _targetSource.SetDirectValue(target);
            _activeSource.SetDirectValue(isActive);
        }

        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            return $"{_targetSource.GetSummary()} -> {_activeSource.GetSummary()}";
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_targetSource.TryGetValue(context, out UnityEngine.Object targetObject))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set GameObject Active node requires one valid target source."));
                return;
            }

            if (!_activeSource.TryGetValue(context, out bool isActive))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set GameObject Active node requires one valid active-state source."));
                return;
            }

            GameObject targetGameObject = ResolveGameObject(targetObject);

            if (targetGameObject == null)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Set GameObject Active node requires one GameObject or Component target."));
                return;
            }

            targetGameObject.SetActive(isActive);
            context.TryComplete(CutsceneNodeResult.Success());
        }

        private void EnsureValueSourcesConfigured()
        {
            _targetSource ??= CutsceneValueSource.CreateDirect<GameObject>(null);
            _activeSource ??= CutsceneValueSource.CreateDirect(true);

            _targetSource.SetExpectedValueType(typeof(GameObject));
            _activeSource.SetExpectedValueType(typeof(bool));
        }

        private static GameObject ResolveGameObject(UnityEngine.Object value)
        {
            return value switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null,
            };
        }
    }
}