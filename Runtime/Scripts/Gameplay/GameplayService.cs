using System;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.LoggerModule;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.GameplayModule
{
    /// <summary>
    /// Coordinates the global gameplay lifecycle and time-scale transitions.
    /// </summary>
    public class GameplayService : HandyBehaviour
    {
        #region Inspector

        #endregion

        #region Fields

        private Status _status = Status.Off;
        private bool _isTransitioning;
        private Status _transitionTargetStatus = Status.Off;
        private int _sessionSequenceCounter;
        private GameplaySessionContext _currentSessionContext;
        private object _interruptionOwner;

        #endregion

        #region Getters

        public bool IsOn => _status == Status.On;
        public bool IsOff => _status == Status.Off;
        public bool IsPaused => _status == Status.Paused;
        public bool IsTransitioning => _isTransitioning;
        public bool HasInterruptionOwner => _interruptionOwner != null;
        public object CurrentInterruptionOwner => _interruptionOwner;
        public string CurrentInterruptionOwnerDescription => DescribeOwner(_interruptionOwner);
        public Status CurrentStatus => _status;
        public Status TransitionTargetStatus => _transitionTargetStatus;
        public GameplaySessionContext CurrentSessionContext => _currentSessionContext;

        #endregion

        #region Behaviour

        #endregion

        #region Starting


        /// <summary>
        /// Starts the gameplay, unfreezing the time and changing the status to <see cref="Status.On"/>.
        /// Uses <see cref="GameplayTimeScaler.Unfreeze"/> to unfreeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not need to await the transition, discard the returned
        /// awaitable.
        /// </summary>
        /// <param name="duration">The duration of the unfreeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An awaitable that completes when the transition is over.</returns>
        public async Awaitable StartGameplay(float duration = 0, UnityAction onComplete = null)
        {
            if (!IsOff)
            {
                HandyLogger.Error(
                    $"{nameof(GameplayService)}",
                    "Trying to start gameplay but it is not off. Use ResumeGameplay to return from an interruption.",
                    this
                );
                return;
            }

            if (!TryBeginTransition(Status.On, "start gameplay"))
            {
                return;
            }

            try
            {
                await GameplayTimeScaler.Unfreeze(duration);
                CompleteTransition(
                    Status.On,
                    GameplayTransitionOrigin.Start,
                    null,
                    onComplete
                );
            }
            finally
            {
                CancelTransitionIfStillPending();
            }
        }

        #endregion

        #region Pausing and Resuming

        /// <summary>
        /// Pauses the gameplay, freezing the time and changing the status to <see cref="Status.Paused"/>.
        /// Uses <see cref="GameplayTimeScaler.Freeze"/> to freeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not need to await the transition, discard the returned
        /// awaitable.
        /// </summary>
        /// <param name="interruptionOwner">
        /// Stable owner reference that becomes the exclusive resume authority
        /// for this interruption.
        /// </param>
        /// <param name="duration">The duration of the freeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An awaitable that completes when the transition is over.</returns>
        public async Awaitable PauseGameplay(
            object interruptionOwner,
            float duration = 0,
            UnityAction onComplete = null
        )
        {
            if (!IsOn)
            {
                HandyLogger.Error(
                    $"{nameof(GameplayService)}",
                    "Trying to interrupt gameplay but it is not currently on.",
                    this
                );
                return;
            }

            if (!TryValidateInterruptionOwner(interruptionOwner, "pause gameplay"))
            {
                return;
            }

            if (!TryBeginTransition(Status.Paused, "interrupt gameplay"))
            {
                return;
            }

            try
            {
                await GameplayTimeScaler.Freeze(duration);
                CompleteTransition(
                    Status.Paused,
                    GameplayTransitionOrigin.Pause,
                    interruptionOwner,
                    onComplete
                );
            }
            finally
            {
                CancelTransitionIfStillPending();
            }
        }

        /// <summary>
        /// Resumes the gameplay, unfreezing the time and changing the status to <see cref="Status.On"/>.
        /// Uses <see cref="GameplayTimeScaler.Unfreeze"/> to unfreeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not need to await the transition, discard the returned
        /// awaitable.
        /// </summary>
        /// <param name="interruptionOwner">
        /// Stable owner reference that must match the owner that paused the
        /// gameplay.
        /// </param>
        /// <param name="duration">The duration of the unfreeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An awaitable that completes when the transition is over.</returns>
        public async Awaitable ResumeGameplay(
            object interruptionOwner,
            float duration = 0,
            UnityAction onComplete = null
        )
        {
            if (!IsPaused)
            {
                HandyLogger.Error(
                    $"{nameof(GameplayService)}",
                    "Trying to resume gameplay but it is not paused",
                    this
                );
                return;
            }

            if (!TryValidateInterruptionOwner(interruptionOwner, "resume gameplay"))
            {
                return;
            }

            if (!ReferenceEquals(_interruptionOwner, interruptionOwner))
            {
                HandyLogger.Warning(
                    $"{nameof(GameplayService)}",
                    $"Trying to resume gameplay with owner '{DescribeOwner(interruptionOwner)}' while the interruption is owned by '{DescribeOwner(_interruptionOwner)}'.",
                    this
                );
                return;
            }

            if (!TryBeginTransition(Status.On, "resume gameplay"))
            {
                return;
            }

            try
            {
                await GameplayTimeScaler.Unfreeze(duration);
                CompleteTransition(
                    Status.On,
                    GameplayTransitionOrigin.Resume,
                    null,
                    onComplete
                );
            }
            finally
            {
                CancelTransitionIfStillPending();
            }
        }

        #endregion

        #region Stopping

        /// <summary>
        /// Stops the gameplay, freezing the time and changing the status to <see cref="Status.Off"/>.
        /// Uses <see cref="GameplayTimeScaler.Freeze"/> to freeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not need to await the transition, discard the returned
        /// awaitable.
        /// </summary>
        /// <param name="duration">The duration of the freeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An awaitable that completes when the transition is over.</returns>
        public async Awaitable StopGameplay(float duration = 0, UnityAction onComplete = null)
        {
            if (IsOff)
            {
                HandyLogger.Error(
                    $"{nameof(GameplayService)}",
                    "Trying to stop gameplay but it is already off",
                    this
                );
                return;
            }

            if (!TryBeginTransition(Status.Off, "stop gameplay"))
            {
                return;
            }

            try
            {
                await GameplayTimeScaler.Freeze(duration);
                CompleteTransition(
                    Status.Off,
                    GameplayTransitionOrigin.Stop,
                    null,
                    onComplete
                );
            }
            finally
            {
                CancelTransitionIfStillPending();
            }
        }

        #endregion

        #region Transitions

        /// <summary>
        /// Attempts to reserve the gameplay service for one state transition.
        /// </summary>
        /// <param name="targetStatus">Status reached when the transition completes.</param>
        /// <param name="actionName">Diagnostic action name.</param>
        /// <returns>True when the transition can start.</returns>
        private bool TryBeginTransition(Status targetStatus, string actionName)
        {
            if (!_isTransitioning)
            {
                _isTransitioning = true;
                _transitionTargetStatus = targetStatus;
                return true;
            }

            HandyLogger.Error(
                $"{nameof(GameplayService)}",
                $"Trying to {actionName} while transitioning from {_status} to {_transitionTargetStatus}.",
                this
            );
            return false;
        }

        /// <summary>
        /// Finalizes one successful state transition and publishes the result.
        /// </summary>
        /// <param name="targetStatus">Reached gameplay status.</param>
        /// <param name="origin">Lifecycle operation that completed.</param>
        /// <param name="interruptionOwner">
        /// Owner associated with the completed interruption, when applicable.
        /// </param>
        /// <param name="onComplete">Optional callback invoked after the event.</param>
        private void CompleteTransition(
            Status targetStatus,
            GameplayTransitionOrigin origin,
            object interruptionOwner,
            UnityAction onComplete
        )
        {
            Status previousStatus = _status;
            GameplaySessionContext sessionContext = BuildCompletedSessionContext(
                origin
            );
            object previousInterruptionOwner = _interruptionOwner;
            object eventInterruptionOwner = ResolveEventInterruptionOwner(
                previousStatus,
                origin,
                previousInterruptionOwner,
                interruptionOwner
            );

            _status = targetStatus;
            _transitionTargetStatus = targetStatus;
            _isTransitioning = false;
            _interruptionOwner = ResolveInterruptionOwner(
                targetStatus,
                origin,
                interruptionOwner
            );

            if (targetStatus != Status.Off)
            {
                _currentSessionContext = sessionContext;
            }

            HandyBus<GameplayStatusChangeEvent>.Raise(
                new()
                {
                    PreviousStatus = previousStatus,
                    Status = _status,
                    Origin = origin,
                    SessionContext = sessionContext,
                    InterruptionOwner = eventInterruptionOwner,
                    InterruptionOwnerDescription = DescribeOwner(
                        eventInterruptionOwner
                    ),
                }
            );

            if (targetStatus == Status.Off)
            {
                _currentSessionContext = default;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Validates whether an interruption request provides a stable owner
        /// reference.
        /// </summary>
        /// <param name="interruptionOwner">Candidate interruption owner.</param>
        /// <param name="actionName">Diagnostic action name.</param>
        /// <returns>True when the owner can be used for ownership tracking.</returns>
        private bool TryValidateInterruptionOwner(
            object interruptionOwner,
            string actionName
        )
        {
            if (interruptionOwner != null)
            {
                return true;
            }

            HandyLogger.Error(
                $"{nameof(GameplayService)}",
                $"Trying to {actionName} without providing an interruption owner.",
                this
            );
            return false;
        }

        /// <summary>
        /// Resolves the interruption owner that should remain attached to the
        /// current stable gameplay state.
        /// </summary>
        /// <param name="targetStatus">Reached stable gameplay status.</param>
        /// <param name="origin">Lifecycle operation that completed.</param>
        /// <param name="interruptionOwner">Owner supplied by the operation.</param>
        /// <returns>The owner that remains attached to the current state.</returns>
        private object ResolveInterruptionOwner(
            Status targetStatus,
            GameplayTransitionOrigin origin,
            object interruptionOwner
        )
        {
            if (targetStatus == Status.Paused
                && origin == GameplayTransitionOrigin.Pause)
            {
                return interruptionOwner;
            }

            return null;
        }

        /// <summary>
        /// Resolves the interruption owner snapshot that should be published
        /// with the completed transition event.
        /// </summary>
        /// <param name="previousStatus">Stable status before the transition.</param>
        /// <param name="origin">Lifecycle operation that completed.</param>
        /// <param name="previousInterruptionOwner">
        /// Owner attached to the previously stable gameplay state.
        /// </param>
        /// <param name="interruptionOwner">
        /// Owner supplied by the transition request.
        /// </param>
        /// <returns>
        /// The interruption owner snapshot that listeners should observe.
        /// </returns>
        private static object ResolveEventInterruptionOwner(
            Status previousStatus,
            GameplayTransitionOrigin origin,
            object previousInterruptionOwner,
            object interruptionOwner
        )
        {
            return origin switch
            {
                GameplayTransitionOrigin.Pause => interruptionOwner,
                GameplayTransitionOrigin.Resume => previousInterruptionOwner,
                GameplayTransitionOrigin.Stop when previousStatus == Status.Paused
                    => previousInterruptionOwner,
                _ => null,
            };
        }

        /// <summary>
        /// Produces a short diagnostic description for one interruption owner.
        /// </summary>
        /// <param name="interruptionOwner">Owner object to describe.</param>
        /// <returns>Human-readable owner description.</returns>
        private static string DescribeOwner(object interruptionOwner)
        {
            if (interruptionOwner == null)
            {
                return "none";
            }

            if (interruptionOwner is UnityEngine.Object unityObject)
            {
                return $"{unityObject.name} ({unityObject.GetType().Name})";
            }

            return interruptionOwner.GetType().Name;
        }

        /// <summary>
        /// Builds the session context associated with one completed transition.
        /// </summary>
        /// <param name="origin">Lifecycle operation that completed.</param>
        /// <returns>The session context associated with the transition.</returns>
        private GameplaySessionContext BuildCompletedSessionContext(
            GameplayTransitionOrigin origin
        )
        {
            if (origin == GameplayTransitionOrigin.Start)
            {
                _sessionSequenceCounter++;
                return new GameplaySessionContext(
                    Guid.NewGuid(),
                    _sessionSequenceCounter,
                    1
                );
            }

            if (!_currentSessionContext.IsValid)
            {
                return default;
            }

            return new GameplaySessionContext(
                _currentSessionContext.SessionId,
                _currentSessionContext.SessionSequence,
                _currentSessionContext.TransitionSequence + 1
            );
        }

        /// <summary>
        /// Restores the idle transition state when a transition exits early.
        /// </summary>
        private void CancelTransitionIfStillPending()
        {
            if (!_isTransitioning)
            {
                return;
            }

            _transitionTargetStatus = _status;
            _isTransitioning = false;
        }

        #endregion


        #region Enums

        public enum Status
        {
            Off,
            On,
            Paused,
        }

        #endregion

        #region Debug

        [ContextMenu("Debug Start Gameplay")]
        public void DebugStart()
        {
            _ = StartGameplay();
        }

        [ContextMenu("Debug Pause Gameplay")]
        public void DebugPause()
        {
            _ = PauseGameplay(this);
        }

        [ContextMenu("Debug Resume Gameplay")]
        public void DebugResume()
        {
            _ = ResumeGameplay(this);
        }

        [ContextMenu("Debug Stop Gameplay")]
        public void DebugStop()
        {
            _ = StopGameplay();
        }

        #endregion
    }
}