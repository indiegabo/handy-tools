using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents one pending scheduled command registration.
    /// </summary>
    public readonly struct CommandScheduleHandle
    {
        /// <summary>
        /// Creates one scheduled command handle.
        /// </summary>
        /// <param name="scheduleId">Schedule identifier.</param>
        /// <param name="sequence">Monotonic schedule sequence.</param>
        /// <param name="scope">Schedule scope.</param>
        /// <param name="queue">Schedule queue.</param>
        /// <param name="delayMode">Delay mode.</param>
        /// <param name="scheduledForUtc">Scheduled UTC execution timestamp.</param>
        public CommandScheduleHandle(
            Guid scheduleId,
            long sequence,
            string scope,
            string queue,
            CommandDelayMode delayMode,
            DateTimeOffset scheduledForUtc)
        {
            ScheduleId = scheduleId;
            Sequence = sequence;
            Scope = scope ?? string.Empty;
            Queue = queue ?? string.Empty;
            DelayMode = delayMode;
            ScheduledForUtc = scheduledForUtc;
        }

        /// <summary>
        /// Gets the schedule identifier.
        /// </summary>
        public Guid ScheduleId { get; }

        /// <summary>
        /// Gets the monotonic schedule sequence.
        /// </summary>
        public long Sequence { get; }

        /// <summary>
        /// Gets the schedule scope.
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Gets the schedule queue.
        /// </summary>
        public string Queue { get; }

        /// <summary>
        /// Gets the delay mode.
        /// </summary>
        public CommandDelayMode DelayMode { get; }

        /// <summary>
        /// Gets the scheduled UTC timestamp.
        /// </summary>
        public DateTimeOffset ScheduledForUtc { get; }
    }
}