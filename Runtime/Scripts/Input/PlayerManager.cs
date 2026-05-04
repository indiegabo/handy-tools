using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.HandyBus;
using IndieGabo.HandyTools.HandyServiceLocator;
using IndieGabo.HandyTools.Logger;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace IndieGabo.HandyTools.HandyInputSystem
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerManager : HandyBehaviour
    {
        #region Static

        public readonly static string SinglePlayerServiceName
            = "SinglePlayerInput";

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

        // Device → Player mapping to prevent duplicate joins per device.
        private readonly Dictionary<int, PlayerInput> _deviceToPlayer
            = new();

        // Track the current shared keyboard/mouse owner instead of a bool.
        private PlayerInput _keyboardOwner;

        #endregion

        #region Properties

        private ProjectInputConfig Config => ProjectInputConfig.Get();

        public PlayerInput SinglePlayerInput => _singlePlayerInput;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playerInputManager = GetComponent<PlayerInputManager>();
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

            ServiceLocator.Global.Register(this);
            ServiceLocator.Global.Register(_playerInputManager);
            ServiceLocator.Global.Register(
                SinglePlayerServiceName,
                _singlePlayerInput
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

        [Button]
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
            List<PlayerInput> playerInputs
                = _playerInputsRegistry.Values.ToList();

            _playerInputsRegistry.Clear();
            _deviceToPlayer.Clear();
            _keyboardOwner = null;

            for (int i = 0; i < playerInputs.Count; i++)
            {
                var pi = playerInputs[i];
                if (pi != null)
                {
                    Destroy(pi.gameObject);
                }
            }

            _playerInputManager.DisableJoining();

            _playerInputManager.playerJoinedEvent.RemoveListener(
                OnPlayerJoined
            );
            _playerInputManager.playerLeftEvent.RemoveListener(
                OnPlayerLeft
            );

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
            var resolved = options ?? _defaultMultiplayerOptions;

            _singlePlayerInput.gameObject.SetActive(false);

            _playerInputManager.splitScreen = false;
            _playerInputManager.joinBehavior
                = PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;

            _playerInputManager.joinAction = resolved.joinAction;

            _playerInputsRegistry.Clear();
            _deviceToPlayer.Clear();
            _keyboardOwner = null;

            TryUnpairSinglePlayerDevices();

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

            _playerInputsRegistry[playerInput.playerIndex] = playerInput;

            playerInput.name
                = $"PlayerInput [{playerInput.playerIndex}] " +
                  $"({device.displayName}#{device.deviceId})";

            playerInput.transform.SetParent(transform);
            playerInput.notificationBehavior
                = PlayerNotifications.InvokeUnityEvents;

            EventBus<PlayerJoinedEvent>.Raise(
                new PlayerJoinedEvent()
                {
                    playerIndex = playerInput.playerIndex,
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
                _playerInputsRegistry.Remove(playerInput.playerIndex);
            }

            EventBus<PlayerLeftEvent>.Raise(
                new PlayerLeftEvent()
                {
                    playerIndex = playerInput != null
                        ? playerInput.playerIndex
                        : -1,
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

            players = _playerInputsRegistry.Values.ToList();
            return true;
        }

        /// <summary>
        /// Returns all active PlayerInput instances including single-player.
        /// </summary>
        public List<PlayerInput> GetAllActivePlayers()
        {
            if (_currentMode == Mode.SinglePlayer)
            {
                return new List<PlayerInput> { _singlePlayerInput };
            }

            return _playerInputsRegistry.Values.ToList();
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
                    HandyLogger.Message(
                        $"{nameof(PlayerManager)}",
                        "Unpaired devices from SinglePlayerInput.",
                        this
                    );
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
            string fieldName = "m_MaxPlayerCount";

            FieldInfo field = _playerInputManager
                .GetType()
                .GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                );

            if (field == null)
            {
                HandyLogger.Warning(
                    $"{nameof(PlayerManager)}",
                    $"Field {fieldName} not found in " +
                    $"{nameof(PlayerInputManager)}",
                    this
                );
                return;
            }

            field.SetValue(
                _playerInputManager,
                ProjectInputConfig.Get().MaxNumberOfPlayers
            );
        }

        /// <summary>
        /// Resolves the most likely device that initiated the join.
        /// Prefers Gamepad; otherwise returns Keyboard or Mouse.
        /// </summary>
        private InputDevice ResolveJoiningDevice(PlayerInput pi)
        {
            var paired = pi.user.valid ? pi.user.pairedDevices : pi.devices;

            var gamepad = paired.FirstOrDefault(d => d is Gamepad);
            if (gamepad != null)
            {
                return gamepad;
            }

            if (paired.Any(d => d is Keyboard))
            {
                return Keyboard.current;
            }

            if (paired.Any(d => d is Mouse))
            {
                return Mouse.current;
            }

            return paired.FirstOrDefault();
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
                // Clear accidental pairings.
                var toUnpair = pi.user.pairedDevices.ToList();
                for (int i = 0; i < toUnpair.Count; i++)
                {
                    pi.user.UnpairDevice(toUnpair[i]);
                }

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
                var devices = new List<InputDevice> { device };
                pi.SwitchCurrentControlScheme(devices.ToArray());
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
                var toUnpair = pi.user.pairedDevices.ToList();
                for (int i = 0; i < toUnpair.Count; i++)
                {
                    pi.user.UnpairDevice(toUnpair[i]);
                }

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
                var list = new List<InputDevice>();
                if (kb != null) list.Add(kb);
                if (ms != null) list.Add(ms);
                if (list.Count > 0)
                {
                    pi.SwitchCurrentControlScheme(list.ToArray());
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
