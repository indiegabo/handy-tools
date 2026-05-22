using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Nodes.Actions
{
    /// <summary>
    /// Stores one authored wait action inside a Conversations graph.
    /// </summary>
    [System.Serializable]
    [ConversationNodeMenu("Actions/Wait", "Wait")]
    public sealed class ConversationWaitNode : ConversationActionNodeBase
    {
        [SerializeField, Min(0f)]
        private float _durationSeconds = 1f;

        [SerializeField]
        private ConversationTimeMode _timeMode = ConversationTimeMode.Scaled;

        /// <summary>
        /// Gets the authored wait duration in seconds.
        /// </summary>
        public float DurationSeconds => Mathf.Max(0f, _durationSeconds);

        /// <summary>
        /// Gets the time source used by the wait node.
        /// </summary>
        public ConversationTimeMode TimeMode => _timeMode;

        /// <summary>
        /// Configures the authored wait duration and time source.
        /// </summary>
        /// <param name="durationSeconds">Duration to wait in seconds.</param>
        /// <param name="timeMode">Time source used while waiting.</param>
        public void Configure(
            float durationSeconds,
            ConversationTimeMode timeMode = ConversationTimeMode.Scaled)
        {
            _durationSeconds = Mathf.Max(0f, durationSeconds);
            _timeMode = timeMode;
        }

        /// <summary>
        /// Gets one short node summary for authoring surfaces.
        /// </summary>
        /// <returns>The current authored wait summary.</returns>
        public override string GetSummary()
        {
            return $"{DurationSeconds:0.###}s ({_timeMode})";
        }
    }
}