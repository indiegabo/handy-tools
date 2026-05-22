using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Stores one authored conversation indexed by one conversation table.
    /// </summary>
    [Serializable]
    public sealed class ConversationDefinition
    {
        [SerializeField]
        private SerializableGuid _conversationId;

        [SerializeField]
        private string _title = "Conversation";

        [SerializeField]
        private ConversationGraph _graph = ConversationGraph.CreateDefault();

        [SerializeField]
        private GameObject _presenterOverridePrefab;

        /// <summary>
        /// Gets the stable authored conversation identifier.
        /// </summary>
        public SerializableGuid ConversationId => _conversationId;

        /// <summary>
        /// Gets the authored conversation path.
        /// </summary>
        public string Title => NormalizeTitlePath(_title);

        /// <summary>
        /// Gets the authored graph owned by this conversation.
        /// </summary>
        public ConversationGraph Graph
        {
            get
            {
                _graph ??= ConversationGraph.CreateDefault();
                _graph.EnsureEntryNode();
                return _graph;
            }
        }

        /// <summary>
        /// Gets the presenter prefab override authored for this conversation.
        /// </summary>
        public GameObject PresenterOverridePrefab => _presenterOverridePrefab;

        /// <summary>
        /// Creates one default authored conversation with one entry node.
        /// </summary>
        /// <param name="title">Optional authored title.</param>
        /// <returns>The created authored conversation.</returns>
        public static ConversationDefinition CreateDefault(string title = null)
        {
            ConversationDefinition conversation = new();
            conversation.SetTitle(title);
            conversation._graph = ConversationGraph.CreateDefault();
            conversation.EnsureAuthoringIds();
            return conversation;
        }

        /// <summary>
        /// Updates the authored conversation path, applying the default fallback when needed.
        /// </summary>
        /// <param name="title">New authored conversation path.</param>
        public void SetTitle(string title)
        {
            _title = NormalizeTitlePath(title);
        }

        /// <summary>
        /// Replaces the authored graph owned by this conversation.
        /// </summary>
        /// <param name="graph">Graph instance that should become the authored graph.</param>
        public void ReplaceGraph(ConversationGraph graph)
        {
            _graph = graph ?? ConversationGraph.CreateDefault();
            _graph.EnsureEntryNode();
        }

        /// <summary>
        /// Stores the presenter prefab override authored for this conversation.
        /// </summary>
        /// <param name="presenterOverridePrefab">Presenter prefab override that should be used.</param>
        public void SetPresenterOverridePrefab(GameObject presenterOverridePrefab)
        {
            _presenterOverridePrefab = presenterOverridePrefab;
        }

        /// <summary>
        /// Ensures the conversation and graph keep stable identifiers.
        /// </summary>
        public void EnsureAuthoringIds()
        {
            if (_conversationId == SerializableGuid.Empty)
            {
                _conversationId = SerializableGuid.NewGuid();
            }

            _title = NormalizeTitlePath(_title);

            Graph.EnsureEntryNode();
            Graph.EnsureNodeIds();
        }

        /// <summary>
        /// Normalizes one authored title path so empty segments and surrounding whitespace do
        /// not leak into submenu paths.
        /// </summary>
        /// <param name="title">Title path that should be normalized.</param>
        /// <returns>The normalized authored title.</returns>
        private static string NormalizeTitlePath(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "Conversation";
            }

            string[] rawSegments = title.Split(
                new[] { '/', '|' },
                StringSplitOptions.RemoveEmptyEntries);
            List<string> normalizedSegments = new(rawSegments.Length);

            for (int index = 0; index < rawSegments.Length; index++)
            {
                string segment = rawSegments[index]?.Trim();

                if (!string.IsNullOrWhiteSpace(segment))
                {
                    normalizedSegments.Add(segment);
                }
            }

            return normalizedSegments.Count == 0
                ? "Conversation"
                : string.Join("/", normalizedSegments);
        }

    }
}