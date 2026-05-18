using System;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents one command execution registration and completion awaitable.
    /// </summary>
    public readonly struct CommandExecutionHandle
    {
        /// <summary>
        /// Creates one execution handle.
        /// </summary>
        /// <param name="executionId">Execution identifier.</param>
        /// <param name="sequence">Monotonic execution sequence.</param>
        /// <param name="scope">Execution scope.</param>
        /// <param name="queue">Execution queue.</param>
        /// <param name="initialStatus">Initial execution status.</param>
        /// <param name="completion">Awaitable completion handle.</param>
        public CommandExecutionHandle(
            Guid executionId,
            long sequence,
            string scope,
            string queue,
            CommandStatus initialStatus,
            Awaitable<CommandExecutionResult> completion)
        {
            ExecutionId = executionId;
            Sequence = sequence;
            Scope = scope ?? string.Empty;
            Queue = queue ?? string.Empty;
            InitialStatus = initialStatus;
            Completion = completion;
        }

        /// <summary>
        /// Gets the execution identifier.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets the monotonic execution sequence.
        /// </summary>
        public long Sequence { get; }

        /// <summary>
        /// Gets the execution scope.
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Gets the execution queue.
        /// </summary>
        public string Queue { get; }

        /// <summary>
        /// Gets the initial execution status.
        /// </summary>
        public CommandStatus InitialStatus { get; }

        /// <summary>
        /// Gets the completion awaitable for the execution.
        /// </summary>
        public Awaitable<CommandExecutionResult> Completion { get; }
    }
}