using System;
using IndieGabo.HandyTools.CommandPatternModule;

namespace IndieGabo.HandyTools.Editor.CommandPatternModule
{
    /// <summary>
    /// Stores the editable filter values applied by the command monitor.
    /// </summary>
    [Serializable]
    internal sealed class CommandMonitorFilters
    {
        /// <summary>
        /// Scope filter text.
        /// </summary>
        public string Scope = string.Empty;

        /// <summary>
        /// Queue filter text.
        /// </summary>
        public string Queue = string.Empty;

        /// <summary>
        /// Owner filter text.
        /// </summary>
        public string OwnerId = string.Empty;

        /// <summary>
        /// Tag filter text.
        /// </summary>
        public string Tag = string.Empty;

        /// <summary>
        /// Command-type filter text.
        /// </summary>
        public string CommandType = string.Empty;

        /// <summary>
        /// Converts the editor filters into a runtime journal query.
        /// </summary>
        /// <returns>The generated query value.</returns>
        public CommandQuery ToQuery()
        {
            return new CommandQuery(
                Scope,
                Queue,
                OwnerId,
                Tag,
                CommandType,
                maxEntriesPerGroup: 0);
        }
    }
}