using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
{
    /// <summary>
    /// Stores configurable gameplay module policies shared by runtime owners.
    /// </summary>
    [GlobalConfig("Resources/Gameplay")]
    public sealed class GameplayConfig : HandyGlobalConfig<GameplayConfig>
    {
        [SerializeField]
        private GameplayTimePersistenceStrategy _timePersistenceStrategy =
            GameplayTimePersistenceStrategy.LocalUserData;

        /// <summary>
        /// Gets or sets the strategy used to persist accumulated gameplay time.
        /// </summary>
        public GameplayTimePersistenceStrategy TimePersistenceStrategy
        {
            get => _timePersistenceStrategy;
            set => SetFieldValue(nameof(_timePersistenceStrategy), value);
        }
    }

    /// <summary>
    /// Defines the available gameplay time persistence strategies.
    /// </summary>
    public enum GameplayTimePersistenceStrategy
    {
        LocalUserData,
        SaveSystem,
    }
}