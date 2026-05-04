using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IndieGabo.HandyTools.Debugging
{
    /// <summary>
    /// Bootstraps the runtime debug panel when the current build and project
    /// configuration allow it.
    /// </summary>
    public static class DebugPanelBootstrapper
    {
        private const string PanelResourcePath = "UI/Debug Panel/DebugPanel";
        private const string BootstrappedPanelName = "[HandyTools] Debug Panel";

        /// <summary>
        /// Gets whether the panel may exist in the current runtime target.
        /// </summary>
        internal static bool IsAvailableInCurrentBuild
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return Debug.isDebugBuild;
#endif
            }
        }

        /// <summary>
        /// Creates the runtime debug panel and registers all approved sections.
        /// </summary>
        public static void Bootstrap()
        {
            var config = DebugPanelConfig.Instance;

            if (!IsAvailableInCurrentBuild || !config.IsEnabled) return;

            IReadOnlyList<Type> sectionTypes = DebugPanelRegistry.GetSectionTypes();
            if (sectionTypes.Count == 0) return;

            var existingPanels = UnityEngine.Object.FindObjectsByType<DebugPanel>(
                FindObjectsInactive.Include
            );

            if (existingPanels.Length > 0) return;

            DebugPanel prefab = Resources.Load<DebugPanel>(
                PanelResourcePath
            );

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[{nameof(DebugPanelBootstrapper)}] Debug panel prefab was not found at '{PanelResourcePath}'."
                );
                return;
            }

            var panel = UnityEngine.Object.Instantiate(prefab);
            panel.gameObject.name = BootstrappedPanelName;
            UnityEngine.Object.DontDestroyOnLoad(panel.gameObject);
            panel.Initialize(config);

            InitializeSections(panel, sectionTypes);
        }

        private static void InitializeSections(
            DebugPanel panel,
            IReadOnlyList<Type> sectionTypes
        )
        {
            var sections = new List<(DebugPanelSection section, UnityEngine.UIElements.VisualElement element)>();

            foreach (Type sectionType in sectionTypes)
            {
                try
                {
                    var section = panel.gameObject.AddComponent(sectionType)
                        as DebugPanelSection;

                    if (section == null) continue;

                    section.Initialize(panel);

                    if (!section.IsAvailable)
                    {
                        UnityEngine.Object.Destroy(section);
                        continue;
                    }

                    var element = section.BuildSectionElement();
                    if (element == null)
                    {
                        UnityEngine.Object.Destroy(section);
                        continue;
                    }

                    sections.Add((section, element));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            foreach (var entry in sections.OrderBy(entry => entry.section.OrderInPanel))
            {
                panel.AddSection(entry.element);
            }
        }
    }
}
