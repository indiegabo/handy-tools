using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Invoke UnityEvent", "Invoke UnityEvent")]
    public sealed class CutsceneInvokeUnityEventNode : CutsceneNodeBase
    {
        [SerializeField] private UnityEvent _event = new();

        public override void OnEnter(CutsceneExecutionContext context)
        {
            _event?.Invoke();
            context.TryComplete(CutsceneNodeResult.Success());
        }
    }
}