using System;
using System.Text;
using IndieGabo.HandyTools.CommandPatternModule;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CommandPatternModule
{
    /// <summary>
    /// Displays play-mode command flow, filters, and safe debug actions.
    /// </summary>
    public sealed class CommandPatternMonitorWindow : EditorWindow
    {
        private const float FilterFieldWidth = 180f;
        private const float RowHeight = 42f;
        private const float LeftPaneWidth = 380f;

        [SerializeField]
        private CommandMonitorFilters _filters = new();

        [SerializeField]
        private int _selectedIndex = -1;

        private readonly CommandMonitorViewModel _viewModel = new();

        private ListView _entriesListView;
        private Label _statusLabel;
        private Label _detailsLabel;
        private Button _cancelPendingButton;
        private Button _undoScopeButton;
        private Button _redoScopeButton;

        [MenuItem(CommandPatternEditorRegistration.MonitorMenuPath, false, 40)]
        private static void OpenWindow()
        {
            CommandPatternMonitorWindow window =
                GetWindow<CommandPatternMonitorWindow>();
            window.titleContent = new GUIContent("Command Monitor");
            window.minSize = new Vector2(960f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        /// <summary>
        /// Creates the monitor window UI tree.
        /// </summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            rootVisualElement.Add(CreateFiltersSection());
            rootVisualElement.Add(CreateStatusBar());
            rootVisualElement.Add(CreateSplitView());

            rootVisualElement.schedule.Execute(RefreshView)
                .Every(CommandPatternEditorRegistration.RefreshIntervalMilliseconds);

            RefreshView();
        }

        private VisualElement CreateFiltersSection()
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;
            container.style.marginBottom = 8f;

            VisualElement firstRow = CreateFilterRow();
            firstRow.Add(CreateFilterField(
                "Scope",
                _filters.Scope,
                value => _filters.Scope = value));
            firstRow.Add(CreateFilterField(
                "Queue",
                _filters.Queue,
                value => _filters.Queue = value));
            firstRow.Add(CreateFilterField(
                "Owner",
                _filters.OwnerId,
                value => _filters.OwnerId = value));

            VisualElement secondRow = CreateFilterRow();
            secondRow.Add(CreateFilterField(
                "Tag",
                _filters.Tag,
                value => _filters.Tag = value));
            secondRow.Add(CreateFilterField(
                "Command Type",
                _filters.CommandType,
                value => _filters.CommandType = value));
            secondRow.Add(CreateRefreshButtonRow());

            container.Add(firstRow);
            container.Add(secondRow);

            return container;
        }

        private static VisualElement CreateFilterRow()
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4f;
            return row;
        }

        private VisualElement CreateRefreshButtonRow()
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            Button refreshButton = new(RefreshView)
            {
                text = "Refresh"
            };

            Button clearButton = new(() =>
            {
                _filters.Scope = string.Empty;
                _filters.Queue = string.Empty;
                _filters.OwnerId = string.Empty;
                _filters.Tag = string.Empty;
                _filters.CommandType = string.Empty;
                CreateGUI();
            })
            {
                text = "Clear Filters"
            };

            refreshButton.style.marginRight = 6f;
            row.Add(refreshButton);
            row.Add(clearButton);
            return row;
        }

        private VisualElement CreateFilterField(
            string label,
            string value,
            Action<string> assignAction)
        {
            TextField field = new(label)
            {
                value = value ?? string.Empty,
            };

            field.style.width = FilterFieldWidth;
            field.style.marginRight = 6f;
            field.RegisterValueChangedCallback(changeEvent =>
            {
                assignAction(changeEvent.newValue ?? string.Empty);
                RefreshView();
            });

            return field;
        }

        private VisualElement CreateStatusBar()
        {
            _statusLabel = new Label();
            _statusLabel.style.marginBottom = 8f;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            return _statusLabel;
        }

        private VisualElement CreateSplitView()
        {
            TwoPaneSplitView splitView = new(0, (int)LeftPaneWidth,
                TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;

            splitView.Add(CreateEntriesPanel());
            splitView.Add(CreateDetailsPanel());

            return splitView;
        }

        private VisualElement CreateEntriesPanel()
        {
            _entriesListView = new ListView
            {
                selectionType = SelectionType.Single,
                fixedItemHeight = RowHeight,
                style =
                {
                    flexGrow = 1f,
                }
            };

            _entriesListView.makeItem = MakeEntryRow;
            _entriesListView.bindItem = BindEntryRow;
            _entriesListView.selectedIndicesChanged += HandleSelectionChanged;

            VisualElement container = new();
            container.style.flexGrow = 1f;
            container.Add(_entriesListView);
            return container;
        }

        private VisualElement MakeEntryRow()
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft = 8f;
            container.style.paddingRight = 8f;
            container.style.paddingTop = 4f;
            container.style.paddingBottom = 4f;

            Label titleLabel = new();
            titleLabel.name = "title";
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;

            Label subtitleLabel = new();
            subtitleLabel.name = "subtitle";
            subtitleLabel.style.fontSize = 11f;
            subtitleLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            subtitleLabel.style.whiteSpace = WhiteSpace.Normal;

            container.Add(titleLabel);
            container.Add(subtitleLabel);
            return container;
        }

        private void BindEntryRow(VisualElement element, int index)
        {
            CommandMonitorViewModel.CommandMonitorRow row = _viewModel.Rows[index];
            element.Q<Label>("title").text = row.Title;
            element.Q<Label>("subtitle").text = row.Subtitle;
        }

        private VisualElement CreateDetailsPanel()
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexGrow = 1f;
            container.style.paddingLeft = 12f;

            VisualElement actionRow = new();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginBottom = 8f;

            _cancelPendingButton = new Button(HandleCancelPendingClicked)
            {
                text = "Cancel Pending"
            };

            _undoScopeButton = new Button(HandleUndoClicked)
            {
                text = "Undo Scope"
            };

            _redoScopeButton = new Button(HandleRedoClicked)
            {
                text = "Redo Scope"
            };

            _cancelPendingButton.style.marginRight = 6f;
            _undoScopeButton.style.marginRight = 6f;

            actionRow.Add(_cancelPendingButton);
            actionRow.Add(_undoScopeButton);
            actionRow.Add(_redoScopeButton);

            ScrollView detailsScrollView = new();
            detailsScrollView.style.flexGrow = 1f;

            _detailsLabel = new Label();
            _detailsLabel.style.whiteSpace = WhiteSpace.Normal;
            _detailsLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            _detailsLabel.style.flexGrow = 1f;
            detailsScrollView.Add(_detailsLabel);

            container.Add(actionRow);
            container.Add(detailsScrollView);

            return container;
        }

        private void HandleSelectionChanged(System.Collections.Generic.IEnumerable<int> selectedIndices)
        {
            foreach (int selectedIndex in selectedIndices)
            {
                _selectedIndex = selectedIndex;
                break;
            }

            RefreshDetails();
        }

        private void HandleCancelPendingClicked()
        {
            if (!TryGetSelectedRow(out CommandMonitorViewModel.CommandMonitorRow row)
                || !row.Entry.ScheduleId.HasValue)
            {
                return;
            }

            if (!_viewModel.TryGetCommandService(out ICommandService commandService)
                || commandService == null)
            {
                return;
            }

            commandService.TryCancelScheduled(new CommandScheduleHandle(
                row.Entry.ScheduleId.Value,
                row.Entry.Sequence,
                row.Entry.Scope,
                row.Entry.Queue,
                row.Entry.DelayMode,
                row.Entry.ScheduledForUtc ?? DateTimeOffset.UtcNow));
            RefreshView();
        }

        private void HandleUndoClicked()
        {
            if (!TryGetSelectedRow(out CommandMonitorViewModel.CommandMonitorRow row)
                || string.IsNullOrWhiteSpace(row.Entry.Scope)
                || !_viewModel.TryGetCommandService(out ICommandService commandService)
                || commandService == null)
            {
                return;
            }

            ExecuteUndoAsync(commandService, row.Entry.Scope, row.Entry.OwnerId);
        }

        private void HandleRedoClicked()
        {
            if (!TryGetSelectedRow(out CommandMonitorViewModel.CommandMonitorRow row)
                || string.IsNullOrWhiteSpace(row.Entry.Scope)
                || !_viewModel.TryGetCommandService(out ICommandService commandService)
                || commandService == null)
            {
                return;
            }

            ExecuteRedoAsync(commandService, row.Entry.Scope, row.Entry.OwnerId);
        }

        private static async void ExecuteUndoAsync(
            ICommandService commandService,
            string scope,
            string ownerId)
        {
            try
            {
                await commandService.UndoAsync(new CommandUndoRequest(
                    scope,
                    ownerId,
                    "Monitor window request"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static async void ExecuteRedoAsync(
            ICommandService commandService,
            string scope,
            string ownerId)
        {
            try
            {
                await commandService.RedoAsync(new CommandRedoRequest(
                    scope,
                    ownerId,
                    "Monitor window request"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void RefreshView()
        {
            _viewModel.Refresh(_filters);
            _statusLabel.text = _viewModel.StatusMessage;

            _entriesListView.itemsSource = _viewModel.RowSource;
            _entriesListView.Rebuild();

            if (_selectedIndex < 0 || _selectedIndex >= _viewModel.Rows.Count)
            {
                _selectedIndex = _viewModel.Rows.Count > 0 ? 0 : -1;
            }

            if (_selectedIndex >= 0)
            {
                _entriesListView.SetSelection(_selectedIndex);
            }
            else
            {
                _entriesListView.ClearSelection();
            }

            RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (!TryGetSelectedRow(out CommandMonitorViewModel.CommandMonitorRow row))
            {
                _detailsLabel.text =
                    "Select one journal entry to inspect its timestamps and metadata.";
                UpdateActionButtons(null);
                return;
            }

            _detailsLabel.text = BuildDetailsText(row.Entry);
            UpdateActionButtons(row.Entry);
        }

        private bool TryGetSelectedRow(
            out CommandMonitorViewModel.CommandMonitorRow row)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _viewModel.Rows.Count)
            {
                row = _viewModel.Rows[_selectedIndex];
                return true;
            }

            row = null;
            return false;
        }

        private void UpdateActionButtons(CommandJournalEntry? entry)
        {
            bool hasEntry = entry.HasValue;
            bool hasService = _viewModel.TryGetCommandService(out ICommandService commandService)
                && commandService != null
                && EditorApplication.isPlaying;

            _cancelPendingButton.SetEnabled(
                hasService
                && hasEntry
                && entry.Value.Status == CommandStatus.Pending
                && entry.Value.ScheduleId.HasValue
                && entry.Value.ExecutionId == Guid.Empty);

            _undoScopeButton.SetEnabled(
                hasService
                && hasEntry
                && !string.IsNullOrWhiteSpace(entry.Value.Scope));

            _redoScopeButton.SetEnabled(
                hasService
                && hasEntry
                && !string.IsNullOrWhiteSpace(entry.Value.Scope));
        }

        private static string BuildDetailsText(CommandJournalEntry entry)
        {
            StringBuilder builder = new();
            builder.AppendLine($"Display Name: {entry.DisplayName}");
            builder.AppendLine($"Command Type: {entry.CommandType}");
            builder.AppendLine($"Status: {entry.Status}");
            builder.AppendLine($"Scope: {entry.Scope}");
            builder.AppendLine($"Queue: {entry.Queue}");
            builder.AppendLine($"Owner: {entry.OwnerId}");
            builder.AppendLine($"Sequence: {entry.Sequence}");
            builder.AppendLine($"Execution Id: {entry.ExecutionId}");
            builder.AppendLine($"Schedule Id: {entry.ScheduleId}");
            builder.AppendLine($"Created At (UTC): {entry.CreatedAtUtc:O}");
            builder.AppendLine($"Scheduled For (UTC): {entry.ScheduledForUtc:O}");
            builder.AppendLine($"Started At (UTC): {entry.StartedAtUtc:O}");
            builder.AppendLine($"Completed At (UTC): {entry.CompletedAtUtc:O}");
            builder.AppendLine($"Delay Mode: {entry.DelayMode}");
            builder.AppendLine($"Undoable: {entry.IsUndoable}");
            builder.AppendLine($"Can Redo: {entry.CanRedo}");

            if (entry.Tags.Count > 0)
            {
                builder.AppendLine($"Tags: {string.Join(", ", entry.Tags)}");
            }

            if (!string.IsNullOrWhiteSpace(entry.FailureReason))
            {
                builder.AppendLine($"Failure: {entry.FailureReason}");
            }

            if (!string.IsNullOrWhiteSpace(entry.CancellationReason)
                && !string.Equals(entry.CancellationReason, "None", StringComparison.Ordinal))
            {
                builder.AppendLine($"Cancellation: {entry.CancellationReason}");
            }

            if (entry.Metadata.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Metadata:");

                foreach (System.Collections.Generic.KeyValuePair<string, string> pair
                    in entry.Metadata)
                {
                    builder.AppendLine($"- {pair.Key}: {pair.Value}");
                }
            }

            return builder.ToString();
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            RefreshView();
        }
    }
}