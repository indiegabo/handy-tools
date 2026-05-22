using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.SaveSystemModule;
using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
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
        private float _currentStartedAt = float.NaN;
        private LoadedSlotService _loadedSlotHandler;

        #endregion

        #region Behaviour

        private void OnEnable()
        {
            GameplayConfig.ReloadInstance();
            _gameplayEventSubscription = HandyBus<GameplayStatusChangeEvent>
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
        /// to an unset sentinel.
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

            if (float.IsNaN(_currentStartedAt))
            {
                return;
            }

            float timeElapsed = Time.time - _currentStartedAt;
            _currentStartedAt = float.NaN;
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
