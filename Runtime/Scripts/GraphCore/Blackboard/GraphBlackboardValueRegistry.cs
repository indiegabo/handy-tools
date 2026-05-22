using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Declares one blackboard value wrapper that can be instantiated by the runtime
    /// and optionally surfaced by authoring pickers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class GraphBlackboardValueDescriptorAttribute : Attribute
    {
        /// <summary>
        /// Initializes one descriptor attribute for a blackboard wrapper type.
        /// </summary>
        /// <param name="displayName">Label shown in authoring UIs.</param>
        /// <param name="runtimeValueType">Runtime type represented by the wrapper.</param>
        public GraphBlackboardValueDescriptorAttribute(
            string displayName,
            Type runtimeValueType)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException(
                    "Blackboard value display name cannot be blank.",
                    nameof(displayName))
                : displayName;
            RuntimeValueType = runtimeValueType
                ?? throw new ArgumentNullException(nameof(runtimeValueType));
        }

        /// <summary>
        /// Gets the label shown in authoring UIs.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the runtime type represented by the wrapper.
        /// </summary>
        public Type RuntimeValueType { get; }

        /// <summary>
        /// Gets or sets the sort order used by picker UIs.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets whether the wrapper can represent derived runtime types.
        /// </summary>
        public bool SupportsDerivedTypes { get; set; }

        /// <summary>
        /// Gets or sets whether the wrapper should be hidden from picker UIs.
        /// </summary>
        public bool HiddenFromPicker { get; set; }

        /// <summary>
        /// Gets or sets one optional icon identifier for authoring UIs.
        /// </summary>
        public string IconName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Discovers and instantiates graph blackboard value wrappers.
    /// </summary>
    public static class GraphBlackboardValueRegistry
    {
        /// <summary>
        /// Describes one discovered wrapper type.
        /// </summary>
        public sealed class Descriptor
        {
            internal Descriptor(
                Type wrapperType,
                GraphBlackboardValueDescriptorAttribute metadata)
            {
                WrapperType = wrapperType
                    ?? throw new ArgumentNullException(nameof(wrapperType));
                Metadata = metadata
                    ?? throw new ArgumentNullException(nameof(metadata));
            }

            /// <summary>
            /// Gets the wrapper implementation type.
            /// </summary>
            public Type WrapperType { get; }

            /// <summary>
            /// Gets the authoring label.
            /// </summary>
            public string DisplayName => Metadata.DisplayName;

            /// <summary>
            /// Gets the runtime type declared by the wrapper.
            /// </summary>
            public Type RuntimeValueType => Metadata.RuntimeValueType;

            /// <summary>
            /// Gets the picker sort order.
            /// </summary>
            public int Order => Metadata.Order;

            /// <summary>
            /// Gets whether the wrapper can represent derived runtime types.
            /// </summary>
            public bool SupportsDerivedTypes => Metadata.SupportsDerivedTypes;

            /// <summary>
            /// Gets whether the wrapper should be hidden from picker UIs.
            /// </summary>
            public bool HiddenFromPicker => Metadata.HiddenFromPicker;

            /// <summary>
            /// Gets the optional icon identifier exposed by the descriptor.
            /// </summary>
            public string IconName => Metadata.IconName;

            /// <summary>
            /// Gets whether the wrapper belongs to the built-in shared value set.
            /// </summary>
            public bool IsBuiltIn => IsBuiltInWrapperType(WrapperType);

            private GraphBlackboardValueDescriptorAttribute Metadata { get; }

            /// <summary>
            /// Determines whether the descriptor can represent the requested type.
            /// </summary>
            /// <param name="valueType">Requested runtime type.</param>
            /// <returns>True when the wrapper can represent the type.</returns>
            public bool CanRepresent(Type valueType)
            {
                if (valueType == null)
                {
                    return false;
                }

                if (RuntimeValueType == valueType)
                {
                    return true;
                }

                if (RuntimeValueType == typeof(Enum))
                {
                    return valueType.IsEnum;
                }

                return SupportsDerivedTypes && RuntimeValueType.IsAssignableFrom(valueType);
            }

            /// <summary>
            /// Creates one fresh wrapper initialized for the requested runtime type.
            /// </summary>
            /// <param name="concreteValueType">Concrete runtime type to represent.</param>
            /// <returns>The initialized wrapper instance.</returns>
            public GraphBlackboardValue CreateValue(Type concreteValueType)
            {
                GraphBlackboardValue value = Activator.CreateInstance(WrapperType)
                    as GraphBlackboardValue;

                if (value == null)
                {
                    throw new InvalidOperationException(
                        $"Could not instantiate blackboard value wrapper '{WrapperType.FullName}'.");
                }

                value.InitializeForValueType(concreteValueType ?? RuntimeValueType);
                return value;
            }
        }

        private static readonly Dictionary<string, HashSet<Type>> _familyWrappers =
            new(StringComparer.Ordinal);

        private static readonly HashSet<Type> _builtInWrapperTypes = new()
        {
            typeof(GraphBlackboardIntValue),
            typeof(GraphBlackboardLongValue),
            typeof(GraphBlackboardFloatValue),
            typeof(GraphBlackboardDoubleValue),
            typeof(GraphBlackboardStringValue),
            typeof(GraphBlackboardBoolValue),
            typeof(GraphBlackboardVector2Value),
            typeof(GraphBlackboardVector2IntValue),
            typeof(GraphBlackboardVector3Value),
            typeof(GraphBlackboardVector3IntValue),
            typeof(GraphBlackboardVector4Value),
            typeof(GraphBlackboardQuaternionValue),
            typeof(GraphBlackboardColorValue),
            typeof(GraphBlackboardRectValue),
            typeof(GraphBlackboardRectIntValue),
            typeof(GraphBlackboardBoundsValue),
            typeof(GraphBlackboardBoundsIntValue),
            typeof(GraphBlackboardAnimationCurveValue),
            typeof(GraphBlackboardGradientValue),
            typeof(GraphBlackboardLayerMaskValue),
            typeof(GraphBlackboardEnumValue),
            typeof(GraphBlackboardUnityObjectValue),
            typeof(GraphBlackboardObjectValue),
            typeof(GraphBlackboardGameObjectValue),
            typeof(GraphBlackboardScriptableObjectValue),
        };

        private static IReadOnlyList<Descriptor> _descriptors;
        private static IReadOnlyDictionary<Type, Descriptor> _descriptorsByWrapperType;

        /// <summary>
        /// Gets the discovered built-in descriptors sorted for authoring pickers.
        /// </summary>
        public static IReadOnlyList<Descriptor> Descriptors => GetDescriptors();

        /// <summary>
        /// Gets the descriptors available to one graph family.
        /// </summary>
        /// <param name="familyId">Optional graph family id.</param>
        /// <param name="includeHidden">Whether hidden descriptors should be returned.</param>
        /// <returns>The descriptors available to the family.</returns>
        public static IReadOnlyList<Descriptor> GetDescriptors(
            string familyId = null,
            bool includeHidden = false)
        {
            EnsureInitialized();

            List<Descriptor> result = new();
            HashSet<Type> seenWrapperTypes = new();

            foreach (Descriptor descriptor in EnumerateFamilyDescriptors(familyId))
            {
                if ((!includeHidden && descriptor.HiddenFromPicker)
                    || !seenWrapperTypes.Add(descriptor.WrapperType))
                {
                    continue;
                }

                result.Add(descriptor);
            }

            return result;
        }

        /// <summary>
        /// Registers one family-specific wrapper type.
        /// </summary>
        /// <param name="familyId">Target graph family id.</param>
        /// <param name="wrapperType">Wrapper type to expose to the family.</param>
        public static void RegisterFamilyWrapper(string familyId, Type wrapperType)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(familyId))
            {
                throw new ArgumentException(
                    "Graph family id cannot be null or whitespace.",
                    nameof(familyId));
            }

            if (wrapperType == null)
            {
                throw new ArgumentNullException(nameof(wrapperType));
            }

            if (!TryGetDescriptor(wrapperType, out _))
            {
                throw new InvalidOperationException(
                    $"Wrapper type '{wrapperType.FullName}' is not registered with a graph blackboard descriptor.");
            }

            string normalizedFamilyId = familyId.Trim();

            if (!_familyWrappers.TryGetValue(normalizedFamilyId, out HashSet<Type> wrappers))
            {
                wrappers = new HashSet<Type>();
                _familyWrappers.Add(normalizedFamilyId, wrappers);
            }

            wrappers.Add(wrapperType);
        }

        /// <summary>
        /// Resolves one descriptor by wrapper type.
        /// </summary>
        /// <param name="wrapperType">Wrapper implementation type.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the descriptor exists.</returns>
        public static bool TryGetDescriptor(Type wrapperType, out Descriptor descriptor)
        {
            EnsureInitialized();

            if (wrapperType == null)
            {
                descriptor = null;
                return false;
            }

            return _descriptorsByWrapperType.TryGetValue(wrapperType, out descriptor);
        }

        /// <summary>
        /// Resolves one descriptor by wrapper instance.
        /// </summary>
        /// <param name="value">Stored blackboard value wrapper.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the descriptor exists.</returns>
        public static bool TryGetDescriptor(GraphBlackboardValue value, out Descriptor descriptor)
        {
            descriptor = null;
            return value != null && TryGetDescriptor(value.GetType(), out descriptor);
        }

        /// <summary>
        /// Resolves one descriptor by display name inside one optional graph family.
        /// </summary>
        /// <param name="displayName">Display name shown in the picker.</param>
        /// <param name="familyId">Optional graph family id.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the descriptor exists.</returns>
        public static bool TryGetDescriptor(
            string displayName,
            string familyId,
            out Descriptor descriptor)
        {
            descriptor = GetDescriptors(familyId)
                .FirstOrDefault(candidate => string.Equals(
                    candidate.DisplayName,
                    displayName,
                    StringComparison.Ordinal));

            return descriptor != null;
        }

        /// <summary>
        /// Creates one new wrapper for the requested runtime type.
        /// </summary>
        /// <param name="valueType">Runtime type that should be represented.</param>
        /// <param name="value">Created wrapper when the type is supported.</param>
        /// <returns>True when the registry can represent the type.</returns>
        public static bool TryCreateValue(Type valueType, out GraphBlackboardValue value)
        {
            return TryCreateValue(valueType, null, out value);
        }

        /// <summary>
        /// Creates one new wrapper for the requested runtime type inside one optional graph family.
        /// </summary>
        /// <param name="valueType">Runtime type that should be represented.</param>
        /// <param name="familyId">Optional graph family id.</param>
        /// <param name="value">Created wrapper when the type is supported.</param>
        /// <returns>True when the registry can represent the type.</returns>
        public static bool TryCreateValue(
            Type valueType,
            string familyId,
            out GraphBlackboardValue value)
        {
            value = null;

            if (!TryGetDescriptorForRuntimeType(valueType, familyId, out Descriptor descriptor))
            {
                return false;
            }

            value = descriptor.CreateValue(valueType);
            return true;
        }

        /// <summary>
        /// Resolves one descriptor for the requested runtime type.
        /// </summary>
        /// <param name="valueType">Runtime type that should be represented.</param>
        /// <param name="familyId">Optional graph family id.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the type is supported.</returns>
        public static bool TryGetDescriptorForRuntimeType(
            Type valueType,
            string familyId,
            out Descriptor descriptor)
        {
            EnsureInitialized();
            descriptor = null;

            if (valueType == null)
            {
                return false;
            }

            List<Descriptor> descriptors = EnumerateFamilyDescriptors(familyId).ToList();

            Descriptor exactDescriptor = descriptors
                .Where(candidate => candidate.RuntimeValueType == valueType)
                .OrderBy(candidate => candidate.HiddenFromPicker ? 1 : 0)
                .ThenBy(candidate => candidate.Order)
                .ThenBy(candidate => candidate.DisplayName, StringComparer.Ordinal)
                .FirstOrDefault();

            if (exactDescriptor != null)
            {
                descriptor = exactDescriptor;
                return true;
            }

            descriptor = descriptors
                .Where(candidate => candidate.CanRepresent(valueType))
                .OrderBy(candidate => candidate.IsBuiltIn ? 1 : 0)
                .ThenBy(candidate => GetInheritanceDistance(candidate.RuntimeValueType, valueType))
                .ThenBy(candidate => candidate.Order)
                .ThenBy(candidate => candidate.DisplayName, StringComparer.Ordinal)
                .FirstOrDefault();

            return descriptor != null;
        }

        /// <summary>
        /// Resolves the human-readable label for one stored wrapper instance.
        /// </summary>
        /// <param name="value">Stored wrapper instance.</param>
        /// <returns>The best available label for the wrapper.</returns>
        public static string GetDisplayName(GraphBlackboardValue value)
        {
            if (value == null)
            {
                return "Unknown";
            }

            return TryGetDescriptor(value, out Descriptor descriptor)
                ? descriptor.DisplayName
                : value.GetType().Name;
        }

        /// <summary>
        /// Clears registered family-specific wrapper contributions.
        /// This is intended for controlled test or domain-reset scenarios.
        /// </summary>
        internal static void ClearFamilyRegistrations()
        {
            _familyWrappers.Clear();
        }

        private static bool IsBuiltInWrapperType(Type wrapperType)
        {
            return wrapperType != null && _builtInWrapperTypes.Contains(wrapperType);
        }

        private static IEnumerable<Descriptor> EnumerateFamilyDescriptors(string familyId)
        {
            EnsureInitialized();

            if (!string.IsNullOrWhiteSpace(familyId)
                && _familyWrappers.TryGetValue(familyId.Trim(), out HashSet<Type> familyTypes))
            {
                foreach (Type wrapperType in familyTypes)
                {
                    if (_descriptorsByWrapperType.TryGetValue(wrapperType, out Descriptor descriptor))
                    {
                        yield return descriptor;
                    }
                }
            }

            for (int i = 0; i < _descriptors.Count; i++)
            {
                Descriptor descriptor = _descriptors[i];

                if (!descriptor.IsBuiltIn)
                {
                    continue;
                }

                yield return descriptor;
            }
        }

        /// <summary>
        /// Ensures the descriptor cache is ready for use.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_descriptors != null && _descriptorsByWrapperType != null)
            {
                return;
            }

            List<Descriptor> descriptors = EnumerateDescriptorTypes()
                .Select(type =>
                {
                    GraphBlackboardValueDescriptorAttribute metadata =
                        type.GetCustomAttribute<GraphBlackboardValueDescriptorAttribute>();
                    return metadata == null ? null : new Descriptor(type, metadata);
                })
                .Where(descriptor => descriptor != null)
                .OrderBy(descriptor => descriptor.Order)
                .ThenBy(descriptor => descriptor.DisplayName, StringComparer.Ordinal)
                .ToList();

            _descriptors = descriptors;
            _descriptorsByWrapperType = descriptors.ToDictionary(
                descriptor => descriptor.WrapperType,
                descriptor => descriptor);
        }

        /// <summary>
        /// Enumerates all non-abstract wrapper types decorated with one descriptor attribute.
        /// </summary>
        /// <returns>The discovered wrapper types.</returns>
        private static IEnumerable<Type> EnumerateDescriptorTypes()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Assembly assembly = assemblies[assemblyIndex];
                Type[] assemblyTypes = Array.Empty<Type>();

                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    assemblyTypes = exception.Types;
                }

                if (assemblyTypes == null)
                {
                    continue;
                }

                for (int typeIndex = 0; typeIndex < assemblyTypes.Length; typeIndex++)
                {
                    Type type = assemblyTypes[typeIndex];

                    if (type == null
                        || type.IsAbstract
                        || !typeof(GraphBlackboardValue).IsAssignableFrom(type)
                        || type.GetCustomAttribute<GraphBlackboardValueDescriptorAttribute>() == null)
                    {
                        continue;
                    }

                    yield return type;
                }
            }
        }

        /// <summary>
        /// Calculates how far one actual type is from the descriptor root type.
        /// Lower numbers represent more specific matches.
        /// </summary>
        /// <param name="descriptorType">Root type declared by the descriptor.</param>
        /// <param name="actualType">Requested runtime type.</param>
        /// <returns>The inheritance distance used for descriptor ordering.</returns>
        private static int GetInheritanceDistance(Type descriptorType, Type actualType)
        {
            if (descriptorType == null || actualType == null)
            {
                return int.MaxValue;
            }

            if (descriptorType == actualType)
            {
                return 0;
            }

            if (descriptorType == typeof(Enum) && actualType.IsEnum)
            {
                return 1;
            }

            if (descriptorType.IsInterface)
            {
                return descriptorType.IsAssignableFrom(actualType)
                    ? 1
                    : int.MaxValue;
            }

            int distance = 0;
            Type currentType = actualType;

            while (currentType != null)
            {
                if (currentType == descriptorType)
                {
                    return distance;
                }

                currentType = currentType.BaseType;
                distance++;
            }

            return int.MaxValue;
        }
    }
}