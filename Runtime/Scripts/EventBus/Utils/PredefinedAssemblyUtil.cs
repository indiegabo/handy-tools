using System;
using System.Collections.Generic;
using System.Reflection;
namespace IndieGabo.HandyTools.HandyBus
{

    /// <summary>
    /// Utility class for working with predefined assemblies.
    /// </summary>
    public static class PredefinedAssemblyUtil
    {
        /// <summary>
        /// Enum representing the different types of predefined assemblies.
        /// </summary>
        private enum AssemblyType
        {
            /// <summary>
            /// Assembly-CSharp assembly.
            /// </summary>
            AssemblyCSharp,

            /// <summary>
            /// Assembly-CSharp-Editor assembly.
            /// </summary>
            AssemblyCSharpEditor,

            /// <summary>
            /// Assembly-CSharp-Editor-firstpass assembly.
            /// </summary>
            AssemblyCSharpEditorFirstPass,

            /// <summary>
            /// Assembly-CSharp-firstpass assembly.
            /// </summary>
            AssemblyCSharpFirstPass,
        }

        /// <summary>
        /// Get the assembly type based on the assembly name.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <returns>The assembly type, or null if the assembly name is not recognized.</returns>
        private static AssemblyType? GetAssemblyType(string assemblyName)
        {
            return assemblyName switch
            {
                "Assembly-CSharp" => AssemblyType.AssemblyCSharp,
                "Assembly-CSharp-Editor" => AssemblyType.AssemblyCSharpEditor,
                "Assembly-CSharp-Editor-firstpass" => AssemblyType.AssemblyCSharpEditorFirstPass,
                "Assembly-CSharp-firstpass" => AssemblyType.AssemblyCSharpFirstPass,
                _ => null
            };
        }

        /// <summary>
        /// Add types from an assembly to a list of types.
        /// </summary>
        /// <param name="assemblyTypes">The types in the assembly.</param>
        /// <param name="types">The list of types to add the types to.</param>
        /// <param name="interfaceType">The interface type to filter the types by.</param>
        static void AddTypesFromAssembly(Type[] assemblyTypes, Type interfaceType, ICollection<Type> results)
        {
            if (assemblyTypes == null) return;
            for (int i = 0; i < assemblyTypes.Length; i++)
            {
                Type type = assemblyTypes[i];
                if (type != interfaceType && interfaceType.IsAssignableFrom(type))
                {
                    results.Add(type);
                }
            }
        }

        /// <summary>
        /// Get the types in the predefined assemblies that implement a specified interface.
        /// </summary>
        /// <param name="interfaceType">The interface type to filter the types by.</param>
        /// <returns>A list of types that implement the interface.</returns>
        public static List<Type> GetTypes(Type interfaceType)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Dictionary<AssemblyType, Type[]> assemblyTypes = new();
            List<Type> types = new();

            for (int i = 0; i < assemblies.Length; i++)
            {
                AssemblyType? assemblyType = GetAssemblyType(assemblies[i].GetName().Name);
                if (assemblyType != null)
                {
                    assemblyTypes.Add((AssemblyType)assemblyType, assemblies[i].GetTypes());
                }
            }

            assemblyTypes.TryGetValue(AssemblyType.AssemblyCSharp, out var assemblyCSharpTypes);
            AddTypesFromAssembly(assemblyCSharpTypes, interfaceType, types);

            assemblyTypes.TryGetValue(AssemblyType.AssemblyCSharpFirstPass, out var assemblyCSharpFirstPassTypes);
            AddTypesFromAssembly(assemblyCSharpFirstPassTypes, interfaceType, types);

            return types;
        }
    }
}