using System.Collections.Generic;
using IndieGabo.HandyTools.CommandPatternModule;

namespace IndieGabo.HandyTools.Editor.CommandPatternModule
{
    /// <summary>
    /// Flattens grouped journal snapshots into monitor rows for the editor UI.
    /// </summary>
    internal static class CommandJournalTreeBuilder
    {
        /// <summary>
        /// Builds flattened monitor rows from one journal snapshot.
        /// </summary>
        /// <param name="snapshot">Snapshot to flatten.</param>
        /// <returns>The ordered monitor rows.</returns>
        public static List<CommandMonitorViewModel.CommandMonitorRow> BuildRows(
            CommandJournalSnapshot snapshot)
        {
            List<CommandMonitorViewModel.CommandMonitorRow> rows = new();

            AppendRows(rows, "Pending", snapshot.Pending);
            AppendRows(rows, "Running", snapshot.Running);
            AppendRows(rows, "Completed", snapshot.Completed);
            AppendRows(rows, "Failed", snapshot.Failed);
            AppendRows(rows, "Cancelled", snapshot.Cancelled);
            AppendRows(rows, "Undone", snapshot.Undone);
            AppendRows(rows, "Redone", snapshot.Redone);

            return rows;
        }

        private static void AppendRows(
            List<CommandMonitorViewModel.CommandMonitorRow> rows,
            string groupLabel,
            IReadOnlyList<CommandJournalEntry> entries)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                rows.Add(new CommandMonitorViewModel.CommandMonitorRow(
                    groupLabel,
                    entries[index]));
            }
        }
    }
}