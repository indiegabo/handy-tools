using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Declares the stable GraphCore family identity used by Cutscenes hosts.
    /// </summary>
    public static class CutsceneGraphFamily
    {
        /// <summary>
        /// Stable graph family identifier used by runtime and editor integrations.
        /// </summary>
        public const string Id = "handytools.cutscenes";

        /// <summary>
        /// Shared family definition registered in the GraphCore family registry.
        /// </summary>
        public static readonly GraphFamilyDefinition Definition = new(
            Id,
            "Cutscenes",
            "Shared graph family used by the HandyTools Cutscenes module.");

        /// <summary>
        /// Registers the Cutscenes graph family in the current application domain.
        /// </summary>
        /// <returns>The registered graph family definition.</returns>
        public static GraphFamilyDefinition Register()
        {
            return GraphFamilyRegistry.Register(Definition);
        }
    }
}