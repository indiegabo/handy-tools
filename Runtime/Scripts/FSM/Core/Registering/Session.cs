using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Registering
{
    [System.Serializable]
    public class Session
    {
        #region Inspector

        [SerializeField]
        private string _date;

        [SerializeField]
        private string _time;

        [SerializeField]
        private FSMBrain _machine;

        [SerializeField]
        private List<IState> _states;

        [SerializeField]
        private List<Record> _records;

        [SerializeField]
        private float _duration;

        #endregion

        #region Fields

        private IState _currentState;
        private int _size;

        #endregion

        #region Getters

        public string Date => _date;
        public string Time => _time;

        public FSMBrain Machine => _machine;
        public List<Record> Records => _records;
        public float Duration => _duration;

        #endregion

        #region Constructors

        public Session(FSMBrain machine, int size)
        {
            _machine = machine;
            _records = new List<Record>();
            _size = size;
            _states = machine.States.GetAllStates();

            DateTime currentDateTime = DateTime.Now;
            _date = currentDateTime.ToShortDateString();
            _time = currentDateTime.ToShortTimeString();
        }

        #endregion

        #region Flow

        public void Close(float duration)
        {
            _duration = duration;
        }

        #endregion

        #region Registering

        public Record Register(IState state)
        {
            return Register(state, StateTransitionReport.Unknown);
        }

        public Record Register(
            IState state,
            StateTransitionReason transitionReason)
        {
            return Register(state, new StateTransitionReport(transitionReason));
        }

        public Record Register(
            IState state,
            StateTransitionReport transitionReport)
        {
            IState previousState = _currentState;
            _currentState = state;
            Record record = new(previousState, state, transitionReport);

            if (_records.Count >= _size)
            {
                _records.RemoveAt(0);
            }

            _records.Add(record);
            return record;
        }

        #endregion
    }
}