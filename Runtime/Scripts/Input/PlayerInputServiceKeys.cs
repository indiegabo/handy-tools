using System;
using IndieGabo.HandyTools.HandyServiceLocator;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem
{
    /// <summary>
    /// Provides the canonical service identifiers used by the input module.
    /// </summary>
    public static class PlayerInputServiceKeys
    {
        #region Constants

        private const string _singlePlayerKey
            = "HandyInput/PlayerInput/SinglePlayer";

        private const string _playerIndexPrefix
            = "HandyInput/PlayerInput/Index:";

        private const string _playerIdPrefix
            = "HandyInput/PlayerInput/Id:";

        private const string _inputUserIdPrefix
            = "HandyInput/PlayerInput/InputUser:";

        #endregion

        #region Fields

        /// <summary>
        /// Gets the identifier reserved for the single-player input instance.
        /// </summary>
        public static readonly ServiceIdentifier SinglePlayer
            = ServiceIdentifier.Create(_singlePlayerKey);

        #endregion

        #region Factory

        /// <summary>
        /// Creates the canonical identifier for a player registered by index.
        /// </summary>
        /// <param name="playerIndex">Zero-based player index.</param>
        /// <returns>The canonical identifier for the requested player.</returns>
        public static ServiceIdentifier ForPlayerIndex(int playerIndex)
        {
            if (playerIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerIndex),
                    playerIndex,
                    "Player index cannot be negative."
                );
            }

            return ServiceIdentifier.Create(
                $"{_playerIndexPrefix}{playerIndex}"
            );
        }

        /// <summary>
        /// Creates the canonical identifier for a player registered by string ID.
        /// </summary>
        /// <param name="playerId">Stable string identifier for the player.</param>
        /// <returns>The canonical identifier for the requested player.</returns>
        public static ServiceIdentifier ForPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException(
                    "Player id cannot be null, empty, or whitespace.",
                    nameof(playerId)
                );
            }

            return ServiceIdentifier.Create(
                $"{_playerIdPrefix}{playerId}"
            );
        }

        /// <summary>
        /// Creates the canonical identifier for a player registered by
        /// InputUser id.
        /// </summary>
        /// <param name="inputUserId">Runtime InputUser identifier.</param>
        /// <returns>The canonical identifier for the requested player.</returns>
        public static ServiceIdentifier ForInputUserId(uint inputUserId)
        {
            if (inputUserId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inputUserId),
                    inputUserId,
                    "InputUser id must be greater than zero."
                );
            }

            return ServiceIdentifier.Create(
                $"{_inputUserIdPrefix}{inputUserId}"
            );
        }

        /// <summary>
        /// Creates the canonical identifier for a player registered by a
        /// persistent GUID.
        /// </summary>
        /// <param name="persistentGuid">Persistent GUID associated with the player.</param>
        /// <returns>The canonical identifier for the requested player.</returns>
        public static ServiceIdentifier ForPersistentGuid(Guid persistentGuid)
        {
            if (persistentGuid == Guid.Empty)
            {
                throw new ArgumentException(
                    "Persistent player GUID cannot be empty.",
                    nameof(persistentGuid)
                );
            }

            return ServiceIdentifier.Create(persistentGuid);
        }

        /// <summary>
        /// Creates the canonical identifier for an instantiated player input.
        /// </summary>
        /// <param name="playerInput">Player input instance used to infer the index.</param>
        /// <returns>The canonical identifier for the requested player.</returns>
        public static ServiceIdentifier ForPlayerInput(PlayerInput playerInput)
        {
            if (playerInput == null)
            {
                throw new ArgumentNullException(nameof(playerInput));
            }

            return ForPlayerIndex(playerInput.playerIndex);
        }

        /// <summary>
        /// Creates the full registration key set for one runtime player input.
        /// </summary>
        /// <param name="playerInput">Player input that owns the registration.</param>
        /// <param name="persistentGuid">Persistent GUID associated with the player.</param>
        /// <returns>The canonical identifiers used to register the player.</returns>
        public static PlayerInputServiceRegistrationKeys CreateRegistration(
            PlayerInput playerInput,
            Guid persistentGuid
        )
        {
            if (playerInput == null)
            {
                throw new ArgumentNullException(nameof(playerInput));
            }

            if (!playerInput.user.valid || playerInput.user.id == 0)
            {
                throw new InvalidOperationException(
                    "PlayerInput must expose a valid InputUser id before registration."
                );
            }

            return new PlayerInputServiceRegistrationKeys(
                playerInput.playerIndex,
                playerInput.user.id,
                persistentGuid
            );
        }

        #endregion
    }

    /// <summary>
    /// Stores the canonical service identifiers associated with one
    /// PlayerInput registration.
    /// </summary>
    public readonly struct PlayerInputServiceRegistrationKeys
    {
        /// <summary>
        /// Initializes the registration key set for one player input.
        /// </summary>
        /// <param name="playerIndex">PlayerInput.playerIndex value.</param>
        /// <param name="inputUserId">InputUser.id value.</param>
        /// <param name="persistentGuid">Persistent GUID assigned to the player.</param>
        public PlayerInputServiceRegistrationKeys(
            int playerIndex,
            uint inputUserId,
            Guid persistentGuid
        )
        {
            PlayerIndex = playerIndex;
            InputUserId = inputUserId;
            PersistentGuid = persistentGuid;
            ByPlayerIndex = PlayerInputServiceKeys.ForPlayerIndex(playerIndex);
            ByInputUserId = PlayerInputServiceKeys.ForInputUserId(inputUserId);
            ByPersistentGuid = PlayerInputServiceKeys.ForPersistentGuid(
                persistentGuid
            );
        }

        /// <summary>
        /// Gets the player index associated with the registration.
        /// </summary>
        public int PlayerIndex { get; }

        /// <summary>
        /// Gets the InputUser id associated with the registration.
        /// </summary>
        public uint InputUserId { get; }

        /// <summary>
        /// Gets the persistent GUID associated with the registration.
        /// </summary>
        public Guid PersistentGuid { get; }

        /// <summary>
        /// Gets the identifier used to resolve the player by index.
        /// </summary>
        public ServiceIdentifier ByPlayerIndex { get; }

        /// <summary>
        /// Gets the identifier used to resolve the player by InputUser id.
        /// </summary>
        public ServiceIdentifier ByInputUserId { get; }

        /// <summary>
        /// Gets the identifier used to resolve the player by persistent GUID.
        /// </summary>
        public ServiceIdentifier ByPersistentGuid { get; }
    }
}