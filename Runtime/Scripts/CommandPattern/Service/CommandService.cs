using System;
using System.Collections.Generic;
using System.Threading;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Hosts the Command Pattern runtime service lifecycle, scheduling, and
    /// history state.
    /// </summary>
    public sealed class CommandService : MonoBehaviour, ICommandService
    {
        #region Constants

        private const int DefaultHistoryLimit = 128;
        private const int DefaultJournalLimit = 256;
        private const string BusyFailureReason =
            "The target command queue is busy.";
        private const string MissingUndoReason =
            "No eligible command is available for undo in the requested scope.";
        private const string MissingRedoReason =
            "No eligible command is available for redo in the requested scope.";

        #endregion

        #region Static Data

        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
            new Dictionary<string, string>(0);

        #endregion

        #region State

        private readonly Dictionary<Guid, ExecutionState> _executionsById = new();
        private readonly Dictionary<Guid, ScheduledState> _scheduleRecordsById = new();
        private readonly Dictionary<Guid, ScheduledState> _pendingSchedulesById = new();
        private readonly Dictionary<QueueKey, QueueState> _queuesByKey = new();
        private readonly Dictionary<string, ScopeState> _scopesByName =
            new(StringComparer.Ordinal);
        private readonly List<ICommandMiddleware> _middlewares = new();

        private bool _isShuttingDown;
        private long _nextSequence;

        #endregion

        #region Events

        /// <inheritdoc />
        public event Action<CommandLifecycleEvent> LifecycleEventPublished;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Clears any static state owned by the command runtime before a new
        /// play session begins.
        /// </summary>
        public static void ResetStaticState()
        {
        }

        private void Update()
        {
            ProcessScheduledEntries();
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;
            CancelAllPendingSchedules(CommandCancellationReason.ServiceReset);

            _ = ServiceLocator.Deregister<ICommandService>(this);
            _ = ServiceLocator.Deregister(this);
        }

        #endregion

        #region ICommandService

        /// <inheritdoc />
        public CommandExecutionHandle Execute(in CommandRequest request)
        {
            EnsureRequestIsValid(request);
            return EnqueueExecution(
                request,
                scheduleId: null,
                delayMode: CommandDelayMode.Immediate,
                scheduledForUtc: null,
                mode: CommandExecutionMode.Normal);
        }

        /// <inheritdoc />
        public CommandScheduleHandle Schedule(in CommandScheduleRequest request)
        {
            EnsureRequestIsValid(request.Request);

            DateTimeOffset scheduledForUtc = ResolveScheduledForUtc(request);
            ScheduledState scheduledState = new(
                handle: new CommandScheduleHandle(
                    Guid.NewGuid(),
                    NextSequence(),
                    request.Request.Scope,
                    request.Request.Queue,
                    request.DelayMode,
                    scheduledForUtc),
                request,
                ResolveOwnerId(request.Request),
                ResolveDisplayName(request.Request),
                ResolveMetadata(request.Request.Command, EmptyMetadata),
                scheduledForUtc,
                ResolveReadyFrame(request),
                ResolveReadyScaledTime(request),
                ResolveReadyUnscaledTime(request));

            QueueState queueState = GetOrCreateQueueState(scheduledState.QueueKey);
            if (request.Request.QueuePolicy == CommandQueuePolicy.RejectWhenBusy
                && queueState.IsOccupied)
            {
                scheduledState.Status = CommandStatus.Failed;
                scheduledState.FailureReason = BusyFailureReason;
                _scheduleRecordsById[scheduledState.Handle.ScheduleId] = scheduledState;

                PublishLifecycle(
                    CommandLifecycleEventKind.Failed,
                    CreateJournalEntry(scheduledState));

                return scheduledState.Handle;
            }

            queueState.PendingScheduledCount++;
            _scheduleRecordsById[scheduledState.Handle.ScheduleId] = scheduledState;
            _pendingSchedulesById[scheduledState.Handle.ScheduleId] = scheduledState;
            ApplyScopeOverrides(request.Request);

            PublishLifecycle(
                CommandLifecycleEventKind.Scheduled,
                CreateJournalEntry(scheduledState));

            return scheduledState.Handle;
        }

        /// <inheritdoc />
        public bool TryCancelScheduled(
            in CommandScheduleHandle handle,
            CommandCancellationReason reason =
                CommandCancellationReason.UserRequested)
        {
            if (!_pendingSchedulesById.TryGetValue(handle.ScheduleId, out ScheduledState scheduledState))
            {
                return false;
            }

            _pendingSchedulesById.Remove(handle.ScheduleId);
            _scheduleRecordsById[handle.ScheduleId] = scheduledState;

            QueueState queueState = GetOrCreateQueueState(scheduledState.QueueKey);
            if (queueState.PendingScheduledCount > 0)
            {
                queueState.PendingScheduledCount--;
            }

            scheduledState.Status = CommandStatus.Cancelled;
            scheduledState.CancellationReason = reason;
            scheduledState.CompletedAtUtc = DateTimeOffset.UtcNow;

            PublishLifecycle(
                CommandLifecycleEventKind.Cancelled,
                CreateJournalEntry(scheduledState));

            return true;
        }

        /// <inheritdoc />
        public async Awaitable<CommandUndoResult> UndoAsync(
            CommandUndoRequest request,
            CancellationToken cancellationToken = default)
        {
            ScopeState scopeState = GetOrCreateScopeState(request.Scope);
            if (!TryFindUndoEntry(scopeState.DoneEntries, request.OwnerId, out int index))
            {
                return new CommandUndoResult(
                    false,
                    Guid.Empty,
                    request.Scope,
                    false,
                    MissingUndoReason);
            }

            CommandHistoryEntry entry = scopeState.DoneEntries[index];
            CommandExecutionContext context = new(
                entry.Request,
                entry.ExecutionId,
                entry.ScheduleId ?? Guid.Empty,
                entry.Sequence,
                CommandExecutionMode.Undo,
                entry.CreatedAtUtc,
                DateTimeOffset.UtcNow,
                cancellationToken);

            try
            {
                await entry.UndoOperation.UndoAsync(context, cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                return new CommandUndoResult(
                    false,
                    entry.ExecutionId,
                    request.Scope,
                    false,
                    exception.Message);
            }
            catch (Exception exception)
            {
                return new CommandUndoResult(
                    false,
                    entry.ExecutionId,
                    request.Scope,
                    false,
                    exception.Message);
            }

            scopeState.DoneEntries.RemoveAt(index);
            if (entry.AllowRedo)
            {
                scopeState.RedoEntries.Add(entry);
            }

            if (_executionsById.TryGetValue(entry.ExecutionId, out ExecutionState executionState))
            {
                executionState.Status = CommandStatus.Undone;
                executionState.CanRedo = entry.AllowRedo;

                PublishLifecycle(
                    CommandLifecycleEventKind.Undone,
                    CreateJournalEntry(executionState));
            }

            return new CommandUndoResult(
                true,
                entry.ExecutionId,
                request.Scope,
                entry.AllowRedo,
                string.Empty);
        }

        /// <inheritdoc />
        public async Awaitable<CommandRedoResult> RedoAsync(
            CommandRedoRequest request,
            CancellationToken cancellationToken = default)
        {
            ScopeState scopeState = GetOrCreateScopeState(request.Scope);
            if (!TryFindUndoEntry(scopeState.RedoEntries, request.OwnerId, out int index))
            {
                return new CommandRedoResult(
                    false,
                    Guid.Empty,
                    request.Scope,
                    MissingRedoReason);
            }

            CommandHistoryEntry entry = scopeState.RedoEntries[index];
            CommandExecutionHandle handle = EnqueueExecution(
                entry.Request,
                entry.ScheduleId,
                CommandDelayMode.Immediate,
                scheduledForUtc: null,
                mode: CommandExecutionMode.Redo);

            CommandExecutionResult result = await handle.Completion;
            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Succeeded)
            {
                return new CommandRedoResult(
                    false,
                    handle.ExecutionId,
                    request.Scope,
                    result.FailureReason);
            }

            scopeState.RedoEntries.RemoveAt(index);
            return new CommandRedoResult(
                true,
                handle.ExecutionId,
                request.Scope,
                string.Empty);
        }

        /// <inheritdoc />
        public CommandJournalSnapshot GetSnapshot(in CommandQuery query)
        {
            List<CommandJournalEntry> pending = new();
            List<CommandJournalEntry> running = new();
            List<CommandJournalEntry> completed = new();
            List<CommandJournalEntry> failed = new();
            List<CommandJournalEntry> cancelled = new();
            List<CommandJournalEntry> undone = new();
            List<CommandJournalEntry> redone = new();

            foreach (ExecutionState executionState in _executionsById.Values)
            {
                CommandJournalEntry entry = CreateJournalEntry(executionState);
                if (!MatchesQuery(entry, query))
                {
                    continue;
                }

                AppendEntryByStatus(
                    entry,
                    pending,
                    running,
                    completed,
                    failed,
                    cancelled,
                    undone,
                    redone);
            }

            foreach (ScheduledState scheduledState in _scheduleRecordsById.Values)
            {
                CommandJournalEntry entry = CreateJournalEntry(scheduledState);
                if (!MatchesQuery(entry, query))
                {
                    continue;
                }

                AppendEntryByStatus(
                    entry,
                    pending,
                    running,
                    completed,
                    failed,
                    cancelled,
                    undone,
                    redone);
            }

            SortAndTrim(pending, query.MaxEntriesPerGroup);
            SortAndTrim(running, query.MaxEntriesPerGroup);
            SortAndTrim(completed, query.MaxEntriesPerGroup);
            SortAndTrim(failed, query.MaxEntriesPerGroup);
            SortAndTrim(cancelled, query.MaxEntriesPerGroup);
            SortAndTrim(undone, query.MaxEntriesPerGroup);
            SortAndTrim(redone, query.MaxEntriesPerGroup);

            return new CommandJournalSnapshot(
                DateTimeOffset.UtcNow,
                pending,
                running,
                completed,
                failed,
                cancelled,
                undone,
                redone);
        }

        /// <inheritdoc />
        public void RegisterMiddleware(ICommandMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            if (_middlewares.Contains(middleware))
            {
                return;
            }

            _middlewares.Add(middleware);
        }

        /// <inheritdoc />
        public bool DeregisterMiddleware(ICommandMiddleware middleware)
        {
            if (middleware == null)
            {
                return false;
            }

            return _middlewares.Remove(middleware);
        }

        #endregion

        #region Scheduling

        private void ProcessScheduledEntries()
        {
            if (_pendingSchedulesById.Count == 0)
            {
                return;
            }

            List<ScheduledState> dueEntries = new();
            foreach (ScheduledState scheduledState in _pendingSchedulesById.Values)
            {
                if (!scheduledState.IsDue())
                {
                    continue;
                }

                dueEntries.Add(scheduledState);
            }

            dueEntries.Sort(static (left, right) =>
            {
                int scheduledComparison = left.ScheduledForUtc.CompareTo(right.ScheduledForUtc);
                if (scheduledComparison != 0)
                {
                    return scheduledComparison;
                }

                return left.Handle.Sequence.CompareTo(right.Handle.Sequence);
            });

            for (int index = 0; index < dueEntries.Count; index++)
            {
                ScheduledState scheduledState = dueEntries[index];
                PromoteScheduledEntry(scheduledState);
            }
        }

        private void PromoteScheduledEntry(ScheduledState scheduledState)
        {
            if (!_pendingSchedulesById.Remove(scheduledState.Handle.ScheduleId))
            {
                return;
            }

            _scheduleRecordsById.Remove(scheduledState.Handle.ScheduleId);

            QueueState queueState = GetOrCreateQueueState(scheduledState.QueueKey);
            if (queueState.PendingScheduledCount > 0)
            {
                queueState.PendingScheduledCount--;
            }

            if (scheduledState.Request.Request.QueuePolicy == CommandQueuePolicy.RejectWhenBusy
                && queueState.IsOccupied)
            {
                scheduledState.Status = CommandStatus.Failed;
                scheduledState.FailureReason = BusyFailureReason;
                scheduledState.CompletedAtUtc = DateTimeOffset.UtcNow;
                _scheduleRecordsById[scheduledState.Handle.ScheduleId] = scheduledState;

                PublishLifecycle(
                    CommandLifecycleEventKind.Failed,
                    CreateJournalEntry(scheduledState));

                return;
            }

            _ = EnqueueExecution(
                scheduledState.Request.Request,
                scheduledState.Handle.ScheduleId,
                scheduledState.Request.DelayMode,
                scheduledState.ScheduledForUtc,
                CommandExecutionMode.Normal);
        }

        private DateTimeOffset ResolveScheduledForUtc(CommandScheduleRequest request)
        {
            return request.DelayMode switch
            {
                CommandDelayMode.NextFrame => DateTimeOffset.UtcNow,
                CommandDelayMode.ScaledDelay => DateTimeOffset.UtcNow.AddSeconds(
                    request.DelaySeconds),
                CommandDelayMode.UnscaledDelay => DateTimeOffset.UtcNow.AddSeconds(
                    request.DelaySeconds),
                _ => DateTimeOffset.UtcNow,
            };
        }

        private long ResolveReadyFrame(CommandScheduleRequest request)
        {
            return request.DelayMode == CommandDelayMode.NextFrame
                ? Math.Max(request.RequestedFrame, Time.frameCount) + 1L
                : 0L;
        }

        private double ResolveReadyScaledTime(CommandScheduleRequest request)
        {
            return request.DelayMode == CommandDelayMode.ScaledDelay
                ? Time.timeAsDouble + request.DelaySeconds
                : 0d;
        }

        private double ResolveReadyUnscaledTime(CommandScheduleRequest request)
        {
            return request.DelayMode == CommandDelayMode.UnscaledDelay
                ? Time.unscaledTimeAsDouble + request.DelaySeconds
                : 0d;
        }

        private void CancelAllPendingSchedules(CommandCancellationReason reason)
        {
            if (_pendingSchedulesById.Count == 0)
            {
                return;
            }

            List<Guid> scheduleIds = new(_pendingSchedulesById.Keys);
            for (int index = 0; index < scheduleIds.Count; index++)
            {
                CommandScheduleHandle handle = _pendingSchedulesById[scheduleIds[index]].Handle;
                _ = TryCancelScheduled(handle, reason);
            }
        }

        #endregion

        #region Execution

        private CommandExecutionHandle EnqueueExecution(
            in CommandRequest request,
            Guid? scheduleId,
            CommandDelayMode delayMode,
            DateTimeOffset? scheduledForUtc,
            CommandExecutionMode mode)
        {
            ApplyScopeOverrides(request);

            ExecutionState executionState = new(
                executionId: Guid.NewGuid(),
                sequence: NextSequence(),
                request: request,
                ownerId: ResolveOwnerId(request),
                displayName: ResolveDisplayName(request),
                metadata: ResolveMetadata(request.Command, EmptyMetadata),
                scheduleId: scheduleId,
                delayMode: delayMode,
                scheduledForUtc: scheduledForUtc,
                mode: mode,
                createdAtUtc: DateTimeOffset.UtcNow);

            _executionsById[executionState.ExecutionId] = executionState;

            QueueState queueState = GetOrCreateQueueState(executionState.QueueKey);
            if (request.QueuePolicy == CommandQueuePolicy.RejectWhenBusy
                && queueState.IsOccupied)
            {
                CompleteExecutionAsFailure(
                    executionState,
                    BusyFailureReason,
                    queueRejected: true);
                return CreateHandle(executionState);
            }

            if (request.QueuePolicy == CommandQueuePolicy.Serial
                && queueState.RunningCount > 0)
            {
                queueState.ReadyExecutions.Enqueue(executionState);
                PublishLifecycle(
                    CommandLifecycleEventKind.Queued,
                    CreateJournalEntry(executionState));

                return CreateHandle(executionState);
            }

            _ = RunExecutionAsync(executionState);
            return CreateHandle(executionState);
        }

        private async Awaitable RunExecutionAsync(ExecutionState executionState)
        {
            QueueState queueState = GetOrCreateQueueState(executionState.QueueKey);
            queueState.RunningCount++;

            executionState.Status = CommandStatus.Running;
            executionState.StartedAtUtc = DateTimeOffset.UtcNow;

            PublishLifecycle(
                CommandLifecycleEventKind.Started,
                CreateJournalEntry(executionState));

            try
            {
                CancellationToken cancellationToken = default;
                CommandExecutionContext context = new(
                    executionState.Request,
                    executionState.ExecutionId,
                    executionState.ScheduleId ?? Guid.Empty,
                    executionState.Sequence,
                    executionState.Mode,
                    executionState.CreatedAtUtc,
                    executionState.StartedAtUtc ?? DateTimeOffset.UtcNow,
                    cancellationToken);

                if (executionState.Request.Command is ICanExecuteCommand canExecuteCommand
                    && !canExecuteCommand.CanExecute(
                        context,
                        out string failureReason))
                {
                    CompleteExecutionAsFailure(
                        executionState,
                        failureReason,
                        queueRejected: false);
                    return;
                }

                CommandExecutionResult result = await InvokePipelineAsync(
                    executionState.Request,
                    context,
                    cancellationToken);

                if (!result.Succeeded)
                {
                    CompleteExecutionAsFailure(
                        executionState,
                        result.FailureReason,
                        queueRejected: false,
                        resultMetadata: result.Metadata);
                    return;
                }

                CompleteExecutionAsSuccess(executionState, result);
            }
            catch (Exception exception)
            {
                CompleteExecutionAsFailure(
                    executionState,
                    exception.Message,
                    queueRejected: false);
            }
            finally
            {
                if (queueState.RunningCount > 0)
                {
                    queueState.RunningCount--;
                }

                TryStartNextSerial(queueState);
            }
        }

        private void TryStartNextSerial(QueueState queueState)
        {
            if (queueState.RunningCount > 0 || queueState.ReadyExecutions.Count == 0)
            {
                return;
            }

            ExecutionState nextExecution = queueState.ReadyExecutions.Dequeue();
            _ = RunExecutionAsync(nextExecution);
        }

        private async Awaitable<CommandExecutionResult> InvokePipelineAsync(
            CommandRequest request,
            ICommandExecutionContext context,
            CancellationToken cancellationToken)
        {
            CommandPipelineDelegate pipeline = (_, token) =>
                request.Command.ExecuteAsync(context, token);

            for (int index = _middlewares.Count - 1; index >= 0; index--)
            {
                ICommandMiddleware middleware = _middlewares[index];
                CommandPipelineDelegate next = pipeline;
                pipeline = (currentRequest, token) => middleware.InvokeAsync(
                    currentRequest,
                    next,
                    token);
            }

            return await pipeline(request, cancellationToken);
        }

        private void CompleteExecutionAsSuccess(
            ExecutionState executionState,
            CommandExecutionResult result)
        {
            executionState.CompletedAtUtc = DateTimeOffset.UtcNow;
            executionState.IsUndoable = result.IsUndoable;
            executionState.CanRedo = result.AllowRedo;
            executionState.Metadata = ResolveMetadata(
                executionState.Request.Command,
                result.Metadata);
            executionState.FinalResult = new CommandExecutionResult(
                true,
                result.IsUndoable,
                result.AllowRedo,
                string.Empty,
                result.UndoOperation,
                executionState.Metadata);
            executionState.HasFinalResult = true;

            ScopeState scopeState = GetOrCreateScopeState(executionState.Request.Scope);
            if (executionState.Mode == CommandExecutionMode.Normal)
            {
                scopeState.RedoEntries.Clear();
            }

            if (result.IsUndoable && result.UndoOperation != null)
            {
                scopeState.DoneEntries.Add(new CommandHistoryEntry(
                    executionState.ExecutionId,
                    executionState.Sequence,
                    executionState.Request,
                    executionState.ScheduleId,
                    result.UndoOperation,
                    result.AllowRedo,
                    executionState.CreatedAtUtc));
                TrimHistory(scopeState);
            }

            executionState.Status = executionState.Mode == CommandExecutionMode.Redo
                ? CommandStatus.Redone
                : CommandStatus.Completed;

            PublishLifecycle(
                executionState.Mode == CommandExecutionMode.Redo
                    ? CommandLifecycleEventKind.Redone
                    : CommandLifecycleEventKind.Completed,
                CreateJournalEntry(executionState));
        }

        private void CompleteExecutionAsFailure(
            ExecutionState executionState,
            string failureReason,
            bool queueRejected,
            IReadOnlyDictionary<string, string> resultMetadata = null)
        {
            executionState.Status = CommandStatus.Failed;
            executionState.CancellationReason = queueRejected
                ? CommandCancellationReason.QueueRejected
                : CommandCancellationReason.None;
            executionState.CompletedAtUtc = DateTimeOffset.UtcNow;
            executionState.Metadata = ResolveMetadata(
                executionState.Request.Command,
                resultMetadata ?? EmptyMetadata);
            executionState.FinalResult = new CommandExecutionResult(
                false,
                failureReason: failureReason,
                metadata: executionState.Metadata);
            executionState.HasFinalResult = true;

            PublishLifecycle(
                CommandLifecycleEventKind.Failed,
                CreateJournalEntry(executionState));
        }

        private CommandExecutionHandle CreateHandle(ExecutionState executionState)
        {
            return new CommandExecutionHandle(
                executionState.ExecutionId,
                executionState.Sequence,
                executionState.Request.Scope,
                executionState.Request.Queue,
                executionState.Status,
                WaitForExecutionCompletionAsync(executionState.ExecutionId));
        }

        private async Awaitable<CommandExecutionResult> WaitForExecutionCompletionAsync(
            Guid executionId)
        {
            while (true)
            {
                if (_executionsById.TryGetValue(executionId, out ExecutionState executionState)
                    && executionState.HasFinalResult)
                {
                    return executionState.FinalResult;
                }

                if (_isShuttingDown)
                {
                    return new CommandExecutionResult(
                        false,
                        failureReason:
                            "Command service stopped before the execution completed.");
                }

                await Awaitable.NextFrameAsync();
            }
        }

        #endregion

        #region Diagnostics

        private static void AppendEntryByStatus(
            CommandJournalEntry entry,
            List<CommandJournalEntry> pending,
            List<CommandJournalEntry> running,
            List<CommandJournalEntry> completed,
            List<CommandJournalEntry> failed,
            List<CommandJournalEntry> cancelled,
            List<CommandJournalEntry> undone,
            List<CommandJournalEntry> redone)
        {
            switch (entry.Status)
            {
                case CommandStatus.Pending:
                    pending.Add(entry);
                    break;
                case CommandStatus.Running:
                    running.Add(entry);
                    break;
                case CommandStatus.Completed:
                    completed.Add(entry);
                    break;
                case CommandStatus.Failed:
                    failed.Add(entry);
                    break;
                case CommandStatus.Cancelled:
                    cancelled.Add(entry);
                    break;
                case CommandStatus.Undone:
                    undone.Add(entry);
                    break;
                case CommandStatus.Redone:
                    redone.Add(entry);
                    break;
            }
        }

        private static void SortAndTrim(
            List<CommandJournalEntry> entries,
            int maxEntries)
        {
            entries.Sort(static (left, right) => left.Sequence.CompareTo(right.Sequence));

            if (maxEntries <= 0 || entries.Count <= maxEntries)
            {
                return;
            }

            entries.RemoveRange(0, entries.Count - maxEntries);
        }

        private static bool MatchesQuery(
            CommandJournalEntry entry,
            CommandQuery query)
        {
            if (!MatchesFilter(query.Scope, entry.Scope))
            {
                return false;
            }

            if (!MatchesFilter(query.Queue, entry.Queue))
            {
                return false;
            }

            if (!MatchesFilter(query.OwnerId, entry.OwnerId))
            {
                return false;
            }

            if (!MatchesFilter(query.CommandType, entry.CommandType))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(query.Tag))
            {
                return true;
            }

            string tagFilter = query.Tag.Trim();
            for (int index = 0; index < entry.Tags.Count; index++)
            {
                if (string.Equals(
                    entry.Tags[index],
                    tagFilter,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesFilter(string filter, string value)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return string.Equals(
                filter.Trim(),
                value ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private CommandJournalEntry CreateJournalEntry(ExecutionState executionState)
        {
            return new CommandJournalEntry(
                executionState.Sequence,
                executionState.ExecutionId,
                executionState.ScheduleId,
                executionState.Request.Command.Descriptor.CommandType,
                executionState.DisplayName,
                executionState.Status,
                executionState.Request.Scope,
                executionState.Request.Queue,
                executionState.OwnerId,
                executionState.Request.Tags,
                executionState.DelayMode,
                executionState.CreatedAtUtc,
                executionState.ScheduledForUtc,
                executionState.StartedAtUtc,
                executionState.CompletedAtUtc,
                executionState.IsUndoable,
                executionState.CanRedo,
                executionState.FinalResult.FailureReason,
                executionState.CancellationReason.ToString(),
                executionState.Metadata);
        }

        private CommandJournalEntry CreateJournalEntry(ScheduledState scheduledState)
        {
            return new CommandJournalEntry(
                scheduledState.Handle.Sequence,
                Guid.Empty,
                scheduledState.Handle.ScheduleId,
                scheduledState.Request.Request.Command.Descriptor.CommandType,
                scheduledState.DisplayName,
                scheduledState.Status,
                scheduledState.Request.Request.Scope,
                scheduledState.Request.Request.Queue,
                scheduledState.OwnerId,
                scheduledState.Request.Request.Tags,
                scheduledState.Request.DelayMode,
                scheduledState.CreatedAtUtc,
                scheduledState.ScheduledForUtc,
                null,
                scheduledState.CompletedAtUtc,
                false,
                false,
                scheduledState.FailureReason,
                scheduledState.CancellationReason.ToString(),
                scheduledState.Metadata);
        }

        private void PublishLifecycle(
            CommandLifecycleEventKind kind,
            CommandJournalEntry entry)
        {
            LifecycleEventPublished?.Invoke(new CommandLifecycleEvent(kind, entry));
        }

        #endregion

        #region Helpers

        private static bool TryFindUndoEntry(
            List<CommandHistoryEntry> entries,
            string ownerId,
            out int index)
        {
            for (int currentIndex = entries.Count - 1; currentIndex >= 0; currentIndex--)
            {
                if (string.IsNullOrWhiteSpace(ownerId)
                    || string.Equals(
                        ownerId,
                        ResolveOwnerId(entries[currentIndex].Request),
                        StringComparison.Ordinal))
                {
                    index = currentIndex;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private static void EnsureRequestIsValid(CommandRequest request)
        {
            if (request.Command == null)
            {
                throw new ArgumentException(
                    "The command request does not contain a command instance.",
                    nameof(request));
            }
        }

        private void ApplyScopeOverrides(CommandRequest request)
        {
            ScopeState scopeState = GetOrCreateScopeState(request.Scope);
            if (request.HistoryLimitOverride > 0)
            {
                scopeState.HistoryLimit = request.HistoryLimitOverride;
            }

            if (request.JournalLimitOverride > 0)
            {
                scopeState.JournalLimit = request.JournalLimitOverride;
            }
        }

        private ScopeState GetOrCreateScopeState(string scope)
        {
            string normalizedScope = string.IsNullOrWhiteSpace(scope)
                ? CommandScope.Global
                : scope.Trim();

            if (_scopesByName.TryGetValue(normalizedScope, out ScopeState scopeState))
            {
                return scopeState;
            }

            scopeState = new ScopeState(DefaultHistoryLimit, DefaultJournalLimit);
            _scopesByName.Add(normalizedScope, scopeState);
            return scopeState;
        }

        private QueueState GetOrCreateQueueState(QueueKey queueKey)
        {
            if (_queuesByKey.TryGetValue(queueKey, out QueueState queueState))
            {
                return queueState;
            }

            queueState = new QueueState();
            _queuesByKey.Add(queueKey, queueState);
            return queueState;
        }

        private void TrimHistory(ScopeState scopeState)
        {
            while (scopeState.DoneEntries.Count > scopeState.HistoryLimit)
            {
                scopeState.DoneEntries.RemoveAt(0);
                scopeState.RedoEntries.Clear();
            }
        }

        private long NextSequence()
        {
            _nextSequence++;
            return _nextSequence;
        }

        private static string ResolveDisplayName(CommandRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.DisplayNameOverride))
            {
                return request.DisplayNameOverride.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Command.Descriptor.DisplayName))
            {
                return request.Command.Descriptor.DisplayName;
            }

            return request.Command.Descriptor.CommandType;
        }

        private static string ResolveOwnerId(CommandRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.OwnerId))
            {
                return request.OwnerId;
            }

            if (request.Command is ICommandOwner commandOwner
                && !string.IsNullOrWhiteSpace(commandOwner.OwnerId))
            {
                return commandOwner.OwnerId;
            }

            return string.Empty;
        }

        private static IReadOnlyDictionary<string, string> ResolveMetadata(
            IHandyCommand command,
            IReadOnlyDictionary<string, string> resultMetadata)
        {
            Dictionary<string, string> mergedMetadata = new(StringComparer.Ordinal);

            if (command is ICommandDiagnosticsSummaryProvider provider)
            {
                CopyMetadata(provider.GetDiagnosticsSummary(), mergedMetadata);
            }

            CopyMetadata(resultMetadata, mergedMetadata);

            return mergedMetadata.Count == 0
                ? EmptyMetadata
                : mergedMetadata;
        }

        private static void CopyMetadata(
            IReadOnlyDictionary<string, string> source,
            Dictionary<string, string> destination)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                destination[pair.Key] = pair.Value ?? string.Empty;
            }
        }

        private readonly struct QueueKey : IEquatable<QueueKey>
        {
            public QueueKey(string scope, string queue)
            {
                Scope = scope ?? string.Empty;
                Queue = queue ?? string.Empty;
            }

            public string Scope { get; }
            public string Queue { get; }

            public bool Equals(QueueKey other)
            {
                return string.Equals(Scope, other.Scope, StringComparison.Ordinal)
                    && string.Equals(Queue, other.Queue, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is QueueKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = StringComparer.Ordinal.GetHashCode(Scope);
                    hashCode = (hashCode * 397)
                        ^ StringComparer.Ordinal.GetHashCode(Queue);
                    return hashCode;
                }
            }

            public static bool operator ==(QueueKey left, QueueKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(QueueKey left, QueueKey right)
            {
                return !left.Equals(right);
            }
        }

        private sealed class QueueState
        {
            public readonly Queue<ExecutionState> ReadyExecutions = new();

            public int RunningCount;
            public int PendingScheduledCount;

            public bool IsOccupied =>
                RunningCount > 0
                || PendingScheduledCount > 0
                || ReadyExecutions.Count > 0;
        }

        private sealed class ScopeState
        {
            public ScopeState(int historyLimit, int journalLimit)
            {
                HistoryLimit = historyLimit;
                JournalLimit = journalLimit;
            }

            public readonly List<CommandHistoryEntry> DoneEntries = new();
            public readonly List<CommandHistoryEntry> RedoEntries = new();

            public int HistoryLimit;
            public int JournalLimit;
        }

        private sealed class ExecutionState
        {
            public ExecutionState(
                Guid executionId,
                long sequence,
                CommandRequest request,
                string ownerId,
                string displayName,
                IReadOnlyDictionary<string, string> metadata,
                Guid? scheduleId,
                CommandDelayMode delayMode,
                DateTimeOffset? scheduledForUtc,
                CommandExecutionMode mode,
                DateTimeOffset createdAtUtc)
            {
                ExecutionId = executionId;
                Sequence = sequence;
                Request = request;
                OwnerId = ownerId;
                DisplayName = displayName;
                Metadata = metadata;
                ScheduleId = scheduleId;
                DelayMode = delayMode;
                ScheduledForUtc = scheduledForUtc;
                Mode = mode;
                CreatedAtUtc = createdAtUtc;
                Status = CommandStatus.Pending;
                FinalResult = new CommandExecutionResult(false, failureReason: string.Empty);
                QueueKey = new QueueKey(request.Scope, request.Queue);
            }

            public Guid ExecutionId { get; }
            public long Sequence { get; }
            public CommandRequest Request { get; }
            public string OwnerId { get; }
            public string DisplayName { get; }
            public Guid? ScheduleId { get; }
            public CommandDelayMode DelayMode { get; }
            public DateTimeOffset? ScheduledForUtc { get; }
            public CommandExecutionMode Mode { get; }
            public DateTimeOffset CreatedAtUtc { get; }
            public QueueKey QueueKey { get; }

            public DateTimeOffset? StartedAtUtc;
            public DateTimeOffset? CompletedAtUtc;
            public CommandStatus Status;
            public bool IsUndoable;
            public bool CanRedo;
            public bool HasFinalResult;
            public IReadOnlyDictionary<string, string> Metadata;
            public CommandCancellationReason CancellationReason;
            public CommandExecutionResult FinalResult;
        }

        private sealed class ScheduledState
        {
            public ScheduledState(
                CommandScheduleHandle handle,
                CommandScheduleRequest request,
                string ownerId,
                string displayName,
                IReadOnlyDictionary<string, string> metadata,
                DateTimeOffset scheduledForUtc,
                long readyFrame,
                double readyScaledTime,
                double readyUnscaledTime)
            {
                Handle = handle;
                Request = request;
                OwnerId = ownerId;
                DisplayName = displayName;
                Metadata = metadata;
                ScheduledForUtc = scheduledForUtc;
                ReadyFrame = readyFrame;
                ReadyScaledTime = readyScaledTime;
                ReadyUnscaledTime = readyUnscaledTime;
                CreatedAtUtc = DateTimeOffset.UtcNow;
                Status = CommandStatus.Pending;
                QueueKey = new QueueKey(
                    request.Request.Scope,
                    request.Request.Queue);
            }

            public CommandScheduleHandle Handle { get; }
            public CommandScheduleRequest Request { get; }
            public string OwnerId { get; }
            public string DisplayName { get; }
            public IReadOnlyDictionary<string, string> Metadata { get; }
            public DateTimeOffset CreatedAtUtc { get; }
            public DateTimeOffset ScheduledForUtc { get; }
            public long ReadyFrame { get; }
            public double ReadyScaledTime { get; }
            public double ReadyUnscaledTime { get; }
            public QueueKey QueueKey { get; }

            public CommandStatus Status;
            public DateTimeOffset? CompletedAtUtc;
            public string FailureReason;
            public CommandCancellationReason CancellationReason;

            public bool IsDue()
            {
                return Request.DelayMode switch
                {
                    CommandDelayMode.NextFrame => Time.frameCount >= ReadyFrame,
                    CommandDelayMode.ScaledDelay =>
                        Time.timeAsDouble >= ReadyScaledTime,
                    CommandDelayMode.UnscaledDelay =>
                        Time.unscaledTimeAsDouble >= ReadyUnscaledTime,
                    _ => true,
                };
            }
        }

        private sealed class CommandExecutionContext : ICommandExecutionContext
        {
            public CommandExecutionContext(
                CommandRequest request,
                Guid executionId,
                Guid scheduleId,
                long sequence,
                CommandExecutionMode mode,
                DateTimeOffset createdAtUtc,
                DateTimeOffset startedAtUtc,
                CancellationToken cancellationToken)
            {
                Request = request;
                ExecutionId = executionId;
                ScheduleId = scheduleId;
                Sequence = sequence;
                Mode = mode;
                CreatedAtUtc = createdAtUtc;
                StartedAtUtc = startedAtUtc;
                CancellationToken = cancellationToken;
            }

            public CommandRequest Request { get; }
            public Guid ExecutionId { get; }
            public Guid ScheduleId { get; }
            public long Sequence { get; }
            public CommandExecutionMode Mode { get; }
            public DateTimeOffset CreatedAtUtc { get; }
            public DateTimeOffset StartedAtUtc { get; }
            public CancellationToken CancellationToken { get; }

            public bool TryGetService<T>(out T service)
                where T : class
            {
                return ServiceLocator.TryGet(out service);
            }

            public T GetRequiredService<T>()
                where T : class
            {
                return ServiceLocator.GetRequired<T>();
            }
        }

        #endregion
    }
}