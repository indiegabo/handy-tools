using IndieGabo.HandyTools.CutscenesModule.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Triggers
{
    /// <summary>
    /// Defines which Unity lifecycle callback automatically starts one
    /// cutscene trigger.
    /// </summary>
    public enum CutsceneTriggerMode
    {
        Manual,
        Awake,
        OnEnable,
        Start,
    }

    /// <summary>
    /// Starts one cutscene director from a configured lifecycle entry point.
    /// </summary>
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
            ResolveDirector();

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

        /// <summary>
        /// Replays the configured automatic trigger mode when the Unity editor
        /// enters play mode without reloading the active scene objects.
        /// </summary>
        public void HandlePlaySessionStartWithoutSceneReload()
        {
            ResolveDirector();

            if (_director == null || _director.TryGetActiveRun(out _))
            {
                return;
            }

            switch (_triggerMode)
            {
                case CutsceneTriggerMode.Awake:
                case CutsceneTriggerMode.OnEnable:
                case CutsceneTriggerMode.Start:
                    Trigger();
                    break;
            }
        }

        /// <summary>
        /// Clears transient trigger state before a new play session starts when
        /// Unity reuses the same scene objects without reloading them.
        /// </summary>
        public void ResetRuntimeState()
        {
            _hasTriggered = false;
        }

        /// <summary>
        /// Resolves the local director reference when the serialized field is
        /// missing and the component shares the same GameObject.
        /// </summary>
        private void ResolveDirector()
        {
            if (_director == null)
            {
                _director = GetComponent<CutsceneDirector>();
            }
        }
    }
}