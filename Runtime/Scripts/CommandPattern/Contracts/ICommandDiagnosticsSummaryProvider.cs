using System.Collections.Generic;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines optional diagnostic metadata provided by one command type.
    /// </summary>
    public interface ICommandDiagnosticsSummaryProvider
    {
        /// <summary>
        /// Returns diagnostic metadata that can be merged into the journal
        /// entry without cloning the command payload graph.
        /// </summary>
        /// <returns>A read-only metadata map for diagnostics.</returns>
        IReadOnlyDictionary<string, string> GetDiagnosticsSummary();
    }
}