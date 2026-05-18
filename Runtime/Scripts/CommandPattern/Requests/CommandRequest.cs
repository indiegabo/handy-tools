using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Wraps one command instance with routing and diagnostic metadata.
    /// </summary>
    [Serializable]
    public readonly struct CommandRequest
    {
        /// <summary>
        /// Creates one command request.
        /// </summary>
        /// <param name="command">Command instance to execute.</param>
        /// <param name="scope">History scope name.</param>
        /// <param name="queue">Queue name within the scope.</param>
        /// <param name="ownerId">Optional owner identifier.</param>
        /// <param name="tags">Optional diagnostic tags.</param>
        /// <param name="queuePolicy">Queue policy override.</param>
        /// <param name="displayNameOverride">Optional display-name override.</param>
        /// <param name="historyLimitOverride">Optional history limit override.</param>
        /// <param name="journalLimitOverride">Optional journal limit override.</param>
        public CommandRequest(
            IHandyCommand command,
            string scope,
            string queue,
            string ownerId = "",
            IReadOnlyList<string> tags = null,
            CommandQueuePolicy queuePolicy = CommandQueuePolicy.Parallel,
            string displayNameOverride = "",
            int historyLimitOverride = 0,
            int journalLimitOverride = 0)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Scope = NormalizeScope(scope);
            Queue = NormalizeQueue(queue);
            OwnerId = ownerId ?? string.Empty;
            Tags = NormalizeTags(tags);
            QueuePolicy = queuePolicy;
            DisplayNameOverride = displayNameOverride ?? string.Empty;
            HistoryLimitOverride = Math.Max(0, historyLimitOverride);
            JournalLimitOverride = Math.Max(0, journalLimitOverride);
        }

        /// <summary>
        /// Gets the command instance to execute.
        /// </summary>
        public IHandyCommand Command { get; }

        /// <summary>
        /// Gets the history scope name.
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Gets the queue name within the scope.
        /// </summary>
        public string Queue { get; }

        /// <summary>
        /// Gets the owner identifier used for diagnostics and bulk control.
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        /// Gets the diagnostic tags associated with the request.
        /// </summary>
        public IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// Gets the queue arbitration policy for the request.
        /// </summary>
        public CommandQueuePolicy QueuePolicy { get; }

        /// <summary>
        /// Gets the optional display-name override.
        /// </summary>
        public string DisplayNameOverride { get; }

        /// <summary>
        /// Gets the optional history limit override.
        /// </summary>
        public int HistoryLimitOverride { get; }

        /// <summary>
        /// Gets the optional journal limit override.
        /// </summary>
        public int JournalLimitOverride { get; }

        private static string NormalizeScope(string scope)
        {
            return string.IsNullOrWhiteSpace(scope)
                ? CommandScope.Global
                : scope.Trim();
        }

        private static string NormalizeQueue(string queue)
        {
            return string.IsNullOrWhiteSpace(queue)
                ? CommandScope.DefaultQueue
                : queue.Trim();
        }

        private static IReadOnlyList<string> NormalizeTags(
            IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> normalizedTags = new(tags.Count);
            HashSet<string> seen = new(StringComparer.Ordinal);

            for (int index = 0; index < tags.Count; index++)
            {
                string tag = tags[index];
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                string normalizedTag = tag.Trim();
                if (!seen.Add(normalizedTag))
                {
                    continue;
                }

                normalizedTags.Add(normalizedTag);
            }

            return normalizedTags.Count == 0
                ? Array.Empty<string>()
                : normalizedTags;
        }
    }
}