using IndieGabo.HandyTools.HandyBus;
using IndieGabo.HandyTools.HandyServiceLocator;
using IndieGabo.HandyTools.SaveSystem;
using UnityEngine;

namespace IndieGabo.HandyTools.Gameplay
{
    public class GameplayTimeRegisterer : HandyBehaviour
    {
        #region Fields

        private EventBinding<GameplayStatusChangeEvent> _gameplayEventBinding;
        private float _currentStartedAt = -1;
        private LoadedSlotService _loadedSlotHandler;

        #endregion

        #region Behaviour

        private void Awake()
        {
            _gameplayEventBinding = new EventBinding<GameplayStatusChangeEvent>(OnGameplayEvent);
        }

        private void OnEnable()
        {
            EventBus<GameplayStatusChangeEvent>.Register(_gameplayEventBinding);
        }

        private void OnDisable()
        {
            EventBus<GameplayStatusChangeEvent>.Deregister(_gameplayEventBinding);
        }

        #endregion

        #region Events

        /// <summary>
        /// Listens to <see cref="GameplayStatusChangeEvent"/>s and handles the gameplay time accordingly.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <remarks>
        /// When the <see cref="GameplayStatusChangeEvent.Status"/> is <see cref="GameplayService.Status.On"/>,
        /// this method stores the current time in <see cref="_currentStartedAt"/>.
        /// When the <see cref="GameplayStatusChangeEvent.Status"/> is not <see cref="GameplayService.Status.On"/>,
        /// this method will register the gameplay time by calculating the difference between
        /// the current time and the stored <see cref="_currentStartedAt"/>, then will reset
        /// <see cref="_currentStartedAt"/> to -1.
        /// </remarks>
        private void OnGameplayEvent(GameplayStatusChangeEvent @event)
        {
            // If the game is being started, store the current time in _currentStartedAt
            if (@event.Status == GameplayService.Status.On)
            {
                _currentStartedAt = Time.time;
                return;
            }

            if (_loadedSlotHandler == null)
            {
                ServiceLocator.Global.Get(out _loadedSlotHandler);
            }

            // If the game is being paused or stopped, register the gameplay time
            if (_currentStartedAt < 0 || !_loadedSlotHandler.HasLoadedSlot) return;

            var timeElapsed = Time.time - _currentStartedAt;
            _loadedSlotHandler.RegisterGameplayTime(timeElapsed);
            _currentStartedAt = -1;
        }

        #endregion
    }
}