using IndieGabo.HandyTools.CutscenesModule.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Triggers
{
    public enum CutsceneTriggerMode
    {
        Manual,
        Awake,
        OnEnable,
        Start,
    }

    [DisallowMultipleComponent]
    public sealed class CutsceneTrigger : MonoBehaviour
    {
        [SerializeField] private CutsceneDirector _director;
        [SerializeField] private CutsceneTriggerMode _triggerMode = CutsceneTriggerMode.Manual;
        [SerializeField] private bool _oneShot;
        [SerializeField] private bool _gate = true;

        private bool _hasTriggered;

        private void Awake()
        {
            if (_director == null)
            {
                _director = GetComponent<CutsceneDirector>();
            }

            if (_triggerMode == CutsceneTriggerMode.Awake)
            {
                Trigger();
            }
        }

        private void OnEnable()
        {
            if (_triggerMode == CutsceneTriggerMode.OnEnable)
            {
                Trigger();
            }
        }

        private void Start()
        {
            if (_triggerMode == CutsceneTriggerMode.Start)
            {
                Trigger();
            }
        }

        [Button(ButtonSizes.Medium)]
        public void Trigger()
        {
            if (!_gate || _director == null || (_oneShot && _hasTriggered))
            {
                return;
            }

            _director.Play();
            _hasTriggered = true;
        }
    }
}