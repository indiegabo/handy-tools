using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Provides the state-loading and state-lookup surface delegated by one
    /// FSM brain.
    /// </summary>
    public sealed class FSMBrainStatesDomain
    {
        #region Fields

        private readonly StateProvider _stateProvider;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes one states domain backed by the provided state
        /// provider.
        /// </summary>
        /// <param name="stateProvider">The provider that owns the state cache.</param>
        public FSMBrainStatesDomain(StateProvider stateProvider)
        {
            _stateProvider = stateProvider;
        }

        #endregion

        #region Loading

        /// <summary>
        /// Loads every runtime state derived from the provided base type.
        /// </summary>
        /// <param name="stateType">The base runtime state type.</param>
        public void LoadStatesFromBaseType(
            Type stateType,
            bool initializeAfterCommit = true)
        {
            _stateProvider.LoadStatesFromBaseType(stateType, initializeAfterCommit);
        }

        /// <summary>
        /// Loads the provided scriptable states into the provider.
        /// </summary>
        /// <param name="states">The scriptable states to load.</param>
        /// <param name="initializeAfterCommit">
        /// Whether the provider should initialize the committed states
        /// immediately.
        /// </param>
        public void LoadStatesFromScriptablesList(
            List<ScriptableState> states,
            bool initializeAfterCommit = true)
        {
            _stateProvider.LoadStatesFromScriptablesList(
                states,
                initializeAfterCommit);
        }

        /// <summary>
        /// Initializes every currently loaded state.
        /// </summary>
        public void InitializeAllStates()
        {
            _stateProvider.InitializeAllStates();
        }

        /// <summary>
        /// Loads one runtime state of the specified type.
        /// </summary>
        /// <param name="stateType">The runtime state type to load.</param>
        public void LoadState(Type stateType)
        {
            _stateProvider.LoadState(stateType);
        }

        /// <summary>
        /// Loads one already-instantiated state.
        /// </summary>
        /// <param name="state">The state instance to load.</param>
        public void LoadState(IState state)
        {
            _stateProvider.LoadState(state);
        }

        #endregion

        #region Retrieval

        /// <summary>
        /// Gets whether the provided state is currently loaded.
        /// </summary>
        /// <param name="state">The state instance to inspect.</param>
        /// <returns>True when the state is already loaded.</returns>
        public bool IsLoaded(IState state)
        {
            return _stateProvider.IsLoaded(state);
        }

        /// <summary>
        /// Retrieves one loaded state by runtime type.
        /// </summary>
        /// <param name="stateType">The runtime state type.</param>
        /// <returns>The loaded state, or null when missing.</returns>
        public IState Get(Type stateType)
        {
            return _stateProvider.Get(stateType);
        }

        /// <summary>
        /// Retrieves one loaded state by key.
        /// </summary>
        /// <param name="key">The registered state key.</param>
        /// <returns>The loaded state, or null when missing.</returns>
        public IState Get(string key)
        {
            return _stateProvider.Get(key);
        }

        /// <summary>
        /// Retrieves one loaded state by generic type.
        /// </summary>
        /// <typeparam name="T">The state type to retrieve.</typeparam>
        /// <returns>The loaded state, or null when missing.</returns>
        public T Get<T>() where T : IState
        {
            return _stateProvider.Get<T>();
        }

        /// <summary>
        /// Tries to retrieve one loaded state by runtime type.
        /// </summary>
        /// <param name="stateType">The runtime state type.</param>
        /// <param name="state">The loaded state when found.</param>
        /// <returns>True when the state is loaded.</returns>
        public bool TryGet(Type stateType, out IState state)
        {
            return _stateProvider.TryGet(stateType, out state);
        }

        /// <summary>
        /// Tries to retrieve one loaded state by key.
        /// </summary>
        /// <param name="key">The registered state key.</param>
        /// <param name="state">The loaded state when found.</param>
        /// <returns>True when the state is loaded.</returns>
        public bool TryGet(string key, out IState state)
        {
            return _stateProvider.TryGet(key, out state);
        }

        /// <summary>
        /// Tries to retrieve one loaded state by generic type.
        /// </summary>
        /// <typeparam name="T">The state type to retrieve.</typeparam>
        /// <param name="state">The loaded state when found.</param>
        /// <returns>True when the state is loaded.</returns>
        public bool TryGet<T>(out IState state) where T : IState
        {
            return _stateProvider.TryGet<T>(out state);
        }

        /// <summary>
        /// Copies every loaded state into a new list.
        /// </summary>
        /// <returns>The loaded states owned by the provider.</returns>
        public List<IState> GetAllStates()
        {
            return _stateProvider.GetAllStates();
        }

        #endregion
    }
}