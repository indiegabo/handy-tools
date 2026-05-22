using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule.Samples
{
    /// <summary>
    /// Moves one sample grid actor by a discrete delta and emits undo data.
    /// </summary>
    public sealed class SampleMoveGridCommand :
        IHandyCommand,
        ICommandDiagnosticsSummaryProvider
    {
        private readonly CommandPatternSampleGridActor _gridActor;
        private readonly Vector2Int _delta;

        /// <summary>
        /// Creates one sample grid movement command.
        /// </summary>
        /// <param name="gridActor">Grid actor affected by the command.</param>
        /// <param name="delta">Discrete movement delta.</param>
        public SampleMoveGridCommand(
            CommandPatternSampleGridActor gridActor,
            Vector2Int delta)
        {
            _gridActor = gridActor;
            _delta = delta;
        }

        /// <inheritdoc />
        public CommandDescriptor Descriptor { get; } =
            CommandDescriptor.Create<SampleMoveGridCommand>(
                "Move Grid Actor",
                "Moves the sample actor one cell inside the starter-kit grid.");

        /// <inheritdoc />
        public Awaitable<CommandExecutionResult> ExecuteAsync(
            ICommandExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CommandExecutionResult result;

            if (_gridActor == null)
            {
                result = new CommandExecutionResult(
                    false,
                    failureReason:
                        "The sample grid actor reference is missing.");
            }
            else
            {
                Vector2Int previousPosition = _gridActor.GridPosition;
                _gridActor.MoveBy(_delta);

                Dictionary<string, string> metadata = new()
                {
                    ["delta"] = _delta.ToString(),
                    ["previousPosition"] = previousPosition.ToString(),
                    ["newPosition"] = _gridActor.GridPosition.ToString(),
                };

                result = new CommandExecutionResult(
                    true,
                    isUndoable: true,
                    allowRedo: true,
                    undoOperation: new MoveGridUndoOperation(
                        _gridActor,
                        previousPosition),
                    metadata: metadata);
            }

            AwaitableCompletionSource<CommandExecutionResult> completionSource = new();
            completionSource.SetResult(result);
            return completionSource.Awaitable;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> GetDiagnosticsSummary()
        {
            return new Dictionary<string, string>
            {
                ["delta"] = _delta.ToString(),
            };
        }

        private sealed class MoveGridUndoOperation : ICommandUndoOperation
        {
            private readonly CommandPatternSampleGridActor _gridActor;
            private readonly Vector2Int _targetPosition;

            public MoveGridUndoOperation(
                CommandPatternSampleGridActor gridActor,
                Vector2Int targetPosition)
            {
                _gridActor = gridActor;
                _targetPosition = targetPosition;
            }

            public Awaitable UndoAsync(
                ICommandExecutionContext context,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_gridActor != null)
                {
                    _gridActor.SnapToGrid(_targetPosition, appendTrailPoint: true);
                }

                AwaitableCompletionSource completionSource = new();
                completionSource.SetResult();
                return completionSource.Awaitable;
            }
        }
    }
}