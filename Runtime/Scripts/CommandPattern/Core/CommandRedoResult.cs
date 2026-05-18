using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents the result of one redo operation.
    /// </summary>
    public readonly struct CommandRedoResult
    {
        /// <summary>
        /// Creates one redo result.
        /// </summary>
        /// <param name="succeeded">Whether the redo succeeded.</param>
        /// <param name="executionId">Affected execution identifier.</param>
        /// <param name="scope">History scope.</param>
        /// <param name="failureReason">Failure reason when redo did not succeed.</param>
        public CommandRedoResult(
            bool succeeded,
            Guid executionId,
            string scope,
            string failureReason)
        {
            Succeeded = succeeded;
            ExecutionId = executionId;
            Scope = scope ?? string.Empty;
            FailureReason = failureReason ?? string.Empty;
        }

        /// <summary>
        /// Gets whether the redo succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the affected execution identifier.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets the history scope.
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Gets the failure reason when redo did not succeed.
        /// </summary>
        public string FailureReason { get; }
    }
}