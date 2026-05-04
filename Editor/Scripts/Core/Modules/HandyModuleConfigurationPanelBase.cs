using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Modules
{
    /// <summary>
    /// Base implementation for UI Toolkit module configuration panels.
    /// </summary>
    public abstract class HandyModuleConfigurationPanelBase : IHandyModuleConfigurationPanel
    {
        /// <inheritdoc />
        public abstract HandyModuleDescriptor Descriptor { get; }

        /// <inheritdoc />
        public virtual IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            System.Array.Empty<HandyModuleDependencyStatus>();

        /// <inheritdoc />
        public VisualElement CreatePanel(HandyModuleEditorContext context)
        {
            VisualElement root = new();
            root.AddToClassList("handy-module-panel");
            ApplyRootStyle(root);

            HandyModuleDependencyGateElement gate = new(Descriptor, () => Dependencies, context);
            root.Add(gate);

            Label lockLabel = new();
            lockLabel.AddToClassList("handy-module-panel__lock-state");
            ApplyLockLabelStyle(lockLabel);
            root.Add(lockLabel);

            VisualElement contentContainer = CreateContentContainer();

            VisualElement content = new();
            content.AddToClassList("handy-module-panel__content");
            content.style.flexDirection = FlexDirection.Column;
            content.SetEnabled(gate.CanEditConfiguration);
            BuildPanel(content, context);
            contentContainer.Add(content);
            root.Add(contentContainer);

            ApplyLockState(lockLabel, gate.LockState);
            ApplyContentContainerState(contentContainer, gate.CanEditConfiguration);
            gate.ConfigurationStateChanged += canEditConfiguration =>
            {
                content.SetEnabled(canEditConfiguration);
                ApplyContentContainerState(contentContainer, canEditConfiguration);
            };
            gate.LockStateChanged += state => ApplyLockState(lockLabel, state);
            return root;
        }

        /// <summary>
        /// Builds the module-specific configuration content.
        /// </summary>
        /// <param name="root">Root visual element for module-specific controls.</param>
        /// <param name="context">Shared module editor context.</param>
        protected abstract void BuildPanel(VisualElement root, HandyModuleEditorContext context);

        private static void ApplyRootStyle(VisualElement root)
        {
            root.style.flexDirection = FlexDirection.Column;
        }

        private static void ApplyLockLabelStyle(Label lockLabel)
        {
            lockLabel.style.marginTop = 10f;
            lockLabel.style.marginBottom = 2f;
            lockLabel.style.whiteSpace = WhiteSpace.Normal;
            lockLabel.style.color = EditorGUIUtility.isProSkin
                ? new Color(0.96f, 0.74f, 0.28f)
                : new Color(0.55f, 0.36f, 0.02f);
        }

        private static VisualElement CreateContentContainer()
        {
            VisualElement container = new();

            Color borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.25f, 0.25f, 1f)
                : new Color(0.76f, 0.76f, 0.76f, 1f);
            Color backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.15f, 0.15f, 0.15f, 0.92f)
                : new Color(0.96f, 0.96f, 0.96f, 1f);

            container.style.marginTop = 12f;
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
            return container;
        }

        private static void ApplyContentContainerState(
            VisualElement contentContainer,
            bool canEditConfiguration
        )
        {
            contentContainer.style.opacity = canEditConfiguration ? 1f : 0.7f;
        }

        private static void ApplyLockState(Label lockLabel, HandyModulePanelLockState lockState)
        {
            if (!lockState.IsLocked)
            {
                lockLabel.text = string.Empty;
                lockLabel.style.display = DisplayStyle.None;
                return;
            }

            lockLabel.text = $"{lockState.Title}: {lockState.Message}";
            lockLabel.style.display = DisplayStyle.Flex;
        }
    }
}