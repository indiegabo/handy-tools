using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    public class StateProvider
    {
        private static readonly Dictionary<Type, Type[]> s_derivedRuntimeStateTypes =
            new();

        private static readonly Dictionary<Type, Func<State>> s_runtimeStateFactories =
            new();

        #region Fields

        protected Dictionary<Type, IState> _statesByType;
        protected Dictionary<string, IState> _statesByKey;
        protected FSMBrain _machine;

        #endregion

        #region Constructors

        public StateProvider(FSMBrain machine)
        {
            _machine = machine;
            _statesByType = new Dictionary<Type, IState>();
            _statesByKey = new Dictionary<string, IState>();
        }

        #endregion

        #region Loading States

        /// <summary>
        /// Loads the states derived from the provided base state type into 
        /// the provider.
        /// </summary>
        /// <param name="baseStateType">The base state type.</param>
        public void LoadStatesFromBaseType<T>() where T : State
        {
            LoadStatesFromBaseType(typeof(T));
        }

        /// <summary>
        /// Loads the states derived from the provided base state type into 
        /// the provider.
        /// </summary>
        /// <param name="baseStateType">The base state type.</param>
        public void LoadStatesFromBaseType(Type baseStateType, bool initializeAfterCommit = true)
        {
            Type[] childrenTypes = GetDerivedRuntimeStateTypes(baseStateType);
            State[] instantiatedStates = initializeAfterCommit
                ? new State[childrenTypes.Length]
                : null;

            for (int index = 0; index < childrenTypes.Length; index++)
            {
                State childState = CreateRuntimeState(childrenTypes[index]);

                if (childState == null)
                {
                    continue;
                }

                CommitState(childState);

                if (initializeAfterCommit)
                {
                    instantiatedStates[index] = childState;
                }
            }

            if (!initializeAfterCommit) return;

            for (int index = 0; index < instantiatedStates.Length; index++)
            {
                InitializeState(instantiatedStates[index]);
            }
        }

        /// <summary>
        /// Receives a list of scriptable states, commits all of them and initializes them.
        /// </summary>
        /// <param name="states">The list of scriptable states to load.</param>
        public void LoadStatesFromScriptablesList(List<ScriptableState> states, bool initializeAfterCommit = true)
        {
            if (states == null || states.Count == 0)
            {
                return;
            }

            ScriptableState[] committedStates = initializeAfterCommit
                ? new ScriptableState[states.Count]
                : null;

            int committedStatesCount = 0;

            for (int index = 0; index < states.Count; index++)
            {
                ScriptableState scriptableState = states[index];

                if (scriptableState == null)
                {
                    continue;
                }

                ScriptableState clone = UnityEngine.Object.Instantiate(scriptableState);

                CommitState(clone);

                if (initializeAfterCommit)
                {
                    committedStates[committedStatesCount++] = clone;
                }
            }

            if (!initializeAfterCommit) return;

            for (int index = 0; index < committedStatesCount; index++)
            {
                InitializeState(committedStates[index]);
            }
        }

        public void InitializeAllStates()
        {
            foreach (IState state in _statesByType.Values)
            {
                InitializeState(state);
            }
        }

        /// <summary>
        /// Loads a state of the specified type into the provider.
        /// </summary>
        /// <param name="stateType">The type of the state to load.</param>
        public void LoadState<T>() where T : State
        {
            LoadState(typeof(T));
        }

        /// <summary>
        /// Loads a state of the specified type into the provider.
        /// </summary>
        /// <param name="stateType">The type of the state to load.</param>
        public void LoadState(Type stateType)
        {
            State state = CreateRuntimeState(stateType);

            if (state == null)
            {
                return;
            }

            CommitState(state);

            InitializeState(state);
        }

        public void LoadState(IState state)
        {
            CommitState(state);
            InitializeState(state);
        }

        private void InitializeState(IState state)
        {
            if (state == null)
            {
                return;
            }

            try
            {
                state.Initialize(_machine);
            }
            catch (StateFailureException exception)
            {
                _machine.HandleStateInitializationFailure(state, exception);
            }
        }

        /// <summary>
        /// Commits the state adding it to the dictionaries.
        /// </summary>
        /// <param name="state">The state to be committed.</param>
        protected void CommitState(IState state)
        {
            if (state == null)
            {
                return;
            }

            Type stateType = state.GetType();

            if (!_statesByType.TryGetValue(stateType, out _))
            {
                _statesByType.Add(stateType, state);
            }

            string key = state.Key;

            if (!string.IsNullOrEmpty(key) && !_statesByKey.TryGetValue(key, out _))
            {
                _statesByKey.Add(key, state);
            }
        }

        #endregion

        #region Serving

        public bool IsLoaded(IState state)
        {
            return _statesByType.ContainsKey(state.GetType());
        }

        public IState Get(Type stateType)
        {
            if (_statesByType.TryGetValue(stateType, out IState state))
            {
                return state;
            }

            return null;
        }

        public IState Get(string key)
        {
            if (_statesByKey.TryGetValue(key, out IState state))
            {
                return state;
            }

            return null;
        }

        public T Get<T>() where T : IState
        {
            return (T)Get(typeof(T));
        }


        public bool TryGet(Type type, out IState state)
        {
            return _statesByType.TryGetValue(type, out state);
        }

        public bool TryGet<T>(out IState state) where T : IState
        {
            return _statesByType.TryGetValue(typeof(T), out state);
        }

        public bool TryGet(string key, out IState state)
        {
            return _statesByKey.TryGetValue(key, out state);
        }

        public List<IState> GetAllStates()
        {
            List<IState> states = new(_statesByType.Count);

            foreach (IState state in _statesByType.Values)
            {
                states.Add(state);
            }

            return states;
        }

        private static Type[] GetDerivedRuntimeStateTypes(Type baseStateType)
        {
            if (s_derivedRuntimeStateTypes.TryGetValue(baseStateType, out Type[] cachedTypes))
            {
                return cachedTypes;
            }

            Type[] assemblyTypes = baseStateType.Assembly.GetTypes();
            List<Type> derivedTypes = new(assemblyTypes.Length);

            for (int index = 0; index < assemblyTypes.Length; index++)
            {
                Type candidateType = assemblyTypes[index];

                if (!candidateType.IsClass
                    || candidateType.IsAbstract
                    || !baseStateType.IsAssignableFrom(candidateType))
                {
                    continue;
                }

                derivedTypes.Add(candidateType);
            }

            cachedTypes = derivedTypes.Count == 0
                ? Array.Empty<Type>()
                : derivedTypes.ToArray();

            s_derivedRuntimeStateTypes.Add(baseStateType, cachedTypes);
            return cachedTypes;
        }

        private static State CreateRuntimeState(Type stateType)
        {
            if (!s_runtimeStateFactories.TryGetValue(stateType, out Func<State> factory))
            {
                factory = CreateRuntimeStateFactory(stateType);
                s_runtimeStateFactories.Add(stateType, factory);
            }

            return factory();
        }

        private static Func<State> CreateRuntimeStateFactory(Type stateType)
        {
            ConstructorInfo constructor = stateType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (constructor == null)
            {
                return () => Activator.CreateInstance(stateType) as State;
            }

            return () => constructor.Invoke(null) as State;
        }

        #endregion
    }
}