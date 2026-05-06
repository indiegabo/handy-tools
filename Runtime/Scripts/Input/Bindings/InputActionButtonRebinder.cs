using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine;
using Sirenix.OdinInspector;
using IndieGabo.HandyTools.LoggerModule;

namespace IndieGabo.HandyTools.HandyInputSystemModule.Bindings
{
    public class InputActionButtonRebinder : HandyBehaviour
    {
        #region Static       

        private static List<InputActionButtonRebinder> _actionButtonRebinders;

        // When the action system re-resolves bindings, we want to update our UI in response. While this will
        // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
        // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
        // will update our UI to reflect the current keyboard layout.
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            if (_actionButtonRebinders == null) return;

            for (var i = 0; i < _actionButtonRebinders.Count; ++i)
            {
                var component = _actionButtonRebinders[i];
                var referencedAction = component.ActionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingInfo();
            }
        }

        public static string ResolveBindingPath(InputControlScheme inputControlScheme, string path)
        {
            return inputControlScheme.name switch
            {
                "GenericGamepads" => $"<Gamepad>/{path}",
                "Xbox" => $"<XInputController>/{path}",
                "Playstation" => $"<DualShockGamepad>/{path}",
                "Switch" => $"<SwitchProController>/{path}",
                _ => $"<Gamepad>/{path}"
            };
        }

        #endregion

        #region Inspector

        [BoxGroup("Configuration")]
        [Tooltip("Reference to action that is to be rebound from the UI.")]
        [SerializeField]
        private InputActionReference _actionReference;

