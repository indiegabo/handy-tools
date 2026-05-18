using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Describes one command type for routing and diagnostics.
    /// </summary>
    [Serializable]
    public readonly struct CommandDescriptor
    {
        /// <summary>
        /// Creates one command descriptor.
        /// </summary>
        /// <param name="commandType">Stable command type name.</param>
        /// <param name="displayName">Human-readable command display name.</param>
        /// <param name="description">Optional command description.</param>
        public CommandDescriptor(
            string commandType,
            string displayName,
            string description = "")
        {
            CommandType = string.IsNullOrWhiteSpace(commandType)
                ? nameof(IHandyCommand)
                : commandType.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? CommandType
                : displayName.Trim();
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// Gets the stable command type name.
        /// </summary>
        public string CommandType { get; }

        /// <summary>
        /// Gets the human-readable display name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the optional command description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Creates a descriptor from a compile-time command type.
        /// </summary>
        /// <typeparam name="TCommand">Command type.</typeparam>
        /// <param name="displayName">Human-readable display name.</param>
        /// <param name="description">Optional command description.</param>
        /// <returns>The created descriptor.</returns>
        public static CommandDescriptor Create<TCommand>(
            string displayName,
            string description = "")
        {
            Type commandType = typeof(TCommand);
            return new CommandDescriptor(
                commandType.FullName ?? commandType.Name,
                displayName,
                description);
        }
    }
}