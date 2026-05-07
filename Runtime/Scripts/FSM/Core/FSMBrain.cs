using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// The state machine base class
    /// </summary>
    [AddComponentMenu("HandyTools/FSM/FSMBrain")]
    public class FSMBrain : MonoBehaviour
    {
        #region Inspector

        /// <summary>
        /// The current machine's status of the MachineStatus enum type. 
        /// </summary>
        [SerializeField]
        protected MachineStatus _status = MachineStatus.Off;

        /// <summary>
        /// The current state name
        /// </summary>
        [SerializeField]
        protected string _currentStateName = "None";

        [SerializeField]
        protected Transform _owner;

        /// <summary>
        /// Optional source component responsible for feeding input values into
        /// this brain.
        /// </summary>
        [SerializeField]
        protected FSMInputSource _inputSource;

        /// <summary>
        /// Enables Simple Blackboard integration for this machine when the package is installed.
        /// </summary>
        [SerializeField]
        protected bool _useSimpleBlackboard;

        /// <summary>
        /// The Simple Blackboard container used by this machine and its states.
        /// </summary>
        [SerializeField]
        protected Component _blackboardContainer;

        /// <summary>
        /// Optional animator reference owned by this machine.
        /// </summary>
        [SerializeField]
        protected Animator _animator;

        /// <summary>
        /// Enables Character Controller Pro integration for this machine when
        /// the package is installed.
        /// </summary>
        [SerializeField]
        protected bool _useCharacterControllerPro;

        /// <summary>
        /// The Character Controller Pro actor used by this machine.
        /// </summary>
        [SerializeField]
        protected Component _characterActor;

        /// <summary>
        /// Determines how movement reference data should be generated for the
        /// optional Character Controller Pro integration.
        /// </summary>
        [SerializeField]
        protected CharacterControllerProMovementReferenceMode _movementReferenceMode =
            CharacterControllerProMovementReferenceMode.World;

        /// <summary>
        /// Optional external movement reference transform used by the Character
        /// Controller Pro integration.
        /// </summary>
        [SerializeField]
        protected Transform _externalReference;

        [SerializeField]
        protected InitializationMode _initializationMode = InitializationMode.Automatic;

        [SerializeField]
        protected bool _transitionsOnUpdate;

        [SerializeField]
        protected bool _transitionsOnLateUpdate;

        [SerializeField]
        protected bool _transitionsOnFixedUpdate;

        /// <summary>
        /// Enables editor-side history capture for state debugging.
        /// </summary>
        [SerializeField]
        protected bool _saveHistory;

        [SerializeField]
        protected ScriptableState _defaultScriptableState;

        [SerializeField]
        protected List<ScriptableState> _scriptableStates;

        [SerializeField]
        protected UnityEvent<MachineStatus> _statusChanged;

        [SerializeField]
        protected UnityEvent<IState, IState> _stateChanged;

        #endregion

        #region Fields        

        protected IState _defaultState;
        protected IState _firstEnteredState;

        protected IState _currentState;
        protected IState _previousState;
        protected StateTransitionReport _lastTransitionReport;

        protected bool _isInitialized;
        protected StateProvider _stateProvider;
        protected TriggersProvider _triggersProvider;

        private FSMBrainBlackboardDomain _blackboardDomain;
        private FSMBrainCCProDomain _characterControllerProDomain;
        private FSMBrainInputDomain _inputDomain;
        private FSMBrainMachineDomain _machineDomain;
        private FSMBrainStatsDomain _statsDomain;
        private FSMBrainStatesDomain _statesDomain;

        protected readonly HashSet<IState> _faultedStates = new();

        private bool _isRecoveringFromStateFailure;
        private bool _moduleInactiveWarningIssued;

        private static readonly Dictionary<Type, CachedBrainLifecycleMethods>
            s_cachedBrainLifecycleMethods = new();

        private static readonly Dictionary<Type, CachedCharacterControllerProStateMethods>
            s_cachedCharacterControllerProStateMethods = new();

        #endregion

        #region Getters

        /// <summary>
        /// The state machine Owner trandform. If not defined on inspector it will be the Transform
        /// of the GameObject in which the script is attached
        /// </summary>
        public Transform Owner => _owner != null ? _owner : transform;

        /// <summary>
        /// Gets whether this machine should use the optional Simple Blackboard integration.
        /// </summary>
        public bool UseSimpleBlackboard =>
            _useSimpleBlackboard && IsSimpleBlackboardAvailable;

        /// <summary>
        /// Gets whether this machine should use the optional Character
        /// Controller Pro integration.
        /// </summary>
        public bool UseCharacterControllerPro =>
            _useCharacterControllerPro && IsCharacterControllerProAvailable;

        /// <summary>
        /// The configured Simple Blackboard container used by this machine.
        /// </summary>
        internal Component ConfiguredBlackboardContainer =>
            UseSimpleBlackboard
                ? _blackboardContainer
                : null;

        /// <summary>
        /// Gets whether the Simple Blackboard package is available in the project.
        /// </summary>
        public static bool IsSimpleBlackboardAvailable => SimpleBlackboardBridge.IsAvailable;

        /// <summary>
        /// Gets the Simple Blackboard container type when the package is installed.
        /// </summary>
        public static Type SimpleBlackboardContainerType =>
            SimpleBlackboardBridge.ContainerType;

        /// <summary>
        /// Gets whether the Character Controller Pro package is available in the project.
        /// </summary>
        public static bool IsCharacterControllerProAvailable =>
            CharacterControllerProBridge.IsAvailable;

        /// <summary>
        /// Gets the Character Controller Pro actor type when the package is installed.
        /// </summary>
        public static Type CharacterActorType => CharacterControllerProBridge.CharacterActorType;

        /// <summary>
        /// Gets the delegated machine domain for this brain.
        /// </summary>
        public FSMBrainMachineDomain Machine =>
            _machineDomain ??= new FSMBrainMachineDomain(this, States);

        /// <summary>
        /// Gets the delegated states domain for this brain.
        /// </summary>
        public FSMBrainStatesDomain States =>
            _statesDomain ??= new FSMBrainStatesDomain(
                _stateProvider ??= new StateProvider(this));

        /// <summary>
        /// Gets the delegated stats domain for this brain.
        /// </summary>
        public FSMBrainStatsDomain Stats =>
            _statsDomain ??= new FSMBrainStatsDomain(this);

        /// <summary>
        /// Gets the delegated input domain for this brain.
        /// </summary>
        public FSMBrainInputDomain Input =>
            _inputDomain ??= new FSMBrainInputDomain(this, CCPro);

        /// <summary>
        /// Gets the delegated blackboard domain for this brain.
        /// </summary>
        public FSMBrainBlackboardDomain Blackboard =>
            _blackboardDomain ??= new FSMBrainBlackboardDomain(this);

        /// <summary>
        /// Gets the delegated Character Controller Pro domain for this brain.
        /// </summary>
        public FSMBrainCCProDomain CCPro =>
            _characterControllerProDomain ??= new FSMBrainCCProDomain(
                this,
                OnCharacterControllerProPreSimulation,
                OnCharacterControllerProPostSimulation,
                OnCharacterControllerProAnimatorIk);

        /// <summary>
        /// Gets or sets the serialized input source configured on the brain.
        /// </summary>
        internal FSMInputSource ConfiguredInputSource
        {
            get => _inputSource;
            set => _inputSource = value;
        }

        /// <summary>
        /// Gets the animator owned by this machine.
        /// </summary>
        internal Animator ConfiguredAnimator => _animator;

        /// <summary>
        /// Gets the configured Character Controller Pro actor component.
        /// </summary>
        internal Component ConfiguredCharacterActor =>
            UseCharacterControllerPro ? _characterActor : null;

        /// <summary>
        /// Gets or sets the authored external reference used by the CCPro domain.
        /// </summary>
        internal Transform ConfiguredExternalReference
        {
            get => _externalReference;
            set
            {
                _externalReference = value;
                _characterControllerProDomain?.RefreshMovementReferenceConfiguration();
            }
        }

        /// <summary>
        /// Gets or sets the authored movement reference mode used by the CCPro domain.
        /// </summary>
        internal CharacterControllerProMovementReferenceMode
            ConfiguredMovementReferenceMode
        {
            get => _movementReferenceMode;
            set
            {
                _movementReferenceMode = value;
                _characterControllerProDomain?.RefreshMovementReferenceConfiguration();
            }
        }

        /// <summary>
        /// If the machine is already
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// If the machine is on
        /// </summary>
        public bool IsOn => _status == MachineStatus.On;

        /// <summary>
        /// If the machine is paused
        /// </summary>
        public bool IsPaused => _status == MachineStatus.Paused;

        /// <summary>
        /// If the machine is off
        /// </summary>
        public bool IsOff => _status == MachineStatus.Off;

        /// <summary>
        /// If the machine is working. Either On or Paused
        /// </summary>
        public bool IsWorking => IsOn || IsPaused;

        /// <summary>
        /// A getter for the machine's Status
        /// </summary>
        public MachineStatus Status => _status;

        /// <summary>
        /// This is the current active state for the this State Machine
        /// </summary>
        public IState CurrentState => _currentState;

        /// <summary>
        /// This is the immediate previous state the machine was in.
        /// </summary>
        public IState PreviousState => _previousState;

        /// <summary>
        /// Gets the reason that produced the latest successful state transition.
        /// </summary>
        public StateTransitionReason LastTransitionReason => _lastTransitionReport.Reason;

        /// <summary>
        /// Gets the latest successful transition report.
        /// </summary>
        public StateTransitionReport LastTransitionReport => _lastTransitionReport;

        /// <summary>
        /// Getter for the machine's default state
        /// </summary>
        public IState DefaultState => _defaultState;

        /// <summary>
        /// Gets the first state that entered successfully after the machine was turned on.
        /// </summary>
        public IState FirstEnteredState => _firstEnteredState;

        /// <summary>
        /// The triggers registered in this machine brain
        /// </summary>
        public TriggersProvider Triggers => _triggersProvider;

        /// <summary>
        /// Gets whether history capture is explicitly enabled for editor debugging.
        /// </summary>
        public bool SaveHistory => _saveHistory;

        /// <summary>
        /// Gets whether history capture should run in the current execution context.
        /// </summary>
        public bool ShouldCaptureHistory
        {
            get
            {
#if UNITY_EDITOR
                return _saveHistory;
#else
                return Debug.isDebugBuild;
#endif
            }
        }

        /// <summary>
        /// If CurrentStateName should be shown in the inspector
        /// </summary>
        protected bool ShowCurrentState => _status != MachineStatus.Off;

        // Events

        /// <summary>
        /// Whenever the machine status changes
        /// </summary>
        public UnityEvent<MachineStatus> StatusChanged => _statusChanged;

        /// <summary>
        /// Whenever the current state changes
        /// </summary>
        public UnityEvent<IState, IState> StateChanged => _stateChanged;

        /// <summary>
        #endregion

        #region Behaviour   

        private bool EnsureModuleRuntimeEnabled()
        {
            if (FSMModuleDefinition.IsActive)
            {
                return true;
            }

            if (!_moduleInactiveWarningIssued)
            {
                _moduleInactiveWarningIssued = true;
                Debug.LogWarning(
                    "[HandyTools FSM] The FSM module is inactive. Re-enable the 'fsm' entry in Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset before using FSMBrain components.",
                    this);
            }

            if (enabled)
            {
                enabled = false;
            }

            return false;
        }

        protected virtual void Awake()
        {
            if (!EnsureModuleRuntimeEnabled())
            {
                return;
            }

            _status = MachineStatus.Off;
            _firstEnteredState = null;
            _lastTransitionReport = StateTransitionReport.Unknown;
            _faultedStates.Clear();
            _isRecoveringFromStateFailure = false;
            _stateProvider = new StateProvider(this);
            _statesDomain = new FSMBrainStatesDomain(_stateProvider);
            _blackboardDomain = new FSMBrainBlackboardDomain(this);
            _characterControllerProDomain = new FSMBrainCCProDomain(
                this,
                OnCharacterControllerProPreSimulation,
                OnCharacterControllerProPostSimulation,
                OnCharacterControllerProAnimatorIk);
            _inputDomain = new FSMBrainInputDomain(this, _characterControllerProDomain);
            _machineDomain = new FSMBrainMachineDomain(this, _statesDomain);

            CCPro.ResetRuntimeState();
            Input.BindSource();
            States.LoadStatesFromScriptablesList(_scriptableStates, false);

            _triggersProvider = new TriggersProvider(this);

            Type machineType = GetType();
            CachedBrainLifecycleMethods lifecycleMethods =
                GetCachedBrainLifecycleMethods(machineType);

            lifecycleMethods.InvokeBeforeInitialized(this);

            if (_defaultState == null && _defaultScriptableState != null)
                _defaultState = States.Get(_defaultScriptableState.GetType());

            States.InitializeAllStates();

            _isInitialized = true;

            lifecycleMethods.InvokeAfterInitialized(this);
        }

        protected virtual void OnEnable()
        {
            if (!EnsureModuleRuntimeEnabled())
            {
                return;
            }

            Input.BindSource();
            CCPro.SubscribeCallbacks();
        }

        protected virtual void Start()
        {
            if (!EnsureModuleRuntimeEnabled())
            {
                return;
            }

            CCPro.InitializeSupport();

            if (_initializationMode != InitializationMode.Automatic) return;

            if (_defaultState == null)
            {
                Debug.LogError($"The machine {name} is marked to initialize automatically but was unable to resolve a default state.", this);
                return;
            }

            TurnOnCore(_defaultState);
        }

        protected virtual void Update()
        {
            if (!FSMModuleDefinition.IsActive)
            {
                return;
            }

            if (UseCharacterControllerPro)
            {
                return;
            }

            if (_status != MachineStatus.On) return;

            if (_transitionsOnUpdate)
                ExecuteMachineOperation(EvaluateTransition, _currentState, "Update transition evaluation");

            ExecuteStateOperation(_currentState, state => state.Tick(), "Update tick");
        }

        protected virtual void LateUpdate()
        {
            if (!FSMModuleDefinition.IsActive)
            {
                return;
            }

            if (UseCharacterControllerPro)
            {
                return;
            }

            if (_status != MachineStatus.On) return;

            if (_transitionsOnLateUpdate)
                ExecuteMachineOperation(EvaluateTransition, _currentState, "LateUpdate transition evaluation");

            ExecuteStateOperation(_currentState, state => state.LateTick(), "LateUpdate tick");
        }

        protected virtual void FixedUpdate()
        {
            if (!FSMModuleDefinition.IsActive)
            {
                return;
            }

            if (_status != MachineStatus.On) return;

            if (UseCharacterControllerPro)
            {
                CCPro.UpdateSupport();

                ExecuteMachineOperation(
                    EvaluateTransition,
                    _currentState,
                    "FixedUpdate transition evaluation");

                TryExecuteCharacterControllerProPreFixedTick(_currentState);
                ExecuteStateOperation(_currentState, state => state.FixedTick(), "FixedUpdate tick");
                TryExecuteCharacterControllerProPostFixedTick(_currentState);
                return;
            }

            if (_transitionsOnFixedUpdate)
                ExecuteMachineOperation(EvaluateTransition, _currentState, "FixedUpdate transition evaluation");

            ExecuteStateOperation(_currentState, state => state.FixedTick(), "FixedUpdate tick");
        }

        protected virtual void OnDisable()
        {
            if (!FSMModuleDefinition.IsActive)
            {
                return;
            }

            CCPro.UnsubscribeCallbacks();
            Input.UnbindSource();
            StopCore();
        }

        #endregion

        #region Machine Engine

        /// <summary>
        /// Turns the machine on and enters the given state
        /// </summary>
        /// <param name="stateType"></param>
        internal void TurnOnCore(IState state)
        {
            if (IsWorking)
            {
                Debug.LogError($"Trying to turn machine on but it is already working", this);
                return;
            }

            if (!_statesDomain.IsLoaded(state))
            {
                Debug.LogError($"Trying to turn machine on but {nameof(state)} is not loaded.", this);
                return;
            }

            _firstEnteredState = null;
            _previousState = null;
            _lastTransitionReport = StateTransitionReport.Unknown;
            _faultedStates.Clear();
            _isRecoveringFromStateFailure = false;
            InitializeCharacterControllerProSupport();

            ChangeStatusCore(MachineStatus.On);
            ChangeState(
                state,
                new StateTransitionReport(StateTransitionReason.InitialEntry));
        }

        /// <summary>
        /// Pauses the machine
        /// </summary>
        internal void ResumeCore()
        {
            if (!IsPaused) return;
            ChangeStatusCore(MachineStatus.On);
        }

        /// <summary>
        /// Pauses the machine
        /// </summary>
        internal void PauseCore()
        {
            if (!IsOn) return;
            ChangeStatusCore(MachineStatus.Paused);
        }

        /// <summary>
        /// Stops the machine
        /// </summary>
        internal void StopCore()
        {
            if (!IsWorking) return;

            ExecuteStateOperation(_currentState, state => state.Exit(), "Stop exit");
            _currentState = null;
            _previousState = null;
            _firstEnteredState = null;
            _lastTransitionReport = StateTransitionReport.Unknown;
            _faultedStates.Clear();
            _isRecoveringFromStateFailure = false;
            ResetCharacterControllerProRuntimeState();

            ChangeStatusCore(MachineStatus.Off);
        }

        /// <summary>
        /// Initializes the optional Character Controller Pro support when the
        /// package is installed and the integration toggle is enabled.
        /// </summary>
        protected void InitializeCharacterControllerProSupport()
        {
            CCPro.InitializeSupport();
            NotifyCharacterControllerProRuntimeReady();
        }

        /// <summary>
        /// Updates the optional Character Controller Pro cached movement data.
        /// </summary>
        protected void UpdateCharacterControllerProSupport()
        {
            CCPro.UpdateSupport();
        }

        /// <summary>
        /// Resets the cached Character Controller Pro runtime data.
        /// </summary>
        protected void ResetCharacterControllerProRuntimeState()
        {
            CCPro.ResetRuntimeState();
        }

        /// <summary>
        /// Binds the configured input source to this brain.
        /// </summary>
        protected void BindInputSource()
        {
            Input.BindSource();
        }

        /// <summary>
        /// Assigns one colocated input source when the brain still has no
        /// explicit source configured.
        /// </summary>
        /// <param name="inputSource">Input source discovered on this GameObject.</param>
        internal void TryAssignInputSource(FSMInputSource inputSource)
        {
            Input.TryAssignSource(inputSource);
        }

        /// <summary>
        /// Unbinds the configured input source from this brain.
        /// </summary>
        protected void UnbindInputSource()
        {
            Input.UnbindSource();
        }

        /// <summary>
        /// Subscribes the machine to Character Controller Pro actor callbacks.
        /// </summary>
        protected void TrySubscribeCharacterControllerProCallbacks()
        {
            CCPro.SubscribeCallbacks();
        }

        /// <summary>
        /// Unsubscribes the machine from Character Controller Pro actor callbacks.
        /// </summary>
        protected void TryUnsubscribeCharacterControllerProCallbacks()
        {
            CCPro.UnsubscribeCallbacks();
        }

        /// <summary>
        /// Changes the status of the machine
        /// </summary>
        /// <param name="status"></param>
        internal void ChangeStatusCore(MachineStatus status)
        {
            _status = status;
            _statusChanged?.Invoke(_status);

            if (status == MachineStatus.Off)
            {
                _currentStateName = "None";
            }
        }

        /// <summary>
        /// Caches optional lifecycle hooks on derived brain types.
        /// </summary>
        private sealed class CachedBrainLifecycleMethods
        {
            private readonly MethodInfo _afterInitializedMethod;
            private readonly MethodInfo _beforeInitializedMethod;

            public CachedBrainLifecycleMethods(Type type)
            {
                _beforeInitializedMethod = GetMethod(type, "BeforeInitialized");
                _afterInitializedMethod = GetMethod(type, "AfterInitialized");
            }

            public void InvokeBeforeInitialized(FSMBrain brain)
            {
                _beforeInitializedMethod?.Invoke(brain, null);
            }

            public void InvokeAfterInitialized(FSMBrain brain)
            {
                _afterInitializedMethod?.Invoke(brain, null);
            }

            private static MethodInfo GetMethod(Type type, string methodName)
            {
                return type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);
            }
        }

        private static CachedBrainLifecycleMethods GetCachedBrainLifecycleMethods(Type type)
        {
            if (s_cachedBrainLifecycleMethods.TryGetValue(
                    type,
                    out CachedBrainLifecycleMethods lifecycleMethods))
            {
                return lifecycleMethods;
            }

            lifecycleMethods = new CachedBrainLifecycleMethods(type);
            s_cachedBrainLifecycleMethods.Add(type, lifecycleMethods);
            return lifecycleMethods;
        }

        /// <summary>
        /// Caches optional Character Controller Pro state hooks on derived state types.
        /// </summary>
        private sealed class CachedCharacterControllerProStateMethods
        {
            private readonly Action<IState> _postFixedTickAction;
            private readonly Action<IState, float> _postSimulationAction;
            private readonly Action<IState> _preFixedTickAction;
            private readonly Action<IState, float> _preSimulationAction;
            private readonly Action<IState> _runtimeReadyAction;
            private readonly Action<IState, int> _tickIkAction;

            public CachedCharacterControllerProStateMethods(Type type)
            {
                _runtimeReadyAction = CreateStateAction(type, "RuntimeReady");
                _preFixedTickAction = CreateStateAction(type, "PreFixedTick");
                _postFixedTickAction = CreateStateAction(type, "PostFixedTick");
                _preSimulationAction = CreateStateAction<float>(type, "PreCharacterSimulation");
                _postSimulationAction = CreateStateAction<float>(type, "PostCharacterSimulation");
                _tickIkAction = CreateStateAction<int>(type, "TickIK");
            }

            public void InvokeRuntimeReady(IState state)
            {
                _runtimeReadyAction?.Invoke(state);
            }

            public void InvokePostFixedTick(IState state)
            {
                _postFixedTickAction?.Invoke(state);
            }

            public void InvokePostSimulation(IState state, float dt)
            {
                _postSimulationAction?.Invoke(state, dt);
            }

            public void InvokePreFixedTick(IState state)
            {
                _preFixedTickAction?.Invoke(state);
            }

            public void InvokePreSimulation(IState state, float dt)
            {
                _preSimulationAction?.Invoke(state, dt);
            }

            public void InvokeTickIk(IState state, int layerIndex)
            {
                _tickIkAction?.Invoke(state, layerIndex);
            }

            private static Action<IState> CreateStateAction(Type type, string methodName)
            {
                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);

                if (method == null)
                {
                    return null;
                }

                ParameterExpression stateParameter = Expression.Parameter(typeof(IState), "state");
                MethodCallExpression callExpression = Expression.Call(
                    Expression.Convert(stateParameter, type),
                    method);

                return Expression.Lambda<Action<IState>>(
                    callExpression,
                    stateParameter)
                    .Compile();
            }

            private static Action<IState, T> CreateStateAction<T>(Type type, string methodName)
            {
                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(T) },
                    null);

                if (method == null)
                {
                    return null;
                }

                ParameterExpression stateParameter = Expression.Parameter(typeof(IState), "state");
                ParameterExpression valueParameter = Expression.Parameter(typeof(T), "value");
                MethodCallExpression callExpression = Expression.Call(
                    Expression.Convert(stateParameter, type),
                    method,
                    valueParameter);

                return Expression.Lambda<Action<IState, T>>(
                    callExpression,
                    stateParameter,
                    valueParameter)
                    .Compile();
            }
        }

        private static CachedCharacterControllerProStateMethods GetCachedCharacterControllerProStateMethods(
            Type type)
        {
            if (s_cachedCharacterControllerProStateMethods.TryGetValue(
                    type,
                    out CachedCharacterControllerProStateMethods cachedMethods))
            {
                return cachedMethods;
            }

            cachedMethods = new CachedCharacterControllerProStateMethods(type);
            s_cachedCharacterControllerProStateMethods.Add(type, cachedMethods);
            return cachedMethods;
        }

        /// <summary>
        /// Resolves Simple Blackboard types and methods without introducing a hard package dependency.
        /// </summary>
        internal static class SimpleBlackboardBridge
        {
            /// <summary>
            /// Gets whether the Simple Blackboard runtime types were resolved
            /// successfully.
            /// </summary>
            public static bool IsAvailable =>
                SimpleBlackboardRuntimeBridge.IsAvailable;

            /// <summary>
            /// Gets the resolved Simple Blackboard container type.
            /// </summary>
            public static Type ContainerType =>
                SimpleBlackboardRuntimeBridge.ContainerType;

            /// <summary>
            /// Tries to resolve the runtime blackboard object from a container
            /// component.
            /// </summary>
            /// <param name="container">The candidate container component.</param>
            /// <param name="blackboard">The resolved blackboard instance.</param>
            /// <returns>True if a valid blackboard was resolved.</returns>
            public static bool TryGetBlackboard(Component container, out object blackboard)
            {
                return SimpleBlackboardRuntimeBridge.TryGetBlackboard(
                    container,
                    out blackboard);
            }

            /// <summary>
            /// Tries to read a typed value from a resolved blackboard instance.
            /// </summary>
            /// <typeparam name="T">The value type to read.</typeparam>
            /// <param name="blackboard">The resolved blackboard instance.</param>
            /// <param name="propertyName">The blackboard property name.</param>
            /// <param name="value">The resolved value if found.</param>
            /// <returns>
            /// True if the value exists and matches the requested type.
            /// </returns>
            public static bool TryGetValue<T>(
                object blackboard,
                string propertyName,
                out T value)
            {
                return SimpleBlackboardRuntimeBridge.TryGetValue(
                    blackboard,
                    propertyName,
                    out value);
            }

            /// <summary>
            /// Writes a typed value into a resolved blackboard instance.
            /// </summary>
            /// <typeparam name="T">The value type to write.</typeparam>
            /// <param name="blackboard">The resolved blackboard instance.</param>
            /// <param name="propertyName">The blackboard property name.</param>
            /// <param name="value">The value to write.</param>
            /// <returns>True if the value was written successfully.</returns>
            public static bool SetValue<T>(
                object blackboard,
                string propertyName,
                T value)
            {
                return SimpleBlackboardRuntimeBridge.SetValue(
                    blackboard,
                    propertyName,
                    value);
            }

            /// <summary>
            /// Tries to read an untyped value from a resolved blackboard
            /// instance.
            /// </summary>
            /// <param name="blackboard">The resolved blackboard instance.</param>
            /// <param name="propertyName">The blackboard property name.</param>
            /// <param name="value">The resolved value if found.</param>
            /// <returns>True if the property exists.</returns>
            public static bool TryGetObjectValue(
                object blackboard,
                string propertyName,
                out object value)
            {
                return SimpleBlackboardRuntimeBridge.TryGetObjectValue(
                    blackboard,
                    propertyName,
                    out value);
            }

            /// <summary>
            /// Gets whether a resolved blackboard contains a property.
            /// </summary>
            /// <param name="blackboard">The resolved blackboard instance.</param>
            /// <param name="propertyName">The blackboard property name.</param>
            /// <returns>True if the property exists.</returns>
            public static bool ContainsValue(object blackboard, string propertyName)
            {
                return SimpleBlackboardRuntimeBridge.ContainsValue(
                    blackboard,
                    propertyName);
            }

            /// <summary>
            /// Recreates the runtime blackboard owned by a container component.
            /// </summary>
            /// <param name="container">The candidate container component.</param>
            /// <returns>
            /// True when the container recreated its runtime blackboard.
            /// </returns>
            public static bool RecreateBlackboard(Component container)
            {
                return SimpleBlackboardRuntimeBridge.RecreateBlackboard(container);
            }

            /// <summary>
            /// Tries to enumerate the available blackboard property names and
            /// their value types.
            /// </summary>
            /// <param name="blackboard">The resolved blackboard instance.</param>
            /// <param name="propertyMetadata">
            /// Receives the discovered property metadata.
            /// </param>
            /// <returns>
            /// True when the blackboard metadata APIs were resolved and
            /// invoked.
            /// </returns>
            public static bool TryGetPropertyMetadata(
                object blackboard,
                Dictionary<string, Type> propertyMetadata)
            {
                return SimpleBlackboardRuntimeBridge.TryGetPropertyMetadata(
                    blackboard,
                    propertyMetadata);
            }
        }

        #endregion

        #region Providing The machine

        /// <summary>
        /// Casts the current instance to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast to.</typeparam>
        /// <returns>The instance casted to the specified type.</returns>
        public T As<T>() where T : FSMBrain
        {
            return this as T;
        }

        #endregion

        #region Machine's Logic

        internal void RequestStateChangeCore(IState state)
        {
            if (_status != MachineStatus.On || state == null) return;

            ChangeState(
                state,
                new StateTransitionReport(StateTransitionReason.ExternalRequest));
        }

        internal void CompleteStateCore(IState target = null)
        {
            if (_status != MachineStatus.On)
            {
                Debug.LogError($"Trying to end state on a machine that is not turned on. ", this);
                return;
            }

            if (target != null)
            {
                ChangeState(
                    target,
                    new StateTransitionReport(StateTransitionReason.NaturalTransition));
                return;
            }

            if (_defaultState != null)
            {
                ChangeState(
                    _defaultState,
                    new StateTransitionReport(StateTransitionReason.NaturalTransition));
                return;
            }

            ExecuteStateOperation(_currentState, state => state.Exit(), "CompleteState exit");
        }

        internal void FailStateCore(IState target = null, string message = null)
        {
            if (_status != MachineStatus.On)
            {
                Debug.LogError($"Trying to fail state on a machine that is not turned on. ", this);
                return;
            }

            StateTransitionReport transitionReport = new(
                StateTransitionReason.ErrorTransition,
                message);

            if (target != null && !IsStateFaulted(target))
            {
                ChangeState(target, transitionReport);
                return;
            }

            IState fallbackState = ResolveErrorFallbackState(_currentState);

            if (fallbackState != null)
            {
                ChangeState(fallbackState, transitionReport);
                return;
            }

            AbortMachineAfterStateFailure(transitionReport);
        }

        /// <summary>
        /// Changes the state.
        /// </summary>
        /// <param name="state">The new state to change to.</param>
        /// <param name="transitionReason">The reason for the transition.</param>
        protected virtual void ChangeState(
            IState state,
            StateTransitionReport transitionReport)
        {
            // Do not change state if it is the same as the current state or null
            if (state == _currentState || state == null) return;

            if (IsStateFaulted(state))
            {
                StateTransitionReport faultedTargetReport = new(
                    StateTransitionReason.ErrorTransition,
                    BuildFaultedTargetMessage(state));

                IState fallbackState = ResolveErrorFallbackState(state);

                if (fallbackState != null && fallbackState != state)
                {
                    if (fallbackState == _currentState)
                    {
                        _lastTransitionReport = faultedTargetReport;
                        return;
                    }

                    ChangeState(fallbackState, faultedTargetReport);
                    return;
                }

                AbortMachineAfterStateFailure(faultedTargetReport);
                return;
            }

            IState previousState = _currentState;

            // Define the previous state
            _previousState = previousState;

            // Invoke the exit action of the current state
            if (!TryExecuteStateOperation(previousState, stateToExit => stateToExit.Exit(), "State exit"))
            {
                return;
            }

            // Change the current state
            _currentState = state;
            _lastTransitionReport = transitionReport;

            // Invoke the enter action of the new state
            if (!TryExecuteStateOperation(_currentState, stateToEnter => stateToEnter.Enter(), "State enter"))
            {
                return;
            }

            _firstEnteredState ??= _currentState;

            // Update the current state name
            _currentStateName = CurrentState.DisplayName;

            // Announce the new state only after the new state entered successfully.
            _stateChanged.Invoke(_currentState, _previousState);
        }

        /// <summary>
        /// Handles the tick event.
        /// </summary>
        protected virtual void EvaluateTransition()
        {
            if (_currentState == null) return;

            if (_currentState.WantsToTransition(out IState targetState))
            {
                ChangeState(
                    targetState,
                    new StateTransitionReport(StateTransitionReason.ConditionTransition));
            }
        }

        /// <summary>
        /// Invokes the optional Character Controller Pro pre-fixed-tick hook on the current state.
        /// </summary>
        /// <param name="state">The state that owns the hook.</param>
        protected void TryExecuteCharacterControllerProPreFixedTick(IState state)
        {
            TryExecuteCharacterControllerProStateAction(
                state,
                static (methods, currentState) => methods.InvokePreFixedTick(currentState),
                "PreFixedTick");
        }

        /// <summary>
        /// Invokes the optional Character Controller Pro runtime-ready hook on
        /// one state.
        /// </summary>
        /// <param name="state">The state that owns the hook.</param>
        protected void TryExecuteCharacterControllerProRuntimeReady(IState state)
        {
            if (!IsCharacterControllerProState(state))
            {
                return;
            }

            TryExecuteCharacterControllerProStateAction(
                state,
                static (methods, currentState) => methods.InvokeRuntimeReady(currentState),
                "RuntimeReady");
        }

        /// <summary>
        /// Invokes the optional Character Controller Pro post-fixed-tick hook on the current state.
        /// </summary>
        /// <param name="state">The state that owns the hook.</param>
        protected void TryExecuteCharacterControllerProPostFixedTick(IState state)
        {
            TryExecuteCharacterControllerProStateAction(
                state,
                static (methods, currentState) => methods.InvokePostFixedTick(currentState),
                "PostFixedTick");
        }

        /// <summary>
        /// Invokes the optional Character Controller Pro pre-simulation hook on the current state.
        /// </summary>
        /// <param name="state">The state that owns the hook.</param>
        /// <param name="dt">The simulation delta time.</param>
        protected void TryExecuteCharacterControllerProPreSimulation(IState state, float dt)
        {
            TryExecuteCharacterControllerProStateAction(
                state,
                dt,
                static (methods, currentState, deltaTime) =>
                    methods.InvokePreSimulation(currentState, deltaTime),
                "PreCharacterSimulation");
        }

        /// <summary>
        /// Invokes the optional Character Controller Pro post-simulation hook on the current state.
        /// </summary>
        /// <param name="state">The state that owns the hook.</param>
        /// <param name="dt">The simulation delta time.</param>
        protected void TryExecuteCharacterControllerProPostSimulation(IState state, float dt)
        {
            TryExecuteCharacterControllerProStateAction(
                state,
                dt,
                static (methods, currentState, deltaTime) =>
                    methods.InvokePostSimulation(currentState, deltaTime),
                "PostCharacterSimulation");
        }

        /// <summary>
        /// Invokes the optional Character Controller Pro IK hook on the current state.
        /// </summary>
        /// <param name="state">The state that owns the hook.</param>
        /// <param name="layerIndex">The animator IK layer index.</param>
        protected void TryExecuteCharacterControllerProTickIk(IState state, int layerIndex)
        {
            TryExecuteCharacterControllerProStateAction(
                state,
                layerIndex,
                static (methods, currentState, currentLayerIndex) =>
                    methods.InvokeTickIk(currentState, currentLayerIndex),
                "TickIK");
        }

        /// <summary>
        /// Dispatches Character Controller Pro actor pre-simulation callbacks.
        /// </summary>
        /// <param name="dt">The simulation delta time.</param>
        protected virtual void OnCharacterControllerProPreSimulation(float dt)
        {
            if (!UseCharacterControllerPro)
            {
                return;
            }

            TryExecuteCharacterControllerProPreSimulation(CurrentState, dt);
        }

        /// <summary>
        /// Dispatches Character Controller Pro actor post-simulation callbacks.
        /// </summary>
        /// <param name="dt">The simulation delta time.</param>
        protected virtual void OnCharacterControllerProPostSimulation(float dt)
        {
            if (!UseCharacterControllerPro)
            {
                return;
            }

            TryExecuteCharacterControllerProPostSimulation(CurrentState, dt);
        }

        /// <summary>
        /// Dispatches Character Controller Pro animator IK callbacks.
        /// </summary>
        /// <param name="layerIndex">The animator IK layer index.</param>
        protected virtual void OnCharacterControllerProAnimatorIk(int layerIndex)
        {
            if (!UseCharacterControllerPro)
            {
                return;
            }

            TryExecuteCharacterControllerProTickIk(CurrentState, layerIndex);
        }

        /// <summary>
        /// Notifies every loaded Character Controller Pro state that the brain
        /// session runtime is ready.
        /// </summary>
        protected void NotifyCharacterControllerProRuntimeReady()
        {
            if (!UseCharacterControllerPro)
            {
                return;
            }

            List<IState> loadedStates = States.GetAllStates();

            for (int index = 0; index < loadedStates.Count; index++)
            {
                TryExecuteCharacterControllerProRuntimeReady(loadedStates[index]);
            }
        }

        private static bool IsCharacterControllerProState(IState state)
        {
            if (state == null)
            {
                return false;
            }

            Type[] interfaces = state.GetType().GetInterfaces();

            for (int index = 0; index < interfaces.Length; index++)
            {
                if (interfaces[index].FullName
                    == "IndieGabo.HandyTools.FSMModule.CCPro.ICCProState")
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryExecuteCharacterControllerProStateAction(
            IState state,
            Action<CachedCharacterControllerProStateMethods, IState> operation,
            string operationName)
        {
            if (!UseCharacterControllerPro || state == null || operation == null)
            {
                return true;
            }

            try
            {
                CachedCharacterControllerProStateMethods cachedMethods =
                    GetCachedCharacterControllerProStateMethods(state.GetType());

                operation(cachedMethods, state);
                return true;
            }
            catch (StateFailureException exception)
            {
                HandleStateFailure(state, exception, operationName);
                return false;
            }
        }

        private bool TryExecuteCharacterControllerProStateAction<T>(
            IState state,
            T value,
            Action<CachedCharacterControllerProStateMethods, IState, T> operation,
            string operationName)
        {
            if (!UseCharacterControllerPro || state == null || operation == null)
            {
                return true;
            }

            try
            {
                CachedCharacterControllerProStateMethods cachedMethods =
                    GetCachedCharacterControllerProStateMethods(state.GetType());

                operation(cachedMethods, state, value);
                return true;
            }
            catch (StateFailureException exception)
            {
                HandleStateFailure(state, exception, operationName);
                return false;
            }
        }

        /// <summary>
        /// Marks a state as unavailable for the current machine session after it
        /// failed during initialization.
        /// </summary>
        /// <param name="state">The state that failed to initialize.</param>
        /// <param name="exception">The state failure exception.</param>
        internal void HandleStateInitializationFailure(
            IState state,
            StateFailureException exception)
        {
            IState failedState = exception?.FailedState ?? state;

            if (failedState == null)
            {
                return;
            }

            _faultedStates.Add(failedState);

            if (_defaultState == failedState)
            {
                _defaultState = null;
            }

            Debug.LogError(BuildStateFailureLogMessage(failedState, "Initialize", exception), this);
            Debug.LogException(exception, this);
        }

        /// <summary>
        /// Executes a brain-owned operation and converts state failure exceptions
        /// into the machine recovery path.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="relatedState">The state related to the current operation.</param>
        /// <param name="operationName">The operation label used for diagnostics.</param>
        protected void ExecuteMachineOperation(
            Action operation,
            IState relatedState,
            string operationName)
        {
            try
            {
                operation?.Invoke();
            }
            catch (StateFailureException exception)
            {
                HandleStateFailure(relatedState, exception, operationName);
            }
        }

        /// <summary>
        /// Executes a state-owned operation and converts state failure exceptions
        /// into the machine recovery path.
        /// </summary>
        /// <param name="state">The state that owns the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The operation label used for diagnostics.</param>
        protected void ExecuteStateOperation(
            IState state,
            Action<IState> operation,
            string operationName)
        {
            TryExecuteStateOperation(state, operation, operationName);
        }

        /// <summary>
        /// Tries to execute a state-owned operation and returns whether the
        /// execution completed successfully.
        /// </summary>
        /// <param name="state">The state that owns the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The operation label used for diagnostics.</param>
        /// <returns>True when the operation completed without a state failure.</returns>
        protected bool TryExecuteStateOperation(
            IState state,
            Action<IState> operation,
            string operationName)
        {
            if (state == null || operation == null)
            {
                return true;
            }

            try
            {
                operation(state);
                return true;
            }
            catch (StateFailureException exception)
            {
                HandleStateFailure(state, exception, operationName);
                return false;
            }
        }

        /// <summary>
        /// Handles a state failure without allowing the exception to leave the machine.
        /// </summary>
        /// <param name="failedState">The state that failed.</param>
        /// <param name="exception">The exception raised by the state.</param>
        /// <param name="operationName">The operation label used for diagnostics.</param>
        protected virtual void HandleStateFailure(
            IState failedState,
            StateFailureException exception,
            string operationName)
        {
            failedState = exception?.FailedState ?? failedState;

            if (failedState != null)
            {
                _faultedStates.Add(failedState);

                if (_defaultState == failedState)
                {
                    _defaultState = null;
                }
            }

            StateTransitionReport transitionReport = new(
                StateTransitionReason.ErrorTransition,
                BuildStateFailureHistoryMessage(failedState, operationName, exception));

            Debug.LogError(BuildStateFailureLogMessage(failedState, operationName, exception), this);
            Debug.LogException(exception, this);

            if (_status != MachineStatus.On)
            {
                _lastTransitionReport = transitionReport;
                return;
            }

            if (_isRecoveringFromStateFailure)
            {
                AbortMachineAfterStateFailure(transitionReport);
                return;
            }

            _isRecoveringFromStateFailure = true;

            try
            {
                IState fallbackState = ResolveErrorFallbackState(failedState);

                if (fallbackState != null)
                {
                    if (fallbackState == _currentState)
                    {
                        _lastTransitionReport = transitionReport;
                        return;
                    }

                    ChangeState(fallbackState, transitionReport);
                    return;
                }

                AbortMachineAfterStateFailure(transitionReport);
            }
            finally
            {
                _isRecoveringFromStateFailure = false;
            }
        }

        /// <summary>
        /// Resolves the state that should receive control after an error transition.
        /// </summary>
        /// <param name="excludedState">The failing state that must not be reused.</param>
        /// <returns>The fallback state, or null when no safe fallback exists.</returns>
        protected virtual IState ResolveErrorFallbackState(IState excludedState)
        {
            if (_defaultState != null
                && _defaultState != excludedState
                && !IsStateFaulted(_defaultState))
            {
                return _defaultState;
            }

            if (_firstEnteredState != null
                && _firstEnteredState != excludedState
                && !IsStateFaulted(_firstEnteredState))
            {
                return _firstEnteredState;
            }

            return null;
        }

        /// <summary>
        /// Gets whether the specified state is marked as faulted in the current machine session.
        /// </summary>
        /// <param name="state">The state to inspect.</param>
        /// <returns>True when the state is currently faulted.</returns>
        protected bool IsStateFaulted(IState state)
        {
            return state != null && _faultedStates.Contains(state);
        }

        /// <summary>
        /// Aborts the current machine execution after an unrecoverable state failure.
        /// </summary>
        /// <param name="transitionReport">The report that describes the unrecoverable failure.</param>
        protected virtual void AbortMachineAfterStateFailure(
            StateTransitionReport transitionReport)
        {
            _lastTransitionReport = transitionReport;
            _currentState = null;
            _previousState = null;
            _currentStateName = "None";
            ChangeStatusCore(MachineStatus.Off);
        }

        /// <summary>
        /// Builds a concise message for history entries generated by state failures.
        /// </summary>
        /// <param name="failedState">The state that failed.</param>
        /// <param name="operationName">The operation label used for diagnostics.</param>
        /// <param name="exception">The exception that triggered recovery.</param>
        /// <returns>The history message to store with the transition report.</returns>
        protected virtual string BuildStateFailureHistoryMessage(
            IState failedState,
            string operationName,
            StateFailureException exception)
        {
            string stateName = failedState?.DisplayName ?? "Unknown state";

            if (!string.IsNullOrWhiteSpace(exception?.Message))
            {
                return $"{stateName} failed during {operationName}: {exception.Message}";
            }

            return $"{stateName} failed during {operationName}.";
        }

        /// <summary>
        /// Builds the runtime log message emitted when a state failure is intercepted.
        /// </summary>
        /// <param name="failedState">The state that failed.</param>
        /// <param name="operationName">The operation label used for diagnostics.</param>
        /// <param name="exception">The exception that triggered recovery.</param>
        /// <returns>The formatted runtime log message.</returns>
        protected virtual string BuildStateFailureLogMessage(
            IState failedState,
            string operationName,
            StateFailureException exception)
        {
            string stateName = failedState?.DisplayName ?? "Unknown state";
            return $"State failure intercepted on '{stateName}' during {operationName}. The machine will recover through an error transition.";
        }

        /// <summary>
        /// Builds the message used when code tries to transition into a state that
        /// has already failed in the current session.
        /// </summary>
        /// <param name="state">The faulted target state.</param>
        /// <returns>The error-transition message.</returns>
        protected virtual string BuildFaultedTargetMessage(IState state)
        {
            string stateName = state?.DisplayName ?? "Unknown state";
            return $"Transition target '{stateName}' is marked as non-functional for this machine session.";
        }

        #endregion

    }

}
