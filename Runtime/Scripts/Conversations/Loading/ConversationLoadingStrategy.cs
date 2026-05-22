namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Declares the runtime loading strategy requested by the Conversations module.
    /// </summary>
    public enum ConversationLoadingStrategy
    {
        /// <summary>
        /// Loads exported payloads only from StreamingAssets-compatible JSON files.
        /// </summary>
        StreamingAssetsOnly,

        /// <summary>
        /// Loads exported payloads only from an Addressables-backed provider.
        /// </summary>
        AddressablesOnly,

        /// <summary>
        /// Prefers Addressables and falls back to StreamingAssets when configured support exists.
        /// </summary>
        AddressablesWithStreamingFallback,

        /// <summary>
        /// Prefers StreamingAssets and falls back to Addressables when configured support exists.
        /// </summary>
        StreamingWithAddressablesFallback,
    }
}