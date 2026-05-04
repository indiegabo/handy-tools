using System.Collections.Generic;
using System.Text;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.Editor.Modules
{
    /// <summary>
    /// Provides editor panels with access to shared module activation state.
    /// </summary>
    public sealed class HandyModuleEditorContext
    {
        /// <summary>
        /// Creates an editor context for module configuration panels.
        /// </summary>
        /// <param name="settings">Project-level module activation settings.</param>
        public HandyModuleEditorContext(HandyModuleSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Gets the project-level module activation settings.
        /// </summary>
        public HandyModuleSettings Settings { get; }

        /// <summary>
        /// Gets whether the provided module descriptor is currently active.
        /// </summary>
        /// <param name="descriptor">Module descriptor to evaluate.</param>
        /// <returns>True when the module is active or mandatory.</returns>
        public bool IsActive(HandyModuleDescriptor descriptor)
        {
            return Settings.IsModuleActive(descriptor);
        }

        /// <summary>
        /// Updates the activation flag for an optional module.
        /// </summary>
        /// <param name="descriptor">Module descriptor to update.</param>
        /// <param name="isActive">Whether the module should be active.</param>
        public void SetActive(HandyModuleDescriptor descriptor, bool isActive)
        {
            if (descriptor.ActivationMode == HandyModuleActivationMode.Required)
            {
                return;
            }

            Settings.SetModuleActive(descriptor.Id, isActive);
        }

        /// <summary>
        /// Evaluates whether a module configuration panel should be locked.
        /// </summary>
        /// <param name="descriptor">Module descriptor to evaluate.</param>
        /// <param name="dependencies">Current dependency statuses.</param>
        /// <returns>The evaluated panel lock state.</returns>
        public HandyModulePanelLockState EvaluateLockState(
            HandyModuleDescriptor descriptor,
            IReadOnlyList<HandyModuleDependencyStatus> dependencies
        )
        {
            if (TryBuildDependencyLockMessage(dependencies, out string dependencyMessage))
            {
                return new HandyModulePanelLockState(
                    true,
                    "Module Locked",
                    dependencyMessage
                );
            }

            if (descriptor.ActivationMode == HandyModuleActivationMode.Optional &&
                !IsActive(descriptor))
            {
                return new HandyModulePanelLockState(
                    true,
                    "Module Locked",
                    "Activate this module to edit its configuration."
                );
            }

            return HandyModulePanelLockState.Unlocked;
        }

        private static bool TryBuildDependencyLockMessage(
            IReadOnlyList<HandyModuleDependencyStatus> dependencies,
            out string message
        )
        {
            message = string.Empty;
            if (dependencies == null || dependencies.Count == 0)
            {
                return false;
            }

            StringBuilder builder = new();
            int missingCount = 0;

            for (int index = 0; index < dependencies.Count; index++)
            {
                HandyModuleDependencyStatus dependency = dependencies[index];
                if (dependency.IsSatisfied)
                {
                    continue;
                }

                if (missingCount > 0)
                {
                    builder.Append(' ');
                }

                string dependencyMessage = string.IsNullOrWhiteSpace(dependency.Message)
                    ? dependency.Dependency.Description
                    : dependency.Message;

                builder.Append($"{dependency.Dependency.DisplayName}: {dependencyMessage}");
                missingCount++;
            }

            if (missingCount == 0)
            {
                return false;
            }

            message = $"Resolve the missing dependencies before editing this module. {builder}";
            return true;
        }
    }
}