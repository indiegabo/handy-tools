using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Validation
{
    /// <summary>
    /// Declares the supported validation severities used by the Conversations editor.
    /// </summary>
    public enum ConversationValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// Declares the editor target kinds that one validation issue can point to.
    /// </summary>
    public enum ConversationValidationTargetKind
    {
        Table,
        Conversation,
        Actor,
        Node,
    }

    /// <summary>
    /// Represents one validation issue emitted for one conversation table.
    /// </summary>
    public readonly struct ConversationValidationIssue
    {
        /// <summary>
        /// Creates one validation issue.
        /// </summary>
        /// <param name="severity">Severity assigned to the issue.</param>
        /// <param name="code">Stable machine-readable issue code.</param>
        /// <param name="message">Human-readable issue message.</param>
        /// <param name="targetKind">Primary target kind for navigation.</param>
        /// <param name="conversationId">Conversation id associated with the issue.</param>
        /// <param name="actorId">Actor id associated with the issue.</param>
        /// <param name="nodeId">Node id associated with the issue.</param>
        public ConversationValidationIssue(
            ConversationValidationSeverity severity,
            string code,
            string message,
            ConversationValidationTargetKind targetKind,
            SerializableGuid conversationId = default,
            SerializableGuid actorId = default,
            SerializableGuid nodeId = default)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            TargetKind = targetKind;
            ConversationId = conversationId;
            ActorId = actorId;
            NodeId = nodeId;
        }

        /// <summary>
        /// Gets the issue severity.
        /// </summary>
        public ConversationValidationSeverity Severity { get; }

        /// <summary>
        /// Gets the stable machine-readable issue code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets the user-facing issue message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the primary target kind for navigation.
        /// </summary>
        public ConversationValidationTargetKind TargetKind { get; }

        /// <summary>
        /// Gets the affected conversation identifier when available.
        /// </summary>
        public SerializableGuid ConversationId { get; }

        /// <summary>
        /// Gets the affected actor identifier when available.
        /// </summary>
        public SerializableGuid ActorId { get; }

        /// <summary>
        /// Gets the affected node identifier when available.
        /// </summary>
        public SerializableGuid NodeId { get; }

        /// <summary>
        /// Gets whether the issue points to a concrete editor target.
        /// </summary>
        public bool CanNavigate => ConversationId != SerializableGuid.Empty
            || ActorId != SerializableGuid.Empty
            || NodeId != SerializableGuid.Empty;

        /// <summary>
        /// Builds one copy-friendly multiline representation of the issue.
        /// </summary>
        /// <returns>The clipboard text for the issue.</returns>
        public string BuildClipboardText()
        {
            List<string> lines = new()
            {
                $"Severity: {Severity}",
                $"Code: {Code}",
                $"Target: {TargetKind}",
                $"Message: {Message}",
            };

            if (ConversationId != SerializableGuid.Empty)
            {
                lines.Add($"ConversationId: {ConversationId.ToHexString()}");
            }

            if (ActorId != SerializableGuid.Empty)
            {
                lines.Add($"ActorId: {ActorId.ToHexString()}");
            }

            if (NodeId != SerializableGuid.Empty)
            {
                lines.Add($"NodeId: {NodeId.ToHexString()}");
            }

            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// Aggregates the current validation counts for one table.
    /// </summary>
    public readonly struct ConversationValidationSummary
    {
        /// <summary>
        /// Creates one validation summary.
        /// </summary>
        /// <param name="errorCount">Current error count.</param>
        /// <param name="warningCount">Current warning count.</param>
        /// <param name="infoCount">Current info count.</param>
        public ConversationValidationSummary(int errorCount, int warningCount, int infoCount)
        {
            ErrorCount = errorCount;
            WarningCount = warningCount;
            InfoCount = infoCount;
        }

        /// <summary>
        /// Gets the current error count.
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Gets the current warning count.
        /// </summary>
        public int WarningCount { get; }

        /// <summary>
        /// Gets the current info count.
        /// </summary>
        public int InfoCount { get; }

        /// <summary>
        /// Gets whether any issue exists.
        /// </summary>
        public bool HasIssues => ErrorCount > 0 || WarningCount > 0 || InfoCount > 0;

        /// <summary>
        /// Gets whether no error or warning exists.
        /// </summary>
        public bool IsSuccess => ErrorCount == 0 && WarningCount == 0;

        /// <summary>
        /// Builds one summary from the provided issue collection.
        /// </summary>
        /// <param name="issues">Issues that should be counted.</param>
        /// <returns>The counted validation summary.</returns>
        public static ConversationValidationSummary FromIssues(
            IReadOnlyList<ConversationValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                return new ConversationValidationSummary(0, 0, 0);
            }

            int errorCount = 0;
            int warningCount = 0;
            int infoCount = 0;

            for (int index = 0; index < issues.Count; index++)
            {
                switch (issues[index].Severity)
                {
                    case ConversationValidationSeverity.Error:
                        errorCount++;
                        break;

                    case ConversationValidationSeverity.Warning:
                        warningCount++;
                        break;

                    default:
                        infoCount++;
                        break;
                }
            }

            return new ConversationValidationSummary(errorCount, warningCount, infoCount);
        }
    }
}
