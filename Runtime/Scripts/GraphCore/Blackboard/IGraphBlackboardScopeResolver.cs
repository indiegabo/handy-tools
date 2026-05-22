using System;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Resolves blackboard variable references that target non-local scopes.
    /// </summary>
    public interface IGraphBlackboardScopeResolver
    {
        /// <summary>
        /// Attempts to resolve one referenced value from one non-local scope.
        /// </summary>
        /// <param name="reference">Reference being resolved.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the reference could be resolved.</returns>
        bool TryGetValue(
            GraphBlackboardVariableReference reference,
            Type requestedType,
            out object value);
    }
}