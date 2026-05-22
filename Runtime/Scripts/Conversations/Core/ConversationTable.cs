using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Stores one asset-owned index of authored conversations.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ConversationTable",
        menuName = "HandyTools/Conversations/Conversation Table")]
    public sealed class ConversationTable : ScriptableObject, IGraphPropertyBindingContextProvider
    {
        private const string DefaultConversationTitle = "Conversation";
        private const string ConversationsPropertyPathToken = "_conversations.Array.data[";

        [SerializeField]
        private List<ConversationDefinition> _conversations = new();

        [SerializeField]
        private List<ConversationActorDefinition> _actors = new();

        [SerializeField]
        private string _displayName = string.Empty;

        [SerializeField]
        private InputActionReference _continueAction;

        [SerializeField]
        private InputActionReference _cancelAction;

        [SerializeField]
        private InputActionReference _skipAction;

        [SerializeField]
        private GameObject _defaultPresenterPrefab;

        /// <summary>
        /// Gets the authored conversations indexed by this table.
        /// </summary>
        public IReadOnlyList<ConversationDefinition> Conversations => _conversations;

        /// <summary>
        /// Gets the authored actor registry shared by the indexed conversations.
        /// </summary>
        /// <remarks>
        /// Shared conversants are the primary authoring source for line speaker and
        /// listener bindings across conversations in the table.
        /// </remarks>
        public IReadOnlyList<ConversationActorDefinition> Actors => _actors;

        /// <summary>
        /// Gets the authored display-name override stored on the table asset.
        /// </summary>
        public string AuthoredDisplayName => _displayName ?? string.Empty;

        /// <summary>
        /// Gets the effective human-readable display name used by editor pickers.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
            ? (string.IsNullOrWhiteSpace(name) ? "Conversation Table" : name)
            : _displayName;

        /// <summary>
        /// Gets the authored continue action explicitly assigned on the table.
        /// </summary>
        public InputActionReference AuthoredContinueAction => _continueAction;

        /// <summary>
        /// Gets the authored cancel action explicitly assigned on the table.
        /// </summary>
        public InputActionReference AuthoredCancelAction => _cancelAction;

        /// <summary>
        /// Gets the authored skip action explicitly assigned on the table.
        /// </summary>
        public InputActionReference AuthoredSkipAction => _skipAction;

        /// <summary>
        /// Gets the default presenter prefab used when conversations do not override it.
        /// </summary>
        public GameObject DefaultPresenterPrefab => _defaultPresenterPrefab;

        /// <summary>
        /// Gets the continue action resolved for runtime progression.
        /// </summary>
        public InputActionReference ContinueAction => _continueAction != null
            ? _continueAction
            : ConversationsModuleDefinition.FallbackContinueAction;

        /// <summary>
        /// Gets the cancel action resolved for runtime cancellation.
        /// </summary>
        public InputActionReference CancelAction => _cancelAction != null
            ? _cancelAction
            : ConversationsModuleDefinition.FallbackCancelAction;

        /// <summary>
        /// Gets the skip action resolved for runtime skipping.
        /// </summary>
        public InputActionReference SkipAction => _skipAction != null
            ? _skipAction
            : ConversationsModuleDefinition.FallbackSkipAction;

        /// <summary>
        /// Gets the stable graph family identifier used by authored conversations.
        /// </summary>
        public string GraphFamilyId => ConversationGraphFamily.Id;

        /// <summary>
        /// Gets the graph host kind represented by this asset.
        /// </summary>
        public GraphHostKind HostKind => GraphHostKind.Asset;

        /// <summary>
        /// Gets the Unity object that owns the authored conversation index.
        /// </summary>
        public UnityEngine.Object HostObject => this;

        /// <summary>
        /// Creates and indexes one authored conversation.
        /// </summary>
        /// <param name="title">Optional authored title for the new conversation.</param>
        /// <returns>The created authored conversation.</returns>
        public ConversationDefinition CreateConversation(string title = null)
        {
            _conversations ??= new List<ConversationDefinition>();

            ConversationDefinition conversation = ConversationDefinition.CreateDefault(
                MakeUniqueConversationTitle(title));
            _conversations.Add(conversation);
            EnsureAuthoringIds();
            return conversation;
        }

        /// <summary>
        /// Removes one authored conversation by stable identifier.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <returns>True when the conversation was removed.</returns>
        public bool RemoveConversation(SerializableGuid conversationId)
        {
            if (conversationId == SerializableGuid.Empty || _conversations == null)
            {
                return false;
            }

            for (int index = 0; index < _conversations.Count; index++)
            {
                ConversationDefinition conversation = _conversations[index];

                if (conversation == null || conversation.ConversationId != conversationId)
                {
                    continue;
                }

                _conversations.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve one authored conversation by stable identifier.
        /// </summary>
        /// <param name="conversationId">Stable authored conversation identifier.</param>
        /// <param name="conversation">Resolved conversation when found.</param>
        /// <returns>True when the table contains the requested conversation.</returns>
        public bool TryGetConversation(
            SerializableGuid conversationId,
            out ConversationDefinition conversation)
        {
            conversation = null;

            if (conversationId == SerializableGuid.Empty || _conversations == null)
            {
                return false;
            }

            for (int index = 0; index < _conversations.Count; index++)
            {
                ConversationDefinition candidate = _conversations[index];

                if (candidate == null || candidate.ConversationId != conversationId)
                {
                    continue;
                }

                conversation = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve the index of one authored conversation.
        /// </summary>
        /// <param name="conversationId">Stable authored conversation identifier.</param>
        /// <param name="index">Resolved list index when found.</param>
        /// <returns>True when the conversation exists in the table.</returns>
        public bool TryGetConversationIndex(
            SerializableGuid conversationId,
            out int index)
        {
            index = -1;

            if (conversationId == SerializableGuid.Empty || _conversations == null)
            {
                return false;
            }

            for (int currentIndex = 0; currentIndex < _conversations.Count; currentIndex++)
            {
                ConversationDefinition candidate = _conversations[currentIndex];

                if (candidate == null || candidate.ConversationId != conversationId)
                {
                    continue;
                }

                index = currentIndex;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a unique conversation title based on the current table contents.
        /// </summary>
        /// <param name="baseTitle">Preferred authored title.</param>
        /// <returns>A title that does not collide with current authored conversations.</returns>
        public string MakeUniqueConversationTitle(string baseTitle)
        {
            string sanitizedTitle = string.IsNullOrWhiteSpace(baseTitle)
                ? DefaultConversationTitle
                : baseTitle.Trim();

            if (_conversations == null || _conversations.Count == 0)
            {
                return sanitizedTitle;
            }

            HashSet<string> existingTitles = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < _conversations.Count; index++)
            {
                if (_conversations[index] == null)
                {
                    continue;
                }

                existingTitles.Add(_conversations[index].Title);
            }

            if (!existingTitles.Contains(sanitizedTitle))
            {
                return sanitizedTitle;
            }

            for (int suffix = 2; suffix < int.MaxValue; suffix++)
            {
                string candidateTitle = $"{sanitizedTitle} {suffix}";

                if (!existingTitles.Contains(candidateTitle))
                {
                    return candidateTitle;
                }
            }

            return sanitizedTitle;
        }

        /// <summary>
        /// Sets the authored continue action explicitly assigned on the table.
        /// </summary>
        /// <param name="continueAction">Continue action that should be stored on the table.</param>
        public void SetContinueAction(InputActionReference continueAction)
        {
            _continueAction = continueAction;
        }

        /// <summary>
        /// Sets the authored cancel action explicitly assigned on the table.
        /// </summary>
        /// <param name="cancelAction">Cancel action that should be stored on the table.</param>
        public void SetCancelAction(InputActionReference cancelAction)
        {
            _cancelAction = cancelAction;
        }

        /// <summary>
        /// Sets the authored skip action explicitly assigned on the table.
        /// </summary>
        /// <param name="skipAction">Skip action that should be stored on the table.</param>
        public void SetSkipAction(InputActionReference skipAction)
        {
            _skipAction = skipAction;
        }

        /// <summary>
        /// Stores the default presenter prefab used by conversations in this table.
        /// </summary>
        /// <param name="presenterPrefab">Presenter prefab that should become the table default.</param>
        public void SetDefaultPresenterPrefab(GameObject presenterPrefab)
        {
            _defaultPresenterPrefab = presenterPrefab;
        }

        /// <summary>
        /// Stores the authored display-name override used by editor-facing pickers.
        /// </summary>
        /// <param name="displayName">Human-readable display name override.</param>
        public void SetDisplayName(string displayName)
        {
            _displayName = displayName?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Resolves the effective presenter prefab for the provided authored conversation.
        /// </summary>
        /// <param name="conversation">Conversation whose override should be honored.</param>
        /// <returns>The resolved presenter prefab or null when none is configured.</returns>
        public GameObject ResolvePresenterPrefab(ConversationDefinition conversation)
        {
            return conversation?.PresenterOverridePrefab != null
                ? conversation.PresenterOverridePrefab
                : _defaultPresenterPrefab;
        }

        /// <summary>
        /// Creates and indexes one authored conversant.
        /// </summary>
        /// <param name="key">Optional authored key for the new conversant.</param>
        /// <returns>The created conversant definition.</returns>
        public ConversationActorDefinition CreateActor(string key = null)
        {
            _actors ??= new List<ConversationActorDefinition>();

            ConversationActorDefinition actor = ConversationActorDefinition.CreateDefault(
                MakeUniqueActorKey(key));
            _actors.Add(actor);
            EnsureAuthoringIds();
            return actor;
        }

        /// <summary>
        /// Duplicates one authored conversant while issuing one new stable identifier.
        /// </summary>
        /// <param name="actorId">Stable actor identifier that should be duplicated.</param>
        /// <returns>The duplicated conversant when found.</returns>
        public ConversationActorDefinition DuplicateActor(SerializableGuid actorId)
        {
            if (!TryGetActor(actorId, out ConversationActorDefinition actor))
            {
                return null;
            }

            _actors ??= new List<ConversationActorDefinition>();

            ConversationActorDefinition duplicate = actor.Duplicate(
                MakeUniqueActorKey($"{actor.Key}-copy"));
            _actors.Add(duplicate);
            EnsureAuthoringIds();
            return duplicate;
        }

        /// <summary>
        /// Removes one authored conversant by stable identifier.
        /// </summary>
        /// <param name="actorId">Stable authored actor identifier.</param>
        /// <returns>True when the actor was removed.</returns>
        public bool RemoveActor(SerializableGuid actorId)
        {
            if (actorId == SerializableGuid.Empty || _actors == null)
            {
                return false;
            }

            for (int index = 0; index < _actors.Count; index++)
            {
                ConversationActorDefinition actor = _actors[index];

                if (actor == null || actor.ActorId != actorId)
                {
                    continue;
                }

                _actors.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve one authored conversant by stable identifier.
        /// </summary>
        /// <param name="actorId">Stable authored actor identifier.</param>
        /// <param name="actor">Resolved conversant when found.</param>
        /// <returns>True when the table contains the requested conversant.</returns>
        public bool TryGetActor(
            SerializableGuid actorId,
            out ConversationActorDefinition actor)
        {
            actor = null;

            if (actorId == SerializableGuid.Empty || _actors == null)
            {
                return false;
            }

            for (int index = 0; index < _actors.Count; index++)
            {
                ConversationActorDefinition candidate = _actors[index];

                if (candidate == null || candidate.ActorId != actorId)
                {
                    continue;
                }

                actor = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve the index of one authored conversant.
        /// </summary>
        /// <param name="actorId">Stable authored actor identifier.</param>
        /// <param name="index">Resolved list index when found.</param>
        /// <returns>True when the actor exists in the table.</returns>
        public bool TryGetActorIndex(
            SerializableGuid actorId,
            out int index)
        {
            index = -1;

            if (actorId == SerializableGuid.Empty || _actors == null)
            {
                return false;
            }

            for (int currentIndex = 0; currentIndex < _actors.Count; currentIndex++)
            {
                ConversationActorDefinition candidate = _actors[currentIndex];

                if (candidate == null || candidate.ActorId != actorId)
                {
                    continue;
                }

                index = currentIndex;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates one unique conversant key based on the current table contents.
        /// </summary>
        /// <param name="baseKey">Preferred conversant key.</param>
        /// <returns>A key that does not collide with current authored conversants.</returns>
        public string MakeUniqueActorKey(string baseKey)
        {
            string sanitizedKey = ConversationActorDefinition.NormalizeKey(baseKey);

            if (_actors == null || _actors.Count == 0)
            {
                return sanitizedKey;
            }

            HashSet<string> existingKeys = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < _actors.Count; index++)
            {
                if (_actors[index] == null)
                {
                    continue;
                }

                existingKeys.Add(_actors[index].Key);
            }

            if (!existingKeys.Contains(sanitizedKey))
            {
                return sanitizedKey;
            }

            for (int suffix = 2; suffix < int.MaxValue; suffix++)
            {
                string candidateKey = $"{sanitizedKey}-{suffix}";

                if (!existingKeys.Contains(candidateKey))
                {
                    return candidateKey;
                }
            }

            return sanitizedKey;
        }

        /// <summary>
        /// Ensures authored conversations, actors, and nodes keep stable identifiers.
        /// </summary>
        public void EnsureAuthoringIds()
        {
            _conversations ??= new List<ConversationDefinition>();
            _actors ??= new List<ConversationActorDefinition>();

            for (int index = 0; index < _actors.Count; index++)
            {
                _actors[index]?.EnsureId();
            }

            for (int index = 0; index < _conversations.Count; index++)
            {
                ConversationDefinition conversation = _conversations[index];
                conversation?.EnsureAuthoringIds();
            }
        }

        /// <inheritdoc />
        public bool TryResolveGraphPropertyBinding(
            string propertyPath,
            out GraphDefinition graph,
            out object hostOwner,
            out string familyId)
        {
            graph = null;
            hostOwner = null;
            familyId = GraphFamilyId;

            if (!TryResolveConversationFromPropertyPath(
                    propertyPath,
                    out ConversationDefinition conversation))
            {
                return false;
            }

            graph = conversation.Graph;
            hostOwner = conversation;
            return graph != null;
        }

        /// <summary>
        /// Ensures the authored table keeps stable identifiers after serialization changes.
        /// </summary>
        private void OnValidate()
        {
            _displayName = _displayName?.Trim() ?? string.Empty;
            EnsureAuthoringIds();
        }

        /// <summary>
        /// Restores the empty authored table when the asset is first created.
        /// </summary>
        private void Reset()
        {
            _conversations = new List<ConversationDefinition>();
            _actors = new List<ConversationActorDefinition>();
            _continueAction = null;
            _cancelAction = null;
            _skipAction = null;
            _displayName = string.Empty;
            EnsureAuthoringIds();
        }

        private bool TryResolveConversationFromPropertyPath(
            string propertyPath,
            out ConversationDefinition conversation)
        {
            conversation = null;

            if (string.IsNullOrWhiteSpace(propertyPath) || _conversations == null)
            {
                return false;
            }

            int tokenIndex = propertyPath.IndexOf(
                ConversationsPropertyPathToken,
                StringComparison.Ordinal);

            if (tokenIndex < 0)
            {
                return false;
            }

            int indexStart = tokenIndex + ConversationsPropertyPathToken.Length;
            int indexEnd = propertyPath.IndexOf(']', indexStart);

            if (indexEnd <= indexStart)
            {
                return false;
            }

            string indexText = propertyPath.Substring(indexStart, indexEnd - indexStart);

            if (!int.TryParse(indexText, out int conversationIndex)
                || conversationIndex < 0
                || conversationIndex >= _conversations.Count)
            {
                return false;
            }

            conversation = _conversations[conversationIndex];
            return conversation != null;
        }
    }
}