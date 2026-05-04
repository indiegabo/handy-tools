using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.Logger;
using IndieGabo.HandyTools.Modules;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.Logger
{
    /// <summary>
    /// UI Toolkit configuration panel for the Logging module.
    /// </summary>
    public sealed class LoggingModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => LoggingModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override System.Collections.Generic.IReadOnlyList<HandyModuleDependencyStatus>
            Dependencies => LoggingModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            HandyLoggerSetup.ReloadInstance();
            HandyLoggerSetup setup = HandyLoggerSetup.Instance;

            Label intro = new(
                "Configure the default colors used by HandyLogger messages across runtime and editor workflows."
            );
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.marginBottom = 8;
            root.Add(intro);

            root.Add(CreateColorField(
                "Success Color",
                setup.SuccessColor,
                color =>
                {
                    setup.SuccessColor = color;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateColorField(
                "Warning Color",
                setup.WarningColor,
                color =>
                {
                    setup.WarningColor = color;
                    AssetDatabase.SaveAssets();
                }
            ));

            root.Add(CreateColorField(
                "Error Color",
                setup.ErrorColor,
                color =>
                {
                    setup.ErrorColor = color;
                    AssetDatabase.SaveAssets();
                }
            ));

            Button previewButton = new(PrintPreview)
            {
                text = "Print Preview Messages"
            };
            previewButton.style.marginTop = 8;
            root.Add(previewButton);
        }

        private static ColorField CreateColorField(string label, Color value, System.Action<Color> onChanged)
        {
            ColorField field = new(label)
            {
                value = value
            };
            field.style.marginBottom = 6;
            field.RegisterValueChangedCallback(changeEvent => onChanged(changeEvent.newValue));
            return field;
        }

        private static void PrintPreview()
        {
            HandyLogger.Message("Logging", "Standard message preview generated from the module panel.");
            HandyLogger.Success("Logging", "Success message preview generated from the module panel.");
            HandyLogger.Warning("Logging", "Warning message preview generated from the module panel.");
            HandyLogger.Error("Logging", "Error message preview generated from the module panel.");
        }
    }
}