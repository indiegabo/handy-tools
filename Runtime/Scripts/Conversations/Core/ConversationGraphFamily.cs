using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Declares the stable GraphCore family identity used by Conversations hosts.
    /// </summary>
    public static class ConversationGraphFamily
    {
        /// <summary>
        /// Stable graph family identifier used by runtime and editor integrations.
        /// </summary>
        public const string Id = "handytools.conversations";

        /// <summary>
        /// Shared family definition registered in the GraphCore family registry.
        /// </summary>
        public static readonly GraphFamilyDefinition Definition = new(
            Id,
            "Conversations",
            "Shared graph family used by the HandyTools Conversations module.");

        /// <summary>
        /// Registers the Conversations graph family in the current application domain.
        /// </summary>
        /// <returns>The registered graph family definition.</returns>
        public static GraphFamilyDefinition Register()
        {
            return GraphFamilyRegistry.Register(Definition);
        }
    }
}