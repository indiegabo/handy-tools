using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Loading;
using IndieGabo.HandyTools.Modules;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Declares the optional HandyTools module descriptor used by Conversations.
    /// </summary>
    public static class ConversationsModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable module descriptor exposed to the HandyTools kernel.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "conversations",
            "Conversations",
            "Conversation tables that index authored conversations built on the shared HandyTools GraphCore layer.",
            HandyModuleActivationMode.Optional,
            176);

        /// <summary>
        /// Gets the static dependency list required by the module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the module is currently enabled in module settings.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);

        /// <summary>
        /// Gets the fallback continue action configured at the module level.
        /// </summary>
        public static InputActionReference FallbackContinueAction =>
            ConversationRuntimeSettings.Instance.FallbackContinueAction;

        /// <summary>
        /// Gets the fallback cancel action configured at the module level.
        /// </summary>
        public static InputActionReference FallbackCancelAction =>
            ConversationRuntimeSettings.Instance.FallbackCancelAction;

        /// <summary>
        /// Gets the fallback skip action configured at the module level.
        /// </summary>
        public static InputActionReference FallbackSkipAction =>
            ConversationRuntimeSettings.Instance.FallbackSkipAction;
    }
}