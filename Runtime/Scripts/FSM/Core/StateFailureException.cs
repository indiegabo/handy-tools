using System;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Identifies a state-level failure that should trigger the machine error
    /// recovery path instead of crashing the runtime.
    /// </summary>
    [Serializable]
    public sealed class StateFailureException : Exception
    {
        /// <summary>
        /// Gets the state instance that raised the failure when the runtime was
        /// able to capture that context.
        /// </summary>
        internal IState FailedState { get; private set; }

        /// <summary>
        /// Creates a new empty state failure exception.
        /// </summary>
        public StateFailureException()
        {
        }

        /// <summary>
        /// Creates a new state failure exception with a human-readable message.
        /// </summary>
        /// <param name="message">The message describing the state failure.</param>
        public StateFailureException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new state failure exception with a human-readable message
        /// and an inner exception.
        /// </summary>
        /// <param name="message">The message describing the state failure.</param>
        /// <param name="innerException">The underlying cause of the failure.</param>
        public StateFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Attaches the state that raised the exception when that information is
        /// not already present.
        /// </summary>
        /// <param name="state">The state that raised the failure.</param>
        /// <returns>The same exception instance so it can be rethrown.</returns>
        public StateFailureException WithState(IState state)
        {
            FailedState ??= state;
            return this;
        }
    }
}