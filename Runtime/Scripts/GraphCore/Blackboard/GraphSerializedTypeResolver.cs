using System;
using System.Reflection;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Resolves serialized runtime type names while tolerating stale assembly-qualified names.
    /// </summary>
    public static class GraphSerializedTypeResolver
    {
        /// <summary>
        /// Resolves one serialized type name to the best available loaded runtime type.
        /// </summary>
        /// <param name="serializedTypeName">Serialized type name or assembly-qualified type name.</param>
        /// <param name="fallbackType">Fallback type returned when resolution fails.</param>
        /// <returns>The resolved runtime type when available; otherwise the fallback type.</returns>
        public static Type Resolve(string serializedTypeName, Type fallbackType = null)
        {
            if (string.IsNullOrWhiteSpace(serializedTypeName))
            {
                return fallbackType;
            }

            Type resolvedType = Type.GetType(serializedTypeName, false);

            if (resolvedType != null)
            {
                return resolvedType;
            }

            string trimmedName = serializedTypeName.Trim();
            string typeName = trimmedName;
            string assemblyIdentity = string.Empty;
            int separatorIndex = trimmedName.IndexOf(',');

            if (separatorIndex >= 0)
            {
                typeName = trimmedName.Substring(0, separatorIndex).Trim();
                assemblyIdentity = trimmedName.Substring(separatorIndex + 1).Trim();
            }

            string assemblyShortName = GetAssemblyShortName(assemblyIdentity);
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int index = 0; index < assemblies.Length; index++)
            {
                Assembly assembly = assemblies[index];

                if (assembly == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(assemblyShortName)
                    && !string.Equals(
                        assembly.GetName().Name,
                        assemblyShortName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                resolvedType = assembly.GetType(typeName, false);

                if (resolvedType != null)
                {
                    return resolvedType;
                }
            }

            if (string.IsNullOrWhiteSpace(assemblyShortName))
            {
                return fallbackType;
            }

            for (int index = 0; index < assemblies.Length; index++)
            {
                Assembly assembly = assemblies[index];

                if (assembly == null)
                {
                    continue;
                }

                resolvedType = assembly.GetType(typeName, false);

                if (resolvedType != null)
                {
                    return resolvedType;
                }
            }

            return fallbackType;
        }

        /// <summary>
        /// Extracts the simple assembly name from one serialized assembly identity.
        /// </summary>
        /// <param name="assemblyIdentity">Serialized assembly identity suffix.</param>
        /// <returns>The simple assembly name when available.</returns>
        private static string GetAssemblyShortName(string assemblyIdentity)
        {
            if (string.IsNullOrWhiteSpace(assemblyIdentity))
            {
                return string.Empty;
            }

            try
            {
                return new AssemblyName(assemblyIdentity).Name ?? string.Empty;
            }
            catch (ArgumentException)
            {
                int separatorIndex = assemblyIdentity.IndexOf(',');
                return separatorIndex >= 0
                    ? assemblyIdentity.Substring(0, separatorIndex).Trim()
                    : assemblyIdentity.Trim();
            }
        }
    }
}