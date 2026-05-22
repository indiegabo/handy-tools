using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using UnityEngine;
using UnityEngine.Playables;

namespace IndieGabo.HandyTools.ConversationsModule.Nodes.Actions
{
    /// <summary>
    /// Stores one authored timeline playback action inside a Conversations graph.
    /// </summary>
    [System.Serializable]
    [ConversationNodeMenu("Actions/Play Timeline", "Play Timeline")]
    public sealed class ConversationPlayTimelineNode : ConversationActionNodeBase
    {
        [SerializeField]
        private PlayableDirector _playableDirector;

        [SerializeField]
        private bool _restartOnEnter = true;

        [SerializeField]
        private bool _stopOnExit = true;

        /// <summary>
        /// Gets the authored playable director used by the node.
        /// </summary>
        public PlayableDirector PlayableDirector => _playableDirector;

        /// <summary>
        /// Gets whether playback restarts from time zero when the node is entered.
        /// </summary>
        public bool RestartOnEnter => _restartOnEnter;

        /// <summary>
        /// Gets whether playback should stop when the node exits early.
        /// </summary>
        public bool StopOnExit => _stopOnExit;

        /// <summary>
        /// Configures the node to play one timeline through the provided director.
        /// </summary>
        /// <param name="playableDirector">Director that should be played.</param>
        /// <param name="restartOnEnter">Whether playback restarts from time zero.</param>
        /// <param name="stopOnExit">Whether playback stops when the node exits early.</param>
        public void Configure(
            PlayableDirector playableDirector,
            bool restartOnEnter = true,
            bool stopOnExit = true)
        {
            _playableDirector = playableDirector;
            _restartOnEnter = restartOnEnter;
            _stopOnExit = stopOnExit;
        }

        /// <summary>
        /// Gets one short node summary for authoring surfaces.
        /// </summary>
        /// <returns>The configured timeline target summary.</returns>
        public override string GetSummary()
        {
            if (_playableDirector == null)
            {
                return "No PlayableDirector";
            }

            string assetName = _playableDirector.playableAsset == null
                ? "No Timeline"
                : _playableDirector.playableAsset.name;
            return $"{_playableDirector.name} -> {assetName}";
        }
    }
}