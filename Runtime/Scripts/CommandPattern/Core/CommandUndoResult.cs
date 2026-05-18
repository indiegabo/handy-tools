using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents the result of one undo operation.
    /// </summary>
    public readonly struct CommandUndoResult
    {
        /// <summary>
        /// Creates one undo result.
        /// </summary>
        /// <param name="succeeded">Whether the undo succeeded.</param>
        /// <param name="executionId">Affected execution identifier.</param>
        /// <param name="scope">History scope.</param>
        /// <param name="addedToRedo">Whether the command entered redo flow.</param>
        /// <param name="failureReason">Failure reason when undo did not succeed.</param>
        public CommandUndoResult(
            bool succeeded,
            Guid executionId,
            string scope,
            bool addedToRedo,
            string failureReason)
        {
            Succeeded = succeeded;
            ExecutionId = executionId;
            Scope = scope ?? string.Empty;
            AddedToRedo = addedToRedo;
            FailureReason = failureReason ?? string.Empty;
        }

        /// <summary>
        /// Gets whether the undo succeeded.
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
        /// Gets whether the command entered redo flow.
        /// </summary>
        public bool AddedToRedo { get; }

        /// <summary>
        /// Gets the failure reason when undo did not succeed.
        /// </summary>
        public string FailureReason { get; }
    }
}