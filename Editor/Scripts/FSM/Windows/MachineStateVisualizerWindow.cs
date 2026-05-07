using IndieGabo.HandyTools.FSMModule;
using IndieGabo.HandyTools.FSMModule.Registering;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    /// <summary>
    /// Displays the current-state inheritance path and the recorded history of an FSM brain.
    /// </summary>
    public class MachineStateVisualizerWindow : EditorWindow
    {
        #region Static

        /// <summary>
        /// Opens the visualizer window.
        /// </summary>
        /// <returns>The opened editor window instance.</returns>
        public static MachineStateVisualizerWindow OpenEditorWindow()
        {
            var window = GetWindow<MachineStateVisualizerWindow>();
            window.titleContent = new GUIContent("State Visualizer");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
            return window;
        }

        [MenuItem("HandyTools/FSM/State Visualizer")]
        private static void OpenFromMenu()
        {
            OpenEditorWindow();
        }

        #endregion

        #region Fields

        private StateVisualizer _stateVisualizer;
        private VisualElement _stateVisualizerRoot;

        private TemplateContainer _root;
        private VisualElement _body;
        private VisualElement _selectStateMachineContainer;

        private ObjectField _machineSelectorField;
        private Button _fromSelectionButton;
        private bool _sessionRefreshQueued;

        #endregion


        #region Getters

        public ObjectField MachineSelectorField
        {
            get
            {
                _machineSelectorField ??= rootVisualElement.Q<ObjectField>("machine-selector-field");

                return _machineSelectorField;
            }
        }

        public MachineStateVisualizerWindowData Data => MachineStateVisualizerWindowData.instance;

        #endregion

        #region Behaviour

        /// <summary>
        /// Builds the window UI and restores the selected machine.
        /// </summary>
        private void OnEnable()
        {
            VisualTreeAsset tree = Resources.Load<VisualTreeAsset>("UI Toolkit/FSM/UI Documents/MachineStateVisualizerWindowUI");

            if (tree == null)
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(new Label(
                    "MachineStateVisualizerWindowUI could not be loaded."));
                return;
            }

            _root = tree.CloneTree();

            StyleSheet styleSheet = Resources.Load<StyleSheet>(
                "UI Toolkit/FSM/Styles/handy-fsm-state-visualizer-styles");

            if (styleSheet != null)
            {
                _root.styleSheets.Add(styleSheet);
            }

            _root.style.flexGrow = 1;

            _body = _root.Q<VisualElement>("body");

            _selectStateMachineContainer = _root.Q("select-machine-container");

            _stateVisualizer = new StateVisualizer();
            _stateVisualizerRoot = _stateVisualizer.Root;
            _body.Add(_stateVisualizerRoot);

            _machineSelectorField = _root.Q<ObjectField>("machine-selector-field");

            _machineSelectorField.RegisterValueChangedCallback((e) =>
            {
                FSMBrain machine = e.newValue as FSMBrain;
                SetMachine(machine);
            });

            _fromSelectionButton = _root.Q<Button>("from-selection-button");
            _fromSelectionButton.clicked += () =>
            {
                GameObject selectedObject = Selection.activeObject as GameObject;
                if (selectedObject == null) return;
                FSMBrain machine = selectedObject.GetComponent<FSMBrain>();
                if (machine == null) return;
                _machineSelectorField.value = machine;
                SetMachine(machine);
            };

            LoadMachineFromData();
            LoadSession();

            EditorApplication.playModeStateChanged += OnEditorModeChanged;

            rootVisualElement.Add(_root);
        }

        /// <summary>
        /// Detaches listeners when the window is disabled.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnEditorModeChanged;
            DetachMachineListeners(Data.Machine);
        }

        /// <summary>
        /// Releases transient listeners when the window is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            DetachMachineListeners(Data.Machine);
        }

        /// <summary>
        /// Keeps the window session aligned with play mode transitions.
        /// </summary>
        /// <param name="mode">The new editor play mode state.</param>
        private void OnEditorModeChanged(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredEditMode)
            {
                LoadMachineFromData();
                LoadSession();
                return;
            }

            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                LoadMachineFromData();
                LoadSession();
            }
        }

        #endregion

        #region Flow

        /// <summary>
        /// Loads the latest tracked session for the currently selected machine.
        /// </summary>
        private void RefreshTrackedSession()
        {
            Session session = null;

            if (Data.Machine != null)
            {
                Data.TryGetLastSession(Data.Machine, out session);
            }

            Data.SetSession(session);
            _stateVisualizer.SetMachine(Data.Machine);
            _stateVisualizer.LoadSession(Data.Session);
        }

        /// <summary>
        /// Shows either the selection prompt or the visualizer itself.
        /// </summary>
        private void EvaluateDisplay()
        {
            if (Data.Machine == null)
            {
                _selectStateMachineContainer.style.display = DisplayStyle.Flex;
                _stateVisualizerRoot.style.display = DisplayStyle.None;
            }
            else
            {
                _selectStateMachineContainer.style.display = DisplayStyle.None;
                _stateVisualizerRoot.style.display = DisplayStyle.Flex;
            }
        }

        #endregion

        #region Machine

        /// <summary>
        /// Binds the window to a specific FSM brain.
        /// </summary>
        /// <param name="machine">The machine to inspect.</param>
        public void SetMachine(FSMBrain machine)
        {
            ApplyMachine(machine, true);
        }

        /// <summary>
        /// Applies a machine to the current window, optionally persisting the selection.
        /// </summary>
        /// <param name="machine">The machine to inspect.</param>
        /// <param name="persistSelection">Whether the selection should be persisted across reloads.</param>
        private void ApplyMachine(FSMBrain machine, bool persistSelection)
        {
            DetachMachineListeners(Data.Machine);

            if (persistSelection)
            {
                Data.SetMachine(machine);
            }
            else
            {
                Data.SetResolvedMachine(machine);
            }

            _stateVisualizer.SetMachine(Data.Machine);

            if (Data.Machine != null)
            {
                Data.Machine.StatusChanged.AddListener(OnStatusChanged);
                Data.Machine.StateChanged.AddListener(OnStateChanged);
            }

            RefreshTrackedSession();

            EvaluateDisplay();
        }

        /// <summary>
        /// Restores the selected machine from the persisted window data.
        /// </summary>
        private void LoadMachineFromData()
        {
            if (!Data.TryResolveMachine(out FSMBrain machine))
            {
                _machineSelectorField.value = null;
                ApplyMachine(null, false);
                return;
            }

            _machineSelectorField.value = null;
            _machineSelectorField.value = machine;
            ApplyMachine(machine, false);
        }

        /// <summary>
        /// Loads a previously captured session into the visualizer.
        /// </summary>
        private void LoadSession()
        {
            RefreshTrackedSession();
        }

        /// <summary>
        /// Refreshes the tracked session after the current editor callback chain finishes.
        /// </summary>
        private void ScheduleSessionRefresh()
        {
            if (_sessionRefreshQueued)
            {
                return;
            }

            _sessionRefreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                _sessionRefreshQueued = false;

                if (this == null)
                {
                    return;
                }

                LoadSession();
            };
        }

        /// <summary>
        /// Reacts to machine status changes.
        /// </summary>
        /// <param name="status">The updated machine status.</param>
        private void OnStatusChanged(MachineStatus status)
        {
            ScheduleSessionRefresh();
        }

        /// <summary>
        /// Removes window listeners from a machine.
        /// </summary>
        /// <param name="machine">The machine that should be detached.</param>
        private void DetachMachineListeners(FSMBrain machine)
        {
            if (machine == null)
                return;

            machine.StatusChanged.RemoveListener(OnStatusChanged);
            machine.StateChanged.RemoveListener(OnStateChanged);
        }

        #endregion

        #region States

        /// <summary>
        /// Registers the latest active state in the visualizer.
        /// </summary>
        /// <param name="state">The active state.</param>
        /// <param name="previous">The previous active state.</param>
        private void SetState(IState state, IState previous)
        {
            ScheduleSessionRefresh();
        }

        /// <summary>
        /// Responds to runtime state transitions.
        /// </summary>
        /// <param name="state">The new active state.</param>
        /// <param name="previous">The previous active state.</param>
        private void OnStateChanged(IState state, IState previous)
        {
            SetState(state, previous);
        }

        #endregion
    }
}