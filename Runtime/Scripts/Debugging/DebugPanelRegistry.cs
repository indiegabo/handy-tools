using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IndieGabo.HandyTools.DebuggingModule
{
    /// <summary>
    /// Resolves which sections are allowed to exist in the debug panel.
    /// The package keeps its core sections, while project-specific sections must
    /// opt in explicitly through <see cref="DebugPanelSectionAttribute"/>.
    /// </summary>
    public static class DebugPanelRegistry
    {
        #region Static Data

        private static readonly HashSet<Type> _coreSections = new()
        {
            typeof(FPSSection),
            typeof(DebugSettingsSection),
        };

        #endregion

        #region Public API

        /// <summary>
        /// Gets all section types that should be instantiated for the current
        /// debug panel session.
        /// </summary>
        /// <returns>Distinct approved section types.</returns>
        public static IReadOnlyList<Type> GetSectionTypes()
        {
            var sectionTypes = new HashSet<Type>(_coreSections);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                TryRegisterAttributedSections(assembly, sectionTypes);
            }

            return sectionTypes.ToList();
        }

        #endregion

        #region Helpers

        private static void TryRegisterAttributedSections(
            Assembly assembly,
            HashSet<Type> sectionTypes
        )
        {
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!IsAttributedSection(type)) continue;

                    sectionTypes.Add(type);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Debug.LogWarning(
                    $"[{nameof(DebugPanelRegistry)}] Failed to inspect assembly '{assembly.FullName}': {ex.Message}"
                );

                foreach (Exception loaderException in ex.LoaderExceptions)
                {
                    Debug.LogException(loaderException);
                }
            }
        }

        private static bool IsAttributedSection(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && typeof(DebugPanelSection).IsAssignableFrom(type)
                && type.GetCustomAttribute<DebugPanelSectionAttribute>() != null;
        }

        #endregion
    }
}