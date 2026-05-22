using System.Threading;
using System.Threading.Tasks;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Loads exported runtime conversation payloads on demand.
    /// </summary>
    public interface IConversationLoader
    {
        /// <summary>
        /// Loads one runtime conversation payload and acquires one active cache reference.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The load result.</returns>
        Task<ConversationLoadResult> LoadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads one runtime conversation payload without acquiring one active cache reference.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The preload result.</returns>
        Task<ConversationLoadResult> PreloadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases one active cache reference for the requested conversation.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        void Release(SerializableGuid conversationId);
    }
}