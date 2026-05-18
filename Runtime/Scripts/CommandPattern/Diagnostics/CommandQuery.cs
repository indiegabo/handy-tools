using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines runtime journal filters applied when generating snapshots.
    /// </summary>
    [Serializable]
    public readonly struct CommandQuery
    {
        /// <summary>
        /// Creates one journal query.
        /// </summary>
        /// <param name="scope">Optional scope filter.</param>
        /// <param name="queue">Optional queue filter.</param>
        /// <param name="ownerId">Optional owner filter.</param>
        /// <param name="tag">Optional tag filter.</param>
        /// <param name="commandType">Optional command-type filter.</param>
        /// <param name="maxEntriesPerGroup">Optional per-group result cap.</param>
        public CommandQuery(
            string scope = "",
            string queue = "",
            string ownerId = "",
            string tag = "",
            string commandType = "",
            int maxEntriesPerGroup = 0)
        {
            Scope = scope ?? string.Empty;
            Queue = queue ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
            Tag = tag ?? string.Empty;
            CommandType = commandType ?? string.Empty;
            MaxEntriesPerGroup = Math.Max(0, maxEntriesPerGroup);
        }

        /// <summary>
        /// Gets the optional scope filter.
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Gets the optional queue filter.
        /// </summary>
        public string Queue { get; }

        /// <summary>
        /// Gets the optional owner filter.
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        /// Gets the optional tag filter.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Gets the optional command-type filter.
        /// </summary>
        public string CommandType { get; }

        /// <summary>
        /// Gets the optional per-group result cap.
        /// </summary>
        public int MaxEntriesPerGroup { get; }
    }
}