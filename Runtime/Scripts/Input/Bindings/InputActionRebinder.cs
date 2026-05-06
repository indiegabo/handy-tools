using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine;
using Sirenix.OdinInspector;
using IndieGabo.HandyTools.LoggerModule;

////TODO: localization support

////TODO: deal with composites that have parts bound in different control schemes

namespace IndieGabo.HandyTools.HandyInputSystemModule.Bindings
{
    /// <summary>
    /// A reusable component with a self-contained UI for rebinding a single action.
    /// </summary>
    public class InputActionRebinder : HandyBehaviour
    {
        #region Inspector

        [Tooltip("Reference to action that is to be rebound from the UI.")]
        [HideInInspector]
        [SerializeField]
        private InputActionReference m_Action;

        [SerializeField]
        [HideInInspector]
        private string m_BindingId;

        [SerializeField]
        [HideInInspector]
        private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [FoldoutGroup("Events")]
        [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
            + "bindings in custom ways, e.g. using images instead of text.")]
        [SerializeField]
        private UpdateBindingUIEvent m_UpdateBindingUIEvent;

        [FoldoutGroup("Events")]
        [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
            + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
            + "customize the rebind.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStartEvent;

        [FoldoutGroup("Events")]
        [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStopEvent;

        #endregion

        #region Properties

        /// <summary>
        /// Reference to the action that is to be rebound.
        /// </summary>
        public InputActionReference actionReference
        {
            get => m_Action;
            set
            {
                m_Action = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// ID (in string form) of the binding that is to be rebound on the action.
        /// </summary>
        /// <seealso cref="InputBinding.id"/>
        public string bindingId
        {
            get => m_BindingId;
            set
            {
                m_BindingId = value;
                UpdateBindingDisplay();
            }
        }

        public InputBinding.DisplayStringOptions displayStringOptions
        {
            get => m_DisplayStringOptions;
            set
            {
                m_DisplayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Event that is triggered every time the UI updates to reflect the current binding.
        /// This can be used to tie custom visualizations to bindings.
        /// </summary>
        public UpdateBindingUIEvent updateBindingUIEvent
        {
            get
            {
                if (m_UpdateBindingUIEvent == null)
                    m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
                return m_UpdateBindingUIEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind is started on the action.
        /// </summary>
        public InteractiveRebindEvent startRebindEvent
        {
            get
            {
                if (m_RebindStartEvent == null)
                    m_RebindStartEvent = new InteractiveRebindEvent();
                return m_RebindStartEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind has been completed or canceled.
        /// </summary>
        public InteractiveRebindEvent stopRebindEvent
        {
            get
            {
                if (m_RebindStopEvent == null)
                    m_RebindStopEvent = new InteractiveRebindEvent();
                return m_RebindStopEvent;
            }
        }

        #endregion

        /// <summary>
        /// When an interactive rebind is in progress, this is the rebind operation controller.
        /// Otherwise, it is <c>null</c>.
        /// </summary>
        public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

        /// <summary>
        /// Return the action and binding index for the binding that is targeted by the component
        /// according to
        /// </summary>
        /// <param name="action"></param>
        /// <param name="bindingIndex"></param>
        /// <returns></returns>
        public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
        {
            bindingIndex = -1;

            action = m_Action?.action;
            if (action == null)
                return false;

            if (string.IsNullOrEmpty(m_BindingId))
                return false;

            // Look up binding index.
            var bindingId = new Guid(m_BindingId);
            bindingIndex = action.bindings.IndexOf(x => x.id == bindingId);
            if (bindingIndex == -1)
            {
                HandyLogger.Error(
                    $"{nameof(InputActionRebinder)}",
                    $"Cannot find binding with ID '{bindingId}' on '{action}'",
                    this
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove currently applied binding overrides.
        /// </summary>
        public void ResetToDefault()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            // Check for duplicate bindings before resetting to default, and if found, swap the two controls.
            if (SwapResetBindings(action, bindingIndex))
            {
                UpdateBindingDisplay();
                return;
            }

            if (action.bindings[bindingIndex].isComposite)
            {
                // It's a composite. Remove overrides from part bindings.
                for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                    action.RemoveBindingOverride(i);
            }
            else
            {
                action.RemoveBindingOverride(bindingIndex);
            }
            UpdateBindingDisplay();
        }

        /// <summary>
        /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
        /// for the action.
        /// </summary>
        public void StartInteractiveRebind()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            // If the binding is a composite, we need to rebind each part in turn.
            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(action, bindingIndex);
            }
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

            // Disable the action before use
            action.Disable();

            // Configure the rebind.
            m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex);

            m_RebindOperation.WithControlsExcluding("<Mouse>/leftButton");
            m_RebindOperation.WithControlsExcluding("<Mouse>/rightButton");
            m_RebindOperation.WithControlsExcluding("<Mouse>/press");
            m_RebindOperation.WithControlsExcluding("<Pointer>/position");

            m_RebindOperation.WithCancelingThrough("<Keyboard>/escape");

            m_RebindOperation.OnCancel(operation =>
            {
                operation.action.Enable();
                m_RebindStopEvent?.Invoke(this, operation);
                UpdateBindingDisplay();
                CleanUp();
            });

            m_RebindOperation.OnComplete(operation =>
            {
                operation.action.Enable();
                m_RebindStopEvent?.Invoke(this, operation);

                if (HasDuplicateBindings(action, bindingIndex, allCompositeParts))
                {
                    action.RemoveBindingOverride(bindingIndex);
                    CleanUp();
                    PerformInteractiveRebind(action, bindingIndex, allCompositeParts);
                    return;
                }

                UpdateBindingDisplay();
                CleanUp();

                // If there's more composite parts we should bind, initiate a rebind
                // for the next part.
                if (!allCompositeParts) return;

                int nextBindingIndex = bindingIndex + 1;
                if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                    PerformInteractiveRebind(action, nextBindingIndex, true);
            });

            // Give listeners a chance to act on the rebind starting.
            m_RebindStartEvent?.Invoke(this, m_RebindOperation);

            m_RebindOperation.Start();
        }

        protected void OnEnable()
        {
            if (s_RebindActionUIs == null)
                s_RebindActionUIs = new List<InputActionRebinder>();

            s_RebindActionUIs.Add(this);

            if (s_RebindActionUIs.Count == 1)
                InputSystem.onActionChange += OnActionChange;

            UpdateBindingDisplay();
        }

        protected void OnDisable()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;

            s_RebindActionUIs.Remove(this);
            if (s_RebindActionUIs.Count == 0)
            {
                s_RebindActionUIs = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

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

            for (var i = 0; i < s_RebindActionUIs.Count; ++i)
            {
                var component = s_RebindActionUIs[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }

        private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

        private static List<InputActionRebinder> s_RebindActionUIs;

        // We want the label for the action name to update in edit mode, too, so
        // we kick that off from here.
#if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateBindingDisplay();
        }
#endif

        /// <summary>
        /// Trigger a refresh of the currently displayed binding.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            string displayString = string.Empty;
            string deviceLayoutName = default(string);
            string controlPath = default(string);

            // Get display string from action.
            InputAction action = m_Action?.action;
            if (action != null)
            {
                int bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
                if (bindingIndex != -1)
                    displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
            }

            // Give listeners a chance to configure UI in response.
            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        private bool HasDuplicateBindings(InputAction action, int bindingIndex, bool allCompositeParts)
        {
            InputBinding newBinding = action.bindings[bindingIndex];

            foreach (InputBinding binding in action.actionMap.bindings)
            {
                if (binding.action == newBinding.action) continue;
                if (binding.effectivePath == newBinding.effectivePath)
                {
                    HandyLogger.Warning(
                        $"{nameof(InputActionRebinder)}",
                        $"Attempting to bind a duplicate: {newBinding.effectivePath}",
                        this
                    );
                    return true;
                }
            }

            if (!allCompositeParts) return false;

            // Checking for duplicate composite bindings
            for (int i = 0; i < bindingIndex; i++)
            {
                if (action.bindings[i].effectivePath == newBinding.effectivePath)
                {
                    HandyLogger.Warning(
                        $"{nameof(InputActionRebinder)}",
                        $"Attempting to bind a duplicate: {newBinding.effectivePath}",
                        this
                    );
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check for duplicate rebindings when the binding is going to be set to default.
        /// </summary>
        /// <param name="action">InputAction we are resetting.</param>
        /// <param name="bindingIndex">Current index of the control we are rebinding.</param>
        /// <returns></returns>
        private bool SwapResetBindings(InputAction action, int bindingIndex)
        {
            // Cache a reference to the current binding.
            InputBinding newBinding = action.bindings[bindingIndex];
            // Check all of the bindings in the current action map to make sure there are no duplicates.
            for (int i = 0; i < action.actionMap.bindings.Count; ++i)
            {
                InputBinding binding = action.actionMap.bindings[i];
                if (binding.action == newBinding.action)
                {
                    continue;
                }
                if (binding.effectivePath == newBinding.path)
                {
                    Debug.Log("Duplicate binding found for reset to default: " + newBinding.effectivePath);
                    // Swap the two actions.
                    action.actionMap.FindAction(binding.action).ApplyBindingOverride(i, newBinding.overridePath);
                    action.RemoveBindingOverride(bindingIndex);
                    return true;
                }
            }
            return false;
        }

        private void CleanUp()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;
        }

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<InputActionRebinder, string, string, string>
        {
        }

        [Serializable]
        public class InteractiveRebindEvent : UnityEvent<InputActionRebinder, InputActionRebindingExtensions.RebindingOperation>
        {
        }
    }
}
