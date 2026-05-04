using Cysharp.Threading.Tasks;
using IndieGabo.HandyTools.HandyBus;
using IndieGabo.HandyTools.Logger;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.Gameplay
{
    public class GameplayService : HandyBehaviour
    {
        #region Inspector

        #endregion

        #region Fields

        private Status _status = Status.Off;

        #endregion

        #region Getters

        public bool IsOn => _status == Status.On;
        public bool IsOff => _status == Status.Off;
        public bool IsPaused => _status == Status.Paused;

        #endregion

        #region Behaviour

        #endregion

        #region Starting


        /// <summary>
        /// Starts the gameplay, unfreezing the time and changing the status to <see cref="Status.On"/>.
        /// Uses <see cref="GameplayTimeScaler.Unfreeze"/> to unfreeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not want to await the transition you can chain this method with <see cref="UniTask.Forget"/> 
        /// </summary>
        /// <param name="duration">The duration of the unfreeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An asynchronous task that completes when the transition is over.</returns>
        public async UniTask StartGameplay(float duration = 0, UnityAction onComplete = null)
        {
            if (IsOn)
            {
                HandyLogger.Error(
                    $"{nameof(GameplayService)}",
                    "Trying to start or resume gameplay but it is already on",
                    this
                );
                return;
            }

            await GameplayTimeScaler.Unfreeze(duration);

            _status = Status.On;
            EventBus<GameplayStatusChangeEvent>.Raise(new() { Status = _status });
            onComplete?.Invoke();
        }

        #endregion

        #region Pausing and Resuming

        /// <summary>
        /// Pauses the gameplay, freezing the time and changing the status to <see cref="Status.Paused"/>.
        /// Uses <see cref="GameplayTimeScaler.Freeze"/> to freeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not want to await the transition you can chain this method with <see cref="UniTask.Forget"/> 
        /// </summary>
        /// <param name="duration">The duration of the freeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An asynchronous task that completes when the transition is over.</returns>
        public async UniTask PauseGameplay(float duration = 0, UnityAction onComplete = null)
        {
            if (IsPaused)
            {
                HandyLogger.Error(
                    $"{nameof(GameplayService)}",
                    "Trying to pause gameplay but it is already paused",
                    this
                );
                return;
            }

            await GameplayTimeScaler.Freeze(duration);

            _status = Status.Paused;
            EventBus<GameplayStatusChangeEvent>.Raise(new() { Status = _status });
            onComplete?.Invoke();
        }

        /// <summary>
        /// Resumes the gameplay, unfreezing the time and changing the status to <see cref="Status.On"/>.
        /// Uses <see cref="GameplayTimeScaler.Unfreeze"/> to unfreeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not want to await the transition you can chain this method with <see cref="UniTask.Forget"/> 
        /// </summary>
        /// <param name="duration">The duration of the unfreeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An asynchronous task that completes when the transition is over.</returns>
        public async UniTask ResumeGameplay(float duration = 0, UnityAction onComplete = null)
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

            await StartGameplay(duration, onComplete);
        }

        #endregion

        #region Stopping

        /// <summary>
        /// Stops the gameplay, freezing the time and changing the status to <see cref="Status.Off"/>.
        /// Uses <see cref="GameplayTimeScaler.Freeze"/> to freeze the time.
        /// If the duration is 0, it will transition instantly.
        /// If you do not want to await the transition you can chain this method with <see cref="UniTask.Forget"/> 
        /// </summary>
        /// <param name="duration">The duration of the freeze transition in seconds.</param>
        /// <param name="onComplete">The action to invoke when the transition is over.</param>
        /// <returns>An asynchronous task that completes when the transition is over.</returns>
        public async UniTask StopGameplay(float duration = 0, UnityAction onComplete = null)
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

            await GameplayTimeScaler.Freeze(duration);

            _status = Status.Off;
            EventBus<GameplayStatusChangeEvent>.Raise(new() { Status = _status });
            onComplete?.Invoke();
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
            _ = PauseGameplay();
        }

        [ContextMenu("Debug Resume Gameplay")]
        public void DebugResume()
        {
            _ = ResumeGameplay();
        }

        [ContextMenu("Debug Stop Gameplay")]
        public void DebugStop()
        {
            _ = StopGameplay();
        }

        #endregion
    }
}