        [FoldoutGroup("Events")]
        [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
            + "bindings in custom ways, e.g. using images instead of text.")]
        [SerializeField]
        private BindingUpdatedEvent _bindingUpdated;

        [FoldoutGroup("Events")]
        [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
            + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
            + "customize the rebind.")]
        [SerializeField]
        private RebindStartedEvent _rebindStarted;

        [FoldoutGroup("Events")]
        [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
        [SerializeField]
        private RebindStoppedEvent _rebindStopped;

        #endregion

        #region Fields

        private InputControlScheme _controlScheme;
        private InputActionRebindingExtensions.RebindingOperation _onGoingRebind;

        private bool _isInitialized;

        #endregion

        #region Properties

        #endregion

        #region  Getters

        public InputActionReference ActionReference => _actionReference;

        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// When an interactive rebind is in progress, this is the rebind operation controller.
        /// Otherwise, it is <c>null</c>.
        /// </summary>
        public InputActionRebindingExtensions.RebindingOperation OnGoingRebind => _onGoingRebind;

        /// <summary>
        /// Event that is triggered every time the UI updates to reflect the current binding.
        /// This can be used to tie custom visualizations to bindings.
        /// </summary>
        public BindingUpdatedEvent BindingUpdated => _bindingUpdated;

        /// <summary>
        /// Event that is triggered when an interactive rebind is started on the action.
        /// </summary>
        public RebindStartedEvent RebindStarted => _rebindStarted;

        /// <summary>
        /// Event that is triggered when an interactive rebind has been completed or canceled.
        /// </summary>
        public RebindStoppedEvent RebindStopped => _rebindStopped;

        #endregion

        #region Behaviour

        public void Initialize(InputControlScheme controlScheme)
        {
            if (_isInitialized)
            {
                if (_controlScheme == controlScheme)
                {
                    UpdateBindingInfo();
                    return;
                }

                Dismiss();
            }

            _controlScheme = controlScheme;

            if (ResolveActionAndBinding(out InputAction action, out int bindingIndex))
            {
                UpdateBindingInfo();
            }

            _actionButtonRebinders ??= new List<InputActionButtonRebinder>();

            if (!_actionButtonRebinders.Contains(this))
            {
                _actionButtonRebinders.Add(this);
            }

            if (_actionButtonRebinders.Count == 1)
                InputSystem.onActionChange += OnActionChange;

            _isInitialized = true;
        }

        public void Dismiss()
        {
            _onGoingRebind?.Cancel();
            _onGoingRebind?.Dispose();
            _onGoingRebind = null;

            if (_actionButtonRebinders != null)
            {
                _actionButtonRebinders.Remove(this);

                if (_actionButtonRebinders.Count == 0)
                {
                    _actionButtonRebinders = null;
                    InputSystem.onActionChange -= OnActionChange;
                }
            }

            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dismiss();
        }

        #endregion

        #region Input Binding

        public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
        {
            bindingIndex = -1;
            action = _actionReference.action;

            if (_controlScheme == null)
            {
                HandyLogger.Error(
                    $"{nameof(InputActionButtonRebinder)}",
                    $"Control scheme {_controlScheme} is null",
                    this
                );
                return false;
            }

            if (_controlScheme.bindingGroup == null)
            {
                HandyLogger.Error(
                    $"{nameof(InputActionButtonRebinder)}",
                    $"Control scheme {_controlScheme.name} has a null 'bindingGroup'",
                    this
                );
                return false;
            }

            bindingIndex = _actionReference.action.bindings.IndexOf(
                b => BindingBelongsToSchemeGroup(b, _controlScheme.bindingGroup)
            );

            if (bindingIndex == -1)
            {
                HandyLogger.Error(
                    $"{nameof(InputActionButtonRebinder)}",
                    $"Binding for {_controlScheme.bindingGroup} not found",
                    this
                );
                return false;
            }

            return true;
        }

        public bool ResolveBinding(out InputBinding binding)
        {
            binding = default;

            if (!ResolveActionAndBinding(out InputAction action, out int bindingIndex))
                return false;

            binding = action.bindings[bindingIndex];

            return true;
        }

        #endregion

        /// <summary>
        /// Remove currently applied binding overrides.
        /// </summary>
        public void ResetToDefault()
        {
            if (!ResolveActionAndBinding(out InputAction action, out int bindingIndex))
                return;

            // Check for duplicate bindings before resetting to default, and if found, swap the two controls.
            if (EraseSameAsDefault(_actionReference.action, bindingIndex))
            {
                UpdateBindingInfo();
                return;
            }

            _actionReference.action.RemoveBindingOverride(bindingIndex);
            UpdateBindingInfo();
        }

        /// <summary>
        /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
        /// for the action.
        /// </summary>
        public void StartInteractiveRebind()
        {
            if (!ResolveActionAndBinding(out InputAction action, out int bindingIndex))
                return;

            PerformInteractiveRebind(action, bindingIndex);
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex)
        {
            _onGoingRebind?.Cancel(); // Will null out m_RebindOperation.

            // Disable the action before use
            action.Disable();

            // Configure the rebind.
            _onGoingRebind = action.PerformInteractiveRebinding(bindingIndex);

            _onGoingRebind.WithControlsExcluding("<Mouse>/leftButton");
            _onGoingRebind.WithControlsExcluding("<Mouse>/rightButton");
            _onGoingRebind.WithControlsExcluding("<Mouse>/press");
            _onGoingRebind.WithControlsExcluding("<Pointer>/position");

            _onGoingRebind.WithCancelingThrough("<Keyboard>/escape");

            _onGoingRebind.OnCancel(operation =>
            {
                operation.action.Enable();
                _rebindStopped.Invoke(this, operation);
                CleanUp();
            });

            _onGoingRebind.OnApplyBinding((operation, path) =>
            {
                string realPath = ResolveBindingPath(_controlScheme, path);
                operation.action.ApplyBindingOverride(bindingIndex, realPath);
            });

            _onGoingRebind.OnComplete(operation =>
            {
                operation.action.Enable();
                _rebindStopped.Invoke(this, operation);

                HandleDuplicates(action, bindingIndex);

                UpdateBindingInfo();
                CleanUp();
            });

            // Give listeners a chance to act on the rebind starting.
            _rebindStarted.Invoke(this, _onGoingRebind);

            _onGoingRebind.Start();
        }

        /// <summary>
        /// Trigger a refresh of the currently displayed binding.
        /// </summary>
        public void UpdateBindingInfo()
        {
            if (!ResolveActionAndBinding(out InputAction action, out int bindingIndex))
                return;

            // if (ResolveBinding(out InputBinding binding))
            //     Debug.Log($"{_pairedDevice} Binding: {binding.path} -> {binding.effectivePath}", this);

            // Give listeners a chance to configure UI in response.
            _bindingUpdated.Invoke(this, action, bindingIndex);
        }

        private void HandleDuplicates(InputAction action, int newBindingIndex)
        {
            InputBinding newBinding = action.bindings[newBindingIndex];

            foreach (InputBinding binding in action.actionMap.bindings)
            {
                // If they are not the same path, we do not care
                if (binding.effectivePath != newBinding.effectivePath) continue;

                // IF they are not at the same action, we do not care.
                if (binding.action == newBinding.action) continue;

                // IF they are not at the same control scheme, we do not care.
                if (!BindingBelongsToSchemeGroup(binding, _controlScheme.bindingGroup)) continue;

                // From here we must empty the other binding.

                // Retrieving the actual Action
                action.actionMap.EraseBinding(binding);
            }
        }

        /// <summary>
        /// Check for duplicate rebindings when the binding is going to be set to default.
        /// </summary>
        /// <param name="action">InputAction we are resetting.</param>
        /// <param name="bindingIndex">Current index of the control we are rebinding.</param>
        /// <returns></returns>
        private bool EraseSameAsDefault(InputAction action, int bindingIndex)
        {
            // Cache a reference to the current binding.
            InputBinding newBinding = action.bindings[bindingIndex];

            // Check all of the bindings in the current action map to make sure there are no duplicates.
            for (int i = 0; i < action.actionMap.bindings.Count; ++i)
            {
                InputBinding binding = action.actionMap.bindings[i];

                // IF they are not at the same control scheme, we do not care.
                if (!BindingBelongsToSchemeGroup(binding, _controlScheme.bindingGroup)) continue;

                if (binding.action == newBinding.action) continue;

                if (binding.effectivePath != newBinding.path) continue;

                action.actionMap.EraseBinding(binding);
            }

            return false;
        }

        private void CleanUp()
        {
            _onGoingRebind?.Dispose();
            _onGoingRebind = null;
        }

        private static bool BindingBelongsToSchemeGroup(
            InputBinding binding,
            string bindingGroup
        )
        {
            string groups = binding.groups;
            if (string.IsNullOrEmpty(groups) || string.IsNullOrEmpty(bindingGroup))
            {
                return false;
            }

            int startIndex = 0;
            while (startIndex < groups.Length)
            {
                int separatorIndex = groups.IndexOf(';', startIndex);
                if (separatorIndex < 0)
                {
                    separatorIndex = groups.Length;
                }

                int tokenLength = separatorIndex - startIndex;
                if (tokenLength == bindingGroup.Length &&
                    string.Compare(
                        groups,
                        startIndex,
                        bindingGroup,
                        0,
                        tokenLength,
                        StringComparison.Ordinal
                    ) == 0)
                {
                    return true;
                }

                startIndex = separatorIndex + 1;
            }

            return false;
        }

        [Serializable]
        public class BindingUpdatedEvent : UnityEvent<InputActionButtonRebinder, InputAction, int>
        {
        }

        [Serializable]
        public class RebindStartedEvent : UnityEvent<InputActionButtonRebinder, InputActionRebindingExtensions.RebindingOperation>
        {
        }

        [Serializable]
        public class RebindStoppedEvent : UnityEvent<InputActionButtonRebinder, InputActionRebindingExtensions.RebindingOperation>
        {
        }
    }
}
