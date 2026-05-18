using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents the result emitted by one command execution.
    /// </summary>
    [Serializable]
    public readonly struct CommandExecutionResult
    {
        /// <summary>
        /// Creates one execution result.
        /// </summary>
        /// <param name="succeeded">Whether the execution succeeded.</param>
        /// <param name="isUndoable">Whether the execution produced undo data.</param>
        /// <param name="allowRedo">Whether a successful undo may enter redo flow.</param>
        /// <param name="failureReason">Human-readable failure reason.</param>
        /// <param name="undoOperation">Undo operation produced by the execution.</param>
        /// <param name="metadata">Diagnostic metadata emitted by the command.</param>
        public CommandExecutionResult(
            bool succeeded,
            bool isUndoable = false,
            bool allowRedo = false,
            string failureReason = "",
            ICommandUndoOperation undoOperation = null,
            IReadOnlyDictionary<string, string> metadata = null)
        {
            Succeeded = succeeded;
            IsUndoable = succeeded && isUndoable && undoOperation != null;
            AllowRedo = Succeeded && IsUndoable && allowRedo;
            FailureReason = failureReason ?? string.Empty;
            UndoOperation = undoOperation;
            Metadata = metadata ?? new Dictionary<string, string>(0);
        }

        /// <summary>
        /// Gets whether the execution succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets whether the execution produced an undo operation.
        /// </summary>
        public bool IsUndoable { get; }

        /// <summary>
        /// Gets whether the execution allows redo after a successful undo.
        /// </summary>
        public bool AllowRedo { get; }

        /// <summary>
        /// Gets the failure reason when execution did not succeed.
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// Gets the undo operation emitted by the execution.
        /// </summary>
        public ICommandUndoOperation UndoOperation { get; }

        /// <summary>
        /// Gets diagnostic metadata emitted by the command.
        /// </summary>
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}