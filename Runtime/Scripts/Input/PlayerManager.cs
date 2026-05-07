using System;
using System.Collections.Generic;
using System.Reflection;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.LoggerModule;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace IndieGabo.HandyTools.HandyInputSystemModule
{
    [RequireComponent(typeof(PlayerInputManager))]
    /// <summary>
    /// Coordinates single-player and multiplayer PlayerInput lifecycle.
    /// </summary>
    public class PlayerManager : HandyBehaviour
    {
        #region Static

        private const string _maxPlayerCountFieldName = "m_MaxPlayerCount";

        private static FieldInfo _cachedMaxPlayerCountField;
        private static bool _didResolveMaxPlayerCountField;

        #endregion

        #region Inspector

        [SerializeField]
        private PlayerInput _playerInputPrefab;

        [SerializeField]
        private MultiplayerModeOptions _defaultMultiplayerOptions;

        #endregion

        #region Fields

        private PlayerInputManager _playerInputManager;
        private Mode _currentMode = Mode.SinglePlayer;
        private PlayerInput _singlePlayerInput;

        // Multiplayer registry by index.
        private readonly Dictionary<int, PlayerInput> _playerInputsRegistry
            = new();

        private readonly Dictionary<PlayerInput, PlayerInputServiceRegistrationKeys>
            _playerInputServiceRegistrations = new();

        // Device → Player mapping to prevent duplicate joins per device.
        private readonly Dictionary<int, PlayerInput> _deviceToPlayer
            = new();

        // Track the current shared keyboard/mouse owner instead of a bool.
        private PlayerInput _keyboardOwner;

        private readonly List<PlayerInput> _playerBuffer = new();
        private readonly List<InputDevice> _deviceBuffer = new();

        #endregion

        #region Properties

        private ProjectInputConfig Config => ProjectInputConfig.Get();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playerInputManager = GetComponent<PlayerInputManager>();

            if (_playerInputPrefab == null)
            {
                HandyLogger.Error(
                    $"{nameof(PlayerManager)}",
                    $"{nameof(PlayerInput)} prefab is not set.",
                    this
                );
                enabled = false;
                return;
            }

            HackManagerLimit();

            _playerInputManager.notificationBehavior
                = PlayerNotifications.InvokeUnityEvents;

            SetupSinglePlayer();

            if (_playerInputPrefab != null)
            {
                _playerInputManager.playerPrefab
                    = _playerInputPrefab.gameObject;
            }

            EnterSinglePlayerMode();

            ServiceLocator.Register(this);
            ServiceLocator.Register(_playerInputManager);
            ServiceLocator.Register(
                PlayerInputServiceKeys.SinglePlayer,
                _singlePlayerInput
            );
        }

        private void OnDestroy()
        {
            DeregisterTrackedPlayerServices();

            if (_playerInputManager != null)
            {
                DisableMultiplayerCallbacks();
            }

            ServiceLocator.Deregister(this);
            ServiceLocator.Deregister(_playerInputManager);
            ServiceLocator.Deregister<PlayerInput>(
                PlayerInputServiceKeys.SinglePlayer
            );
        }

        #endregion

        #region SinglePlayer Setup

        /// <summary>
        /// Creates a hidden PlayerInput for single-player mode.
        /// </summary>
        private void SetupSinglePlayer()
        {
            _singlePlayerInput = Instantiate(
                _playerInputPrefab,
                transform
            );

            _singlePlayerInput.gameObject.name
                = "PlayerInput [Single Player]";

            _singlePlayerInput.neverAutoSwitchControlSchemes = false;
            _singlePlayerInput.gameObject.SetActive(false);
        }

        #endregion

        #region Mode Switching API

        protected void ChangeMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.SinglePlayer:
                    EnterSinglePlayerMode();
                    break;
                case Mode.Multiplayer:
                    EnterMultiplayerMode();
                    break;
            }
        }

        /// <summary>
        /// Switch to single-player mode. Clears multiplayer players,
        /// disables joining, and re-enables the single input.
        /// </summary>
        public void EnterSinglePlayerMode()
        {
            CopyRegisteredPlayers(_playerBuffer);

            for (int i = 0; i < _playerBuffer.Count; i++)
            {
                DeregisterPlayerServiceRegistrations(_playerBuffer[i]);
            }

            _playerInputsRegistry.Clear();
            _deviceToPlayer.Clear();
            _keyboardOwner = null;

            for (int i = 0; i < _playerBuffer.Count; i++)
            {
                PlayerInput pi = _playerBuffer[i];
                if (pi != null)
                {
                    Destroy(pi.gameObject);
                }
            }

            _playerBuffer.Clear();

            DisableMultiplayerCallbacks();

            _singlePlayerInput.gameObject.SetActive(true);
            _singlePlayerInput.neverAutoSwitchControlSchemes = false;

            gameObject.name = $"{nameof(PlayerManager)} [Single Player]";
            _currentMode = Mode.SinglePlayer;
        }

        /// <summary>
        /// Switch to multiplayer mode. Deactivates single input, sets
        /// join-by-action, enables joining, and unpairs single devices.
        /// </summary>
        public void EnterMultiplayerMode(MultiplayerModeOptions options = null)
        {
            MultiplayerModeOptions resolved = options ?? _defaultMultiplayerOptions;

            _singlePlayerInput.gameObject.SetActive(false);

            _playerInputManager.splitScreen = false;
            _playerInputManager.joinBehavior
                = PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;

            _playerInputManager.joinAction = resolved.joinAction;

            _playerInputsRegistry.Clear();
            _deviceToPlayer.Clear();
            _keyboardOwner = null;

            TryUnpairSinglePlayerDevices();

            DisableMultiplayerCallbacks();

            _playerInputManager.playerJoinedEvent.AddListener(
                OnPlayerJoined
            );
            _playerInputManager.playerLeftEvent.AddListener(
                OnPlayerLeft
            );

            _playerInputManager.EnableJoining();

            gameObject.name = $"{nameof(PlayerManager)} [Multiplayer]";
            _currentMode = Mode.Multiplayer;
        }

        #endregion

        #region PlayerInputManager Callbacks

        /// <summary>
        /// Called by PlayerInputManager when a player joins. Locks the
        /// PlayerInput to the device used and enforces one player per device.
        /// For keyboard/mouse, enforces a single shared owner and, if
        /// already owned, re-pairs Keyboard/Mouse back to the owner before
        /// rejecting the new join to avoid stealing the device.
        /// </summary>
        public void OnPlayerJoined(PlayerInput playerInput)
        {
            if (_singlePlayerInput == playerInput)
            {
                return;
            }

            var device = ResolveJoiningDevice(playerInput);

            // Fallback: if device cannot be resolved, reject this join.
            if (device == null)
            {
                HandyLogger.Error(
                    $"{nameof(PlayerManager)}",
                    "Join rejected: could not resolve joining device.",
                    this
                );
                SafeRejectJoin(playerInput);
                return;
            }

            if (!HasValidInputUser(playerInput))
            {
                HandyLogger.Error(
                    $"{nameof(PlayerManager)}",
                    "Join rejected: PlayerInput does not expose a valid InputUser id.",
                    this
                );
                SafeRejectJoin(playerInput);
                return;
            }

            // Handle shared Keyboard/Mouse ownership.
            bool isKb = device is Keyboard;
            bool isMs = device is Mouse;
            if (isKb || isMs)
            {
                if (_keyboardOwner != null &&
                    _keyboardOwner != playerInput)
                {
                    // Already owned: restore pairing to the current owner
                    // before destroying the newcomer, to avoid device theft.
                    RebindKeyboardMouseTo(_keyboardOwner);

                    HandyLogger.Error(
                        $"{nameof(PlayerManager)}",
                        "Join rejected: keyboard/mouse already assigned.",
                        this
                    );
                    SafeRejectJoin(playerInput);
                    return;
                }

                // First KB/M owner: claim both Keyboard and Mouse.
                _keyboardOwner = playerInput;
                LockPlayerToKeyboardMouse(playerInput);
            }
            else
            {
                // Enforce 1 player per physical non-KB device (e.g., Gamepad).
                if (_deviceToPlayer.ContainsKey(device.deviceId))
                {
                    HandyLogger.Error(
                        $"{nameof(PlayerManager)}",
                        $"Join rejected: device already in use " +
                        $"(id={device.deviceId}).",
                        this
                    );
                    SafeRejectJoin(playerInput);
                    return;
                }

                // Lock this PlayerInput to the device that joined.
                LockPlayerToDevice(playerInput, device);
                _deviceToPlayer[device.deviceId] = playerInput;
            }

            if (!TryRegisterPlayerServiceRegistrations(playerInput))
            {
                CleanupMappings(playerInput);
                SafeRejectJoin(playerInput);
                return;
            }

            _playerInputsRegistry[playerInput.playerIndex] = playerInput;

            Guid persistentGuid = Guid.Empty;
            if (TryGetPlayerServiceRegistration(
                playerInput,
                out PlayerInputServiceRegistrationKeys registrationKeys
            ))
            {
                persistentGuid = registrationKeys.PersistentGuid;
            }

            playerInput.name
                = $"PlayerInput [{playerInput.playerIndex}] " +
                  $"({device.displayName}#{device.deviceId})";

            playerInput.transform.SetParent(transform);
            playerInput.notificationBehavior
                = PlayerNotifications.InvokeUnityEvents;

            HandyBus<PlayerJoinedEvent>.Raise(
                new PlayerJoinedEvent()
                {
                    playerIndex = playerInput.playerIndex,
                    persistentGuid = persistentGuid,
                    playerInput = playerInput
                }
            );
        }

        /// <summary>
        /// Called by PlayerInputManager when a player leaves.
        /// Cleans internal registries and raises PlayerLeftEvent.
        /// NOTE: the GameObject was already destroyed by the remover.
        /// </summary>
        public void OnPlayerLeft(PlayerInput playerInput)
        {
            if (_singlePlayerInput == playerInput)
            {
                return;
            }

            Guid persistentGuid = Guid.Empty;
            if (TryGetPlayerServiceRegistration(
                playerInput,
                out PlayerInputServiceRegistrationKeys registrationKeys
            ))
            {
                persistentGuid = registrationKeys.PersistentGuid;
            }

            // Remove device mappings for all paired devices.
            var devices = playerInput != null && playerInput.user.valid
                ? playerInput.user.pairedDevices
                : playerInput != null ? playerInput.devices
                : Array.Empty<InputDevice>();

            foreach (var dev in devices)
            {
                if (dev != null)
                {
                    _deviceToPlayer.Remove(dev.deviceId);
                }
            }

            // If the leaving PI was the keyboard owner, release the ownership.
            if (playerInput != null && playerInput == _keyboardOwner)
            {
                _keyboardOwner = null;
            }

            if (playerInput != null)
            {
                DeregisterPlayerServiceRegistrations(playerInput);
                _playerInputsRegistry.Remove(playerInput.playerIndex);
            }

            HandyBus<PlayerLeftEvent>.Raise(
                new PlayerLeftEvent()
                {
                    playerIndex = playerInput != null
                        ? playerInput.playerIndex
                        : -1,
                    persistentGuid = persistentGuid,
                    playerInput = playerInput
                }
            );
        }

        #endregion

        #region Public Helpers: Player Queries

        /// <summary>
        /// Returns multiplayer players into 'players' if in multiplayer mode.
        /// </summary>
        public bool TryGetAllPlayers(out List<PlayerInput> players)
        {
            if (_currentMode != Mode.Multiplayer)
            {
                HandyLogger.Error(
                    $"{nameof(PlayerManager)} ",
                    $"Trying to get all players but current mode is " +
                    $"{nameof(Mode.SinglePlayer)}",
                    this
                );
                players = default;
                return false;
            }

            players = new List<PlayerInput>(_playerInputsRegistry.Count);
            CopyRegisteredPlayers(players);
            return true;
        }

        /// <summary>
        /// Copies all multiplayer players into the provided results list.
        /// </summary>
        /// <param name="results">Destination list to populate.</param>
        /// <returns>True when the current mode is multiplayer.</returns>
        public bool TryGetAllPlayers(List<PlayerInput> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (_currentMode != Mode.Multiplayer)
            {
                return false;
            }

            CopyRegisteredPlayers(results);
            return true;
        }

        /// <summary>
        /// Returns all active PlayerInput instances including single-player.
        /// </summary>
        public List<PlayerInput> GetAllActivePlayers()
        {
            List<PlayerInput> players = new();
            GetAllActivePlayers(players);
            return players;
        }

        /// <summary>
        /// Copies all active PlayerInput instances into the provided list.
        /// </summary>
        /// <param name="results">Destination list to populate.</param>
        public void GetAllActivePlayers(List<PlayerInput> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            if (_currentMode == Mode.SinglePlayer)
            {
                if (_singlePlayerInput != null)
                {
                    results.Add(_singlePlayerInput);
                }

                return;
            }

            CopyRegisteredPlayers(results);
        }

        /// <summary>
        /// Resolves the PlayerInput reserved for the single-player flow.
        /// </summary>
        /// <returns>The single-player PlayerInput managed by this instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this manager is not running in single-player mode or
        /// when the single-player input is not initialized correctly.
        /// </exception>
        public PlayerInput GetRequiredSinglePlayerInput()
        {
            if (_currentMode != Mode.SinglePlayer)
            {
                throw new InvalidOperationException(
                    $"Trying to get the single-player {nameof(PlayerInput)} but "
                    + $"current mode is {_currentMode}."
                );
            }

            if (_singlePlayerInput == null || _singlePlayerInput.actions == null)
            {
                throw new InvalidOperationException(
                    $"The single-player {nameof(PlayerInput)} is not initialized "
                    + "correctly."
                );
            }

            return _singlePlayerInput;
        }

        /// <summary>
        /// Attempts to get a player by index (multiplayer only).
        /// </summary>
        public bool TryGetPlayer(int playerIndex, out PlayerInput playerInput)
        {
            if (_currentMode != Mode.Multiplayer)
            {
                throw new InvalidOperationException(
                    $"Trying to get player {playerIndex} but current mode is " +
                    $"{nameof(Mode.SinglePlayer)}"
                );
            }

            return _playerInputsRegistry.TryGetValue(
                playerIndex,
                out playerInput
            );
        }

        /// <summary>
        /// Attempts to get a player by InputUser id (multiplayer only).
        /// </summary>
        /// <param name="inputUserId">InputUser.id value associated with the player.</param>
        /// <param name="playerInput">Resolved player input when found.</param>
        /// <returns>True when a matching player exists.</returns>
        public bool TryGetPlayerByInputUserId(
            uint inputUserId,
            out PlayerInput playerInput
        )
        {
            if (_currentMode != Mode.Multiplayer)
            {
                throw new InvalidOperationException(
                    $"Trying to get player user {inputUserId} but current mode is "
                    + $"{nameof(Mode.SinglePlayer)}"
                );
            }

            return ServiceLocator.TryGet<PlayerInput>(
                PlayerInputServiceKeys.ForInputUserId(inputUserId),
                out playerInput
            );
        }

        /// <summary>
        /// Attempts to get a player by persistent GUID (multiplayer only).
        /// </summary>
        /// <param name="persistentGuid">Persistent GUID associated with the player.</param>
        /// <param name="playerInput">Resolved player input when found.</param>
        /// <returns>True when a matching player exists.</returns>
        public bool TryGetPlayerByPersistentGuid(
            Guid persistentGuid,
            out PlayerInput playerInput
        )
        {
            if (_currentMode != Mode.Multiplayer)
            {
                throw new InvalidOperationException(
                    $"Trying to get player GUID {persistentGuid} but current mode is "
                    + $"{nameof(Mode.SinglePlayer)}"
                );
            }

            return ServiceLocator.TryGet<PlayerInput>(
                PlayerInputServiceKeys.ForPersistentGuid(persistentGuid),
                out playerInput
            );
        }

        /// <summary>
        /// Attempts to resolve the registration keys tracked for one player.
        /// </summary>
        /// <param name="playerInput">Player input to inspect.</param>
        /// <param name="registrationKeys">Tracked registration keys when found.</param>
        /// <returns>True when the player is currently tracked.</returns>
        public bool TryGetPlayerServiceRegistration(
            PlayerInput playerInput,
            out PlayerInputServiceRegistrationKeys registrationKeys
        )
        {
            if (playerInput != null
                && _playerInputServiceRegistrations.TryGetValue(
                    playerInput,
                    out registrationKeys
                ))
            {
                return true;
            }

            registrationKeys = default;
            return false;
        }

        #endregion

        #region Public Helpers: Forced Removal

        /// <summary>
        /// Force a player to leave: unpairs devices, cleans mappings,
        /// and destroys the PlayerInput GameObject. This will cause
        /// PlayerInputManager to fire playerLeftEvent → OnPlayerLeft.
        /// </summary>
        public void RequestPlayerLeave(PlayerInput playerInput)
        {
            if (playerInput == null || playerInput == _singlePlayerInput)
            {
                return;
            }

            try
            {
                // Pre-clean mappings to be safe/idempotent.
                CleanupMappings(playerInput);

                // Destroying triggers PlayerInputManager.playerLeftEvent.
                Destroy(playerInput.gameObject);
            }
            catch (Exception ex)
            {
                HandyLogger.Error(
                    $"{nameof(PlayerManager)}",
                    $"RequestPlayerLeave error: {ex.Message}",
                    this
                );
            }
        }

        /// <summary>
        /// Force removal by device (e.g., from an action callback).
        /// </summary>
        public void RequestPlayerLeaveByDevice(InputDevice device)
        {
            if (device == null) return;

            if (_deviceToPlayer.TryGetValue(device.deviceId, out var pi))
            {
                RequestPlayerLeave(pi);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Unpairs all devices from the single-player user to allow join.
        /// </summary>
        protected void TryUnpairSinglePlayerDevices()
        {
            try
            {
                var single = _singlePlayerInput;
                if (single == null)
                {
                    return;
                }

                var user = single.user;
                if (user.valid)
                {
                    user.UnpairDevices();
                }
            }
            catch (Exception ex)
            {
                HandyLogger.Error(
                    $"{nameof(PlayerManager)}",
                    ex.Message,
                    this
                );
            }
        }

        /// <summary>
        /// Reflection hack to set private max-player count field.
        /// </summary>
        protected virtual void HackManagerLimit()
        {
            FieldInfo field = ResolveMaxPlayerCountField();

            if (field == null)
            {
                HandyLogger.Warning(
                    $"{nameof(PlayerManager)}",
                    $"Field {_maxPlayerCountFieldName} not found in " +
                    $"{nameof(PlayerInputManager)}",
                    this
                );
                return;
            }

            field.SetValue(
                _playerInputManager,
                Config.MaxNumberOfPlayers
            );
        }

        private static FieldInfo ResolveMaxPlayerCountField()
        {
            if (_didResolveMaxPlayerCountField)
            {
                return _cachedMaxPlayerCountField;
            }

            _didResolveMaxPlayerCountField = true;
            _cachedMaxPlayerCountField = typeof(PlayerInputManager).GetField(
                _maxPlayerCountFieldName,
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
            );

            return _cachedMaxPlayerCountField;
        }

        /// <summary>
        /// Resolves the most likely device that initiated the join.
        /// Prefers Gamepad; otherwise returns Keyboard or Mouse.
        /// </summary>
        private static InputDevice ResolveJoiningDevice(PlayerInput pi)
        {
            IReadOnlyList<InputDevice> paired = pi.user.valid
                ? pi.user.pairedDevices
                : pi.devices;

            InputDevice firstDevice = null;
            bool hasKeyboard = false;
            bool hasMouse = false;

            for (int i = 0; i < paired.Count; i++)
            {
                InputDevice device = paired[i];
                if (device == null)
                {
                    continue;
                }

                firstDevice ??= device;
                if (device is Gamepad)
                {
                    return device;
                }

                if (device is Keyboard)
                {
                    hasKeyboard = true;
                    continue;
                }

                if (device is Mouse)
                {
                    hasMouse = true;
                }
            }

            if (hasKeyboard && Keyboard.current != null)
            {
                return Keyboard.current;
            }

            if (hasMouse && Mouse.current != null)
            {
                return Mouse.current;
            }

            return firstDevice;
        }

        /// <summary>
        /// Determines whether a PlayerInput exposes a valid InputUser id.
        /// </summary>
        /// <param name="playerInput">Player input to inspect.</param>
        /// <returns>True when the player has a valid InputUser id.</returns>
        private static bool HasValidInputUser(PlayerInput playerInput)
        {
            return playerInput != null
                && playerInput.user.valid
                && playerInput.user.id != 0;
        }

        /// <summary>
        /// Locks a PlayerInput to a single non-keyboard device and disables
        /// auto-switch.
        /// </summary>
        private void LockPlayerToDevice(PlayerInput pi, InputDevice device)
        {
            pi.neverAutoSwitchControlSchemes = true;

            if (pi.user.valid)
            {
                UnpairPairedDevices(pi);

                // Pair explicitly with the given device.
                InputUser.PerformPairingWithDevice(
                    device,
                    pi.user,
                    InputUserPairingOptions.None
                );
            }
            else
            {
                // Fallback when no valid user exists.
                pi.SwitchCurrentControlScheme(device);
            }
        }

        /// <summary>
        /// Locks a PlayerInput to both Keyboard and Mouse as a single owner.
        /// </summary>
        private void LockPlayerToKeyboardMouse(PlayerInput pi)
        {
            pi.neverAutoSwitchControlSchemes = true;

            var kb = Keyboard.current;
            var ms = Mouse.current;

            if (pi.user.valid)
            {
                UnpairPairedDevices(pi);

                if (kb != null)
                {
                    InputUser.PerformPairingWithDevice(kb, pi.user);
                }
                if (ms != null)
                {
                    InputUser.PerformPairingWithDevice(ms, pi.user);
                }
            }
            else
            {
                if (kb != null && ms != null)
                {
                    pi.SwitchCurrentControlScheme(kb, ms);
                }
                else if (kb != null)
                {
                    pi.SwitchCurrentControlScheme(kb);
                }
                else if (ms != null)
                {
                    pi.SwitchCurrentControlScheme(ms);
                }
            }
        }

        /// <summary>
        /// Re-pairs the Keyboard and Mouse devices back to the provided owner.
        /// Used when rejecting a duplicate KB/M join so the original player
        /// keeps control and does not lose the devices.
        /// </summary>
        private void RebindKeyboardMouseTo(PlayerInput owner)
        {
            if (owner == null) return;

            var kb = Keyboard.current;
            var ms = Mouse.current;

            if (owner.user.valid)
            {
                if (kb != null) InputUser.PerformPairingWithDevice(kb, owner.user);
                if (ms != null) InputUser.PerformPairingWithDevice(ms, owner.user);
            }
        }

        /// <summary>
        /// Clean internal mappings for a PlayerInput and unpair its devices.
        /// Idempotent: safe to call multiple times.
        /// </summary>
        private void CleanupMappings(PlayerInput playerInput)
        {
            if (playerInput == null) return;

            DeregisterPlayerServiceRegistrations(playerInput);

            var devices = playerInput.user.valid
                ? playerInput.user.pairedDevices
                : playerInput.devices;

            foreach (var dev in devices)
            {
                if (dev != null)
                {
                    _deviceToPlayer.Remove(dev.deviceId);
                }
            }

            if (playerInput == _keyboardOwner)
            {
                _keyboardOwner = null;
            }

            if (playerInput.user.valid)
            {
                try { playerInput.user.UnpairDevices(); }
                catch { /* ignore */ }
            }

            _playerInputsRegistry.Remove(playerInput.playerIndex);
        }

        /// <summary>
        /// Registers every canonical service identifier associated with one
        /// multiplayer PlayerInput instance.
        /// </summary>
        /// <param name="playerInput">Player input being registered.</param>
        /// <returns>True when all identifiers were registered successfully.</returns>
        private bool TryRegisterPlayerServiceRegistrations(PlayerInput playerInput)
        {
            try
            {
                if (_playerInputServiceRegistrations.ContainsKey(playerInput))
                {
                    DeregisterPlayerServiceRegistrations(playerInput);
                }

                PlayerInputServiceRegistrationKeys registrationKeys
                    = PlayerInputServiceKeys.CreateRegistration(
                        playerInput,
                        Guid.NewGuid()
                    );

                _playerInputServiceRegistrations[playerInput]
                    = registrationKeys;

                ServiceLocator.Register(
                    registrationKeys.ByPlayerIndex,
                    playerInput
                );
                ServiceLocator.Register(
                    registrationKeys.ByInputUserId,
                    playerInput
                );
                ServiceLocator.Register(
                    registrationKeys.ByPersistentGuid,
                    playerInput
                );

                return true;
            }
            catch (Exception ex)
            {
                DeregisterPlayerServiceRegistrations(playerInput);

                HandyLogger.Error(
                    $"{nameof(PlayerManager)}",
                    $"Join rejected: failed to register player service identifiers. {ex.Message}",
                    this
                );
                return false;
            }
        }

        /// <summary>
        /// Removes every named service identifier tracked for one multiplayer
        /// PlayerInput instance.
        /// </summary>
        /// <param name="playerInput">Player input being deregistered.</param>
        private void DeregisterPlayerServiceRegistrations(PlayerInput playerInput)
        {
            if (playerInput == null
                || !_playerInputServiceRegistrations.TryGetValue(
                    playerInput,
                    out PlayerInputServiceRegistrationKeys registrationKeys
                ))
            {
                return;
            }

            ServiceLocator.Deregister<PlayerInput>(
                registrationKeys.ByPlayerIndex
            );
            ServiceLocator.Deregister<PlayerInput>(
                registrationKeys.ByInputUserId
            );
            ServiceLocator.Deregister<PlayerInput>(
                registrationKeys.ByPersistentGuid
            );

            _playerInputServiceRegistrations.Remove(playerInput);
        }

        /// <summary>
        /// Removes every tracked multiplayer player registration from the
        /// service locator.
        /// </summary>
        private void DeregisterTrackedPlayerServices()
        {
            if (_playerInputServiceRegistrations.Count == 0)
            {
                return;
            }

            _playerBuffer.Clear();

            foreach (
                KeyValuePair<PlayerInput, PlayerInputServiceRegistrationKeys> pair
                in _playerInputServiceRegistrations
            )
            {
                if (pair.Key != null)
                {
                    _playerBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < _playerBuffer.Count; i++)
            {
                DeregisterPlayerServiceRegistrations(_playerBuffer[i]);
            }

            _playerBuffer.Clear();
        }

        private void DisableMultiplayerCallbacks()
        {
            _playerInputManager.DisableJoining();
            _playerInputManager.playerJoinedEvent.RemoveListener(OnPlayerJoined);
            _playerInputManager.playerLeftEvent.RemoveListener(OnPlayerLeft);
        }

        private void CopyRegisteredPlayers(List<PlayerInput> results)
        {
            results.Clear();

            foreach (KeyValuePair<int, PlayerInput> pair in _playerInputsRegistry)
            {
                if (pair.Value != null)
                {
                    results.Add(pair.Value);
                }
            }
        }

        private void UnpairPairedDevices(PlayerInput playerInput)
        {
            _deviceBuffer.Clear();

            for (int i = 0; i < playerInput.user.pairedDevices.Count; i++)
            {
                InputDevice device = playerInput.user.pairedDevices[i];
                if (device != null)
                {
                    _deviceBuffer.Add(device);
                }
            }

            for (int i = 0; i < _deviceBuffer.Count; i++)
            {
                playerInput.user.UnpairDevice(_deviceBuffer[i]);
            }

            _deviceBuffer.Clear();
        }

        /// <summary>
        /// Destroys an unwanted PlayerInput instance safely.
        /// </summary>
        private void SafeRejectJoin(PlayerInput pi)
        {
            try
            {
                if (pi != null)
                {
                    Destroy(pi.gameObject);
                }
            }
            catch (Exception ex)
            {
                HandyLogger.Error(
                    $"{nameof(PlayerManager)}",
                    $"SafeRejectJoin error: {ex.Message}",
                    this
                );
            }
        }

        #endregion

        #region Types

        public enum Mode
        {
            SinglePlayer,
            Multiplayer,
        }

        #endregion
    }
}
