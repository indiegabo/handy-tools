using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule.Samples
{
    /// <summary>
    /// Exposes sample-friendly entry points for immediate, scheduled, undo,
    /// and redo command flows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommandPatternSampleController : MonoBehaviour
    {
        private const string SampleUndoReason = "Sample controller request";

        [SerializeField]
        private CommandPatternSampleGridActor _gridActor;

        [SerializeField]
        private CommandPatternSampleRequestLog _requestLog;

        [SerializeField]
        private string _scope = CommandScope.Global;

        [SerializeField]
        private string _queue = CommandScope.DefaultQueue;

        [SerializeField]
        private string _ownerId = "command-pattern-sample";

        [SerializeField]
        private CommandQueuePolicy _queuePolicy = CommandQueuePolicy.Serial;

        [SerializeField]
        private string[] _tags = Array.Empty<string>();

        [SerializeField]
        private float _scheduledDelaySeconds = 0.5f;

        /// <summary>
        /// Gets the current scope configured for the sample.
        /// </summary>
        public string Scope => _scope;

        /// <summary>
        /// Gets the current owner identifier configured for the sample.
        /// </summary>
        public string OwnerId => _ownerId;

        /// <summary>
        /// Gets the sample grid actor.
        /// </summary>
        public CommandPatternSampleGridActor GridActor => _gridActor;

        /// <summary>
        /// Gets the ordered sample request log.
        /// </summary>
        public CommandPatternSampleRequestLog RequestLog => _requestLog;

        /// <summary>
        /// Submits one immediate upward movement command.
        /// </summary>
        public void MoveUp()
        {
            SubmitMove(Vector2Int.up, CommandDelayMode.Immediate, 0d);
        }

        /// <summary>
        /// Submits one immediate downward movement command.
        /// </summary>
        public void MoveDown()
        {
            SubmitMove(Vector2Int.down, CommandDelayMode.Immediate, 0d);
        }

        /// <summary>
        /// Submits one immediate left movement command.
        /// </summary>
        public void MoveLeft()
        {
            SubmitMove(Vector2Int.left, CommandDelayMode.Immediate, 0d);
        }

        /// <summary>
        /// Submits one immediate right movement command.
        /// </summary>
        public void MoveRight()
        {
            SubmitMove(Vector2Int.right, CommandDelayMode.Immediate, 0d);
        }

        /// <summary>
        /// Submits one next-frame upward movement command.
        /// </summary>
        public void ScheduleMoveUpNextFrame()
        {
            SubmitMove(Vector2Int.up, CommandDelayMode.NextFrame, 0d);
        }

        /// <summary>
        /// Submits one scaled-delay right movement command.
        /// </summary>
        public void ScheduleMoveRightScaled()
        {
            SubmitMove(
                Vector2Int.right,
                CommandDelayMode.ScaledDelay,
                _scheduledDelaySeconds);
        }

        /// <summary>
        /// Submits one unscaled-delay left movement command.
        /// </summary>
        public void ScheduleMoveLeftUnscaled()
        {
            SubmitMove(
                Vector2Int.left,
                CommandDelayMode.UnscaledDelay,
                _scheduledDelaySeconds);
        }

        /// <summary>
        /// Requests undo in the configured sample scope.
        /// </summary>
        public void UndoLast()
        {
            CommandService commandService = ResolveCommandService();
            if (commandService == null)
            {
                return;
            }

            ExecuteUndoAsync(commandService, _scope, _ownerId);
        }

        /// <summary>
        /// Requests redo in the configured sample scope.
        /// </summary>
        public void RedoLast()
        {
            CommandService commandService = ResolveCommandService();
            if (commandService == null)
            {
                return;
            }

            ExecuteRedoAsync(commandService, _scope, _ownerId);
        }

        /// <summary>
        /// Restores the sample actor and request log to their initial state.
        /// </summary>
        public void ResetSample()
        {
            _requestLog?.Clear();
            _gridActor?.ResetToInitialState();
        }

        /// <summary>
        /// Returns the current runtime snapshot for the configured scope.
        /// </summary>
        /// <returns>The current command snapshot.</returns>
        public CommandJournalSnapshot GetSnapshot()
        {
            CommandService commandService = ResolveCommandService();
            if (commandService == null)
            {
                return default;
            }

            return commandService.GetSnapshot(new CommandQuery(scope: _scope));
        }

        private void SubmitMove(
            Vector2Int delta,
            CommandDelayMode delayMode,
            double delaySeconds)
        {
            CommandService commandService = ResolveCommandService();
            if (commandService == null || _gridActor == null)
            {
                return;
            }

            SampleMoveGridCommand command = new(_gridActor, delta);
            CommandRequest request = new(
                command,
                _scope,
                _queue,
                _ownerId,
                _tags,
                _queuePolicy,
                displayNameOverride: $"Move {delta}");

            _requestLog?.Add($"{DateTime.Now:HH:mm:ss} · {request.DisplayNameOverride}");

            if (delayMode == CommandDelayMode.Immediate)
            {
                commandService.Execute(request);
                return;
            }

            commandService.Schedule(new CommandScheduleRequest(
                request,
                delayMode,
                delaySeconds,
                Time.frameCount));
        }

        private static async void ExecuteUndoAsync(
            CommandService commandService,
            string scope,
            string ownerId)
        {
            try
            {
                await commandService.UndoAsync(new CommandUndoRequest(
                    scope,
                    ownerId,
                    SampleUndoReason));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static async void ExecuteRedoAsync(
            CommandService commandService,
            string scope,
            string ownerId)
        {
            try
            {
                await commandService.RedoAsync(new CommandRedoRequest(
                    scope,
                    ownerId,
                    SampleUndoReason));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private CommandService ResolveCommandService()
        {
            return FindAnyObjectByType<CommandService>();
        }
    }
}