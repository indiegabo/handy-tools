using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Describes one redo request for a history scope.
    /// </summary>
    [Serializable]
    public readonly struct CommandRedoRequest
    {
        /// <summary>
        /// Creates one redo request.
        /// </summary>
        /// <param name="scope">History scope name.</param>
        /// <param name="ownerId">Optional owner filter.</param>
        /// <param name="reason">Human-readable reason for the redo.</param>
        public CommandRedoRequest(
            string scope,
            string ownerId = "",
            string reason = "")
        {
            Scope = string.IsNullOrWhiteSpace(scope)
                ? CommandScope.Global
                : scope.Trim();
            OwnerId = ownerId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        /// <summary>
        /// Gets the history scope name.
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Gets the optional owner filter.
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        /// Gets the human-readable reason for the redo.
        /// </summary>
        public string Reason { get; }
    }
}