using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Stores one lightweight editor drag session for graph blackboard variables.
    /// </summary>
    public static class GraphBlackboardDragSession
    {
        private static object s_activeOwner;
        private static SerializableGuid s_activeEntryId;
        private static string s_activeEntryLabel = string.Empty;

        /// <summary>
        /// Gets whether one graph-blackboard drag session is currently active.
        /// </summary>
        public static bool HasActiveDrag => s_activeOwner != null
            && s_activeEntryId != SerializableGuid.Empty;

        /// <summary>
        /// Gets the current dragged entry label used by host-side badges.
        /// </summary>
        public static string ActiveEntryLabel => s_activeEntryLabel;

        /// <summary>
        /// Starts one graph-blackboard drag session.
        /// </summary>
        /// <param name="owner">Host object that owns the dragged entry.</param>
        /// <param name="entryId">Stable dragged entry identifier.</param>
        /// <param name="entryLabel">Human-readable entry label.</param>
        public static void BeginDrag(
            object owner,
            SerializableGuid entryId,
            string entryLabel)
        {
            if (owner == null || entryId == SerializableGuid.Empty)
            {
                return;
            }

            s_activeOwner = owner;
            s_activeEntryId = entryId;
            s_activeEntryLabel = entryLabel ?? string.Empty;
        }

        /// <summary>
        /// Cancels the current graph-blackboard drag session.
        /// </summary>
        public static void CancelDrag()
        {
            s_activeOwner = null;
            s_activeEntryId = SerializableGuid.Empty;
            s_activeEntryLabel = string.Empty;
        }

        /// <summary>
        /// Attempts to resolve the active dragged entry identifier for one expected owner.
        /// </summary>
        /// <param name="expectedOwner">Owner expected by the current host.</param>
        /// <param name="entryId">Resolved entry identifier when the session matches.</param>
        /// <returns>True when the current drag belongs to the expected owner.</returns>
        public static bool TryGetActiveEntryId(
            object expectedOwner,
            out SerializableGuid entryId)
        {
            entryId = SerializableGuid.Empty;

            if (!ReferenceEquals(s_activeOwner, expectedOwner)
                || s_activeEntryId == SerializableGuid.Empty)
            {
                return false;
            }

            entryId = s_activeEntryId;
            return true;
        }
    }
}