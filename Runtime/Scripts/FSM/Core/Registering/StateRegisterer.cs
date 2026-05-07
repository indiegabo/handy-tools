using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Registering
{
    [RequireComponent(typeof(FSMBrain))]
    public class StateRegisterer : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private StateRegistry _registry;

        #endregion

        #region Fields

        private FSMBrain _machine;
        private float _sessionDuration;
        private bool _isRecording;

        #endregion

        #region Behaviour

        private void Awake()
        {
            _machine = GetComponent<FSMBrain>();
        }

        private void Start()
        {
            _sessionDuration = 0f;
            _isRecording = false;
        }

        private void Update()
        {
            if (!_isRecording) return;

            _sessionDuration += Time.deltaTime;
        }

        private void OnEnable()
        {
            if (_registry == null || _machine == null || !_machine.ShouldCaptureHistory)
            {
                _isRecording = false;
                return;
            }

            _registry.OpenSession(_machine);
            _registry.Register(_machine.CurrentState, _machine.LastTransitionReport);
            _machine.StateChanged.AddListener(OnStateChanged);
            _isRecording = true;
        }

        private void OnDisable()
        {
            if (!_isRecording || _registry == null || _machine == null)
            {
                _isRecording = false;
                return;
            }

            _machine.StateChanged.RemoveListener(OnStateChanged);
            _registry.CloseSession(_sessionDuration);
            _isRecording = false;
        }

        #endregion

        #region Callbacks

        private void OnStateChanged(IState newState, IState previous)
        {
            if (!_isRecording)
            {
                return;
            }

            _registry.Register(newState, _machine.LastTransitionReport);
        }

        #endregion
    }
}