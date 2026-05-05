using IndieGabo.HandyTools.HandyBus;
using IndieGabo.HandyTools.HandyServiceLocator;
using IndieGabo.HandyTools.SaveSystem;
using UnityEngine;

namespace IndieGabo.HandyTools.Gameplay
{
    /// <summary>
    /// Tracks active gameplay time and persists it according to the configured
    /// gameplay persistence strategy.
    /// </summary>
    public class GameplayTimeRegisterer : HandyBehaviour
    {
        #region Fields

        private EventSubscription<GameplayStatusChangeEvent>
            _gameplayEventSubscription;
        private float _currentStartedAt = -1;
        private LoadedSlotService _loadedSlotHandler;

        #endregion

        #region Behaviour

        private void OnEnable()
        {
            GameplayConfig.ReloadInstance();
            _gameplayEventSubscription = EventBus<GameplayStatusChangeEvent>
                .Subscribe(OnGameplayEvent);
        }

        private void OnDisable()
        {
            _gameplayEventSubscription.Dispose();
        }

        #endregion

        #region Events

        /// <summary>
        /// Listens to <see cref="GameplayStatusChangeEvent"/>s and handles the
        /// gameplay time accordingly.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <remarks>
        /// When the transition origin is <see cref="GameplayTransitionOrigin.Start"/>
        /// or <see cref="GameplayTransitionOrigin.Resume"/>, this method stores
        /// the current time in <see cref="_currentStartedAt"/>.
        /// When the transition origin is <see cref="GameplayTransitionOrigin.Pause"/>
        /// or <see cref="GameplayTransitionOrigin.Stop"/>, this method registers
        /// the elapsed gameplay time, then resets <see cref="_currentStartedAt"/>
        /// to -1.
        /// </remarks>
        private void OnGameplayEvent(GameplayStatusChangeEvent @event)
        {
            if (@event.Origin == GameplayTransitionOrigin.Start
                || @event.Origin == GameplayTransitionOrigin.Resume)
            {
                _currentStartedAt = Time.time;
                return;
            }

            if (@event.Origin != GameplayTransitionOrigin.Pause
                && @event.Origin != GameplayTransitionOrigin.Stop)
            {
                return;
            }

            if (_currentStartedAt < 0)
            {
                return;
            }

            float timeElapsed = Time.time - _currentStartedAt;
            _currentStartedAt = -1;
            PersistGameplayTime(timeElapsed);
        }

        private void PersistGameplayTime(float timeElapsed)
        {
            if (timeElapsed <= 0f)
            {
                return;
            }

            GameplayTimePersistenceStrategy strategy =
                ResolvePersistenceStrategy();

            if (strategy == GameplayTimePersistenceStrategy.LocalUserData)
            {
                GameplayLocalUserData.RegisterGameplayTime(timeElapsed);
                return;
            }

            if (_loadedSlotHandler == null
                && !ServiceLocator.TryGet(out _loadedSlotHandler))
            {
                GameplayLocalUserData.RegisterGameplayTime(timeElapsed);
                return;
            }

            if (!_loadedSlotHandler.HasLoadedSlot)
            {
                return;
            }

            _loadedSlotHandler.RegisterGameplayTime(timeElapsed);
        }

        private static GameplayTimePersistenceStrategy ResolvePersistenceStrategy()
        {
            GameplayTimePersistenceStrategy configuredStrategy =
                GameplayConfig.Instance.TimePersistenceStrategy;

            if (configuredStrategy == GameplayTimePersistenceStrategy.SaveSystem
                && !SaveSystemModuleDefinition.IsActive)
            {
                return GameplayTimePersistenceStrategy.LocalUserData;
            }

            return configuredStrategy;
        }

        #endregion
    }
}
