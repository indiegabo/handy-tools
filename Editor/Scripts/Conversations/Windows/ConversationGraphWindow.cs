using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.Editor.ConversationsModule.Validation;
using IndieGabo.HandyTools.Editor.ConversationsModule.Graph;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Hosts one tabbed Conversations table window with shared table controls and
    /// dedicated authoring surfaces for conversation graphs and shared conversants.
    /// </summary>
    public sealed class ConversationGraphWindow : EditorWindow
    {
        private enum WindowTab
        {
            Conversations = 0,
            Conversants = 1,
            Validation = 2,
            Input = 3,
            Presentation = 4,
        }

        private static readonly Color TableSectionBackgroundColor =
            new(0.19f, 0.19f, 0.19f, 1f);
        private static readonly Color ConversationSectionBackgroundColor =
            new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color TabStripBackgroundColor =
            new(0.145f, 0.145f, 0.145f, 1f);
        private static readonly Color SectionBorderColor =
            new(0.24f, 0.24f, 0.24f, 1f);
        private static readonly Color MutedTextColor =
            new(0.66f, 0.66f, 0.66f, 1f);
        private static readonly Color ActiveTabColor =
            new(0.23f, 0.31f, 0.40f, 1f);
        private static readonly Color InactiveTabColor =
            new(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color ValidationSuccessColor =
            new(0.32f, 0.67f, 0.39f, 1f);
        private static readonly Color ValidationInfoColor =
            new(0.35f, 0.57f, 0.79f, 1f);
        private static readonly Color ValidationWarningColor =
            new(0.87f, 0.64f, 0.24f, 1f);
        private static readonly Color ValidationErrorColor =
            new(0.76f, 0.27f, 0.25f, 1f);

        private const string WindowMenuPath =
            HandyToolsEditorMenuPaths.Root + "Conversations/Conversations Window";
        private const string PersistedTableGlobalIdPrefKey =
            "HandyTools.Conversations.Window.TableGlobalId";
        private const string PersistedTabPrefKey =
            "HandyTools.Conversations.Window.Tab";
        private const string PersistedConversationIdPrefKey =
            "HandyTools.Conversations.Window.SelectedConversationId";
        private const string PersistedActorIdPrefKey =
            "HandyTools.Conversations.Window.SelectedActorId";
        private const string PersistedNodeIdPrefKey =
            "HandyTools.Conversations.Window.SelectedNodeId";
        private const float BlackboardOverlayMargin = 12f;
        private const float BlackboardOverlayWidth = 220f;
        private const float BlackboardOverlayMaxHeight = 420f;
        private const float PresentationOverlayWidth = 260f;
        private const float PresentationOverlayMaxHeight = 220f;
        private const float InspectorMinWidth = 320f;
        private const float MinimumWindowHeight = 520f;
        private const int DefaultGraphWidth = 700;
        private const float DragBadgePointerOffsetX = 10f;
        private const float DragBadgePointerOffsetY = -10f;
        private const float DragBadgeViewportPadding = 4f;
        private const float ConversationSelectorMinWidth = 180f;
        private const float ConversationTitleMinWidth = 220f;

        [SerializeField]
        private ConversationTable _table;

        [SerializeField]
        private string _tableGlobalId;

        [SerializeField]
        private string _selectedConversationIdHex;

        [SerializeField]
        private string _selectedActorIdHex;

        [SerializeField]
        private string _selectedNodeIdHex;

        [SerializeField]
        private WindowTab _selectedTab = WindowTab.Conversations;

        private ConversationGraphView _graphView;
        private ConversationGraphBlackboardView _blackboardView;
        private ConversationGraphPresentationOverrideView _presentationOverrideView;
        private ConversationGraphInspectorView _inspectorView;
        private ConversationConversantsView _conversantsView;
        private ConversationValidationView _validationView;
        private ObjectField _tableField;
        private TextField _tableDisplayNameField;
        private ObjectField _defaultPresenterPrefabField;
        private ObjectField _inputContinueActionField;
        private ObjectField _inputCancelActionField;
        private ObjectField _inputSkipActionField;
        private Label _tableSummaryLabel;
        private Button _conversationSelectorButton;
        private TextField _conversationTitleField;
        private Button _createConversationButton;
        private Button _deleteConversationButton;
        private Button _createNodeButton;
        private Button _frameAllButton;
        private Button _conversationsTabButton;
        private Button _presentationTabButton;
        private Button _inputTabButton;
        private Button _conversantsTabButton;
        private Button _validationTabButton;
        private VisualElement _validationTabStatusIndicator;
        private Label _validationTabLabel;
        private Label _conversationWorkspaceTitleLabel;
        private Label _conversationWorkspaceHintLabel;
        private VisualElement _conversationTabRoot;
        private VisualElement _presentationTabRoot;
        private VisualElement _inputTabRoot;
        private VisualElement _validationTabRoot;
        private VisualElement _dragBadge;
        private Label _dragBadgeLabel;
        private ConversationValidationSummary _validationSummary;
        private ConversationNodeSearchProvider _searchProvider;
        private Vector2 _pendingNodeCreationScreenPosition;
        private Vector2 _pendingNodeCreationGraphPosition;
        private SerializableGuid _pendingConnectionFromNodeId;
        private string _pendingConnectionOutputKey;
        private bool _hasPendingNodeCreationScreenPosition;
        private bool _hasPendingNodeCreationGraphPosition;
        private bool _hasPendingConnectionRequest;
        private bool _pendingNodeCreationFromConnectionDrop;

        /// <summary>
        /// Opens the Conversations window from the top-level editor menu.
        /// </summary>
        [MenuItem(WindowMenuPath, false, 20)]
        private static void OpenWindow()
        {
            Open(null);
        }

        /// <summary>
        /// Opens the Conversations window bound to the provided table.
        /// </summary>
        /// <param name="table">Conversation table that should be bound when opening.</param>
        public static void Open(ConversationTable table)
        {
            ConversationGraphWindow window = GetWindow<ConversationGraphWindow>();
            window.titleContent = new GUIContent("Conversations");
            window.minSize = new Vector2(
                DefaultGraphWidth + InspectorMinWidth,
                MinimumWindowHeight);
            window.Show();

            if (table != null)
            {
                window.BindTable(table);
                return;
            }

            window.RestorePersistedWindowState();
            window.ApplyBinding();
        }

        /// <summary>
        /// Restores the persisted editor state when the window becomes active.
        /// </summary>
        private void OnEnable()
        {
            RestorePersistedWindowState();
        }

        /// <summary>
        /// Persists the current editor state when the window is disabled.
        /// </summary>
        private void OnDisable()
        {
            SavePersistedWindowState();
        }

        /// <summary>
        /// Creates the hosted UI Toolkit hierarchy.
        /// </summary>
        public void CreateGUI()
        {
            RestorePersistedWindowState();
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.minHeight = 0f;

            rootVisualElement.Add(CreateTableHeader());
            rootVisualElement.Add(CreateTabStrip());
            rootVisualElement.Add(CreateTabContent());

            CreateDragBadge();
            rootVisualElement.Add(_dragBadge);

            rootVisualElement.UnregisterCallback<MouseMoveEvent>(HandleWindowMouseMove);
            rootVisualElement.UnregisterCallback<MouseUpEvent>(HandleWindowMouseUp);
            rootVisualElement.RegisterCallback<MouseMoveEvent>(HandleWindowMouseMove);
            rootVisualElement.RegisterCallback<MouseUpEvent>(HandleWindowMouseUp);

            ApplyBinding();
        }

        /// <summary>
        /// Creates the shared top section that communicates table-level context.
        /// </summary>
        /// <returns>The styled table header container.</returns>
        private VisualElement CreateTableHeader()
        {
            VisualElement toolbarContainer = CreateSectionContainer(TableSectionBackgroundColor);
            toolbarContainer.Add(CreateSectionCaptionLabel("Conversation Table"));
            toolbarContainer.Add(CreateSectionHintLabel(
                "Choose the table you want to fill for your game. The tabs below split conversations, presentation, input, conversants, and checks."));

            Toolbar configurationToolbar = new();
            configurationToolbar.style.marginTop = 6f;
            configurationToolbar.style.flexWrap = Wrap.Wrap;

            _tableField = new ObjectField("Table")
            {
                objectType = typeof(ConversationTable),
                allowSceneObjects = false,
                value = _table,
            };
            _tableField.style.flexGrow = 1f;
            _tableField.RegisterValueChangedCallback(
                evt => BindTable(evt.newValue as ConversationTable));
            configurationToolbar.Add(_tableField);

            _tableSummaryLabel = CreateSectionHintLabel(string.Empty);
            _tableSummaryLabel.style.marginLeft = 10f;
            _tableSummaryLabel.style.alignSelf = Align.Center;
            configurationToolbar.Add(_tableSummaryLabel);

            toolbarContainer.Add(configurationToolbar);

            _tableDisplayNameField = new TextField("Display Name")
            {
                isDelayed = true,
                tooltip = "Human-readable table name shown by ConversationReference pickers. When empty, the asset name is used.",
            };
            _tableDisplayNameField.style.marginTop = 6f;
            _tableDisplayNameField.RegisterValueChangedCallback(
                evt => HandleTableDisplayNameChanged(evt.newValue));
            toolbarContainer.Add(_tableDisplayNameField);
            return toolbarContainer;
        }

        /// <summary>
        /// Creates the tab strip used to switch between table-owned authoring surfaces.
        /// </summary>
        /// <returns>The configured tab strip.</returns>
        private VisualElement CreateTabStrip()
        {
            VisualElement tabStrip = new();
            tabStrip.style.flexDirection = FlexDirection.Row;
            tabStrip.style.flexShrink = 0f;
            tabStrip.style.paddingLeft = 10f;
            tabStrip.style.paddingRight = 10f;
            tabStrip.style.paddingTop = 8f;
            tabStrip.style.paddingBottom = 8f;
            tabStrip.style.backgroundColor = TabStripBackgroundColor;
            tabStrip.style.borderBottomWidth = 1f;
            tabStrip.style.borderBottomColor = SectionBorderColor;

            _conversationsTabButton = CreateTabButton(
                "Conversations",
                WindowTab.Conversations);
            tabStrip.Add(_conversationsTabButton);

            _presentationTabButton = CreateTabButton(
                "Presentation",
                WindowTab.Presentation);
            _presentationTabButton.style.marginLeft = 6f;
            tabStrip.Add(_presentationTabButton);

            _inputTabButton = CreateTabButton(
                "Input",
                WindowTab.Input);
            _inputTabButton.style.marginLeft = 6f;
            tabStrip.Add(_inputTabButton);

            _conversantsTabButton = CreateTabButton(
                "Conversants",
                WindowTab.Conversants);
            _conversantsTabButton.style.marginLeft = 6f;
            tabStrip.Add(_conversantsTabButton);

            _validationTabButton = CreateValidationTabButton();
            _validationTabButton.style.marginLeft = 6f;
            tabStrip.Add(_validationTabButton);

            return tabStrip;
        }

        /// <summary>
        /// Creates the main tab content host.
        /// </summary>
        /// <returns>The configured tab content host.</returns>
        private VisualElement CreateTabContent()
        {
            VisualElement contentHost = new();
            contentHost.style.flexDirection = FlexDirection.Column;
            contentHost.style.flexGrow = 1f;
            contentHost.style.minHeight = 0f;

            _conversationTabRoot = CreateConversationTab();
            contentHost.Add(_conversationTabRoot);

            _presentationTabRoot = CreatePresentationTab();
            contentHost.Add(_presentationTabRoot);

            _inputTabRoot = CreateInputTab();
            contentHost.Add(_inputTabRoot);

            _conversantsView = new ConversationConversantsView();
            _conversantsView.StructureChanged += HandleConversantsStructureChanged;
            _conversantsView.ContentChanged += HandleConversantsContentChanged;
            _conversantsView.SelectedActorChanged += HandleSelectedActorChanged;
            contentHost.Add(_conversantsView);

            _validationTabRoot = CreateValidationTab();
            contentHost.Add(_validationTabRoot);
            return contentHost;
        }

        /// <summary>
        /// Creates the conversation-authoring tab content.
        /// </summary>
        /// <returns>The configured Conversations tab.</returns>
        private VisualElement CreateConversationTab()
        {
            VisualElement tabRoot = new();
            tabRoot.style.flexDirection = FlexDirection.Column;
            tabRoot.style.flexGrow = 1f;
            tabRoot.style.minHeight = 0f;
            tabRoot.Add(CreateConversationToolbar());
            tabRoot.Add(CreateConversationWorkspace());
            return tabRoot;
        }

        /// <summary>
        /// Creates the table-level input tab content.
        /// </summary>
        /// <returns>The configured Input tab.</returns>
        private VisualElement CreateInputTab()
        {
            VisualElement tabRoot = new();
            tabRoot.style.flexDirection = FlexDirection.Column;
            tabRoot.style.flexGrow = 1f;
            tabRoot.style.minHeight = 0f;
            tabRoot.Add(CreateInputSettingsSection());
            return tabRoot;
        }

        /// <summary>
        /// Creates the table-level presentation tab content.
        /// </summary>
        /// <returns>The configured Presentation tab.</returns>
        private VisualElement CreatePresentationTab()
        {
            VisualElement tabRoot = new();
            tabRoot.style.flexDirection = FlexDirection.Column;
            tabRoot.style.flexGrow = 1f;
            tabRoot.style.minHeight = 0f;
            tabRoot.Add(CreatePresentationSettingsSection());
            return tabRoot;
        }

        /// <summary>
        /// Creates the conversation-specific toolbar shown inside the Conversations tab.
        /// </summary>
        /// <returns>The configured toolbar section.</returns>
        private VisualElement CreateConversationToolbar()
        {
            VisualElement toolbarContainer = CreateSectionContainer(TableSectionBackgroundColor);
            toolbarContainer.Add(CreateSectionCaptionLabel("Conversations"));
            toolbarContainer.Add(CreateSectionHintLabel(
                "Choose the conversation you want to work on, create new ones, or remove the current selection here."));

            Toolbar conversationToolbar = new();
            conversationToolbar.style.marginTop = 6f;
            conversationToolbar.style.flexWrap = Wrap.Wrap;

            _conversationSelectorButton = new(ShowConversationSelectionMenu)
            {
                text = "Select Conversation",
            };
            _conversationSelectorButton.style.minWidth = ConversationSelectorMinWidth;
            conversationToolbar.Add(_conversationSelectorButton);

            _createConversationButton = new(HandleCreateConversationRequested)
            {
                text = "New Conversation",
            };
            conversationToolbar.Add(_createConversationButton);

            _deleteConversationButton = new(HandleDeleteConversationRequested)
            {
                text = "Delete Conversation",
            };
            conversationToolbar.Add(_deleteConversationButton);

            toolbarContainer.Add(conversationToolbar);
            return toolbarContainer;
        }

        /// <summary>
        /// Creates the dedicated authoring workspace shown below the table section.
        /// </summary>
        /// <returns>The conversation workspace container.</returns>
        private VisualElement CreateConversationWorkspace()
        {
            VisualElement workspace = new();
            workspace.style.flexDirection = FlexDirection.Column;
            workspace.style.flexGrow = 1f;
            workspace.style.minHeight = 0f;

            workspace.Add(CreateConversationWorkspaceHeader());
            workspace.Add(CreateConversationSplitView());
            return workspace;
        }

        /// <summary>
        /// Creates the section used to configure table-level input bindings.
        /// </summary>
        /// <returns>The configured input-settings section.</returns>
        private VisualElement CreateInputSettingsSection()
        {
            VisualElement section = CreateSectionContainer(TableSectionBackgroundColor);
            section.style.flexGrow = 1f;
            section.style.minHeight = 0f;

            section.Add(CreateSectionCaptionLabel("Input"));
            section.Add(CreateSectionHintLabel(
                "Configure the table-level actions that advance, cancel, or skip one conversation. Leave fields empty to use the module fallbacks."));

            VisualElement fieldsContainer = new();
            fieldsContainer.style.flexDirection = FlexDirection.Column;
            fieldsContainer.style.marginTop = 8f;
            fieldsContainer.style.maxWidth = 420f;

            _inputContinueActionField = CreateInputActionField(
                "Advance Action",
                "When empty, the table uses the fallback advance action configured in the Conversations module settings.",
                HandleContinueActionChanged);
            fieldsContainer.Add(_inputContinueActionField);

            _inputCancelActionField = CreateInputActionField(
                "Cancel Action",
                "When empty, the table uses the fallback cancel action configured in the Conversations module settings.",
                HandleCancelActionChanged);
            fieldsContainer.Add(_inputCancelActionField);

            _inputSkipActionField = CreateInputActionField(
                "Skip Action",
                "When empty, the table uses the fallback skip action configured in the Conversations module settings.",
                HandleSkipActionChanged);
            fieldsContainer.Add(_inputSkipActionField);

            section.Add(fieldsContainer);

            HelpBox helpBox = new(
                "These bindings are stored on the ConversationTable and resolve against module-level fallbacks when left empty.",
                HelpBoxMessageType.Info);
            helpBox.style.marginTop = 8f;
            helpBox.style.maxWidth = 560f;
            section.Add(helpBox);
            return section;
        }

        /// <summary>
        /// Creates the section used to configure the table default presenter prefab.
        /// </summary>
        /// <returns>The configured presentation-settings section.</returns>
        private VisualElement CreatePresentationSettingsSection()
        {
            VisualElement section = CreateSectionContainer(TableSectionBackgroundColor);
            section.style.flexGrow = 1f;
            section.style.minHeight = 0f;

            section.Add(CreateSectionCaptionLabel("Presentation"));
            section.Add(CreateSectionHintLabel(
                "Register the default presenter prefab used by this table. Individual conversations can override it from the graph overlay."));

            VisualElement fieldsContainer = new();
            fieldsContainer.style.flexDirection = FlexDirection.Column;
            fieldsContainer.style.marginTop = 8f;
            fieldsContainer.style.maxWidth = 460f;

            _defaultPresenterPrefabField = new ObjectField("Default Presenter Prefab")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
            };
            _defaultPresenterPrefabField.tooltip =
                "The assigned prefab should contain ConversationPresenterRoot so composed presenter components can bind to playback.";
            _defaultPresenterPrefabField.RegisterValueChangedCallback(
                evt => HandleDefaultPresenterPrefabChanged(evt.newValue as GameObject));
            fieldsContainer.Add(_defaultPresenterPrefabField);
            section.Add(fieldsContainer);

            HelpBox helpBox = new(
                "Presenter prefabs stay rendering-strategy agnostic. They can host Canvas, UI Toolkit, or any other runtime presentation so long as the prefab root contains ConversationPresenterRoot.",
                HelpBoxMessageType.Info);
            helpBox.style.marginTop = 8f;
            helpBox.style.maxWidth = 640f;
            section.Add(helpBox);
            return section;
        }

        /// <summary>
        /// Creates one input-action field hosted by the Input tab.
        /// </summary>
        /// <param name="label">Field label.</param>
        /// <param name="tooltip">Field tooltip.</param>
        /// <param name="changedHandler">Callback invoked after one value change.</param>
        /// <returns>The configured object field.</returns>
        private static ObjectField CreateInputActionField(
            string label,
            string tooltip,
            Action<InputActionReference> changedHandler)
        {
            ObjectField field = new(label)
            {
                objectType = typeof(InputActionReference),
                allowSceneObjects = false,
            };

            field.tooltip = tooltip;
            field.style.minWidth = 220f;
            field.style.marginBottom = 6f;
            field.RegisterValueChangedCallback(evt =>
                changedHandler(evt.newValue as InputActionReference));
            return field;
        }

        /// <summary>
        /// Creates the table-level validation surface.
        /// </summary>
        /// <returns>The configured Validation tab.</returns>
        private VisualElement CreateValidationTab()
        {
            _validationView = new ConversationValidationView();
            _validationView.NavigateRequested += HandleValidationNavigateRequested;
            _validationView.SummaryChanged += HandleValidationSummaryChanged;
            return _validationView;
        }

        /// <summary>
        /// Creates the selected-conversation header that sits above the graph canvas.
        /// </summary>
        /// <returns>The configured workspace header.</returns>
        private VisualElement CreateConversationWorkspaceHeader()
        {
            VisualElement header = CreateSectionContainer(ConversationSectionBackgroundColor);
            header.style.marginTop = 6f;

            header.Add(CreateSectionCaptionLabel("Selected Conversation"));

            _conversationWorkspaceTitleLabel = new Label("No Conversation Selected");
            _conversationWorkspaceTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _conversationWorkspaceTitleLabel.style.fontSize = 13f;
            _conversationWorkspaceTitleLabel.style.marginTop = 2f;
            header.Add(_conversationWorkspaceTitleLabel);

            VisualElement authoringRow = new();
            authoringRow.style.flexDirection = FlexDirection.Row;
            authoringRow.style.alignItems = Align.FlexEnd;
            authoringRow.style.flexWrap = Wrap.Wrap;
            authoringRow.style.marginTop = 8f;

            _conversationTitleField = new TextField("Path")
            {
                isDelayed = true,
                tooltip = "Use '/' or '|' to group this conversation in the list. The last part becomes the conversation name.",
            };
            _conversationTitleField.style.flexGrow = 1f;
            _conversationTitleField.style.minWidth = ConversationTitleMinWidth;
            _conversationTitleField.RegisterValueChangedCallback(
                evt => HandleConversationPathChanged(evt.newValue));
            authoringRow.Add(_conversationTitleField);

            VisualElement actionsContainer = new();
            actionsContainer.style.flexDirection = FlexDirection.Row;
            actionsContainer.style.alignItems = Align.FlexEnd;
            actionsContainer.style.marginLeft = 8f;

            _createNodeButton = new(OpenToolbarNodeSearch)
            {
                text = "Add Node",
            };
            actionsContainer.Add(_createNodeButton);

            _frameAllButton = new(() => _graphView?.FrameAll())
            {
                text = "Show Whole Graph",
            };
            _frameAllButton.style.marginLeft = 6f;
            actionsContainer.Add(_frameAllButton);

            authoringRow.Add(actionsContainer);
            header.Add(authoringRow);

            _conversationWorkspaceHintLabel = CreateSectionHintLabel(
                "Choose or create a conversation above to start building its flow here.");
            _conversationWorkspaceHintLabel.style.marginTop = 6f;
            header.Add(_conversationWorkspaceHintLabel);

            return header;
        }

        /// <summary>
        /// Creates the main split view that hosts the graph canvas and inspector.
        /// </summary>
        /// <returns>The configured split view.</returns>
        private TwoPaneSplitView CreateConversationSplitView()
        {
            TwoPaneSplitView splitView = new(
                1,
                InspectorMinWidth,
                TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;
            splitView.style.minHeight = 0f;

            VisualElement graphCanvasPane = new();
            graphCanvasPane.style.flexGrow = 1f;
            graphCanvasPane.style.position = Position.Relative;
            graphCanvasPane.style.minHeight = 0f;

            _graphView = new ConversationGraphView();
            _graphView.NodeSelected += HandleNodeSelected;
            _graphView.NodeCreationRequested += OpenNodeSearch;
            _graphView.ConnectedNodeCreationRequested += OpenConnectedNodeSearch;
            _graphView.GraphModified += HandleGraphModified;
            graphCanvasPane.Add(_graphView);

            _blackboardView = new ConversationGraphBlackboardView();
            _blackboardView.BlackboardChanged += HandleGraphModified;
            _blackboardView.style.position = Position.Absolute;
            _blackboardView.style.left = BlackboardOverlayMargin;
            _blackboardView.style.top = BlackboardOverlayMargin;
            _blackboardView.style.width = BlackboardOverlayWidth;
            _blackboardView.style.maxHeight = BlackboardOverlayMaxHeight;
            graphCanvasPane.Add(_blackboardView);

            _presentationOverrideView = new ConversationGraphPresentationOverrideView();
            _presentationOverrideView.PresentationChanged += HandlePresentationChanged;
            _presentationOverrideView.style.position = Position.Absolute;
            _presentationOverrideView.style.right = BlackboardOverlayMargin;
            _presentationOverrideView.style.top = BlackboardOverlayMargin;
            _presentationOverrideView.style.width = PresentationOverlayWidth;
            _presentationOverrideView.style.maxHeight = PresentationOverlayMaxHeight;
            graphCanvasPane.Add(_presentationOverrideView);
            splitView.Add(graphCanvasPane);

            VisualElement inspectorPane = new();
            inspectorPane.style.flexDirection = FlexDirection.Column;
            inspectorPane.style.flexGrow = 1f;
            inspectorPane.style.minWidth = InspectorMinWidth;
            inspectorPane.style.paddingLeft = 8f;
            inspectorPane.style.paddingRight = 8f;
            inspectorPane.style.paddingTop = 8f;
            inspectorPane.style.paddingBottom = 8f;

            _inspectorView = new ConversationGraphInspectorView();
            _inspectorView.InspectorChanged += HandleInspectorChanged;
            inspectorPane.Add(_inspectorView);
            splitView.Add(inspectorPane);

            return splitView;
        }

        /// <summary>
        /// Binds the window to one authored table.
        /// </summary>
        /// <param name="table">Table that should drive the window.</param>
        private void BindTable(ConversationTable table)
        {
            _table = table;
            _tableGlobalId = _table == null
                ? string.Empty
                : GlobalObjectId.GetGlobalObjectIdSlow(_table).ToString();
            _tableField?.SetValueWithoutNotify(table);

            if (_table == null)
            {
                _selectedConversationIdHex = string.Empty;
                _selectedActorIdHex = string.Empty;
                _selectedNodeIdHex = string.Empty;
            }

            ResolveSelectedConversationOrFallback();
            ApplyBinding();
        }

        /// <summary>
        /// Applies the current table and selected-conversation binding to all child views.
        /// </summary>
        private void ApplyBinding()
        {
            ConversationDefinition conversation = ResolveSelectedConversationOrFallback();
            ResolveSelectedActorOrFallback();

            if (conversation == null)
            {
                _selectedNodeIdHex = string.Empty;
            }
            else if (!conversation.Graph.TryGetNode(GetSelectedNodeId(), out _))
            {
                _selectedNodeIdHex = string.Empty;
            }

            _graphView?.BindConversation(_table, conversation);
            _blackboardView?.BindConversation(_table, conversation);
            _presentationOverrideView?.BindConversation(_table, conversation);
            _inspectorView?.BindSelection(_table, conversation, GetSelectedNodeId());
            _conversantsView?.BindTable(_table, _selectedActorIdHex);
            _validationView?.BindTable(_table);
            RefreshWindowState(conversation);
            SavePersistedWindowState();
        }

        /// <summary>
        /// Refreshes the window state after one structural conversant change.
        /// </summary>
        private void HandleConversantsStructureChanged()
        {
            ApplyBinding();
        }

        /// <summary>
        /// Refreshes validation after one non-structural conversant edit.
        /// </summary>
        private void HandleConversantsContentChanged()
        {
            _validationView?.RequestValidation();
        }

        /// <summary>
        /// Stores the current conversant selection when the dedicated tab changes it.
        /// </summary>
        /// <param name="actorIdHex">Serialized selected conversant id.</param>
        private void HandleSelectedActorChanged(string actorIdHex)
        {
            _selectedActorIdHex = actorIdHex ?? string.Empty;
            SavePersistedWindowState();
        }

        /// <summary>
        /// Stores one table-level continue action change.
        /// </summary>
        /// <param name="continueAction">Continue action assigned on the table.</param>
        private void HandleContinueActionChanged(InputActionReference continueAction)
        {
            HandleInputActionChanged(
                continueAction,
                _table?.AuthoredContinueAction,
                resolvedAction => _table.SetContinueAction(resolvedAction),
                "Set Conversation Advance Action");
        }

        /// <summary>
        /// Stores one table-level cancel action change.
        /// </summary>
        /// <param name="cancelAction">Cancel action assigned on the table.</param>
        private void HandleCancelActionChanged(InputActionReference cancelAction)
        {
            HandleInputActionChanged(
                cancelAction,
                _table?.AuthoredCancelAction,
                resolvedAction => _table.SetCancelAction(resolvedAction),
                "Set Conversation Cancel Action");
        }

        /// <summary>
        /// Stores one table-level skip action change.
        /// </summary>
        /// <param name="skipAction">Skip action assigned on the table.</param>
        private void HandleSkipActionChanged(InputActionReference skipAction)
        {
            HandleInputActionChanged(
                skipAction,
                _table?.AuthoredSkipAction,
                resolvedAction => _table.SetSkipAction(resolvedAction),
                "Set Conversation Skip Action");
        }

        /// <summary>
        /// Stores one table-level input action change.
        /// </summary>
        /// <param name="newAction">New authored input action.</param>
        /// <param name="currentAction">Current authored input action.</param>
        /// <param name="applyAction">Mutation applied to the bound table.</param>
        /// <param name="undoLabel">Undo label recorded for the mutation.</param>
        private void HandleInputActionChanged(
            InputActionReference newAction,
            InputActionReference currentAction,
            Action<InputActionReference> applyAction,
            string undoLabel)
        {
            if (_table == null || currentAction == newAction)
            {
                return;
            }

            Undo.RecordObject(_table, undoLabel);
            applyAction(newAction);
            EditorUtility.SetDirty(_table);
            RefreshWindowState(ResolveSelectedConversationOrFallback());
            SavePersistedWindowState();
        }

        /// <summary>
        /// Stores one table-level display-name change.
        /// </summary>
        /// <param name="displayName">New human-readable display name.</param>
        private void HandleTableDisplayNameChanged(string displayName)
        {
            if (_table == null)
            {
                return;
            }

            string normalizedDisplayName = displayName?.Trim() ?? string.Empty;

            if (string.Equals(
                    _table.AuthoredDisplayName,
                    normalizedDisplayName,
                    StringComparison.Ordinal))
            {
                return;
            }

            Undo.RecordObject(_table, "Set Conversation Table Display Name");
            _table.SetDisplayName(normalizedDisplayName);
            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssetIfDirty(_table);
            _blackboardView?.Refresh();
            _presentationOverrideView?.Refresh();
            _inspectorView?.Refresh();
            RefreshWindowState(ResolveSelectedConversationOrFallback());
            SavePersistedWindowState();
        }

        /// <summary>
        /// Stores the default presenter prefab configured on the bound table.
        /// </summary>
        /// <param name="presenterPrefab">Presenter prefab that should become the table default.</param>
        private void HandleDefaultPresenterPrefabChanged(GameObject presenterPrefab)
        {
            if (_table == null || _table.DefaultPresenterPrefab == presenterPrefab)
            {
                return;
            }

            Undo.RecordObject(_table, "Set Default Conversation Presenter Prefab");
            _table.SetDefaultPresenterPrefab(presenterPrefab);
            EditorUtility.SetDirty(_table);
            _presentationOverrideView?.Refresh();
            RefreshWindowState(ResolveSelectedConversationOrFallback());
            SavePersistedWindowState();
        }

        /// <summary>
        /// Reacts to graph-canvas selection changes.
        /// </summary>
        /// <param name="nodeView">Selected node view.</param>
        private void HandleNodeSelected(ConversationGraphNodeView nodeView)
        {
            _selectedNodeIdHex = nodeView?.NodeId.ToHexString() ?? string.Empty;
            _inspectorView?.BindSelection(
                _table,
                ResolveSelectedConversationOrFallback(),
                GetSelectedNodeId());
            SavePersistedWindowState();
        }

        /// <summary>
        /// Refreshes graph-side surfaces after one graph mutation.
        /// </summary>
        private void HandleGraphModified()
        {
            ConversationDefinition conversation = ResolveSelectedConversationOrFallback();
            SerializableGuid selectedNodeId = GetSelectedNodeId();
            _graphView?.RebuildGraph(selectedNodeId);
            _blackboardView?.Refresh();
            _presentationOverrideView?.Refresh();
            _inspectorView?.Refresh();
            RefreshWindowState(conversation);
            _validationView?.RequestValidation();
            Repaint();
        }

        /// <summary>
        /// Refreshes authoring surfaces after one presentation override mutation.
        /// </summary>
        private void HandlePresentationChanged()
        {
            RefreshWindowState(ResolveSelectedConversationOrFallback());
            _validationView?.RequestValidation();
            SavePersistedWindowState();
            Repaint();
        }

        /// <summary>
        /// Reacts to mutations produced by the node inspector.
        /// </summary>
        /// <param name="selectedNodeId">Node id that should remain selected after the mutation.</param>
        private void HandleInspectorChanged(SerializableGuid selectedNodeId)
        {
            _table?.EnsureAuthoringIds();
            _selectedNodeIdHex = selectedNodeId == SerializableGuid.Empty
                ? string.Empty
                : selectedNodeId.ToHexString();

            if (_table != null)
            {
                EditorUtility.SetDirty(_table);
            }

            _graphView?.RefreshNodePresentation(selectedNodeId);
            _blackboardView?.Refresh();
            _validationView?.RequestValidation();
            SavePersistedWindowState();
            Repaint();
        }

        /// <summary>
        /// Creates one new conversation in the bound table.
        /// </summary>
        private void HandleCreateConversationRequested()
        {
            if (_table == null)
            {
                return;
            }

            Undo.RecordObject(_table, "Create Conversation");
            ConversationDefinition conversation = _table.CreateConversation();
            EditorUtility.SetDirty(_table);
            SelectConversation(conversation);
            ApplyBinding();
        }

        /// <summary>
        /// Deletes the currently selected conversation from the table.
        /// </summary>
        private void HandleDeleteConversationRequested()
        {
            if (_table == null
                || !_table.TryGetConversationIndex(
                    GetSelectedConversationId(),
                    out int conversationIndex))
            {
                return;
            }

            SerializableGuid selectedConversationId = GetSelectedConversationId();
            Undo.RecordObject(_table, "Delete Conversation");

            if (!_table.RemoveConversation(selectedConversationId))
            {
                return;
            }

            EditorUtility.SetDirty(_table);
            _selectedNodeIdHex = string.Empty;

            ConversationDefinition fallbackConversation = null;

            if (_table.Conversations.Count > 0)
            {
                int nextIndex = Mathf.Clamp(
                    conversationIndex,
                    0,
                    _table.Conversations.Count - 1);
                fallbackConversation = _table.Conversations[nextIndex];
            }

            SelectConversation(fallbackConversation);
            ApplyBinding();
        }

        /// <summary>
        /// Updates the authored path of the currently selected conversation.
        /// </summary>
        /// <param name="newPath">Authored path entered in the workspace header.</param>
        private void HandleConversationPathChanged(string newPath)
        {
            ConversationDefinition conversation = ResolveSelectedConversationOrFallback();

            if (_table == null || conversation == null)
            {
                return;
            }

            string sanitizedTitle = string.IsNullOrWhiteSpace(newPath)
                ? "Conversation"
                : newPath.Trim();

            if (string.Equals(
                    conversation.Title,
                    sanitizedTitle,
                    StringComparison.Ordinal))
            {
                return;
            }

            Undo.RecordObject(_table, "Rename Conversation");
            conversation.SetTitle(sanitizedTitle);
            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);
            _blackboardView?.Refresh();
            _presentationOverrideView?.Refresh();
            _inspectorView?.Refresh();
            RefreshWindowState(conversation);
            _validationView?.RequestValidation();
            Repaint();
        }

        /// <summary>
        /// Opens the conversation-selection menu for the bound table.
        /// </summary>
        private void ShowConversationSelectionMenu()
        {
            if (_table == null)
            {
                return;
            }

            GenericMenu menu = new();
            ConversationDefinition selectedConversation = ResolveSelectedConversationOrFallback();

            if (_table.Conversations == null || _table.Conversations.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Conversations"));
                menu.ShowAsContext();
                return;
            }

            Dictionary<string, int> menuPathCounts = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < _table.Conversations.Count; index++)
            {
                ConversationDefinition conversation = _table.Conversations[index];
                string menuPath = BuildConversationMenuPath(conversation);

                if (menuPathCounts.TryGetValue(menuPath, out int currentCount))
                {
                    menuPathCounts[menuPath] = currentCount + 1;
                }
                else
                {
                    menuPathCounts.Add(menuPath, 1);
                }
            }

            for (int index = 0; index < _table.Conversations.Count; index++)
            {
                ConversationDefinition conversation = _table.Conversations[index];
                string menuLabel = BuildConversationSelectionMenuLabel(
                    conversation,
                    menuPathCounts);
                bool isSelected = conversation != null
                    && selectedConversation != null
                    && conversation.ConversationId == selectedConversation.ConversationId;

                menu.AddItem(
                    new GUIContent(menuLabel),
                    isSelected,
                    () =>
                    {
                        SelectConversation(conversation);
                        ApplyBinding();
                    });
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Opens the node-creation menu for the selected conversation.
        /// </summary>
        private void OpenNodeSearch(Vector2 screenPosition)
        {
            if (_graphView == null || ResolveSelectedConversationOrFallback() == null)
            {
                ShowNotification(new GUIContent(
                    "Choose or create one conversation before adding nodes."));
                return;
            }

            _pendingNodeCreationScreenPosition = GraphViewLocalToScreenPosition(screenPosition);
            _hasPendingNodeCreationScreenPosition = true;
            _pendingNodeCreationGraphPosition = _graphView.ChangeCoordinatesTo(
                _graphView.contentViewContainer,
                screenPosition);
            _hasPendingNodeCreationGraphPosition = true;
            _hasPendingConnectionRequest = false;
            _pendingConnectionFromNodeId = default;
            _pendingConnectionOutputKey = null;
            _pendingNodeCreationFromConnectionDrop = false;

            _searchProvider ??= CreateInstance<ConversationNodeSearchProvider>();
            _searchProvider.Initialize(this);
            SearchWindow.Open(
                new SearchWindowContext(_pendingNodeCreationScreenPosition),
                _searchProvider);
        }

        /// <summary>
        /// Opens the connected-node search after one edge is dropped on the canvas.
        /// </summary>
        /// <param name="request">Connected-node creation request emitted by the graph.</param>
        private void OpenConnectedNodeSearch(
            ConversationGraphView.ConnectedNodeCreationRequest request)
        {
            if (_graphView == null || ResolveSelectedConversationOrFallback() == null)
            {
                ShowNotification(new GUIContent(
                    "Choose or create one conversation before adding nodes."));
                return;
            }

            _pendingNodeCreationScreenPosition = GraphViewLocalToScreenPosition(
                request.ScreenPosition);
            _hasPendingNodeCreationScreenPosition = true;
            _pendingNodeCreationGraphPosition = _graphView.contentViewContainer.WorldToLocal(
                request.ScreenPosition);
            _hasPendingNodeCreationGraphPosition = true;
            _pendingConnectionFromNodeId = request.FromNodeId;
            _pendingConnectionOutputKey = request.OutputKey;
            _hasPendingConnectionRequest = true;
            _pendingNodeCreationFromConnectionDrop = true;

            _searchProvider ??= CreateInstance<ConversationNodeSearchProvider>();
            _searchProvider.Initialize(this);
            SearchWindow.Open(
                new SearchWindowContext(_pendingNodeCreationScreenPosition),
                _searchProvider);
        }

        /// <summary>
        /// Opens the node search from the toolbar button.
        /// </summary>
        private void OpenToolbarNodeSearch()
        {
            if (_graphView == null)
            {
                return;
            }

            OpenNodeSearch(_graphView.contentRect.center);
        }

        /// <summary>
        /// Creates one node from the active search selection.
        /// </summary>
        /// <param name="nodeType">Concrete node type to create.</param>
        /// <param name="fallbackScreenPosition">Fallback screen position used by the search window.</param>
        internal void CreateNodeFromSearch(Type nodeType, Vector2 fallbackScreenPosition)
        {
            Vector2 screenPosition = _hasPendingNodeCreationScreenPosition
                ? _pendingNodeCreationScreenPosition
                : fallbackScreenPosition;
            Vector2? graphPositionOverride = _hasPendingNodeCreationGraphPosition
                ? _pendingNodeCreationGraphPosition
                : null;
            SerializableGuid connectFromNodeId = _hasPendingConnectionRequest
                ? _pendingConnectionFromNodeId
                : default;
            string connectOutputKey = _hasPendingConnectionRequest
                ? _pendingConnectionOutputKey
                : null;
            bool fromConnectionDrop = _pendingNodeCreationFromConnectionDrop;

            _hasPendingNodeCreationScreenPosition = false;
            _hasPendingNodeCreationGraphPosition = false;
            _hasPendingConnectionRequest = false;
            _pendingConnectionFromNodeId = default;
            _pendingConnectionOutputKey = null;
            _pendingNodeCreationFromConnectionDrop = false;

            CreateNode(
                nodeType,
                screenPosition,
                connectFromNodeId,
                connectOutputKey,
                fromConnectionDrop,
                graphPositionOverride);
        }

        /// <summary>
        /// Creates one node in the selected conversation graph.
        /// </summary>
        /// <param name="nodeType">Concrete node type to create.</param>
        /// <param name="screenPosition">Requested creation position in screen space.</param>
        /// <param name="connectFromNodeId">Optional origin node that should connect to the new node.</param>
        /// <param name="connectOutputKey">Optional origin output key.</param>
        /// <param name="fromConnectionDrop">True when the creation started from one dropped edge.</param>
        /// <param name="graphPositionOverride">Optional precomputed graph-space position.</param>
        private void CreateNode(
            Type nodeType,
            Vector2 screenPosition,
            SerializableGuid connectFromNodeId = default,
            string connectOutputKey = null,
            bool fromConnectionDrop = false,
            Vector2? graphPositionOverride = null)
        {
            if (_graphView == null || ResolveSelectedConversationOrFallback() == null)
            {
                return;
            }

            Vector2 graphPosition = graphPositionOverride
                ?? ScreenPointToGraphPosition(screenPosition);
            ConversationNodeBase node = _graphView.CreateNode(
                nodeType,
                graphPosition,
                connectFromNodeId,
                connectOutputKey);

            if (node == null)
            {
                ShowNotification(new GUIContent("Could not create the requested node."));
                return;
            }

            _selectedNodeIdHex = node.Id.ToHexString();
            _graphView.RebuildGraph(node.Id);
            _blackboardView?.Refresh();
            _inspectorView?.BindSelection(_table, ResolveSelectedConversationOrFallback(), node.Id);

            if (fromConnectionDrop)
            {
                _graphView.AlignNodeInputPortToDropPosition(node.Id);
            }

            RefreshWindowState(ResolveSelectedConversationOrFallback());
            _validationView?.RequestValidation();
            SavePersistedWindowState();
            Repaint();
        }

        /// <summary>
        /// Converts one screen-space position into one graph-space creation position.
        /// </summary>
        /// <param name="screenPosition">Position in screen space.</param>
        /// <returns>The converted graph-space position.</returns>
        private Vector2 ScreenPointToGraphPosition(Vector2 screenPosition)
        {
            if (_graphView == null)
            {
                return Vector2.zero;
            }

            VisualElement windowRoot = rootVisualElement;
            VisualElement windowParent = windowRoot.parent;
            Vector2 windowLocalPosition = screenPosition - position.position;
            Vector2 windowMousePosition = windowParent == null
                ? windowLocalPosition
                : windowRoot.ChangeCoordinatesTo(windowParent, windowLocalPosition);

            return _graphView.contentViewContainer.WorldToLocal(windowMousePosition);
        }

        /// <summary>
        /// Converts one graph-view local position into screen space for search windows.
        /// </summary>
        /// <param name="graphViewLocalPosition">Position local to the graph view.</param>
        /// <returns>The converted screen position.</returns>
        private Vector2 GraphViewLocalToScreenPosition(Vector2 graphViewLocalPosition)
        {
            if (_graphView == null)
            {
                return position.position;
            }

            Vector2 rootLocalPosition = _graphView.ChangeCoordinatesTo(
                rootVisualElement,
                graphViewLocalPosition);
            return rootLocalPosition + position.position;
        }

        /// <summary>
        /// Gets the currently selected node id.
        /// </summary>
        /// <returns>The selected node id, or empty when none is selected.</returns>
        private SerializableGuid GetSelectedNodeId()
        {
            return string.IsNullOrWhiteSpace(_selectedNodeIdHex)
                ? SerializableGuid.Empty
                : SerializableGuid.FromHexString(_selectedNodeIdHex);
        }

        /// <summary>
        /// Gets the currently selected conversation id.
        /// </summary>
        /// <returns>The selected conversation id, or empty when none is selected.</returns>
        private SerializableGuid GetSelectedConversationId()
        {
            return string.IsNullOrWhiteSpace(_selectedConversationIdHex)
                ? SerializableGuid.Empty
                : SerializableGuid.FromHexString(_selectedConversationIdHex);
        }

        /// <summary>
        /// Selects the provided conversation and clears the current node selection.
        /// </summary>
        /// <param name="conversation">Conversation that should become selected.</param>
        private void SelectConversation(ConversationDefinition conversation)
        {
            _selectedConversationIdHex = conversation?.ConversationId.ToHexString() ?? string.Empty;
            _selectedNodeIdHex = string.Empty;
        }

        /// <summary>
        /// Gets the currently selected conversant id.
        /// </summary>
        /// <returns>The selected conversant id, or empty when none is selected.</returns>
        private SerializableGuid GetSelectedActorId()
        {
            return string.IsNullOrWhiteSpace(_selectedActorIdHex)
                ? SerializableGuid.Empty
                : SerializableGuid.FromHexString(_selectedActorIdHex);
        }

        /// <summary>
        /// Resolves the selected conversant or falls back to the first table entry.
        /// </summary>
        /// <returns>The resolved conversant, or null when none exists.</returns>
        private ConversationActorDefinition ResolveSelectedActorOrFallback()
        {
            if (_table == null)
            {
                _selectedActorIdHex = string.Empty;
                return null;
            }

            if (_table.TryGetActor(
                    GetSelectedActorId(),
                    out ConversationActorDefinition selectedActor))
            {
                return selectedActor;
            }

            if (_table.Actors != null && _table.Actors.Count > 0)
            {
                ConversationActorDefinition firstActor = _table.Actors[0];
                _selectedActorIdHex = firstActor?.ActorId.ToHexString()
                    ?? string.Empty;
                return firstActor;
            }

            _selectedActorIdHex = string.Empty;
            return null;
        }

        /// <summary>
        /// Resolves the selected conversation or falls back to the first table entry.
        /// </summary>
        /// <returns>The resolved conversation, or null when the table is empty.</returns>
        private ConversationDefinition ResolveSelectedConversationOrFallback()
        {
            if (_table == null)
            {
                _selectedConversationIdHex = string.Empty;
                return null;
            }

            if (_table.TryGetConversation(
                    GetSelectedConversationId(),
                    out ConversationDefinition selectedConversation))
            {
                return selectedConversation;
            }

            if (_table.Conversations != null && _table.Conversations.Count > 0)
            {
                ConversationDefinition firstConversation = _table.Conversations[0];
                _selectedConversationIdHex = firstConversation?.ConversationId.ToHexString()
                    ?? string.Empty;
                return firstConversation;
            }

            _selectedConversationIdHex = string.Empty;
            return null;
        }

        /// <summary>
        /// Refreshes the table controls and selected-conversation workspace header.
        /// </summary>
        /// <param name="selectedConversation">Conversation currently bound to the workspace.</param>
        private void RefreshWindowState(ConversationDefinition selectedConversation)
        {
            _tableField?.SetValueWithoutNotify(_table);

            if (_tableSummaryLabel != null)
            {
                _tableSummaryLabel.text = _table == null
                    ? "Choose a table to get started."
                    : BuildTableSummaryLabel();
            }

            if (_tableDisplayNameField != null)
            {
                _tableDisplayNameField.SetEnabled(_table != null);
                _tableDisplayNameField.SetValueWithoutNotify(
                    _table?.AuthoredDisplayName ?? string.Empty);
            }

            if (_inputContinueActionField != null)
            {
                _inputContinueActionField.SetEnabled(_table != null);
                _inputContinueActionField.SetValueWithoutNotify(_table?.AuthoredContinueAction);
            }

            if (_defaultPresenterPrefabField != null)
            {
                _defaultPresenterPrefabField.SetEnabled(_table != null);
                _defaultPresenterPrefabField.SetValueWithoutNotify(_table?.DefaultPresenterPrefab);
            }

            if (_inputCancelActionField != null)
            {
                _inputCancelActionField.SetEnabled(_table != null);
                _inputCancelActionField.SetValueWithoutNotify(_table?.AuthoredCancelAction);
            }

            if (_inputSkipActionField != null)
            {
                _inputSkipActionField.SetEnabled(_table != null);
                _inputSkipActionField.SetValueWithoutNotify(_table?.AuthoredSkipAction);
            }

            if (_conversationSelectorButton != null)
            {
                _conversationSelectorButton.text = selectedConversation == null
                    ? (_table == null ? "Choose Conversation" : "No Conversations Yet")
                    : BuildConversationDisplayPath(selectedConversation);
                _conversationSelectorButton.SetEnabled(_table != null);
            }

            _createConversationButton?.SetEnabled(_table != null);
            _deleteConversationButton?.SetEnabled(selectedConversation != null);
            _createNodeButton?.SetEnabled(selectedConversation != null);
            _frameAllButton?.SetEnabled(selectedConversation != null);

            if (_conversationTitleField != null)
            {
                _conversationTitleField.SetEnabled(selectedConversation != null);
                _conversationTitleField.SetValueWithoutNotify(selectedConversation?.Title ?? string.Empty);
            }

            if (_conversationWorkspaceTitleLabel != null)
            {
                _conversationWorkspaceTitleLabel.text = selectedConversation == null
                    ? (_table == null ? "Choose a Table First" : "No Conversation Selected")
                    : BuildConversationWorkspaceTitle(selectedConversation);
            }

            if (_conversationWorkspaceHintLabel != null)
            {
                _conversationWorkspaceHintLabel.text = selectedConversation == null
                    ? (_table == null
                        ? "Choose the conversation table you want to work on above."
                        : "Pick a conversation from the list above or create a new one.")
                    : "Use '/' or '|' to group this conversation in the list. The last part becomes its name.";
            }

            RefreshTabState();
        }

        /// <summary>
        /// Updates the visible tab content and button styling.
        /// </summary>
        private void RefreshTabState()
        {
            if (_conversationTabRoot != null)
            {
                _conversationTabRoot.style.display = _selectedTab == WindowTab.Conversations
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_presentationTabRoot != null)
            {
                _presentationTabRoot.style.display = _selectedTab == WindowTab.Presentation
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_conversantsView != null)
            {
                _conversantsView.style.display = _selectedTab == WindowTab.Conversants
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_inputTabRoot != null)
            {
                _inputTabRoot.style.display = _selectedTab == WindowTab.Input
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_validationTabRoot != null)
            {
                _validationTabRoot.style.display = _selectedTab == WindowTab.Validation
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            UpdateTabButtonState(_conversationsTabButton, WindowTab.Conversations);
            UpdateTabButtonState(_presentationTabButton, WindowTab.Presentation);
            UpdateTabButtonState(_inputTabButton, WindowTab.Input);
            UpdateTabButtonState(_conversantsTabButton, WindowTab.Conversants);
            UpdateTabButtonState(_validationTabButton, WindowTab.Validation);
            UpdateValidationTabStatusIndicator();
        }

        /// <summary>
        /// Creates the custom Validation tab button with one inline severity indicator.
        /// </summary>
        /// <returns>The configured Validation tab button.</returns>
        private Button CreateValidationTabButton()
        {
            Button button = CreateTabButton(string.Empty, WindowTab.Validation);

            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            _validationTabStatusIndicator = new VisualElement();
            _validationTabStatusIndicator.style.width = 8f;
            _validationTabStatusIndicator.style.minWidth = 8f;
            _validationTabStatusIndicator.style.height = 8f;
            _validationTabStatusIndicator.style.marginRight = 6f;
            _validationTabStatusIndicator.style.borderTopLeftRadius = 99f;
            _validationTabStatusIndicator.style.borderTopRightRadius = 99f;
            _validationTabStatusIndicator.style.borderBottomLeftRadius = 99f;
            _validationTabStatusIndicator.style.borderBottomRightRadius = 99f;
            row.Add(_validationTabStatusIndicator);

            _validationTabLabel = new Label("Validation");
            _validationTabLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(_validationTabLabel);

            button.Add(row);
            UpdateValidationTabStatusIndicator();
            return button;
        }

        /// <summary>
        /// Creates one styled tab button.
        /// </summary>
        /// <param name="text">Tab label.</param>
        /// <param name="tab">Tab represented by the button.</param>
        /// <returns>The configured tab button.</returns>
        private Button CreateTabButton(string text, WindowTab tab)
        {
            Button button = new(() => SetSelectedTab(tab))
            {
                text = text,
            };

            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.paddingLeft = 12f;
            button.style.paddingRight = 12f;
            button.style.paddingTop = 6f;
            button.style.paddingBottom = 6f;
            button.style.borderTopLeftRadius = 8f;
            button.style.borderTopRightRadius = 8f;
            button.style.borderBottomLeftRadius = 8f;
            button.style.borderBottomRightRadius = 8f;
            button.style.borderLeftWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderTopWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftColor = SectionBorderColor;
            button.style.borderRightColor = SectionBorderColor;
            button.style.borderTopColor = SectionBorderColor;
            button.style.borderBottomColor = SectionBorderColor;
            return button;
        }

        /// <summary>
        /// Stores the current tab and refreshes the visible content.
        /// </summary>
        /// <param name="tab">New selected tab.</param>
        private void SetSelectedTab(WindowTab tab)
        {
            if (_selectedTab == tab)
            {
                return;
            }

            _selectedTab = tab;
            RefreshTabState();

            if (tab == WindowTab.Validation)
            {
                _validationView?.RequestValidation(immediate: true);
            }

            SavePersistedWindowState();
        }

        /// <summary>
        /// Builds the short summary shown next to the table field.
        /// </summary>
        /// <returns>The summary text for the currently bound table.</returns>
        private string BuildTableSummaryLabel()
        {
            if (_table == null)
            {
                return "Choose a table to get started.";
            }

            int actorCount = _table.Actors?.Count ?? 0;
            string actorLabel = actorCount == 1
                ? "1 conversant"
                : $"{actorCount} conversants";

            return $"{_table.Conversations.Count} conversations • {actorLabel}";
        }

        /// <summary>
        /// Restores the previously used table, tab, and selection state.
        /// </summary>
        private void RestorePersistedWindowState()
        {
            int persistedTabValue = EditorPrefs.GetInt(
                PersistedTabPrefKey,
                (int)_selectedTab);
            _selectedTab = Enum.IsDefined(typeof(WindowTab), persistedTabValue)
                ? (WindowTab)persistedTabValue
                : WindowTab.Conversations;
            _selectedConversationIdHex = EditorPrefs.GetString(
                PersistedConversationIdPrefKey,
                _selectedConversationIdHex ?? string.Empty);
            _selectedActorIdHex = EditorPrefs.GetString(
                PersistedActorIdPrefKey,
                _selectedActorIdHex ?? string.Empty);
            _selectedNodeIdHex = EditorPrefs.GetString(
                PersistedNodeIdPrefKey,
                _selectedNodeIdHex ?? string.Empty);

            if (_table != null)
            {
                _tableGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_table).ToString();
                return;
            }

            _tableGlobalId = EditorPrefs.GetString(
                PersistedTableGlobalIdPrefKey,
                _tableGlobalId ?? string.Empty);

            if (string.IsNullOrWhiteSpace(_tableGlobalId)
                || !GlobalObjectId.TryParse(_tableGlobalId, out GlobalObjectId globalObjectId))
            {
                return;
            }

            _table = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId)
                as ConversationTable;

            if (_table == null)
            {
                _tableGlobalId = string.Empty;
            }
        }

        /// <summary>
        /// Persists the current table, tab, and selection state for future openings.
        /// </summary>
        private void SavePersistedWindowState()
        {
            _tableGlobalId = _table == null
                ? string.Empty
                : GlobalObjectId.GetGlobalObjectIdSlow(_table).ToString();

            EditorPrefs.SetString(PersistedTableGlobalIdPrefKey, _tableGlobalId);
            EditorPrefs.SetInt(PersistedTabPrefKey, (int)_selectedTab);
            EditorPrefs.SetString(
                PersistedConversationIdPrefKey,
                _selectedConversationIdHex ?? string.Empty);
            EditorPrefs.SetString(
                PersistedActorIdPrefKey,
                _selectedActorIdHex ?? string.Empty);
            EditorPrefs.SetString(
                PersistedNodeIdPrefKey,
                _selectedNodeIdHex ?? string.Empty);
        }

        /// <summary>
        /// Updates one tab button to reflect the currently selected surface.
        /// </summary>
        /// <param name="button">Button that should be refreshed.</param>
        /// <param name="tab">Tab represented by the button.</param>
        private void UpdateTabButtonState(Button button, WindowTab tab)
        {
            if (button == null)
            {
                return;
            }

            bool isSelected = _selectedTab == tab;
            button.style.backgroundColor = isSelected
                ? ActiveTabColor
                : InactiveTabColor;
            button.style.color = isSelected
                ? Color.white
                : MutedTextColor;

            if (tab == WindowTab.Validation && _validationTabLabel != null)
            {
                _validationTabLabel.style.color = isSelected
                    ? Color.white
                    : MutedTextColor;
            }
        }

        /// <summary>
        /// Stores the latest validation summary and refreshes the tab indicator.
        /// </summary>
        /// <param name="summary">Current validation summary.</param>
        private void HandleValidationSummaryChanged(ConversationValidationSummary summary)
        {
            _validationSummary = summary;
            UpdateValidationTabStatusIndicator();
        }

        /// <summary>
        /// Navigates from one validation issue back into the owning editor surface.
        /// </summary>
        /// <param name="issue">Issue that requested navigation.</param>
        private void HandleValidationNavigateRequested(ConversationValidationIssue issue)
        {
            if (_table == null)
            {
                return;
            }

            if (issue.ActorId != SerializableGuid.Empty)
            {
                _selectedActorIdHex = issue.ActorId.ToHexString();
                _selectedTab = WindowTab.Conversants;
                ApplyBinding();
                return;
            }

            if (issue.ConversationId != SerializableGuid.Empty)
            {
                _selectedConversationIdHex = issue.ConversationId.ToHexString();
            }

            _selectedNodeIdHex = issue.NodeId == SerializableGuid.Empty
                ? string.Empty
                : issue.NodeId.ToHexString();

            _selectedTab = issue.ConversationId == SerializableGuid.Empty
                ? WindowTab.Validation
                : WindowTab.Conversations;

            ApplyBinding();

            if (_selectedTab == WindowTab.Conversations
                && issue.NodeId != SerializableGuid.Empty)
            {
                FrameSelectedNodeAfterBinding();
            }
        }

        /// <summary>
        /// Refreshes the Validation tab indicator from the current summary.
        /// </summary>
        private void UpdateValidationTabStatusIndicator()
        {
            if (_validationTabStatusIndicator == null)
            {
                return;
            }

            if (_table == null)
            {
                _validationTabStatusIndicator.style.display = DisplayStyle.None;

                if (_validationTabButton != null)
                {
                    _validationTabButton.tooltip = "Validation";
                }

                return;
            }

            _validationTabStatusIndicator.style.display = DisplayStyle.Flex;
            _validationTabStatusIndicator.style.backgroundColor =
                _validationSummary.ErrorCount > 0
                    ? ValidationErrorColor
                    : _validationSummary.WarningCount > 0
                        ? ValidationWarningColor
                        : _validationSummary.InfoCount > 0
                            ? ValidationInfoColor
                            : ValidationSuccessColor;

            if (_validationTabButton != null)
            {
                _validationTabButton.tooltip = _validationSummary.HasIssues
                    ? $"{_validationSummary.ErrorCount} error(s), {_validationSummary.WarningCount} warning(s), {_validationSummary.InfoCount} info item(s)"
                    : "No active validation issues.";
            }
        }

        /// <summary>
        /// Frames the currently selected node after the graph binding finishes rebuilding.
        /// </summary>
        private void FrameSelectedNodeAfterBinding()
        {
            rootVisualElement.schedule.Execute(() => _graphView?.FrameSelection()).ExecuteLater(0);
        }

        /// <summary>
        /// Builds the display path shown by the workspace header and selector button.
        /// </summary>
        /// <param name="conversation">Conversation that should be represented.</param>
        /// <returns>The authored title path.</returns>
        private static string BuildConversationDisplayPath(ConversationDefinition conversation)
        {
            return string.IsNullOrWhiteSpace(conversation?.Title)
                ? "Conversation"
                : NormalizeConversationPath(conversation.Title);
        }

        /// <summary>
        /// Builds the leaf title shown by the workspace header.
        /// </summary>
        /// <param name="conversation">Conversation that should be represented.</param>
        /// <returns>The leaf title extracted from the authored path.</returns>
        private static string BuildConversationWorkspaceTitle(ConversationDefinition conversation)
        {
            string normalizedPath = BuildConversationDisplayPath(conversation);
            int separatorIndex = normalizedPath.LastIndexOf('/');

            return separatorIndex < 0
                ? normalizedPath
                : normalizedPath[(separatorIndex + 1)..];
        }

        /// <summary>
        /// Builds the menu path consumed by Unity's GenericMenu nested item API.
        /// </summary>
        /// <param name="conversation">Conversation that should be represented.</param>
        /// <returns>The nested menu path.</returns>
        private static string BuildConversationMenuPath(ConversationDefinition conversation)
        {
            return BuildConversationDisplayPath(conversation);
        }

        /// <summary>
        /// Builds the final selector label, disambiguating duplicate paths when needed.
        /// </summary>
        /// <param name="conversation">Conversation that should be represented.</param>
        /// <param name="menuPathCounts">Number of conversations that resolve to each menu path.</param>
        /// <returns>The final selector label.</returns>
        private static string BuildConversationSelectionMenuLabel(
            ConversationDefinition conversation,
            IReadOnlyDictionary<string, int> menuPathCounts)
        {
            string menuPath = BuildConversationMenuPath(conversation);

            if (!menuPathCounts.TryGetValue(menuPath, out int count) || count <= 1)
            {
                return menuPath;
            }

            string shortId = conversation?.ConversationId.ToHexString() ?? string.Empty;
            shortId = shortId.Length > 8
                ? shortId[..8]
                : shortId;

            return string.IsNullOrWhiteSpace(shortId)
                ? menuPath
                : $"{menuPath} [{shortId}]";
        }

        /// <summary>
        /// Normalizes one authored path into the canonical menu/display format.
        /// </summary>
        /// <param name="path">Authored path that should be normalized.</param>
        /// <returns>The canonical path using '/' as the grouping separator.</returns>
        private static string NormalizeConversationPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? "Conversation"
                : path.Replace('|', '/').Trim();
        }

        /// <summary>
        /// Creates one shared section container used by the window chrome.
        /// </summary>
        /// <param name="backgroundColor">Background color applied to the section.</param>
        /// <returns>The styled section container.</returns>
        private static VisualElement CreateSectionContainer(Color backgroundColor)
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexShrink = 0f;
            container.style.paddingLeft = 10f;
            container.style.paddingRight = 10f;
            container.style.paddingTop = 8f;
            container.style.paddingBottom = 8f;
            container.style.backgroundColor = backgroundColor;
            container.style.borderBottomWidth = 1f;
            container.style.borderBottomColor = SectionBorderColor;
            return container;
        }

        /// <summary>
        /// Creates one small caption label used by section headers.
        /// </summary>
        /// <param name="text">Caption text.</param>
        /// <returns>The styled caption label.</returns>
        private static Label CreateSectionCaptionLabel(string text)
        {
            Label label = new(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 10f;
            label.style.color = MutedTextColor;
            return label;
        }

        /// <summary>
        /// Creates one muted helper label used by section headers.
        /// </summary>
        /// <param name="text">Helper text.</param>
        /// <returns>The styled helper label.</returns>
        private static Label CreateSectionHintLabel(string text)
        {
            Label label = new(text);
            label.style.color = MutedTextColor;
            label.style.fontSize = 11f;
            return label;
        }

        /// <summary>
        /// Creates the floating drag badge used during blackboard drag sessions.
        /// </summary>
        private void CreateDragBadge()
        {
            _dragBadge = new VisualElement();
            _dragBadge.pickingMode = PickingMode.Ignore;
            _dragBadge.style.position = Position.Absolute;
            _dragBadge.style.left = 0f;
            _dragBadge.style.top = 0f;
            _dragBadge.style.display = DisplayStyle.None;
            _dragBadge.style.paddingLeft = 10f;
            _dragBadge.style.paddingRight = 10f;
            _dragBadge.style.paddingTop = 6f;
            _dragBadge.style.paddingBottom = 6f;
            _dragBadge.style.backgroundColor = new StyleColor(
                new Color(0.18f, 0.25f, 0.20f, 0.96f));
            _dragBadge.style.borderLeftWidth = 1f;
            _dragBadge.style.borderRightWidth = 1f;
            _dragBadge.style.borderTopWidth = 1f;
            _dragBadge.style.borderBottomWidth = 1f;
            _dragBadge.style.borderLeftColor = new Color(0.49f, 0.65f, 0.54f, 0.98f);
            _dragBadge.style.borderRightColor = new Color(0.49f, 0.65f, 0.54f, 0.98f);
            _dragBadge.style.borderTopColor = new Color(0.49f, 0.65f, 0.54f, 0.98f);
            _dragBadge.style.borderBottomColor = new Color(0.49f, 0.65f, 0.54f, 0.98f);
            _dragBadge.style.borderTopLeftRadius = 10f;
            _dragBadge.style.borderTopRightRadius = 10f;
            _dragBadge.style.borderBottomLeftRadius = 10f;
            _dragBadge.style.borderBottomRightRadius = 10f;

            _dragBadgeLabel = new Label();
            _dragBadgeLabel.style.color = new Color(0.92f, 0.94f, 0.92f, 0.98f);
            _dragBadgeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _dragBadgeLabel.style.fontSize = 11f;
            _dragBadge.Add(_dragBadgeLabel);
        }

        /// <summary>
        /// Updates the drag badge position while the mouse moves across the window.
        /// </summary>
        /// <param name="evt">Mouse move event payload.</param>
        private void HandleWindowMouseMove(MouseMoveEvent evt)
        {
            UpdateDragBadge(evt.mousePosition);
        }

        /// <summary>
        /// Completes one active blackboard drag session when the inspector accepts the drop.
        /// </summary>
        /// <param name="evt">Mouse up event payload.</param>
        private void HandleWindowMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0 || !GraphBlackboardDragSession.HasActiveDrag)
            {
                return;
            }

            bool acceptedByInspector = _inspectorView?.TryPerformBlackboardSessionDrop(
                evt.mousePosition)
                ?? false;

            GraphBlackboardDragSession.CancelDrag();
            UpdateDragBadge(evt.mousePosition);

            if (acceptedByInspector)
            {
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Repositions and updates the floating blackboard drag badge.
        /// </summary>
        /// <param name="mousePosition">Current mouse position in panel space.</param>
        private void UpdateDragBadge(Vector2 mousePosition)
        {
            if (_dragBadge == null || _dragBadgeLabel == null)
            {
                return;
            }

            if (!GraphBlackboardDragSession.HasActiveDrag)
            {
                _dragBadge.style.display = DisplayStyle.None;
                return;
            }

            string entryLabel = string.IsNullOrWhiteSpace(GraphBlackboardDragSession.ActiveEntryLabel)
                ? "Blackboard Variable"
                : $"@{GraphBlackboardDragSession.ActiveEntryLabel}";
            _dragBadgeLabel.text = entryLabel;
            _dragBadge.style.display = DisplayStyle.Flex;

            float badgeWidth = _dragBadge.resolvedStyle.width;
            float badgeHeight = _dragBadge.resolvedStyle.height;
            Rect contentRect = rootVisualElement.contentRect;
            float badgeLeft = mousePosition.x + DragBadgePointerOffsetX;
            float badgeTop = mousePosition.y + DragBadgePointerOffsetY;

            if (badgeWidth > 0f && contentRect.width > 0f)
            {
                badgeLeft = Mathf.Min(
                    badgeLeft,
                    contentRect.width - badgeWidth - DragBadgeViewportPadding);
            }

            if (badgeHeight > 0f && contentRect.height > 0f)
            {
                badgeTop = Mathf.Min(
                    badgeTop,
                    contentRect.height - badgeHeight - DragBadgeViewportPadding);
            }

            _dragBadge.style.left = Mathf.Max(DragBadgeViewportPadding, badgeLeft);
            _dragBadge.style.top = Mathf.Max(DragBadgeViewportPadding, badgeTop);
        }

        /// <summary>
        /// Provides the Conversations node search-window contents.
        /// </summary>
        private sealed class ConversationNodeSearchProvider : ScriptableObject, ISearchWindowProvider
        {
            private const int LeafIndentWidth = 4;

            private ConversationGraphWindow _window;

            /// <summary>
            /// Binds the provider to the owning window.
            /// </summary>
            /// <param name="window">Owning Conversations window.</param>
            public void Initialize(ConversationGraphWindow window)
            {
                _window = window;
            }

            /// <inheritdoc />
            public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
            {
                List<SearchTreeEntry> entries = new()
                {
                    new SearchTreeGroupEntry(new GUIContent("Create Conversation Node"), 0),
                };

                HashSet<string> addedGroups = new(StringComparer.OrdinalIgnoreCase);
                IReadOnlyList<ConversationNodeCreationRegistry.NodeDescriptor> descriptors =
                    ConversationNodeCreationRegistry.Descriptors;

                for (int index = 0; index < descriptors.Count; index++)
                {
                    ConversationNodeCreationRegistry.NodeDescriptor descriptor = descriptors[index];
                    string[] pathSegments = descriptor.MenuPath.Split('/');
                    string groupPath = string.Empty;

                    for (int segmentIndex = 0; segmentIndex < pathSegments.Length - 1; segmentIndex++)
                    {
                        groupPath = string.IsNullOrWhiteSpace(groupPath)
                            ? pathSegments[segmentIndex]
                            : $"{groupPath}/{pathSegments[segmentIndex]}";

                        if (addedGroups.Add(groupPath))
                        {
                            entries.Add(new SearchTreeGroupEntry(
                                new GUIContent(pathSegments[segmentIndex]),
                                segmentIndex + 1));
                        }
                    }

                    entries.Add(new SearchTreeEntry(new GUIContent(
                        CreateLeafEntryLabel(pathSegments.Length - 1, descriptor.DisplayName)))
                    {
                        level = pathSegments.Length,
                        userData = descriptor,
                    });
                }

                return entries;
            }

            /// <inheritdoc />
            public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
            {
                if (searchTreeEntry.userData
                    is not ConversationNodeCreationRegistry.NodeDescriptor descriptor)
                {
                    return false;
                }

                _window?.CreateNodeFromSearch(descriptor.NodeType, context.screenMousePosition);
                return true;
            }

            /// <summary>
            /// Creates one indented label for one leaf entry.
            /// </summary>
            /// <param name="indentLevel">Hierarchy depth of the leaf entry.</param>
            /// <param name="label">Visible label for the entry.</param>
            /// <returns>The indented label string.</returns>
            private static string CreateLeafEntryLabel(int indentLevel, string label)
            {
                int indentWidth = (indentLevel < 0 ? 0 : indentLevel) * LeafIndentWidth;
                return string.Concat(new string(' ', indentWidth), label);
            }
        }
    }
}