using System;
using IndieGabo.HandyTools.FSMModule;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    [CustomEditor(typeof(FSMBrain), true)]
    public class BrainInspector : UnityEditor.Editor
    {
        private static readonly string DocumentName = "BrainUI";
        private const string CCProEnvironmentSourceTypeName =
            "IndieGabo.HandyTools.FSMModule.CCPro.CCProEnvironmentSource";

        [SerializeField]
        private MonoScript _scriptAsset;

        private FSMBrain _brain;
        private VisualElement _containerMain;
        private VisualElement _inputDiagnosticsContent;
        private Label _statusText;
        private EnumField _statusField;
        private ObjectField _fieldOwner;
        private Button _openVisualizerButton;

        private readonly System.Collections.Generic.List<FSMInputSnapshot>
            _inputSnapshots = new();

        public FSMBrain Brain => _brain;

        /// <summary>
        /// Creates the custom inspector UI for an FSM brain.
        /// </summary>
        /// <returns>The root visual element for the inspector.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            _brain = target as FSMBrain;

            _containerMain = Resources.Load<VisualTreeAsset>($"UI Toolkit/FSM/UI Documents/{DocumentName}").Instantiate();

            ObjectField scriptField = _containerMain.Query<ObjectField>("script-field");
            scriptField.SetEnabled(false);
            scriptField.value = MonoScript.FromMonoBehaviour(target as MonoBehaviour);

            _statusField = _containerMain.Q<EnumField>("status-field");
            _statusField.RegisterValueChangedCallback(OnStatusChanged);

            _statusText = _containerMain.Q<Label>("status-text");
            SetStatusText(_brain.Status);

            _fieldOwner = _containerMain.Q<ObjectField>("field-owner");
            _fieldOwner.objectType = typeof(Transform);

            Label generalLabel = _containerMain.Q<Label>("label-general");

            if (generalLabel != null)
            {
                _containerMain.Insert(
                    _containerMain.IndexOf(generalLabel),
                    CreateInputDiagnosticsCard());
            }

            Label transitionsLabel = _containerMain.Q<Label>("label-transitions");

            if (transitionsLabel != null)
            {
                int insertIndex = _containerMain.IndexOf(transitionsLabel);

                _containerMain.Insert(
                    insertIndex++,
                    CreateObjectField("Animator", "_animator", typeof(Animator)));
                _containerMain.Insert(insertIndex, CreateInputSection());
            }

            Label debugLabel = _containerMain.Q<Label>("label-debug");

            if (debugLabel != null)
            {
                _containerMain.Insert(
                    _containerMain.IndexOf(debugLabel),
                    CreateThirdPartySection());
            }

            _openVisualizerButton = new Button(OpenVisualizer)
            {
                text = "Open State Visualizer"
            };

            _openVisualizerButton.AddToClassList("primary");
            _openVisualizerButton.style.marginTop = 8f;
            _openVisualizerButton.style.paddingLeft = 10f;
            _openVisualizerButton.style.paddingRight = 10f;
            _openVisualizerButton.style.paddingTop = 4f;
            _openVisualizerButton.style.paddingBottom = 4f;

            _containerMain.Add(_openVisualizerButton);

            return _containerMain;
        }

        /// <summary>
        /// Creates a bound object field for the custom inspector.
        /// </summary>
        /// <param name="label">The field label to show in the inspector.</param>
        /// <param name="bindingPath">The serialized property binding path.</param>
        /// <param name="objectType">The allowed object type.</param>
        /// <returns>The configured object field.</returns>
        private ObjectField CreateObjectField(
            string label,
            string bindingPath,
            Type objectType)
        {
            ObjectField field = new(label)
            {
                objectType = objectType ?? typeof(UnityEngine.Object)
            };

            SerializedProperty property = serializedObject.FindProperty(bindingPath);

            if (property != null)
            {
                field.BindProperty(property);
            }

            field.style.marginBottom = 4f;

            return field;
        }

        /// <summary>
        /// Creates the inspector section that reports missing third-party
        /// integrations and nests configuration fields under their owning
        /// toggles.
        /// </summary>
        /// <returns>The configured third-party section.</returns>
        private VisualElement CreateThirdPartySection()
        {
            VisualElement section = new();

            Label header = new("Third Party");
            header.AddToClassList("separation-label");
            section.Add(header);

            if (!FSMBrain.IsSimpleBlackboardAvailable)
            {
                section.Add(CreateSimpleBlackboardInstallPrompt());
            }
            else
            {
                SerializedProperty useSimpleBlackboardProperty =
                    serializedObject.FindProperty("_useSimpleBlackboard");
                Toggle useSimpleBlackboardToggle = CreateToggleField(
                    useSimpleBlackboardProperty,
                    "Use Simple Blackboard?");
                VisualElement blackboardFields = CreateNestedFieldGroup();

                blackboardFields.Add(CreateObjectField(
                    "Blackboard Container",
                    "_blackboardContainer",
                    FSMBrain.SimpleBlackboardContainerType ?? typeof(Component)));

                if (useSimpleBlackboardToggle != null)
                {
                    section.Add(useSimpleBlackboardToggle);
                    section.Add(blackboardFields);
                    BindChildSectionVisibility(
                        useSimpleBlackboardToggle,
                        blackboardFields,
                        useSimpleBlackboardProperty != null
                            && useSimpleBlackboardProperty.boolValue);
                }
            }

            if (!FSMBrain.IsCharacterControllerProAvailable)
            {
                section.Add(CreateMissingIntegrationStatusBox(
                    "Character Controller Pro",
                    "Install Character Controller Pro to compile CCPro state bases and expose the FSMBrain CCPro fields."
                ));
            }
            else
            {
                SerializedProperty useCharacterControllerProProperty =
                    serializedObject.FindProperty("_useCharacterControllerPro");
                Toggle useCharacterControllerProToggle = CreateToggleField(
                    useCharacterControllerProProperty,
                    "Use Character Controller Pro?");
                Button setupCharacterControllerProButton =
                    CreateSetupCharacterControllerProButton();
                VisualElement characterControllerProContent = new();
                VisualElement characterControllerProFields = CreateNestedFieldGroup();

                characterControllerProFields.Add(CreateObjectField(
                    "Character Actor",
                    "_characterActor",
                    FSMBrain.CharacterActorType ?? typeof(Component)));

                PropertyField movementReferenceField = CreatePropertyField(
                    "_movementReferenceMode",
                    "Movement Reference");
                SerializedProperty movementReferenceProperty =
                    serializedObject.FindProperty("_movementReferenceMode");
                HelpBox externalReferenceInjectionHelpBox =
                    CreateExternalReferenceInjectionHelpBox();

                if (movementReferenceField != null)
                {
                    characterControllerProFields.Add(movementReferenceField);
                    characterControllerProFields.Add(
                        externalReferenceInjectionHelpBox);
                    BindExternalReferenceInjectionNoticeVisibility(
                        movementReferenceField,
                        movementReferenceProperty,
                        externalReferenceInjectionHelpBox);
                }

                if (useCharacterControllerProToggle != null)
                {
                    characterControllerProContent.Add(
                        setupCharacterControllerProButton);
                    characterControllerProContent.Add(
                        characterControllerProFields);

                    section.Add(useCharacterControllerProToggle);
                    section.Add(characterControllerProContent);
                    BindChildSectionVisibility(
                        useCharacterControllerProToggle,
                        characterControllerProContent,
                        useCharacterControllerProProperty != null
                            && useCharacterControllerProProperty.boolValue);
                }
            }

            return section;
        }

        /// <summary>
        /// Creates the action button that configures the core CCPro FSM
        /// composition on the inspected brain branch.
        /// </summary>
        /// <returns>The configured setup button.</returns>
        private Button CreateSetupCharacterControllerProButton()
        {
            Button button = new(SetupCharacterControllerProComposition)
            {
                text = "Setup CCPro FSM"
            };

            button.style.alignSelf = Align.FlexStart;
            button.style.marginLeft = 18f;
            button.style.marginTop = 2f;
            button.style.marginBottom = 6f;

            return button;
        }

        /// <summary>
        /// Ensures the HandyTools-owned components required by the CCPro FSM
        /// workflow exist on every inspected brain.
        /// </summary>
        private void SetupCharacterControllerProComposition()
        {
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup CCPro FSM");

            int configuredBrains = 0;
            int addedComponents = 0;
            int assignedReferences = 0;

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] is not FSMBrain inspectedBrain)
                {
                    continue;
                }

                SetupCharacterControllerProComposition(
                    inspectedBrain,
                    ref configuredBrains,
                    ref addedComponents,
                    ref assignedReferences);
            }

            Undo.CollapseUndoOperations(undoGroup);
            serializedObject.Update();
            Repaint();

            if (configuredBrains > 0)
            {
                Debug.Log(
                    $"[HandyTools FSM] CCPro setup processed {configuredBrains} brain(s), added {addedComponents} component(s), and assigned {assignedReferences} reference(s).");
            }
        }

        /// <summary>
        /// Ensures one brain owns the minimum editor-side CCPro setup.
        /// </summary>
        /// <param name="brain">The brain to configure.</param>
        /// <param name="configuredBrains">Running count of configured brains.</param>
        /// <param name="addedComponents">Running count of added components.</param>
        /// <param name="assignedReferences">Running count of assigned references.</param>
        private static void SetupCharacterControllerProComposition(
            FSMBrain brain,
            ref int configuredBrains,
            ref int addedComponents,
            ref int assignedReferences)
        {
            if (brain == null)
            {
                return;
            }

            SerializedObject brainSerializedObject = new(brain);
            brainSerializedObject.Update();

            SerializedProperty useCharacterControllerProProperty =
                brainSerializedObject.FindProperty("_useCharacterControllerPro");
            SerializedProperty inputSourceProperty =
                brainSerializedObject.FindProperty("_inputSource");
            SerializedProperty characterActorProperty =
                brainSerializedObject.FindProperty("_characterActor");

            bool wasChanged = false;

            if (useCharacterControllerProProperty != null
                && !useCharacterControllerProProperty.boolValue)
            {
                useCharacterControllerProProperty.boolValue = true;
                wasChanged = true;
            }

            FSMPlayerInputSource inputSource = EnsureComponent<FSMPlayerInputSource>(
                brain.gameObject,
                ref addedComponents,
                ref wasChanged);

            EnsureOptionalComponent(
                brain.gameObject,
                ResolveRuntimeType(CCProEnvironmentSourceTypeName),
                ref addedComponents,
                ref wasChanged);

            EnsureComponent<FSMStatsRegistry>(
                brain.gameObject,
                ref addedComponents,
                ref wasChanged);

            if (inputSourceProperty != null
                && inputSourceProperty.objectReferenceValue == null
                && inputSource != null)
            {
                inputSourceProperty.objectReferenceValue = inputSource;
                assignedReferences++;
                wasChanged = true;
            }

            if (characterActorProperty != null
                && characterActorProperty.objectReferenceValue == null)
            {
                Component characterActor = ResolveExistingCharacterActor(brain);

                if (characterActor != null)
                {
                    characterActorProperty.objectReferenceValue = characterActor;
                    assignedReferences++;
                    wasChanged = true;
                }
                else
                {
                    Debug.LogWarning(
                        "[HandyTools FSM] Setup CCPro FSM could not resolve an existing CharacterActor for the selected brain.",
                        brain);
                }
            }

            if (!wasChanged)
            {
                return;
            }

            Undo.RecordObject(brain, "Setup CCPro FSM");
            brainSerializedObject.ApplyModifiedProperties();
            MarkUnityObjectDirty(brain);
            configuredBrains++;
        }

        /// <summary>
        /// Resolves the existing CharacterActor in the owned hierarchy without
        /// creating runtime Character Controller Pro components.
        /// </summary>
        /// <param name="brain">The inspected brain.</param>
        /// <returns>The resolved CharacterActor component, or null.</returns>
        private static Component ResolveExistingCharacterActor(
            FSMBrain brain)
        {
            Type characterActorType = FSMBrain.CharacterActorType;

            if (brain == null || characterActorType == null)
            {
                return null;
            }

            Component characterActor =
                FindFirstComponentInParents(brain.transform, characterActorType)
                ?? FindFirstComponentInChildren(brain.transform, characterActorType)
                ?? FindFirstComponentOnTransform(brain.Owner, characterActorType);

            if (characterActor != null)
            {
                return characterActor;
            }

            return null;
        }

        /// <summary>
        /// Ensures one required component exists on the provided GameObject.
        /// </summary>
        /// <typeparam name="T">The component type to resolve.</typeparam>
        /// <param name="gameObject">The GameObject that should own the component.</param>
        /// <param name="addedComponents">Running count of added components.</param>
        /// <param name="wasChanged">Whether the setup modified the scene.</param>
        /// <returns>The resolved or newly added component.</returns>
        private static T EnsureComponent<T>(
            GameObject gameObject,
            ref int addedComponents,
            ref bool wasChanged) where T : Component
        {
            if (gameObject == null)
            {
                return null;
            }

            T existingComponent = gameObject.GetComponent<T>();

            if (existingComponent != null)
            {
                return existingComponent;
            }

            T addedComponent = Undo.AddComponent<T>(gameObject);
            addedComponents++;
            wasChanged = true;
            MarkUnityObjectDirty(addedComponent);
            return addedComponent;
        }

        /// <summary>
        /// Ensures one optional runtime-resolved component exists on the
        /// provided GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject that should own the component.</param>
        /// <param name="componentType">The component type to resolve.</param>
        /// <param name="addedComponents">Running count of added components.</param>
        /// <param name="wasChanged">Whether the setup modified the scene.</param>
        /// <returns>The resolved or newly added component.</returns>
        private static Component EnsureOptionalComponent(
            GameObject gameObject,
            Type componentType,
            ref int addedComponents,
            ref bool wasChanged)
        {
            if (gameObject == null
                || componentType == null
                || !typeof(Component).IsAssignableFrom(componentType))
            {
                return null;
            }

            Component existingComponent = gameObject.GetComponent(componentType);

            if (existingComponent != null)
            {
                return existingComponent;
            }

            Component addedComponent = Undo.AddComponent(gameObject, componentType);

            if (addedComponent == null)
            {
                return null;
            }

            addedComponents++;
            wasChanged = true;
            MarkUnityObjectDirty(addedComponent);
            return addedComponent;
        }

        /// <summary>
        /// Resolves the first component of one type on the provided transform.
        /// </summary>
        /// <param name="transform">The transform to inspect.</param>
        /// <param name="componentType">The component type to resolve.</param>
        /// <returns>The first matching component, or null when missing.</returns>
        private static Component FindFirstComponentOnTransform(
            Transform transform,
            Type componentType)
        {
            return transform != null && componentType != null
                ? transform.GetComponent(componentType)
                : null;
        }

        /// <summary>
        /// Resolves the first matching component on the transform parents.
        /// </summary>
        /// <param name="transform">The transform whose parents should be inspected.</param>
        /// <param name="componentType">The component type to resolve.</param>
        /// <returns>The first matching component, or null when missing.</returns>
        private static Component FindFirstComponentInParents(
            Transform transform,
            Type componentType)
        {
            if (transform == null || componentType == null)
            {
                return null;
            }

            Component[] components = transform.GetComponentsInParent(
                componentType,
                true);

            return components != null && components.Length > 0
                ? components[0]
                : null;
        }

        /// <summary>
        /// Resolves the first matching component on the transform children.
        /// </summary>
        /// <param name="transform">The transform whose children should be inspected.</param>
        /// <param name="componentType">The component type to resolve.</param>
        /// <returns>The first matching component, or null when missing.</returns>
        private static Component FindFirstComponentInChildren(
            Transform transform,
            Type componentType)
        {
            if (transform == null || componentType == null)
            {
                return null;
            }

            Component[] components = transform.GetComponentsInChildren(
                componentType,
                true);

            return components != null && components.Length > 0
                ? components[0]
                : null;
        }

        /// <summary>
        /// Resolves one runtime type by scanning the loaded application
        /// domain.
        /// </summary>
        /// <param name="fullTypeName">The full type name to resolve.</param>
        /// <returns>The resolved type, or null when it cannot be found.</returns>
        private static Type ResolveRuntimeType(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            Type directType = Type.GetType(fullTypeName);

            if (directType != null)
            {
                return directType;
            }

            System.Reflection.Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (int index = 0; index < assemblies.Length; index++)
            {
                Type resolvedType = assemblies[index].GetType(fullTypeName);

                if (resolvedType != null)
                {
                    return resolvedType;
                }
            }

            return null;
        }

        /// <summary>
        /// Marks one edited Unity object as dirty and records prefab-instance
        /// property modifications when applicable.
        /// </summary>
        /// <param name="unityObject">The object that changed.</param>
        private static void MarkUnityObjectDirty(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            EditorUtility.SetDirty(unityObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(unityObject);
        }

        /// <summary>
        /// Creates the editor-only input section.
        /// </summary>
        /// <returns>The configured input section.</returns>
        private VisualElement CreateInputSection()
        {
            VisualElement section = new();

            Label header = new("Input");
            header.AddToClassList("separation-label");
            section.Add(header);

            section.Add(CreateObjectField(
                "Input Source",
                "_inputSource",
                typeof(FSMInputSource)));

            return section;
        }

        /// <summary>
        /// Creates the editor-only current input diagnostics card.
        /// </summary>
        /// <returns>The configured diagnostics card.</returns>
        private VisualElement CreateInputDiagnosticsCard()
        {
            VisualElement cardWrapper = new();
            cardWrapper.AddToClassList("row");

            VisualElement card = new();
            card.style.alignItems = Align.Stretch;
            card.style.backgroundColor = new StyleColor(
                new Color32(46, 46, 46, 255));
            card.style.borderTopLeftRadius = 5f;
            card.style.borderTopRightRadius = 5f;
            card.style.borderBottomRightRadius = 5f;
            card.style.borderBottomLeftRadius = 5f;
            card.style.paddingTop = 5f;
            card.style.paddingRight = 5f;
            card.style.paddingBottom = 5f;
            card.style.paddingLeft = 5f;

            _inputDiagnosticsContent = new VisualElement();
            card.Add(_inputDiagnosticsContent);
            cardWrapper.Add(card);

            cardWrapper.schedule.Execute(RefreshInputDiagnostics).Every(125);
            RefreshInputDiagnostics();

            return cardWrapper;
        }

        /// <summary>
        /// Creates the optional-install prompt for the Simple Blackboard
        /// integration.
        /// </summary>
        /// <returns>The configured install prompt.</returns>
        private static VisualElement CreateSimpleBlackboardInstallPrompt()
        {
            VisualElement prompt = new();
            prompt.style.marginBottom = 6f;

            HelpBox helpBox = new(
                "Simple Blackboard is an optional integration. Install it in this project if you want blackboard-backed FSM workflows.",
                HelpBoxMessageType.Info);
            helpBox.style.marginBottom = 4f;
            prompt.Add(helpBox);

            Button installButton = new(SimpleBlackboardPackageInstaller.Install)
            {
                text = "Install Simple Blackboard"
            };

            installButton.style.alignSelf = Align.FlexStart;
            installButton.style.marginLeft = 4f;
            installButton.style.marginBottom = 2f;

            SimpleBlackboardPackageInstaller.RegisterButton(installButton);
            prompt.RegisterCallback<DetachFromPanelEvent>(
                _ => SimpleBlackboardPackageInstaller.UnregisterButton(installButton));

            prompt.Add(installButton);
            return prompt;
        }

        /// <summary>
        /// Creates a bound toggle field for one third-party integration flag.
        /// </summary>
        /// <param name="property">Serialized toggle property.</param>
        /// <param name="label">The field label to show in the inspector.</param>
        /// <returns>The configured toggle, or null when the property is missing.</returns>
        private static Toggle CreateToggleField(SerializedProperty property, string label)
        {
            if (property == null)
            {
                return null;
            }

            Toggle toggle = new(label);
            toggle.BindProperty(property);
            toggle.style.marginBottom = 2f;
            return toggle;
        }

        /// <summary>
        /// Creates the nested container used to visually present third-party
        /// configuration fields as a child subsection of their toggle.
        /// </summary>
        /// <returns>The configured nested field group.</returns>
        private static VisualElement CreateNestedFieldGroup()
        {
            VisualElement group = new();
            group.style.marginLeft = 18f;
            group.style.marginTop = 2f;
            group.style.marginBottom = 8f;
            group.style.paddingLeft = 10f;
            group.style.paddingTop = 4f;
            group.style.paddingBottom = 2f;
            group.style.borderLeftWidth = 2f;
            group.style.borderLeftColor = new Color(0.25f, 0.25f, 0.25f);
            return group;
        }

        /// <summary>
        /// Keeps one nested configuration subsection synchronized with its
        /// owning toggle state.
        /// </summary>
        /// <param name="toggle">Owning toggle.</param>
        /// <param name="childSection">Nested child section.</param>
        /// <param name="isVisible">Current visibility state.</param>
        private static void BindChildSectionVisibility(
            Toggle toggle,
            VisualElement childSection,
            bool isVisible)
        {
            if (toggle == null || childSection == null)
            {
                return;
            }

            SetChildSectionVisibility(childSection, isVisible);
            toggle.RegisterValueChangedCallback(
                evt => SetChildSectionVisibility(childSection, evt.newValue));
        }

        /// <summary>
        /// Applies the visible or collapsed state to one nested subsection.
        /// </summary>
        /// <param name="childSection">Nested child section.</param>
        /// <param name="isVisible">Whether the child section should be visible.</param>
        private static void SetChildSectionVisibility(
            VisualElement childSection,
            bool isVisible)
        {
            childSection.style.display = isVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        /// <summary>
        /// Keeps the external-reference warning synchronized with the selected
        /// Character Controller Pro movement reference mode.
        /// </summary>
        /// <param name="movementReferenceField">Owning movement reference field.</param>
        /// <param name="movementReferenceProperty">Serialized movement reference property.</param>
        /// <param name="helpBox">Warning box displayed for external mode.</param>
        private static void BindExternalReferenceInjectionNoticeVisibility(
            PropertyField movementReferenceField,
            SerializedProperty movementReferenceProperty,
            HelpBox helpBox)
        {
            if (movementReferenceField == null
                || movementReferenceProperty == null
                || helpBox == null)
            {
                return;
            }

            SetChildSectionVisibility(
                helpBox,
                IsExternalMovementReferenceSelected(movementReferenceProperty));

            movementReferenceField.RegisterCallback<SerializedPropertyChangeEvent>(
                _ => SetChildSectionVisibility(
                    helpBox,
                    IsExternalMovementReferenceSelected(movementReferenceProperty)));
        }

        /// <summary>
        /// Gets whether the movement reference property is currently set to external mode.
        /// </summary>
        /// <param name="movementReferenceProperty">Serialized movement reference property.</param>
        /// <returns>True when the external reference mode is selected.</returns>
        private static bool IsExternalMovementReferenceSelected(
            SerializedProperty movementReferenceProperty)
        {
            return movementReferenceProperty != null
                && movementReferenceProperty.enumValueIndex
                    == (int)CharacterControllerProMovementReferenceMode.External;
        }

        /// <summary>
        /// Creates a bound property field for the custom inspector.
        /// </summary>
        /// <param name="bindingPath">The serialized property binding path.</param>
        /// <param name="label">The field label to show in the inspector.</param>
        /// <returns>The configured property field, or null when the property is missing.</returns>
        private PropertyField CreatePropertyField(string bindingPath, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(bindingPath);

            if (property == null)
            {
                return null;
            }

            PropertyField field = new(property, label);
            field.style.marginBottom = 4f;
            return field;
        }

        /// <summary>
        /// Creates a warning help box for one missing third-party integration.
        /// </summary>
        /// <param name="integrationName">Display name of the integration.</param>
        /// <param name="missingMessage">Message shown when the integration is missing.</param>
        /// <returns>The configured help box.</returns>
        private static HelpBox CreateMissingIntegrationStatusBox(
            string integrationName,
            string missingMessage)
        {
            HelpBox helpBox = new(
                $"Missing: {integrationName}. {missingMessage}",
                HelpBoxMessageType.Warning);

            helpBox.style.marginBottom = 6f;
            return helpBox;
        }

        /// <summary>
        /// Creates the warning shown when external movement reference mode
        /// requires runtime injection.
        /// </summary>
        /// <returns>The configured warning box.</returns>
        private static HelpBox CreateExternalReferenceInjectionHelpBox()
        {
            HelpBox helpBox = new(
                "External movement reference must be injected manually at runtime. Set FSMBrain.CCPro.ExternalReference from your composition code after selecting External mode, for example: brain.CCPro.ExternalReference = cameraTransform;",
                HelpBoxMessageType.Warning);

            helpBox.style.marginBottom = 4f;
            return helpBox;
        }

        /// <summary>
        /// Installs the public Simple Blackboard package through the Unity
        /// Package Manager and keeps inspector install buttons synchronized with
        /// the request state.
        /// </summary>
        private static class SimpleBlackboardPackageInstaller
        {
            private const string _packageUrl =
                "https://github.com/ZorPastaman/Simple-Blackboard.git";

            private static readonly System.Collections.Generic.List<Button> s_buttons =
                new();

            private static AddRequest s_installRequest;

            /// <summary>
            /// Starts the package installation if no other installation is in progress.
            /// </summary>
            public static void Install()
            {
                if (IsInstalling)
                {
                    EditorUtility.DisplayDialog(
                        "Simple Blackboard",
                        "Simple Blackboard installation is already in progress.",
                        "OK");
                    return;
                }

                s_installRequest = Client.Add(_packageUrl);
                RefreshButtons();
                EditorApplication.update += TrackInstallProgress;
            }

            /// <summary>
            /// Registers one install button so its text and enabled state follow
            /// the current installation request.
            /// </summary>
            /// <param name="button">Button to register.</param>
            public static void RegisterButton(Button button)
            {
                if (button == null || s_buttons.Contains(button))
                {
                    return;
                }

                s_buttons.Add(button);
                ApplyButtonState(button);
            }

            /// <summary>
            /// Removes one install button from request-state synchronization.
            /// </summary>
            /// <param name="button">Button to unregister.</param>
            public static void UnregisterButton(Button button)
            {
                if (button == null)
                {
                    return;
                }

                s_buttons.Remove(button);
            }

            /// <summary>
            /// Gets whether the package installation request is still running.
            /// </summary>
            private static bool IsInstalling =>
                s_installRequest != null && !s_installRequest.IsCompleted;

            /// <summary>
            /// Monitors the package installation request and reports the final result.
            /// </summary>
            private static void TrackInstallProgress()
            {
                if (s_installRequest == null || !s_installRequest.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= TrackInstallProgress;

                if (s_installRequest.Status == StatusCode.Success)
                {
                    Debug.Log(
                        "[HandyTools FSM] Simple Blackboard installation completed from the public Git repository.");
                }
                else if (s_installRequest.Status >= StatusCode.Failure)
                {
                    string errorMessage = s_installRequest.Error?.message
                        ?? "Unknown package manager error.";
                    Debug.LogError(
                        $"[HandyTools FSM] Failed to install Simple Blackboard. {errorMessage}");
                    EditorUtility.DisplayDialog(
                        "Simple Blackboard",
                        $"Failed to install Simple Blackboard.\n\n{errorMessage}",
                        "OK");
                }

                s_installRequest = null;
                RefreshButtons();
            }

            /// <summary>
            /// Applies the current request state to all tracked install buttons.
            /// </summary>
            private static void RefreshButtons()
            {
                for (int index = s_buttons.Count - 1; index >= 0; index--)
                {
                    Button button = s_buttons[index];

                    if (button == null)
                    {
                        s_buttons.RemoveAt(index);
                        continue;
                    }

                    ApplyButtonState(button);
                }
            }

            /// <summary>
            /// Updates one install button according to the current request state.
            /// </summary>
            /// <param name="button">Button to update.</param>
            private static void ApplyButtonState(Button button)
            {
                bool isInstalling = IsInstalling;
                button.text = isInstalling
                    ? "Installing Simple Blackboard..."
                    : "Install Simple Blackboard";
                button.SetEnabled(!isInstalling);
            }
        }

        /// <summary>
        /// Marks the inspected brain as dirty inside the editor.
        /// </summary>
        /// <param name="persist">Whether the change should be saved immediately.</param>
        public void MarkAsDirty(bool persist = false)
        {
            EditorUtility.SetDirty(_brain);

            if (persist)
                AssetDatabase.SaveAssetIfDirty(_brain);
        }

        /// <summary>
        /// Opens the state visualizer window focused on the current brain.
        /// </summary>
        private void OpenVisualizer()
        {
            MachineStateVisualizerWindow window =
                MachineStateVisualizerWindow.OpenEditorWindow();

            EditorApplication.delayCall += () =>
            {
                if (window == null || _brain == null)
                    return;

                window.SetMachine(_brain);
                window.MachineSelectorField.value = _brain;
            };
        }

        /// <summary>
        /// Rebuilds the input diagnostics card using the brain runtime cache.
        /// </summary>
        private void RefreshInputDiagnostics()
        {
            if (_inputDiagnosticsContent == null)
            {
                return;
            }

            _inputDiagnosticsContent.Clear();

            if (_brain == null)
            {
                AddInputInfoRow("Current Inputs", "Unavailable");
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                AddInputInfoRow("Current Inputs", "Play Mode only");
                return;
            }

            if (_brain.Input.Source == null)
            {
                AddInputInfoRow("Current Inputs", "No source assigned");
                return;
            }

            _brain.Input.CopySnapshots(_inputSnapshots);

            if (_inputSnapshots.Count == 0)
            {
                AddInputInfoRow("Current Inputs", "No cached values");
                return;
            }

            for (int index = 0; index < _inputSnapshots.Count; index++)
            {
                FSMInputSnapshot snapshot = _inputSnapshots[index];
                string valueClass = snapshot.ValueKind == FSMInputValueKind.Button
                    ? snapshot.ButtonValue ? "on" : "off"
                    : "state-name";

                AddInputInfoRow(
                    snapshot.EffectiveDisplayName,
                    snapshot.FormattedValue,
                    valueClass);
            }
        }

        /// <summary>
        /// Adds one row to the input diagnostics card.
        /// </summary>
        /// <param name="labelText">Text shown on the left side.</param>
        /// <param name="valueText">Text shown on the right side.</param>
        /// <param name="valueClass">Optional class applied to the value label.</param>
        private void AddInputInfoRow(
            string labelText,
            string valueText,
            string valueClass = "state-name")
        {
            if (_inputDiagnosticsContent == null)
            {
                return;
            }

            VisualElement row = new();
            row.AddToClassList("field-row");
            row.AddToClassList("runtime-info-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            Label label = new($"{labelText}:");
            label.AddToClassList("runtime-label");
            label.style.marginRight = 4f;

            Label value = new(valueText);
            value.AddToClassList(valueClass);

            row.Add(label);
            row.Add(value);
            _inputDiagnosticsContent.Add(row);
        }

        #region Status

        /// <summary>
        /// Updates the runtime status badge styling.
        /// </summary>
        /// <param name="status">The status currently displayed by the brain.</param>
        private void SetStatusText(MachineStatus status)
        {
            _statusText.text = status.ToString().ToUpper();

            _statusText.RemoveFromClassList("off");
            _statusText.RemoveFromClassList("on");
            _statusText.RemoveFromClassList("paused");

            switch (status)
            {
                case MachineStatus.On:
                    _statusText.AddToClassList("on");
                    break;
                case MachineStatus.Paused:
                    _statusText.AddToClassList("paused");
                    break;
                case MachineStatus.Off:
                    _statusText.AddToClassList("off");
                    break;
                default:
                    _statusText.AddToClassList("off");
                    break;
            }
        }

        /// <summary>
        /// Reacts to status value changes coming from the hidden enum field.
        /// </summary>
        /// <param name="evt">The enum field change event.</param>
        private void OnStatusChanged(ChangeEvent<System.Enum> evt)
        {
            if (evt == null) return;
            SetStatusText((MachineStatus)evt.newValue);
        }

        #endregion
    }
}