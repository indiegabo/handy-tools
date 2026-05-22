using System;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Defines the runtime playback contract consumed by composed presenter prefabs.
    /// </summary>
    public interface IConversationPlaybackController
    {
        #region Events

        /// <summary>
        /// Raised after the playback state, active session, or active line changes.
        /// </summary>
        event Action PlaybackStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the authored table that owns the active playback configuration.
        /// </summary>
        ConversationTable Table { get; }

        /// <summary>
        /// Gets the active runtime session when one conversation is bound.
        /// </summary>
        ConversationSession Session { get; }

        /// <summary>
        /// Gets whether the controller is still preparing playback.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Gets the latest runtime status message.
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// Gets the latest failure reason recorded by the controller.
        /// </summary>
        string FailureReason { get; }

        #endregion

        #region Public API

        /// <summary>
        /// Starts playback for the currently configured conversation selection.
        /// </summary>
        void Play();

        /// <summary>
        /// Advances the active conversation line.
        /// </summary>
        /// <returns>True when the request was accepted.</returns>
        bool AdvanceConversation();

        /// <summary>
        /// Ends the active conversation through the skip action.
        /// </summary>
        /// <returns>True when the request was accepted.</returns>
        bool SkipConversation();

        /// <summary>
        /// Cancels the active conversation playback.
        /// </summary>
        /// <returns>True when the request was accepted.</returns>
        bool CancelConversation();

        /// <summary>
        /// Resolves the authored portrait associated with one runtime conversant.
        /// </summary>
        /// <param name="actor">Runtime conversant that should be presented.</param>
        /// <returns>The resolved portrait when one is authored; otherwise <c>null</c>.</returns>
        Sprite ResolveActorPortrait(ConversationActorData actor);

        #endregion
    }
}