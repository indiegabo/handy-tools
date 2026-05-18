using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.CommandPatternModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor.CommandPatternModule
{
    /// <summary>
    /// Bridges runtime command snapshots into a simple editor-facing state bag.
    /// </summary>
    internal sealed class CommandMonitorViewModel
    {
        /// <summary>
        /// Represents one flattened row in the monitor list.
        /// </summary>
        internal sealed class CommandMonitorRow
        {
            /// <summary>
            /// Creates one monitor row.
            /// </summary>
            /// <param name="groupLabel">Owning snapshot group label.</param>
            /// <param name="entry">Journal entry displayed by the row.</param>
            public CommandMonitorRow(string groupLabel, CommandJournalEntry entry)
            {
                GroupLabel = groupLabel ?? string.Empty;
                Entry = entry;
            }

            /// <summary>
            /// Gets the owning group label.
            /// </summary>
            public string GroupLabel { get; }

            /// <summary>
            /// Gets the journal entry displayed by the row.
            /// </summary>
            public CommandJournalEntry Entry { get; }

            /// <summary>
            /// Gets the primary row label.
            /// </summary>
            public string Title => $"{GroupLabel} - {Entry.DisplayName}";

            /// <summary>
            /// Gets the secondary row label.
            /// </summary>
            public string Subtitle =>
                $"{Entry.Scope}/{Entry.Queue} · {Entry.CommandType}";
        }

        /// <summary>
        /// Gets the latest flattened rows.
        /// </summary>
        public IReadOnlyList<CommandMonitorRow> Rows => _rows;

        /// <summary>
        /// Gets the mutable row list used as the UI Toolkit items source.
        /// </summary>
        public List<CommandMonitorRow> RowSource => _rows;

        /// <summary>
        /// Gets the latest status message displayed by the monitor.
        /// </summary>
        public string StatusMessage { get; private set; } =
            "Enter Play Mode to inspect command flow.";

        /// <summary>
        /// Gets the latest journal snapshot.
        /// </summary>
        public CommandJournalSnapshot Snapshot { get; private set; }

        private readonly List<CommandMonitorRow> _rows = new();

        /// <summary>
        /// Refreshes the view-model from the current runtime service state.
        /// </summary>
        /// <param name="filters">Editor filters applied to the snapshot.</param>
        public void Refresh(CommandMonitorFilters filters)
        {
            _rows.Clear();

            if (!EditorApplication.isPlaying)
            {
                Snapshot = default;
                StatusMessage = "Enter Play Mode to inspect command flow.";
                return;
            }

            if (!TryGetCommandService(out ICommandService commandService)
                || commandService == null)
            {
                Snapshot = default;
                StatusMessage =
                    "Command service is not available in the current play session.";
                return;
            }

            Snapshot = commandService.GetSnapshot(filters.ToQuery());
            _rows.AddRange(CommandJournalTreeBuilder.BuildRows(Snapshot));
            StatusMessage = $"{_rows.Count} command entries loaded.";
        }

        /// <summary>
        /// Attempts to resolve the runtime command service.
        /// </summary>
        /// <param name="commandService">Resolved command service.</param>
        /// <returns>True when the service exists.</returns>
        public bool TryGetCommandService(out ICommandService commandService)
        {
            return ServiceLocator.TryGet(out commandService);
        }
    }
}