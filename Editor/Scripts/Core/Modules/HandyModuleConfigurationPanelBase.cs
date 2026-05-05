using System;
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

        /// <summary>
        /// Gets whether the module exposes a starter setup action.
        /// </summary>
        protected virtual bool SupportsStarterSetup => false;

        /// <summary>
        /// Gets the explanatory text shown above the starter setup action.
        /// </summary>
        protected virtual string StarterSetupDescription =>
            "Create the default project-side assets or files required by this module.";

        /// <summary>
        /// Gets the message shown when the module does not provide a starter
        /// setup action yet.
        /// </summary>
        protected virtual string StarterSetupUnavailableMessage =>
            "Starter setup is not available for this module yet.";

        /// <summary>
        /// Applies the shared spacing used by module help boxes regardless of
        /// whether they are informational, warning, or error states.
        /// </summary>
        /// <param name="helpBox">Help box to style.</param>
        protected static void ApplyInformativeBoxStyle(HelpBox helpBox)
        {
            helpBox.style.paddingLeft = 12f;
            helpBox.style.paddingRight = 12f;
            helpBox.style.paddingTop = 10f;
            helpBox.style.paddingBottom = 10f;
        }

        /// <summary>
        /// Applies the shared container style used for configurable value
        /// controls across module panels.
        /// </summary>
        /// <param name="container">Container to style.</param>
        internal static void ApplyConfigurableValueContainerStyle(VisualElement container)
        {
            container.style.marginBottom = 6f;
            container.style.paddingLeft = 8f;
            container.style.paddingRight = 8f;
            container.style.paddingTop = 6f;
            container.style.paddingBottom = 6f;
            container.style.flexShrink = 0f;
            container.style.backgroundColor = new StyleColor(
                new Color32(0x38, 0x38, 0x38, 0xFF)
            );
            container.style.borderLeftWidth = 1f;
            container.style.borderRightWidth = 1f;
            container.style.borderTopWidth = 1f;
            container.style.borderBottomWidth = 1f;
            container.style.borderLeftColor = new Color(0.30f, 0.30f, 0.30f, 1f);
            container.style.borderRightColor = new Color(0.30f, 0.30f, 0.30f, 1f);
            container.style.borderTopColor = new Color(0.30f, 0.30f, 0.30f, 1f);
            container.style.borderBottomColor = new Color(0.30f, 0.30f, 0.30f, 1f);
            container.style.borderTopLeftRadius = 6f;
            container.style.borderTopRightRadius = 6f;
            container.style.borderBottomLeftRadius = 6f;
            container.style.borderBottomRightRadius = 6f;
        }

        /// <summary>
        /// Wraps a configurable value control inside the shared module-panel
        /// container style.
        /// </summary>
        /// <param name="valueElement">Control element to wrap.</param>
        /// <returns>
        /// The styled wrapper that contains the provided value element.
        /// </returns>
        internal static VisualElement WrapConfigurableValueElement(VisualElement valueElement)
        {
            VisualElement container = new();
            ApplyConfigurableValueContainerStyle(container);

            valueElement.style.marginTop = 0f;
            valueElement.style.marginBottom = 0f;
            container.Add(valueElement);
            return container;
        }

        /// <inheritdoc />
        public VisualElement CreatePanel(HandyModuleEditorContext context)
        {
            VisualElement root = new();
            root.AddToClassList("handy-module-panel");
            ApplyRootStyle(root);

            HandyModuleDependencyGateElement gate = new(Descriptor, () => Dependencies, context);
            gate.style.flexShrink = 0f;
            root.Add(gate);

            Label lockLabel = new();
            lockLabel.AddToClassList("handy-module-panel__lock-state");
            ApplyLockLabelStyle(lockLabel);
            root.Add(lockLabel);

            VisualElement contentContainer = CreateContentContainer();

            VisualElement content = new();
            content.AddToClassList("handy-module-panel__content");
            content.style.flexDirection = FlexDirection.Column;
            content.style.flexShrink = 0f;
            content.SetEnabled(gate.CanEditConfiguration);
            BuildPanel(content, context);
            WrapDirectConfigurableValueElements(content);
            content.Add(CreateStarterSetupSection(context));
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

        /// <summary>
        /// Executes the module starter setup action and returns a user-facing
        /// status message.
        /// </summary>
        /// <param name="context">Shared module editor context.</param>
        /// <returns>
        /// A status message describing the outcome of the action.
        /// </returns>
        protected virtual string RunStarterSetup(HandyModuleEditorContext context)
        {
            return StarterSetupUnavailableMessage;
        }

        private static void WrapDirectConfigurableValueElements(VisualElement root)
        {
            List<VisualElement> configurableElements = new();

            for (int index = 0; index < root.childCount; index++)
            {
                VisualElement child = root[index];
                if (!IsConfigurableValueElement(child))
                {
                    continue;
                }

                configurableElements.Add(child);
            }

            for (int index = 0; index < configurableElements.Count; index++)
            {
                VisualElement child = configurableElements[index];
                int childIndex = root.IndexOf(child);
                if (childIndex < 0)
                {
                    continue;
                }

                root.RemoveAt(childIndex);
                root.Insert(childIndex, WrapConfigurableValueElement(child));
            }
        }

        private static bool IsConfigurableValueElement(VisualElement element)
        {
            return element is IMGUIContainer || InheritsFromGenericBaseField(element.GetType());
        }

        private static bool InheritsFromGenericBaseField(Type type)
        {
            Type currentType = type;

            while (currentType != null)
            {
                if (currentType.IsGenericType
                    && currentType.GetGenericTypeDefinition() == typeof(BaseField<>))
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        private VisualElement CreateStarterSetupSection(HandyModuleEditorContext context)
        {
            VisualElement section = new();
            section.style.flexDirection = FlexDirection.Column;
            section.style.marginTop = 14f;

            Label title = new("Starter Setup");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6f;
            section.Add(title);

            HelpBox description = new(
                SupportsStarterSetup
                    ? StarterSetupDescription
                    : StarterSetupUnavailableMessage,
                SupportsStarterSetup
                    ? HelpBoxMessageType.Info
                    : HelpBoxMessageType.Warning
            );
            ApplyInformativeBoxStyle(description);
            description.style.marginBottom = 8f;
            section.Add(description);

            HelpBox status = new(string.Empty, HelpBoxMessageType.None);
            ApplyInformativeBoxStyle(status);
            status.style.display = DisplayStyle.None;
            section.Add(status);

            Button button = new(() => ExecuteStarterSetup(context))
            {
                text = "Starter Setup",
            };
            button.style.alignSelf = Align.FlexStart;
            button.style.marginBottom = 8f;
            button.SetEnabled(SupportsStarterSetup);
            section.Add(button);

            return section;

            void ExecuteStarterSetup(HandyModuleEditorContext currentContext)
            {
                try
                {
                    string message = RunStarterSetup(currentContext);
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        status.style.display = DisplayStyle.None;
                        return;
                    }

                    status.text = message;
                    status.messageType = HelpBoxMessageType.Info;
                    status.style.display = DisplayStyle.Flex;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    status.text = exception.Message;
                    status.messageType = HelpBoxMessageType.Error;
                    status.style.display = DisplayStyle.Flex;
                }
            }
        }

        private static void ApplyRootStyle(VisualElement root)
        {
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexShrink = 0f;
        }

        private static void ApplyLockLabelStyle(Label lockLabel)
        {
            lockLabel.style.marginTop = 10f;
            lockLabel.style.marginBottom = 2f;
            lockLabel.style.whiteSpace = WhiteSpace.Normal;
            lockLabel.style.flexShrink = 0f;
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
            container.style.flexShrink = 0f;
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