using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.Editor.CutscenesModule;
using IndieGabo.HandyTools.Editor.CutscenesModule.Validation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Inspectors
{
    [CustomEditor(typeof(CutsceneDirector), true)]
    public sealed class CutsceneDirectorInspector : UnityEditor.Editor
    {
        private const float PrimaryButtonHeight = 36f;
        private const float ActionButtonHeight = 24f;
        private const int RefreshIntervalMilliseconds = 150;

        private SerializedProperty _titleProperty;
        private SerializedProperty _descriptionProperty;
        private SerializedProperty _playPolicyProperty;
        private SerializedProperty _timeModeProperty;
        private SerializedProperty _autoplayOnStartProperty;
        private SerializedProperty _oneShotProperty;
        private SerializedProperty _cancelOnDisableProperty;

        private Toggle _isRunningField;
        private EnumField _runtimeStatusField;
        private TextField _runtimeFailureReasonField;
        private VisualElement _validationSection;
        private VisualElement _validationContent;

        private void OnEnable()
        {
            _titleProperty = serializedObject.FindProperty("_title");
            _descriptionProperty = serializedObject.FindProperty("_description");
            _playPolicyProperty = serializedObject.FindProperty("_playPolicy");
            _timeModeProperty = serializedObject.FindProperty("_timeMode");
            _autoplayOnStartProperty = serializedObject.FindProperty("_autoplayOnStart");
            _oneShotProperty = serializedObject.FindProperty("_oneShot");
            _cancelOnDisableProperty = serializedObject.FindProperty("_cancelOnDisable");
        }

        /// <summary>
        /// Creates the custom inspector UI for one cutscene director.
        /// </summary>
        /// <returns>The root visual element for the inspector.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            CutsceneDirector director = target as CutsceneDirector;

            if (director == null)
            {
                return new VisualElement();
            }

            VisualElement root = new();
            root.style.marginTop = 2f;

            root.Add(CreateScriptReferenceField());
            root.Add(CreateOpenGraphButton(director));
            root.Add(CreateIdentitySection());
            root.Add(CreatePlaybackSection());
            root.Add(CreateRuntimeSection());
            root.Add(CreateActionButtons(director));

            _validationSection = CreateValidationSection();
            root.Add(_validationSection);

            root.Bind(serializedObject);
            RefreshRuntimeSection(director);
            RefreshValidation(director);

            root.schedule.Execute(() =>
            {
                if (target == null)
                {
                    return;
                }

                serializedObject.UpdateIfRequiredOrScript();
                RefreshRuntimeSection(director);
                RefreshValidation(director);
            }).Every(RefreshIntervalMilliseconds);

            return root;
        }

        /// <summary>
        /// Creates the disabled script reference field.
        /// </summary>
        /// <returns>The configured property field.</returns>
        private PropertyField CreateScriptReferenceField()
        {
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            PropertyField field = new(scriptProperty);
            field.SetEnabled(false);
            field.style.marginBottom = 6f;
            return field;
        }

        /// <summary>
        /// Creates the primary button that opens the graph window.
        /// </summary>
        /// <param name="director">Inspected cutscene director.</param>
        /// <returns>The configured action button.</returns>
        private Button CreateOpenGraphButton(CutsceneDirector director)
        {
            Button button = new(() =>
            {
                serializedObject.ApplyModifiedProperties();
                CutsceneGraphWindow.Open(director);
            })
            {
                text = "Open Graph"
            };

            button.style.height = PrimaryButtonHeight;
            button.style.marginBottom = 6f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;

            return button;
        }

        /// <summary>
        /// Creates the identity section for title and description fields.
        /// </summary>
        /// <returns>The configured foldout.</returns>
        private Foldout CreateIdentitySection()
        {
            Foldout foldout = CreateFoldout("Identity");
            foldout.Add(CreatePropertyField(_titleProperty));
            foldout.Add(CreatePropertyField(_descriptionProperty));
            return foldout;
        }

        /// <summary>
        /// Creates the playback configuration section.
        /// </summary>
        /// <returns>The configured foldout.</returns>
        private Foldout CreatePlaybackSection()
        {
            Foldout foldout = CreateFoldout("Playback");
            foldout.Add(CreatePropertyField(_playPolicyProperty));
            foldout.Add(CreatePropertyField(_timeModeProperty));
            foldout.Add(CreatePropertyField(_autoplayOnStartProperty));
            foldout.Add(CreatePropertyField(_oneShotProperty));
            foldout.Add(CreatePropertyField(_cancelOnDisableProperty));
            return foldout;
        }

        /// <summary>
        /// Creates the runtime diagnostics section.
        /// </summary>
        /// <returns>The configured foldout.</returns>
        private Foldout CreateRuntimeSection()
        {
            Foldout foldout = CreateFoldout("Runtime");

            _isRunningField = new Toggle("Is Running");
            _isRunningField.SetEnabled(false);
            _isRunningField.style.marginBottom = 4f;

            _runtimeStatusField = new EnumField("Runtime Status");
            _runtimeStatusField.SetEnabled(false);
            _runtimeStatusField.style.marginBottom = 4f;

            _runtimeFailureReasonField = new TextField("Runtime Failure Reason")
            {
                multiline = true,
            };

            _runtimeFailureReasonField.SetEnabled(false);
            _runtimeFailureReasonField.style.marginBottom = 4f;

            foldout.Add(_isRunningField);
            foldout.Add(_runtimeStatusField);
            foldout.Add(_runtimeFailureReasonField);

            return foldout;
        }

        /// <summary>
        /// Creates the runtime action buttons.
        /// </summary>
        /// <param name="director">Inspected cutscene director.</param>
        /// <returns>The configured button row.</returns>
        private VisualElement CreateActionButtons(CutsceneDirector director)
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6f;

            row.Add(CreateActionButton("Play", director.Play, true));
            row.Add(CreateActionButton("Restart", director.Restart));
            row.Add(CreateActionButton("Cancel", director.Cancel, false));

            return row;
        }

        /// <summary>
        /// Refreshes the runtime diagnostics fields.
        /// </summary>
        /// <param name="director">Inspected cutscene director.</param>
        private void RefreshRuntimeSection(CutsceneDirector director)
        {
            _isRunningField?.SetValueWithoutNotify(director.IsRunning);

            if (_runtimeStatusField != null)
            {
                _runtimeStatusField.Init(director.RuntimeStatus);
                _runtimeStatusField.SetValueWithoutNotify(director.RuntimeStatus);
            }

            _runtimeFailureReasonField?.SetValueWithoutNotify(
                director.RuntimeFailureReason ?? string.Empty);
        }

        /// <summary>
        /// Refreshes the validation summary displayed by the inspector.
        /// </summary>
        /// <param name="director">Inspected cutscene director.</param>
        private void RefreshValidation(CutsceneDirector director)
        {
            if (_validationSection == null || _validationContent == null)
            {
                return;
            }

            IReadOnlyList<CutsceneGraphValidationIssue> issues =
                CutsceneGraphValidator.Validate(director);

            _validationContent.Clear();
            _validationSection.style.display = issues.Count == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            for (int index = 0; index < issues.Count; index++)
            {
                CutsceneGraphValidationIssue issue = issues[index];
                HelpBox helpBox = new(
                    issue.Message,
                    ToHelpBoxMessageType(issue.Severity));
                helpBox.style.marginBottom = 4f;
                _validationContent.Add(helpBox);
            }
        }

        /// <summary>
        /// Creates one standard property field with consistent spacing.
        /// </summary>
        /// <param name="property">Serialized property to bind.</param>
        /// <returns>The configured property field.</returns>
        private static PropertyField CreatePropertyField(SerializedProperty property)
        {
            PropertyField field = new(property);
            field.style.marginBottom = 4f;
            return field;
        }

        /// <summary>
        /// Creates one foldout section with consistent spacing.
        /// </summary>
        /// <param name="title">Section title.</param>
        /// <returns>The configured foldout.</returns>
        private static Foldout CreateFoldout(string title)
        {
            Foldout foldout = new()
            {
                text = title,
                value = true,
            };

            foldout.style.marginBottom = 6f;
            return foldout;
        }

        /// <summary>
        /// Creates one action button bound to one runtime command.
        /// </summary>
        /// <param name="text">Button label.</param>
        /// <param name="action">Command executed on click.</param>
        /// <param name="addRightMargin">Whether to add right spacing.</param>
        /// <returns>The configured button.</returns>
        private Button CreateActionButton(
            string text,
            System.Action action,
            bool addRightMargin = true)
        {
            Button button = new(() =>
            {
                serializedObject.ApplyModifiedProperties();
                action?.Invoke();
            })
            {
                text = text,
            };

            button.style.height = ActionButtonHeight;
            button.style.flexGrow = 1f;

            if (addRightMargin)
            {
                button.style.marginRight = 4f;
            }

            return button;
        }

        /// <summary>
        /// Creates the validation section container.
        /// </summary>
        /// <returns>The configured validation section.</returns>
        private VisualElement CreateValidationSection()
        {
            VisualElement section = new();
            section.style.display = DisplayStyle.None;

            Label header = new("Validation");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 4f;

            _validationContent = new VisualElement();

            section.Add(header);
            section.Add(_validationContent);

            return section;
        }

        /// <summary>
        /// Maps validation severities to UI Toolkit help box types.
        /// </summary>
        /// <param name="severity">Validation severity.</param>
        /// <returns>The UI Toolkit help box type.</returns>
        private static HelpBoxMessageType ToHelpBoxMessageType(
            CutsceneGraphValidationSeverity severity)
        {
            return severity switch
            {
                CutsceneGraphValidationSeverity.Error => HelpBoxMessageType.Error,
                CutsceneGraphValidationSeverity.Warning => HelpBoxMessageType.Warning,
                _ => HelpBoxMessageType.Info,
            };
        }
    }
}