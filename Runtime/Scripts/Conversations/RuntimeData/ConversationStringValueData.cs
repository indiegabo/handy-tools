using System;
using IndieGabo.HandyTools.GraphCore;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.RuntimeData
{
    /// <summary>
    /// Stores one exported string source that can resolve from direct text or one blackboard variable.
    /// </summary>
    [Serializable]
    public sealed class ConversationStringValueData
    {
        [SerializeField]
        private GraphValueSourceMode _mode;

        [SerializeField]
        private string _directValue = string.Empty;

        [SerializeField]
        private GraphBlackboardVariableReference _blackboardVariable;

        /// <summary>
        /// Initializes one empty direct string source.
        /// </summary>
        public ConversationStringValueData()
        {
            _mode = GraphValueSourceMode.Direct;
            _directValue = string.Empty;
        }

        /// <summary>
        /// Initializes one exported string source.
        /// </summary>
        /// <param name="mode">Selected runtime resolution mode.</param>
        /// <param name="directValue">Direct fallback text payload.</param>
        /// <param name="blackboardVariable">Optional blackboard variable binding.</param>
        public ConversationStringValueData(
            GraphValueSourceMode mode,
            string directValue,
            GraphBlackboardVariableReference blackboardVariable)
        {
            _mode = mode;
            _directValue = directValue ?? string.Empty;
            _blackboardVariable = CloneReference(blackboardVariable);
        }

        /// <summary>
        /// Gets the runtime resolution mode represented by the exported value source.
        /// </summary>
        public GraphValueSourceMode Mode => _mode;

        /// <summary>
        /// Gets the direct text payload stored in the export.
        /// </summary>
        public string DirectValue => _directValue ?? string.Empty;

        /// <summary>
        /// Gets the blackboard reference used when the source resolves from shared state.
        /// </summary>
        public GraphBlackboardVariableReference BlackboardVariable => _blackboardVariable;

        /// <summary>
        /// Creates one exported direct string source.
        /// </summary>
        /// <param name="value">Direct text payload.</param>
        /// <returns>The created exported direct-value DTO.</returns>
        public static ConversationStringValueData CreateDirect(string value)
        {
            return new ConversationStringValueData(
                GraphValueSourceMode.Direct,
                value,
                null);
        }

        /// <summary>
        /// Creates one exported blackboard-backed string source.
        /// </summary>
        /// <param name="blackboardVariable">Referenced variable metadata.</param>
        /// <returns>The created exported blackboard-value DTO.</returns>
        public static ConversationStringValueData CreateBlackboard(
            GraphBlackboardVariableReference blackboardVariable)
        {
            return new ConversationStringValueData(
                GraphValueSourceMode.Blackboard,
                string.Empty,
                blackboardVariable);
        }

        /// <summary>
        /// Clones one blackboard reference so exported payloads do not retain mutable authoring state.
        /// </summary>
        /// <param name="source">Source reference to clone.</param>
        /// <returns>The cloned reference when assigned.</returns>
        private static GraphBlackboardVariableReference CloneReference(
            GraphBlackboardVariableReference source)
        {
            if (source == null)
            {
                return null;
            }

            GraphBlackboardVariableReference clone = new();

            if (!source.IsAssigned)
            {
                return clone;
            }

            clone.BindScoped(
                source.Scope,
                source.ScopeKey,
                source.EntryId,
                source.EntryKey,
                source.ValueType);

            return clone;
        }
    }
}