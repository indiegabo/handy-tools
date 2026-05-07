using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Owns the public machine lifecycle and transition surface delegated by
    /// one FSM brain.
    /// </summary>
    public sealed class FSMBrainMachineDomain
    {
        #region Fields

        private readonly FSMBrain _brain;
        private readonly FSMBrainStatesDomain _statesDomain;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes one machine domain for the provided brain.
        /// </summary>
        /// <param name="brain">The owning brain.</param>
        /// <param name="statesDomain">The states domain used for lookup operations.</param>
        public FSMBrainMachineDomain(
            FSMBrain brain,
            FSMBrainStatesDomain statesDomain)
        {
            _brain = brain;
            _statesDomain = statesDomain;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the owning brain finished its initialization sequence.
        /// </summary>
        public bool IsInitialized => _brain.IsInitialized;

        /// <summary>
        /// Gets whether the machine is currently on.
        /// </summary>
        public bool IsOn => _brain.IsOn;

        /// <summary>
        /// Gets whether the machine is currently paused.
        /// </summary>
        public bool IsPaused => _brain.IsPaused;

        /// <summary>
        /// Gets whether the machine is currently off.
        /// </summary>
        public bool IsOff => _brain.IsOff;

        /// <summary>
        /// Gets whether the machine is currently working.
        /// </summary>
        public bool IsWorking => _brain.IsWorking;

        /// <summary>
        /// Gets the current machine status.
        /// </summary>
        public MachineStatus Status => _brain.Status;

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public IState CurrentState => _brain.CurrentState;

        /// <summary>
        /// Gets the immediate previous state.
        /// </summary>
        public IState PreviousState => _brain.PreviousState;

        /// <summary>
        /// Gets the latest transition reason.
        /// </summary>
        public StateTransitionReason LastTransitionReason => _brain.LastTransitionReason;

        /// <summary>
        /// Gets the latest transition report.
        /// </summary>
        public StateTransitionReport LastTransitionReport => _brain.LastTransitionReport;

        /// <summary>
        /// Gets the configured default state.
        /// </summary>
        public IState DefaultState => _brain.DefaultState;

        /// <summary>
        /// Gets the first state that entered successfully.
        /// </summary>
        public IState FirstEnteredState => _brain.FirstEnteredState;

        #endregion

        #region Public API

        /// <summary>
        /// Turns the machine on and enters the provided state type.
        /// </summary>
        /// <param name="stateType">The type of the state to activate.</param>
        public void TurnOn(Type stateType)
        {
            if (!_statesDomain.TryGet(stateType, out IState state))
            {
                Debug.LogError(
                    $"Trying to turn machine on but {stateType.Name} is not loaded.",
                    _brain);
                return;
            }

            TurnOn(state);
        }

        /// <summary>
        /// Turns the machine on and enters the provided state.
        /// </summary>
        /// <param name="state">The state that should receive control.</param>
        public void TurnOn(IState state)
        {
            _brain.TurnOnCore(state);
        }

        /// <summary>
        /// Resumes the machine when it is paused.
        /// </summary>
        public void Resume()
        {
            _brain.ResumeCore();
        }

        /// <summary>
        /// Pauses the machine when it is on.
        /// </summary>
        public void Pause()
        {
            _brain.PauseCore();
        }

        /// <summary>
        /// Stops the machine when it is running.
        /// </summary>
        public void Stop()
        {
            _brain.StopCore();
        }

        /// <summary>
        /// Changes the machine status directly.
        /// </summary>
        /// <param name="status">The new machine status.</param>
        public void ChangeStatus(MachineStatus status)
        {
            _brain.ChangeStatusCore(status);
        }

        /// <summary>
        /// Requests an external state change.
        /// </summary>
        /// <param name="state">The state that should receive control.</param>
        public void RequestStateChange(IState state)
        {
            _brain.RequestStateChangeCore(state);
        }

        /// <summary>
        /// Requests an external state change to the specified runtime state.
        /// </summary>
        /// <typeparam name="T">The runtime state type to activate.</typeparam>
        public void RequestStateChange<T>() where T : State
        {
            if (!_statesDomain.TryGet<T>(out IState state))
            {
                Debug.LogError(
                    $"A state under the Type {nameof(T)} was requested but it is not present int the state factory ",
                    _brain);
                return;
            }

            RequestStateChange(state);
        }

        /// <summary>
        /// Completes the current state and performs a natural transition.
        /// </summary>
        /// <param name="target">The explicit target state, if any.</param>
        public void CompleteState(IState target = null)
        {
            _brain.CompleteStateCore(target);
        }

        /// <summary>
        /// Fails the current state and performs an error transition.
        /// </summary>
        /// <param name="target">The explicit fallback target, if any.</param>
        /// <param name="message">Optional history message.</param>
        public void FailState(IState target = null, string message = null)
        {
            _brain.FailStateCore(target, message);
        }

        /// <summary>
        /// Completes the current state and transitions naturally to the
        /// specified state type.
        /// </summary>
        /// <typeparam name="T">The target state type.</typeparam>
        public void CompleteState<T>() where T : IState
        {
            if (!_statesDomain.TryGet<T>(out IState state))
            {
                Debug.LogError(
                    $"A state under the Type {nameof(T)} was requested but it is not present in the state factory ",
                    _brain);
                return;
            }

            CompleteState(state);
        }

        /// <summary>
        /// Completes the current state and transitions naturally to the
        /// specified key.
        /// </summary>
        /// <param name="targetStateKey">The target state key.</param>
        public void CompleteState(string targetStateKey)
        {
            if (!_statesDomain.TryGet(targetStateKey, out IState state))
            {
                Debug.LogError(
                    $"A state under the key {targetStateKey} was requested but it is not present in the state factory ",
                    _brain);
                return;
            }

            CompleteState(state);
        }

        /// <summary>
        /// Fails the current state and transitions to the specified type.
        /// </summary>
        /// <typeparam name="T">The target state type.</typeparam>
        /// <param name="message">Optional history message.</param>
        public void FailState<T>(string message = null) where T : IState
        {
            if (!_statesDomain.TryGet<T>(out IState state))
            {
                Debug.LogError(
                    $"A state under the Type {nameof(T)} was requested but it is not present in the state factory ",
                    _brain);
                return;
            }

            FailState(state, message);
        }

        /// <summary>
        /// Fails the current state and transitions to the specified key.
        /// </summary>
        /// <param name="targetStateKey">The target state key.</param>
        /// <param name="message">Optional history message.</param>
        public void FailState(string targetStateKey, string message = null)
        {
            if (!_statesDomain.TryGet(targetStateKey, out IState state))
            {
                Debug.LogError(
                    $"A state under the key {targetStateKey} was requested but it is not present in the state factory ",
                    _brain);
                return;
            }

            FailState(state, message);
        }

        #endregion
    }
}