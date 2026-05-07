using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Registering
{
    [CreateAssetMenu(fileName = "StateRegistry", menuName = "HandyTools/FSM/StateRegistry")]
    public class StateRegistry : ScriptableObject
    {
        #region Inspector

        [Tooltip("The maximum number of sessions recorded")]
        [SerializeField]
        private int _numberOfSessions = 10;

        [Tooltip("The maximum amount of registries in each registry list")]
        [SerializeField]
        private int _registrySize = 50;

        [SerializeField]
        private List<Session> _sessions;

        #endregion

        #region Field

        private Session _currentSession;

        #endregion

        #region Getters

        public List<Session> Sessions => _sessions;

        #endregion

        #region Sessions

        public void OpenSession(FSMBrain machine)
        {
            if (_sessions.Count >= _numberOfSessions)
            {
                _sessions.RemoveAt(0);
            }

            _currentSession = new Session(machine, _registrySize);
            _sessions.Add(_currentSession);
        }

        public void CloseSession(float duration)
        {
            _currentSession?.Close(duration);
            _currentSession = null;
        }

        #endregion

        #region Registering

        public void Register(IState state)
        {
            Register(state, StateTransitionReport.Unknown);
        }

        public void Register(
            IState state,
            StateTransitionReason transitionReason)
        {
            Register(state, new StateTransitionReport(transitionReason));
        }

        public void Register(
            IState state,
            StateTransitionReport transitionReport)
        {
            if (_currentSession == null) return;
            if (state == null) return;

            _currentSession.Register(state, transitionReport);
        }

        #endregion
    }
}