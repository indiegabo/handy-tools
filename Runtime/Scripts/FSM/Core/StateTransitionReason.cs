namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Describes why the machine transitioned from one state to another.
    /// </summary>
    public enum StateTransitionReason
    {
        /// <summary>
        /// The transition source could not be resolved.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The machine entered its first active state when it was turned on.
        /// </summary>
        InitialEntry = 1,

        /// <summary>
        /// The active state ended itself through the EndState API.
        /// </summary>
        VoluntaryExit = 2,

        /// <summary>
        /// The active state was replaced by the transition evaluation pipeline.
        /// </summary>
        Interrupted = 3,

        /// <summary>
        /// The state change was requested explicitly through the external request API.
        /// </summary>
        ExternalRequest = 4,

        /// <summary>
        /// One of the active state's transition conditions evaluated to true.
        /// </summary>
        ConditionTransition = 5,

        /// <summary>
        /// The active state completed its work and advanced normally.
        /// </summary>
        NaturalTransition = 6,

        /// <summary>
        /// The active state failed and transitioned through an error path.
        /// </summary>
        ErrorTransition = 7
    }

    /// <summary>
    /// Captures a transition reason together with optional context for history and diagnostics.
    /// </summary>
    [System.Serializable]
    public struct StateTransitionReport
    {
        /// <summary>
        /// A reusable report that represents the absence of a known transition source.
        /// </summary>
        public static readonly StateTransitionReport Unknown =
            new(StateTransitionReason.Unknown);

        [UnityEngine.SerializeField]
        private StateTransitionReason _reason;

        [UnityEngine.SerializeField]
        private string _message;

        /// <summary>
        /// Creates a new transition report.
        /// </summary>
        /// <param name="reason">The reason that caused the transition.</param>
        /// <param name="message">Optional context that should be shown in the history UI.</param>
        public StateTransitionReport(
            StateTransitionReason reason,
            string message = null)
        {
            _reason = reason;
            _message = string.IsNullOrWhiteSpace(message)
                ? string.Empty
                : message.Trim();
        }

        /// <summary>
        /// Gets the recorded transition reason.
        /// </summary>
        public StateTransitionReason Reason => _reason;

        /// <summary>
        /// Gets the optional transition message.
        /// </summary>
        public string Message => _message ?? string.Empty;

        /// <summary>
        /// Gets whether the report contains a non-empty message.
        /// </summary>
        public bool HasMessage => !string.IsNullOrEmpty(Message);
    }
}