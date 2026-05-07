using IndieGabo.HandyTools.FSMModule;
using IndieGabo.HandyTools.HandyInputSystemModule;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    /// <summary>
    /// Custom inspector for FSMPlayerInputSource.
    /// </summary>
    [CustomEditor(typeof(FSMPlayerInputSource))]
    public sealed class FSMPlayerInputSourceInspector : UnityEditor.Editor
    {
        private static readonly GUIContent[] StrategyOptions =
        {
            new(
                "Single Player",
                "Resolves the PlayerManager from the ServiceLocator and requests its single-player PlayerInput."),
            new(
                "Inspector",
                "Uses the PlayerInput assigned directly on this component."),
            new(
                "Provider",
                "Requires custom project implementation. Another runtime system must call SetPlayerInput. See Assets/HandyTools/Docs/FSMModule/03-FSMBrain-and-Machine-Flow.md, section 'Runtime PlayerInput Injection'.")
        };

        private static readonly GUIContent[] StrategyOptionsWithDisabledSinglePlayer =
        {
            new(
                "Single Player",
                "Requires the Input module to be active. Enable the Input module in the HandyTools/Modules window, Input panel, or in Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset before using the Single Player strategy."),
            StrategyOptions[1],
            StrategyOptions[2]
        };

        private SerializedProperty _playerInputResolutionStrategyProperty;
        private SerializedProperty _playerInputProperty;
        private SerializedProperty _movementInputActionProperty;
        private SerializedProperty _inputActionsProperty;

        /// <summary>
        /// Resolves serialized properties used by the inspector.
        /// </summary>
        private void OnEnable()
        {
            _playerInputResolutionStrategyProperty =
                serializedObject.FindProperty("_playerInputResolutionStrategy");

            _playerInputProperty =
                serializedObject.FindProperty("_playerInput");

            _movementInputActionProperty =
                serializedObject.FindProperty("_movementInputAction");

            _inputActionsProperty =
                serializedObject.FindProperty("_inputActions");
        }

        /// <summary>
        /// Draws the custom inspector UI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            DrawStrategyToolbar();
            DrawStrategySpecificFields();
            DrawResolvedPlayerInput();
            DrawMovementInputField();
            DrawInputActionsField();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the disabled script field.
        /// </summary>
        private void DrawScriptField()
        {
            MonoBehaviour behaviour = target as MonoBehaviour;
            MonoScript scriptAsset = behaviour != null
                ? MonoScript.FromMonoBehaviour(behaviour)
                : null;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    scriptAsset,
                    typeof(MonoScript),
                    false);
            }
        }

        /// <summary>
        /// Draws the toolbar used to select the resolution strategy.
        /// </summary>
        private void DrawStrategyToolbar()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("PlayerInput Strategy", EditorStyles.boldLabel);

            bool isInputModuleActive = InputModuleDefinition.IsActive;
            int currentIndex = _playerInputResolutionStrategyProperty.enumValueIndex;
            int nextIndex = DrawStrategyToolbar(currentIndex, isInputModuleActive);

            if (nextIndex != currentIndex)
            {
                _playerInputResolutionStrategyProperty.enumValueIndex = nextIndex;
            }
        }

        /// <summary>
        /// Draws the strategy toolbar with per-button enablement rules.
        /// </summary>
        /// <param name="currentIndex">Currently selected strategy index.</param>
        /// <param name="isInputModuleActive">
        /// Whether the Input module is currently active.
        /// </param>
        /// <returns>The selected strategy index after drawing.</returns>
        private static int DrawStrategyToolbar(
            int currentIndex,
            bool isInputModuleActive)
        {
            GUIContent[] options = isInputModuleActive
                ? StrategyOptions
                : StrategyOptionsWithDisabledSinglePlayer;

            Rect toolbarRect = EditorGUILayout.GetControlRect(false);
            Rect[] buttonRects = new Rect[options.Length];
            float buttonWidth = toolbarRect.width / options.Length;

            for (int index = 0; index < options.Length; index++)
            {
                buttonRects[index] = new Rect(
                    toolbarRect.x + buttonWidth * index,
                    toolbarRect.y,
                    index == options.Length - 1
                        ? toolbarRect.xMax - (toolbarRect.x + buttonWidth * index)
                        : buttonWidth,
                    toolbarRect.height);
            }

            int nextIndex = currentIndex;

            for (int index = 0; index < options.Length; index++)
            {
                bool isEnabled = index != 0 || isInputModuleActive;

                using (new EditorGUI.DisabledScope(!isEnabled))
                {
                    if (GUI.Toggle(
                            buttonRects[index],
                            currentIndex == index,
                            options[index],
                            EditorStyles.miniButton)
                        && index != currentIndex)
                    {
                        nextIndex = index;
                    }
                }
            }

            return nextIndex;
        }

        /// <summary>
        /// Draws fields that depend on the selected strategy.
        /// </summary>
        private void DrawStrategySpecificFields()
        {
            FSMPlayerInputSource.PlayerInputResolutionStrategy strategy =
                (FSMPlayerInputSource.PlayerInputResolutionStrategy)
                _playerInputResolutionStrategyProperty.enumValueIndex;

            if (strategy == FSMPlayerInputSource.PlayerInputResolutionStrategy.InspectorReference)
            {
                EditorGUILayout.PropertyField(_playerInputProperty);
            }
        }

        /// <summary>
        /// Draws the currently resolved PlayerInput during play mode.
        /// </summary>
        private void DrawResolvedPlayerInput()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            FSMPlayerInputSource source = target as FSMPlayerInputSource;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Resolved PlayerInput",
                    source != null ? source.ResolvedPlayerInput : null,
                    typeof(UnityEngine.InputSystem.PlayerInput),
                    true);
            }
        }

        /// <summary>
        /// Draws the optional semantic movement input action used by the
        /// bound brain when CCPro support is enabled.
        /// </summary>
        private void DrawMovementInputField()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Movement Semantics",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _movementInputActionProperty,
                new GUIContent(
                    "Movement Input Action",
                    "Optional semantic movement input reported to the bound FSMBrain when CCPro movement reference support is active. Assign a Vector2 action here instead of configuring movement input on the brain."));
        }

        /// <summary>
        /// Draws the input action list.
        /// </summary>
        private void DrawInputActionsField()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(
                _inputActionsProperty,
                new GUIContent(
                    "Other Input Actions",
                    "Additional InputActionReference entries reported into the generic FSMBrain input cache. The dedicated Movement Input Action above already reports itself, so it does not need to be repeated here."),
                true);
        }
    }
}