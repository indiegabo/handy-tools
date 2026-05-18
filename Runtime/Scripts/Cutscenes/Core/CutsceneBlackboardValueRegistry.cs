using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Declares one blackboard value wrapper that can be selected by the
    /// cutscene graph blackboard authoring UI and instantiated by the runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CutsceneBlackboardValueDescriptorAttribute : Attribute
    {
        /// <summary>
        /// Initializes one descriptor attribute for a blackboard wrapper type.
        /// </summary>
        /// <param name="displayName">Label shown in authoring UIs.</param>
        /// <param name="runtimeValueType">Runtime type represented by the wrapper.</param>
        public CutsceneBlackboardValueDescriptorAttribute(
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
        /// Gets or sets whether the wrapper should be hidden from the picker UI.
        /// </summary>
        public bool HiddenFromPicker { get; set; }
    }

    /// <summary>
    /// Discovers and instantiates graph blackboard value wrappers.
    /// </summary>
    public static class CutsceneBlackboardValueRegistry
    {
        /// <summary>
        /// Describes one discovered wrapper type.
        /// </summary>
        public sealed class Descriptor
        {
            internal Descriptor(
                Type wrapperType,
                CutsceneBlackboardValueDescriptorAttribute metadata)
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

            private CutsceneBlackboardValueDescriptorAttribute Metadata { get; }

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

                return SupportsDerivedTypes
                    && RuntimeValueType.IsAssignableFrom(valueType);
            }

            /// <summary>
            /// Creates one fresh wrapper initialized for the requested type.
            /// </summary>
            /// <param name="concreteValueType">Concrete runtime type the wrapper should represent.</param>
            /// <returns>The initialized wrapper instance.</returns>
            public CutsceneGraphBlackboardValue CreateValue(Type concreteValueType)
            {
                CutsceneGraphBlackboardValue value =
                    Activator.CreateInstance(WrapperType)
                    as CutsceneGraphBlackboardValue;

                if (value == null)
                {
                    throw new InvalidOperationException(
                        $"Could not instantiate blackboard value wrapper '{WrapperType.FullName}'.");
                }

                value.InitializeForValueType(concreteValueType ?? RuntimeValueType);
                return value;
            }
        }

        private static IReadOnlyList<Descriptor> s_descriptors;
        private static IReadOnlyDictionary<Type, Descriptor> s_descriptorsByWrapperType;

        /// <summary>
        /// Gets the discovered descriptors sorted for authoring pickers.
        /// </summary>
        public static IReadOnlyList<Descriptor> Descriptors
        {
            get
            {
                EnsureInitialized();
                return s_descriptors;
            }
        }

        /// <summary>
        /// Resolves one descriptor by wrapper type.
        /// </summary>
        /// <param name="wrapperType">Wrapper implementation type.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the descriptor exists.</returns>
        public static bool TryGetDescriptor(
            Type wrapperType,
            out Descriptor descriptor)
        {
            EnsureInitialized();

            if (wrapperType == null)
            {
                descriptor = null;
                return false;
            }

            return s_descriptorsByWrapperType.TryGetValue(wrapperType, out descriptor);
        }

        /// <summary>
        /// Resolves one descriptor by the wrapper instance currently stored in the graph.
        /// </summary>
        /// <param name="value">Stored blackboard value wrapper.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the descriptor exists.</returns>
        public static bool TryGetDescriptor(
            CutsceneGraphBlackboardValue value,
            out Descriptor descriptor)
        {
            descriptor = null;
            return value != null && TryGetDescriptor(value.GetType(), out descriptor);
        }

        /// <summary>
        /// Resolves one descriptor by its authoring display name.
        /// </summary>
        /// <param name="displayName">Display name shown in the picker.</param>
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the descriptor exists.</returns>
        public static bool TryGetDescriptor(
            string displayName,
            out Descriptor descriptor)
        {
            EnsureInitialized();
            descriptor = s_descriptors.FirstOrDefault(
                candidate => string.Equals(
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
        public static bool TryCreateValue(
            Type valueType,
            out CutsceneGraphBlackboardValue value)
        {
            value = null;

            if (!TryGetDescriptorForRuntimeType(valueType, out Descriptor descriptor))
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
        /// <param name="descriptor">Resolved descriptor when available.</param>
        /// <returns>True when the type is supported.</returns>
        public static bool TryGetDescriptorForRuntimeType(
            Type valueType,
            out Descriptor descriptor)
        {
            EnsureInitialized();
            descriptor = null;

            if (valueType == null)
            {
                return false;
            }

            Descriptor exactDescriptor = s_descriptors.FirstOrDefault(
                candidate => !candidate.SupportsDerivedTypes
                    && candidate.RuntimeValueType == valueType);

            if (exactDescriptor != null)
            {
                descriptor = exactDescriptor;
                return true;
            }

            descriptor = s_descriptors
                .Where(candidate => candidate.CanRepresent(valueType))
                .OrderBy(candidate => GetInheritanceDistance(
                    candidate.RuntimeValueType,
                    valueType))
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
        public static string GetDisplayName(CutsceneGraphBlackboardValue value)
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
        /// Ensures the descriptor cache is ready for use.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (s_descriptors != null && s_descriptorsByWrapperType != null)
            {
                return;
            }

            List<Descriptor> descriptors = EnumerateDescriptorTypes()
                .Select(type =>
                {
                    CutsceneBlackboardValueDescriptorAttribute metadata =
                        type.GetCustomAttribute<CutsceneBlackboardValueDescriptorAttribute>();
                    return metadata == null ? null : new Descriptor(type, metadata);
                })
                .Where(descriptor => descriptor != null)
                .OrderBy(descriptor => descriptor.Order)
                .ThenBy(descriptor => descriptor.DisplayName, StringComparer.Ordinal)
                .ToList();

            s_descriptors = descriptors;
            s_descriptorsByWrapperType = descriptors.ToDictionary(
                descriptor => descriptor.WrapperType,
                descriptor => descriptor);
        }

        /// <summary>
        /// Enumerates all non-abstract wrapper types decorated with a descriptor attribute.
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
                        || !typeof(CutsceneGraphBlackboardValue).IsAssignableFrom(type)
                        || type.GetCustomAttribute<CutsceneBlackboardValueDescriptorAttribute>() == null)
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
        private static int GetInheritanceDistance(
            Type descriptorType,
            Type actualType)
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
