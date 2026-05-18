using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Wraps one command request with scheduling metadata.
    /// </summary>
    [Serializable]
    public readonly struct CommandScheduleRequest
    {
        /// <summary>
        /// Creates one scheduled command request.
        /// </summary>
        /// <param name="request">Command request to schedule.</param>
        /// <param name="delayMode">Delay mode for the schedule.</param>
        /// <param name="delaySeconds">Delay value in seconds for delayed modes.</param>
        /// <param name="requestedFrame">Origin frame used by next-frame scheduling.</param>
        public CommandScheduleRequest(
            CommandRequest request,
            CommandDelayMode delayMode,
            double delaySeconds = 0d,
            long requestedFrame = 0)
        {
            Request = request;
            DelayMode = delayMode;
            DelaySeconds = Math.Max(0d, delaySeconds);
            RequestedFrame = requestedFrame;
        }

        /// <summary>
        /// Gets the wrapped command request.
        /// </summary>
        public CommandRequest Request { get; }

        /// <summary>
        /// Gets the scheduling delay mode.
        /// </summary>
        public CommandDelayMode DelayMode { get; }

        /// <summary>
        /// Gets the delay in seconds used by delayed modes.
        /// </summary>
        public double DelaySeconds { get; }

        /// <summary>
        /// Gets the origin frame used by next-frame scheduling.
        /// </summary>
        public long RequestedFrame { get; }
    }
}