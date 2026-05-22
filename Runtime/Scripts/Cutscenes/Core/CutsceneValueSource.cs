using System;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Identifies the origin used to resolve one serialized cutscene value.
    /// </summary>
    public enum CutsceneValueSourceMode
    {
        /// <summary>
        /// Reads the value from the locally serialized payload.
        /// </summary>
        Direct,

        /// <summary>
        /// Reads the value from one blackboard variable reference.
        /// </summary>
        Blackboard,
    }

    /// <summary>
    /// Declares the runtime type one <see cref="CutsceneValueSource"/> field should represent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class CutsceneValueSourceTypeAttribute : PropertyAttribute
    {
        /// <summary>
        /// Initializes one type constraint for a value source field.
        /// </summary>
        /// <param name="valueType">Runtime type the field should resolve.</param>
        public CutsceneValueSourceTypeAttribute(Type valueType)
        {
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        }

        /// <summary>
        /// Gets the runtime type the field should resolve.
        /// </summary>
        public Type ValueType { get; }
    }

    /// <summary>
    /// Stores one stable reference to a blackboard entry.
    /// </summary>
    [Serializable]
    public sealed class CutsceneBlackboardVariableReference : GraphBlackboardVariableReference
    {
        public void Bind(CutsceneGraphBlackboardEntry entry)
        {
            base.Bind(entry);
        }

        /// <summary>
        /// Restores the serialized binding fields during migration and import flows.
        /// </summary>
        /// <param name="entryId">Stable blackboard entry identifier.</param>
        /// <param name="entryKey">Authored blackboard key fallback.</param>
        /// <param name="valueType">Expected runtime value type.</param>
        internal void RestoreBinding(
            SerializableGuid entryId,
            string entryKey,
            Type valueType)
        {
            BindScoped(
                GraphBlackboardReferenceScope.GraphLocal,
                string.Empty,
                entryId,
                entryKey,
                valueType);
        }

        /// <summary>
        /// Attempts to resolve the referenced entry from one blackboard instance.
        /// </summary>
        /// <param name="blackboard">Blackboard that owns the variable.</param>
        /// <param name="entry">Resolved entry when available.</param>
        /// <returns>True when the reference could be resolved.</returns>
        public bool TryResolveEntry(
            CutsceneGraphBlackboard blackboard,
            out CutsceneGraphBlackboardEntry entry)
        {
            entry = null;

            if (!base.TryResolveEntry(blackboard, out GraphBlackboardEntry candidate)
                || candidate is not CutsceneGraphBlackboardEntry cutsceneEntry)
            {
                return false;
            }

            entry = cutsceneEntry;
            return true;
        }

        /// <summary>
        /// Attempts to resolve the referenced payload as one typed value.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="blackboard">Blackboard that owns the variable.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
        public bool TryGetValue<T>(
            CutsceneGraphBlackboard blackboard,
            out T value)
        {
            return base.TryGetValue(blackboard, out value);
        }

        /// <summary>
        /// Attempts to resolve the referenced payload as one runtime type.
        /// </summary>
        /// <param name="blackboard">Blackboard that owns the variable.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the reference exists and matches the requested type.</returns>
        public bool TryGetValue(
            CutsceneGraphBlackboard blackboard,
            Type requestedType,
            out object value)
        {
            return base.TryGetValue(blackboard, requestedType, out value);
        }
    }

    /// <summary>
    /// Stores one value that can be authored directly or resolved from the graph blackboard.
    /// </summary>
    [Serializable]
    public sealed class CutsceneValueSource : GraphValueSource, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Gets or sets the currently selected value origin.
        /// </summary>
        public new CutsceneValueSourceMode Mode
        {
            get => (CutsceneValueSourceMode)base.Mode;
            set => base.Mode = (GraphValueSourceMode)value;
        }

        /// <summary>
        /// Gets the referenced blackboard variable metadata.
        /// </summary>
        public new CutsceneBlackboardVariableReference BlackboardVariable
        {
            get
            {
                if (BlackboardVariableInternal is not CutsceneBlackboardVariableReference reference)
                {
                    reference = CutsceneGraphCoreRuntimeMigrationUtility
                        .CreateCutsceneVariableReference(BlackboardVariableInternal);
                    BlackboardVariableInternal = reference;
                }

                return reference;
            }
        }

        /// <inheritdoc />
        protected override string GetFamilyId()
        {
            return CutsceneGraphFamily.Id;
        }

        /// <inheritdoc />
        protected override GraphBlackboardVariableReference CreateBlackboardVariableReference()
        {
            return new CutsceneBlackboardVariableReference();
        }

        /// <summary>
        /// Creates one value source initialized with one direct value.
        /// </summary>
        /// <typeparam name="T">Runtime type represented by the value.</typeparam>
        /// <param name="value">Direct value payload.</param>
        /// <returns>The created value source.</returns>
        public static CutsceneValueSource CreateDirect<T>(T value)
        {
            CutsceneValueSource source = new();
            source.SetDirectValue(value);
            return source;
        }

        /// <summary>
        /// Creates one value source initialized from one blackboard variable.
        /// </summary>
        /// <param name="entry">Referenced blackboard entry.</param>
        /// <returns>The created value source.</returns>
        public static CutsceneValueSource CreateBlackboard(
            CutsceneGraphBlackboardEntry entry)
        {
            CutsceneValueSource source = new();
            source.BindBlackboardVariable(entry);
            return source;
        }

        public void BindBlackboardVariable(CutsceneGraphBlackboardEntry entry)
        {
            base.BindBlackboardVariable(entry);
        }

        /// <summary>
        /// Attempts to resolve one typed value from the current source mode.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="context">Execution context that owns the graph blackboard.</param>
        /// <param name="value">Resolved value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue<T>(
            CutsceneExecutionContext context,
            out T value)
        {
            return base.TryGetValue(context?.RuntimeBlackboard, out value);
        }

        /// <summary>
        /// Attempts to resolve one runtime type from the current source mode.
        /// </summary>
        /// <param name="blackboard">Blackboard used for blackboard-backed lookups.</param>
        /// <param name="requestedType">Runtime type requested by the caller.</param>
        /// <param name="value">Resolved boxed value when available.</param>
        /// <returns>True when the source resolves to the requested type.</returns>
        public bool TryGetValue(
            CutsceneGraphBlackboard blackboard,
            Type requestedType,
            out object value)
        {
            return base.TryGetValue(blackboard, requestedType, out value);
        }

        /// <summary>
        /// Resolves one typed value or returns a fallback when resolution fails.
        /// </summary>
        /// <typeparam name="T">Requested value type.</typeparam>
        /// <param name="context">Execution context that owns the graph blackboard.</param>
        /// <param name="fallbackValue">Fallback returned when resolution fails.</param>
        /// <returns>The resolved or fallback value.</returns>
        public T GetValueOrDefault<T>(
            CutsceneExecutionContext context,
            T fallbackValue = default)
        {
            return base.GetValueOrDefault(context?.RuntimeBlackboard, fallbackValue);
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            NormalizeLegacyState();
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            NormalizeLegacyState();
        }

        private void NormalizeLegacyState()
        {
            if (BlackboardVariableInternal is not CutsceneBlackboardVariableReference)
            {
                BlackboardVariableInternal = CutsceneGraphCoreRuntimeMigrationUtility
                    .CreateCutsceneVariableReference(BlackboardVariableInternal);
            }

            if (DirectValueInternal is CutsceneGraphBlackboardValue legacyDirectValue)
            {
                DirectValueInternal = CutsceneGraphCoreRuntimeMigrationUtility
                    .CreateGraphBlackboardValue(legacyDirectValue)
                    ?? DirectValueInternal;
            }
        }
    }
}