using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Validation
{
    public enum CutsceneGraphValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct CutsceneGraphValidationIssue
    {
        public CutsceneGraphValidationIssue(
            CutsceneGraphValidationSeverity severity,
            string message,
            SerializableGuid nodeId)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            NodeId = nodeId;
        }

        public CutsceneGraphValidationSeverity Severity { get; }

        public string Message { get; }

        public SerializableGuid NodeId { get; }
    }
}