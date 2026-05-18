using System;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.Playables;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    /// <summary>
    /// Plays one PlayableDirector and completes the current cutscene node when
    /// the director stops.
    /// </summary>
    [Serializable]
    [CutsceneNodeMenu("Actions/Play Timeline", "Play Timeline")]
    public sealed class CutscenePlayTimelineNode : CutsceneNodeBase
    {
        private const string RuntimeStateKey = "RuntimeState";

        [SerializeField]
        [CutsceneValueSourceType(typeof(PlayableDirector))]
        private CutsceneValueSource _playableDirectorSource =
            CutsceneValueSource.CreateDirect<PlayableDirector>(null);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _restartOnEnterSource =
            CutsceneValueSource.CreateDirect(true);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _stopOnExitSource =
            CutsceneValueSource.CreateDirect(true);

        /// <summary>
        /// Configures the timeline playback target and exit behavior.
        /// </summary>
        /// <param name="playableDirector">Director that owns the Timeline asset.</param>
        /// <param name="restartOnEnter">Whether playback should restart from time zero.</param>
        /// <param name="stopOnExit">Whether playback should stop when the node exits early.</param>
        public void Configure(
            PlayableDirector playableDirector,
            bool restartOnEnter = true,
            bool stopOnExit = true)
        {
            EnsureValueSourcesConfigured();
            _playableDirectorSource.SetDirectValue(playableDirector);
            _restartOnEnterSource.SetDirectValue(restartOnEnter);
            _stopOnExitSource.SetDirectValue(stopOnExit);
        }

        /// <summary>
        /// Returns one concise description of the configured playback target.
        /// </summary>
        /// <returns>One summary string shown in the graph node body.</returns>
        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();

            if (_playableDirectorSource.Mode != CutsceneValueSourceMode.Direct)
            {
                return _playableDirectorSource.GetSummary();
            }

            if (_playableDirectorSource.DirectValue?.GetBoxedValue() is not PlayableDirector playableDirector)
            {
                return "No PlayableDirector";
            }

            string assetName = playableDirector.playableAsset == null
                ? "No Timeline"
                : playableDirector.playableAsset.name;

            return $"{playableDirector.name} -> {assetName}";
        }

        /// <summary>
        /// Binds the director stopped callback and starts playback.
        /// </summary>
        /// <param name="context">Execution context for the active cutscene run.</param>
        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_playableDirectorSource.TryGetValue(context, out PlayableDirector playableDirector))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Play Timeline node requires one valid PlayableDirector source."));
                return;
            }

            if (!_restartOnEnterSource.TryGetValue(context, out bool restartOnEnter)
                || !_stopOnExitSource.TryGetValue(context, out bool stopOnExit))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Play Timeline node requires valid bool sources for enter and exit behavior."));
                return;
            }

            if (playableDirector.playableAsset == null)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Play Timeline node requires the PlayableDirector to reference a Timeline asset."));
                return;
            }

            RuntimeState state = context.GetOrCreateNodeState(
                RuntimeStateKey,
                static () => new RuntimeState());
            Unbind(state);
            state.StopOnExit = stopOnExit;

            if (restartOnEnter)
            {
                if (playableDirector.state == PlayState.Playing)
                {
                    playableDirector.Stop();
                }

                playableDirector.time = 0d;
                playableDirector.Evaluate();
            }

            SerializableGuid executionId = context.CurrentNodeExecutionId;
            PlayableDirector targetDirector = playableDirector;

            state.Director = targetDirector;
            state.StoppedHandler = _ => context.TryCompleteNode(
                executionId,
                CutsceneNodeResult.Success());

            targetDirector.stopped += state.StoppedHandler;
            context.SetNodeState(RuntimeStateKey, state);
            targetDirector.Play();
        }

        /// <summary>
        /// Removes the director callback and optionally stops playback.
        /// </summary>
        /// <param name="context">Execution context for the active cutscene run.</param>
        public override void OnExit(CutsceneExecutionContext context)
        {
            if (!context.TryGetNodeState(RuntimeStateKey, out RuntimeState state))
            {
                return;
            }

            PlayableDirector boundDirector = state.Director;
            Unbind(state);

            if (state.StopOnExit
                && boundDirector != null
                && boundDirector.state == PlayState.Playing)
            {
                boundDirector.Stop();
            }

            context.SetNodeState(RuntimeStateKey, state);
        }

        /// <summary>
        /// Detaches any previously registered stopped callback from the runtime state.
        /// </summary>
        /// <param name="state">Mutable runtime state for this node instance.</param>
        private static void Unbind(RuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            if (state.Director != null && state.StoppedHandler != null)
            {
                state.Director.stopped -= state.StoppedHandler;
            }

            state.Director = null;
            state.StoppedHandler = null;
        }

        /// <summary>
        /// Stores the runtime callback binding for one active node execution.
        /// </summary>
        private sealed class RuntimeState
        {
            public PlayableDirector Director;
            public Action<PlayableDirector> StoppedHandler;
            public bool StopOnExit;
        }

        private void EnsureValueSourcesConfigured()
        {
            _playableDirectorSource ??= CutsceneValueSource.CreateDirect<PlayableDirector>(null);
            _restartOnEnterSource ??= CutsceneValueSource.CreateDirect(true);
            _stopOnExitSource ??= CutsceneValueSource.CreateDirect(true);

            _playableDirectorSource.SetExpectedValueType(typeof(PlayableDirector));
            _restartOnEnterSource.SetExpectedValueType(typeof(bool));
            _stopOnExitSource.SetExpectedValueType(typeof(bool));
        }
    }
}