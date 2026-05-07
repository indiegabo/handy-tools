namespace IndieGabo.HandyTools.FSMModule.Implementations
{
    /// <summary>
    /// The state machine
    /// </summary>
    public abstract class GenericHandyFSMBrain<TBaseState, TDefaultState> : FSMBrain
    {
        #region Machine Engine

        /// <summary>
        /// This method recognizes and initializes the states for the machine.
        /// </summary>
        protected virtual void BeforeInitialized()
        {
            States.LoadStatesFromBaseType(typeof(TBaseState), false);
            _defaultState = States.Get(typeof(TDefaultState));
        }

        #endregion
    }

    /// <summary>
    /// The state machine
    /// </summary>
    public abstract class GenericHandyFSMBrain<TBaseState> : FSMBrain
    {
        #region Machine Engine

        /// <summary>
        /// This method recognizes and initializes the states for the machine.
        /// </summary>
        protected virtual void BeforeInitialized()
        {
            States.LoadStatesFromBaseType(typeof(TBaseState), false);
        }

        #endregion
    }

}
