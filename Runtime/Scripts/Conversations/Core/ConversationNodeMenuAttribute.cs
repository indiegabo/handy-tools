using System;
using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Declares the Conversations graph creation metadata exposed in editor catalogs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConversationNodeMenuAttribute : Attribute, IGraphNodeMenuMetadata
    {
        /// <summary>
        /// Initializes one menu attribute that infers the node title from the path.
        /// </summary>
        /// <param name="menuPath">Menu path exposed by creation surfaces.</param>
        public ConversationNodeMenuAttribute(string menuPath)
        {
            MenuPath = menuPath;
            DefaultTitle = string.Empty;
        }

        /// <summary>
        /// Initializes one menu attribute with one explicit default node title.
        /// </summary>
        /// <param name="menuPath">Menu path exposed by creation surfaces.</param>
        /// <param name="defaultTitle">Fallback authored title used by the node.</param>
        public ConversationNodeMenuAttribute(string menuPath, string defaultTitle)
        {
            MenuPath = menuPath;
            DefaultTitle = defaultTitle ?? string.Empty;
        }

        /// <summary>
        /// Gets the menu path exposed by authoring surfaces.
        /// </summary>
        public string MenuPath { get; }

        /// <summary>
        /// Gets the fallback node title declared by the menu metadata.
        /// </summary>
        public string DefaultTitle { get; }
    }
}