using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Modules
{
    /// <summary>
    /// Draws a reusable UI Toolkit dependency and activation gate for modules.
    /// </summary>
    public sealed class HandyModuleDependencyGateElement : VisualElement
    {
        private readonly HandyModuleDescriptor _descriptor;
        private readonly Func<IReadOnlyList<HandyModuleDependencyStatus>> _dependenciesProvider;
        private readonly HandyModuleEditorContext _context;
        private readonly Toggle _activationToggle;
        private readonly VisualElement _dependencyList;
        private readonly Label _statusLabel;

        /// <summary>
        /// Creates a dependency gate for one module descriptor.
        /// </summary>
        /// <param name="descriptor">Module descriptor displayed by the gate.</param>
        /// <param name="dependenciesProvider">Dependency status provider for the module.</param>
        /// <param name="context">Shared module editor context.</param>
        public HandyModuleDependencyGateElement(
            HandyModuleDescriptor descriptor,
            Func<IReadOnlyList<HandyModuleDependencyStatus>> dependenciesProvider,
            HandyModuleEditorContext context
        )
        {
            _descriptor = descriptor;
            _dependenciesProvider = dependenciesProvider;
            _context = context;

            AddToClassList("handy-module-gate");
            ApplyContainerStyle(this);

            Label title = new(descriptor.DisplayName);
            title.AddToClassList("handy-module-gate__title");
            ApplyTitleStyle(title);
            Add(title);

            Label description = new(descriptor.Description);
            description.AddToClassList("handy-module-gate__description");
            ApplyDescriptionStyle(description);
            Add(description);

            _activationToggle = new Toggle("Activate Module?");
            ApplyToggleStyle(_activationToggle);
            _activationToggle.RegisterValueChangedCallback(OnActivationChanged);
            Add(HandyModuleConfigurationPanelBase.WrapConfigurableValueElement(_activationToggle));

            _statusLabel = new Label();
            _statusLabel.AddToClassList("handy-module-gate__status");
            ApplyStatusStyle(_statusLabel);
            Add(_statusLabel);

            _dependencyList = new VisualElement();
            _dependencyList.AddToClassList("handy-module-gate__dependencies");
            ApplyDependencyListStyle(_dependencyList);
            Add(_dependencyList);

            Refresh(false);
        }

        /// <summary>
        /// Occurs when the module configuration area should be enabled or disabled.
        /// </summary>
        public event Action<bool> ConfigurationStateChanged;

        /// <summary>
        /// Occurs when the evaluated panel lock state changes.
        /// </summary>
        public event Action<HandyModulePanelLockState> LockStateChanged;

        /// <summary>
        /// Gets the evaluated lock state for the current module panel.
        /// </summary>
        public HandyModulePanelLockState LockState { get; private set; }

        /// <summary>
        /// Gets whether the module-specific configuration can be edited.
        /// </summary>
        public bool CanEditConfiguration => !LockState.IsLocked;

        /// <summary>
        /// Re-evaluates dependencies, activation state, and the panel lock state.
        /// </summary>
        public void Refresh()
        {
            Refresh(true);
        }

        private bool CanToggleActivation =>
            _descriptor.ActivationMode == HandyModuleActivationMode.Optional &&
            AreDependenciesSatisfied;

        private IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependenciesProvider?.Invoke() ?? Array.Empty<HandyModuleDependencyStatus>();

        private bool AreDependenciesSatisfied
        {
            get
            {
                IReadOnlyList<HandyModuleDependencyStatus> dependencies = Dependencies;
                for (int index = 0; index < dependencies.Count; index++)
                {
                    if (!dependencies[index].IsSatisfied)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void OnActivationChanged(ChangeEvent<bool> changeEvent)
        {
            _context.SetActive(_descriptor, changeEvent.newValue);
            Refresh(true);
        }

        private void Refresh(bool notify)
        {
            _activationToggle.SetValueWithoutNotify(_context.IsActive(_descriptor));
            _activationToggle.SetEnabled(CanToggleActivation);
            _activationToggle.tooltip = ResolveToggleTooltip();

            LockState = _context.EvaluateLockState(_descriptor, Dependencies);
            ApplyStatusMessage();
            RenderDependencies();

            if (!notify)
            {
                return;
            }

            ConfigurationStateChanged?.Invoke(CanEditConfiguration);
            LockStateChanged?.Invoke(LockState);
        }

        private void ApplyStatusMessage()
        {
            if (LockState.IsLocked)
            {
                _statusLabel.text = $"{LockState.Title}: {LockState.Message}";
                _statusLabel.style.display = DisplayStyle.Flex;
                return;
            }

            if (_descriptor.ActivationMode == HandyModuleActivationMode.Required)
            {
                _statusLabel.text = "Required infrastructure modules cannot be disabled.";
                _statusLabel.style.display = DisplayStyle.Flex;
                return;
            }

            _statusLabel.text = string.Empty;
            _statusLabel.style.display = DisplayStyle.None;
        }

        private string ResolveToggleTooltip()
        {
            if (_descriptor.ActivationMode == HandyModuleActivationMode.Required)
            {
                return "Required infrastructure modules cannot be disabled.";
            }

            if (!AreDependenciesSatisfied)
            {
                return "Resolve the missing dependencies before enabling this module.";
            }

            return string.Empty;
        }

        private void RenderDependencies()
        {
            _dependencyList.Clear();

            IReadOnlyList<HandyModuleDependencyStatus> dependencies = Dependencies;
            if (dependencies.Count == 0)
            {
                Label noDependenciesLabel = new("No additional dependencies required.");
                noDependenciesLabel.AddToClassList("handy-module-gate__dependency");
                ApplyDependencyLabelStyle(noDependenciesLabel, true, false);
                _dependencyList.Add(noDependenciesLabel);
                return;
            }

            for (int index = 0; index < dependencies.Count; index++)
            {
                HandyModuleDependencyStatus status = dependencies[index];
                string marker = status.IsSatisfied ? "Available" : "Missing";
                string message = string.IsNullOrWhiteSpace(status.Message)
                    ? status.Dependency.Description
                    : status.Message;

                Label dependencyLabel = new(
                    $"{marker}: {status.Dependency.DisplayName} - {message}"
                );
                dependencyLabel.AddToClassList("handy-module-gate__dependency");
                dependencyLabel.AddToClassList(
                    status.IsSatisfied
                        ? "handy-module-gate__dependency--available"
                        : "handy-module-gate__dependency--missing"
                );
                ApplyDependencyLabelStyle(dependencyLabel, status.IsSatisfied, true);
                _dependencyList.Add(dependencyLabel);
            }
        }

        private static void ApplyContainerStyle(VisualElement container)
        {
            Color borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.28f, 0.28f, 1f)
                : new Color(0.78f, 0.78f, 0.78f, 1f);
            Color backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.17f, 0.17f, 0.95f)
                : new Color(0.98f, 0.98f, 0.98f, 1f);

            container.style.marginBottom = 6f;
            container.style.paddingLeft = 12f;
            container.style.paddingRight = 12f;
            container.style.paddingTop = 12f;
            container.style.paddingBottom = 12f;
            container.style.backgroundColor = backgroundColor;
            container.style.borderLeftWidth = 1f;
            container.style.borderRightWidth = 1f;
            container.style.borderTopWidth = 1f;
            container.style.borderBottomWidth = 1f;
            container.style.borderLeftColor = borderColor;
            container.style.borderRightColor = borderColor;
            container.style.borderTopColor = borderColor;
            container.style.borderBottomColor = borderColor;
            container.style.borderTopLeftRadius = 8f;
            container.style.borderTopRightRadius = 8f;
            container.style.borderBottomLeftRadius = 8f;
            container.style.borderBottomRightRadius = 8f;
        }

        private static void ApplyTitleStyle(Label title)
        {
            title.style.fontSize = 14f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4f;
            title.style.whiteSpace = WhiteSpace.Normal;
        }

        private static void ApplyDescriptionStyle(Label description)
        {
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 10f;
            description.style.opacity = 0.9f;
        }

        private static void ApplyToggleStyle(Toggle activationToggle)
        {
            activationToggle.style.marginBottom = 0f;
        }

        private static void ApplyStatusStyle(Label statusLabel)
        {
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            statusLabel.style.marginBottom = 8f;
        }

        private static void ApplyDependencyListStyle(VisualElement dependencyList)
        {
            dependencyList.style.flexDirection = FlexDirection.Column;
        }

        private static void ApplyDependencyLabelStyle(
            Label dependencyLabel,
            bool isAvailable,
            bool isDependencyStatus
        )
        {
            Color backgroundColor;
            Color borderColor;

            if (!isDependencyStatus)
            {
                backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.21f, 0.21f, 0.21f, 0.9f)
                    : new Color(0.94f, 0.94f, 0.94f, 1f);
                borderColor = EditorGUIUtility.isProSkin
                    ? new Color(0.30f, 0.30f, 0.30f, 1f)
                    : new Color(0.78f, 0.78f, 0.78f, 1f);
            }
            else if (isAvailable)
            {
                backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.18f, 0.24f, 0.18f, 0.95f)
                    : new Color(0.89f, 0.95f, 0.89f, 1f);
                borderColor = EditorGUIUtility.isProSkin
                    ? new Color(0.29f, 0.42f, 0.29f, 1f)
                    : new Color(0.56f, 0.74f, 0.56f, 1f);
            }
            else
            {
                backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.30f, 0.20f, 0.20f, 0.95f)
                    : new Color(0.98f, 0.90f, 0.90f, 1f);
                borderColor = EditorGUIUtility.isProSkin
                    ? new Color(0.52f, 0.32f, 0.32f, 1f)
                    : new Color(0.86f, 0.57f, 0.57f, 1f);
            }

            dependencyLabel.style.whiteSpace = WhiteSpace.Normal;
            dependencyLabel.style.marginTop = 4f;
            dependencyLabel.style.paddingLeft = 8f;
            dependencyLabel.style.paddingRight = 8f;
            dependencyLabel.style.paddingTop = 6f;
            dependencyLabel.style.paddingBottom = 6f;
            dependencyLabel.style.backgroundColor = backgroundColor;
            dependencyLabel.style.borderLeftWidth = 1f;
            dependencyLabel.style.borderRightWidth = 1f;
            dependencyLabel.style.borderTopWidth = 1f;
            dependencyLabel.style.borderBottomWidth = 1f;
            dependencyLabel.style.borderLeftColor = borderColor;
            dependencyLabel.style.borderRightColor = borderColor;
            dependencyLabel.style.borderTopColor = borderColor;
            dependencyLabel.style.borderBottomColor = borderColor;
            dependencyLabel.style.borderTopLeftRadius = 6f;
            dependencyLabel.style.borderTopRightRadius = 6f;
            dependencyLabel.style.borderBottomLeftRadius = 6f;
            dependencyLabel.style.borderBottomRightRadius = 6f;
        }
    }
}