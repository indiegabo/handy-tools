using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Registering
{
    [System.Serializable]
    public class Record
    {
        #region Inspector

        [SerializeField]
        private StateTransitionReport _transitionReport;

        [SerializeField]
        private bool _isScriptable;

        [SerializeField]
        private State _fromState;

        [SerializeField]
        private State _state;

        [SerializeField]
        private ScriptableState _fromScriptable;

        [SerializeField]
        private ScriptableState _scriptableState;

        public void DisplayStateName()
        {
            Debug.Log($"{_state.DisplayName}");
        }

        #endregion

        #region Getters

        public StateTransitionReason TransitionReason => _transitionReport.Reason;

        public StateTransitionReport TransitionReport => _transitionReport;

        public string TransitionMessage => _transitionReport.Message;

        public bool IsScriptable => _isScriptable;

        public ScriptableState FromScriptable => _fromScriptable;
        public State FromState => _fromState;
        public ScriptableState ScriptableState => _scriptableState;
        public State State => _state;

        #endregion

        #region Consctructors

        public Record(IState from, IState state)
            : this(from, state, StateTransitionReport.Unknown)
        {
        }

        public Record(
            IState from,
            IState state,
            StateTransitionReason transitionReason)
            : this(from, state, new StateTransitionReport(transitionReason))
        {
        }

        public Record(
            IState from,
            IState state,
            StateTransitionReport transitionReport)
        {
            _transitionReport = transitionReport;

            if (from is State)
            {
                _fromState = from as State;
            }
            else if (from is ScriptableState)
            {
                _fromScriptable = from as ScriptableState;
            }

            if (state is State)
            {
                _state = state as State;
            }
            else if (state is ScriptableState)
            {
                _isScriptable = true;
                _scriptableState = state as ScriptableState;
            }
        }

        #endregion
    }
}