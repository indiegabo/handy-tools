using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.Editor.CutscenesModule.Graph;
using IndieGabo.HandyTools.Editor.CutscenesModule.Validation;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    public sealed class CutsceneGraphWindow : EditorWindow
    {
        private const string WindowMenuPath = HandyToolsEditorMenuPaths.Root + "Cutscenes/Graph Editor";
        private const int DefaultGraphWidth = 720;
        private const float InspectorMinWidth = 300f;
        private const float MinimumWindowHeight = 560f;
        private const float BlackboardOverlayMargin = 12f;
        private const float BlackboardOverlayWidth = 160f;
        private const float BlackboardOverlayMaxHeight = 420f;
        private const float DragBadgePointerOffsetX = 10f;
        private const float DragBadgePointerOffsetY = -10f;
        private const float DragBadgeViewportPadding = 4f;

        [SerializeField]
        private CutsceneDirector _director;

        [SerializeField]
        private string _directorGlobalId;

        [SerializeField]
        private EntityId _directorEntityId = EntityId.None;

        [SerializeField]
        private string _selectedNodeIdHex;

        private CutsceneGraphView _graphView;
        private CutsceneGraphBlackboardView _blackboardView;
        private CutsceneGraphInspectorView _inspectorView;
        private HelpBox _validationBox;
        private VisualElement _dragBadge;
        private Label _dragBadgeLabel;
        private ObjectField _directorField;
        private CutsceneNodeSearchProvider _searchProvider;
        private Vector2 _pendingNodeCreationScreenPosition;
        private bool _hasPendingNodeCreationScreenPosition;
        private Vector2 _pendingNodeCreationGraphPosition;
        private bool _hasPendingNodeCreationGraphPosition;
        private SerializableGuid _pendingConnectionFromNodeId;
        private string _pendingConnectionOutputKey;
        private bool _hasPendingConnectionRequest;
        private bool _pendingNodeCreationFromConnectionDrop;
        private bool _hasRefreshedRuntimeStateDuringPlay;

        [MenuItem(WindowMenuPath, false, 20)]
        private static void OpenWindow()
        {
            Open(null);
        }

        public static void Open(CutsceneDirector director)
        {
            CutsceneGraphWindow window = GetWindow<CutsceneGraphWindow>();
            window.titleContent = new GUIContent("Cutscene Graph");
            window.minSize = new Vector2(
                DefaultGraphWidth + InspectorMinWidth,
                MinimumWindowHeight);
            window.Show();
            window.BindDirector(director);
        }

        private void OnEnable()
        {
            EditorApplication.update += HandleEditorUpdate;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            SyncDirectorBinding();
        }

        private void OnDisable()
        {
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;

            if (_searchProvider != null)
            {
                DestroyImmediate(_searchProvider);
            }
        }

        public void CreateGUI()
        {
            SyncDirectorBinding();

            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1f;

            rootVisualElement.Add(CreateToolbar());

            _validationBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            _validationBox.style.marginLeft = 8f;
            _validationBox.style.marginRight = 8f;
            _validationBox.style.marginBottom = 6f;
            _validationBox.style.display = DisplayStyle.None;
            rootVisualElement.Add(_validationBox);

            CreateDragBadge();

            TwoPaneSplitView splitView = new(
                1,
                InspectorMinWidth,
                TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;

            VisualElement graphCanvasPane = new();
            graphCanvasPane.style.flexGrow = 1f;
            graphCanvasPane.style.position = Position.Relative;

            _graphView = new CutsceneGraphView();
            _graphView.NodeSelected += HandleNodeSelected;
            _graphView.NodeCreationRequested += OpenNodeSearch;
            _graphView.ConnectedNodeCreationRequested += OpenConnectedNodeSearch;
            _graphView.GraphModified += HandleGraphModified;
            graphCanvasPane.Add(_graphView);

            _blackboardView = new CutsceneGraphBlackboardView();
            _blackboardView.BlackboardChanged += HandleGraphModified;
            _blackboardView.style.position = Position.Absolute;
            _blackboardView.style.left = BlackboardOverlayMargin;
            _blackboardView.style.top = BlackboardOverlayMargin;
            _blackboardView.style.width = BlackboardOverlayWidth;
            _blackboardView.style.maxHeight = BlackboardOverlayMaxHeight;
            graphCanvasPane.Add(_blackboardView);

            splitView.Add(graphCanvasPane);

            VisualElement inspectorPane = new();
            inspectorPane.style.flexDirection = FlexDirection.Column;
            inspectorPane.style.flexGrow = 1f;
            inspectorPane.style.minWidth = InspectorMinWidth;
            inspectorPane.style.paddingLeft = 8f;
            inspectorPane.style.paddingRight = 8f;
            inspectorPane.style.paddingTop = 8f;
            inspectorPane.style.paddingBottom = 8f;

            _inspectorView = new CutsceneGraphInspectorView();
            _inspectorView.InspectorChanged += HandleInspectorChanged;
            _inspectorView.InspectorPreviewChanged += HandleInspectorPreviewChanged;
            inspectorPane.Add(_inspectorView);
            splitView.Add(inspectorPane);

            rootVisualElement.UnregisterCallback<DragUpdatedEvent>(
                HandleWindowDragUpdated,
                TrickleDown.TrickleDown);
            rootVisualElement.UnregisterCallback<DragPerformEvent>(
                HandleWindowDragPerform,
                TrickleDown.TrickleDown);
            rootVisualElement.UnregisterCallback<MouseMoveEvent>(
                HandleWindowMouseMove,
                TrickleDown.TrickleDown);
            rootVisualElement.UnregisterCallback<MouseUpEvent>(
                HandleWindowMouseUp,
                TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<DragUpdatedEvent>(
                HandleWindowDragUpdated,
                TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<DragPerformEvent>(
                HandleWindowDragPerform,
                TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<MouseMoveEvent>(
                HandleWindowMouseMove,
                TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<MouseUpEvent>(
                HandleWindowMouseUp,
                TrickleDown.TrickleDown);

            rootVisualElement.Add(splitView);
            rootVisualElement.Add(_dragBadge);

            ApplyDirectorBinding();
        }

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
                new Color(0.18f, 0.18f, 0.18f, 0.96f));
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
            _dragBadge.style.opacity = 0.98f;

            _dragBadgeLabel = new Label();
            _dragBadgeLabel.style.color = new Color(0.92f, 0.94f, 0.92f, 0.98f);
            _dragBadgeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _dragBadgeLabel.style.fontSize = 11f;
            _dragBadge.Add(_dragBadgeLabel);
        }

        private VisualElement CreateToolbar()
        {
            VisualElement toolbarContainer = new();
            toolbarContainer.style.flexDirection = FlexDirection.Column;

            Toolbar configurationToolbar = new();
            Toolbar actionsToolbar = new();

            _directorField = new ObjectField("Director")
            {
                objectType = typeof(CutsceneDirector),
                allowSceneObjects = true,
            };
            _directorField.RegisterValueChangedCallback(evt => BindDirector(evt.newValue as CutsceneDirector));
            configurationToolbar.Add(_directorField);

            Button useSelectedButton = new(() => BindDirector(CutsceneEditorUtility.GetSelectedDirector()))
            {
                text = "Use Selected",
            };
            configurationToolbar.Add(useSelectedButton);

            Button createNodeButton = new(OpenToolbarNodeSearch)
            {
                text = "Create Node",
            };
            actionsToolbar.Add(createNodeButton);

            Button autoArrangeButton = new(HandleAutoArrangeRequested)
            {
                text = "Auto Arrange",
            };
            actionsToolbar.Add(autoArrangeButton);

            Button frameAllButton = new(() => _graphView?.FrameAll())
            {
                text = "Frame All",
            };
            actionsToolbar.Add(frameAllButton);

            Button validateButton = new(RefreshValidationSummary)
            {
                text = "Validate",
            };
            actionsToolbar.Add(validateButton);

            toolbarContainer.Add(configurationToolbar);
            toolbarContainer.Add(actionsToolbar);

            return toolbarContainer;
        }

        private void BindDirector(CutsceneDirector director)
        {
            _director = director;

            if (_director != null)
            {
                _directorGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_director).ToString();
                _directorEntityId = GetEntityId(_director);
            }
            else
            {
                _directorGlobalId = string.Empty;
                _directorEntityId = EntityId.None;
            }

            ApplyDirectorBinding();
        }

        private void ApplyDirectorBinding()
        {
            if (_directorField != null)
            {
                _directorField.SetValueWithoutNotify(_director);
            }

            _graphView?.BindDirector(_director);
            _blackboardView?.BindDirector(_director);
            _inspectorView?.BindSelection(_director, GetSelectedNodeId());
            RefreshValidationSummary();
        }

        private bool RestoreBoundDirector()
        {
            CutsceneDirector previousDirector = _director;
            string previousDirectorGlobalId = _directorGlobalId;

            if (string.IsNullOrWhiteSpace(_directorGlobalId)
                && !HasEntityId(_directorEntityId)
                && _director != null)
            {
                _directorGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_director).ToString();
                _directorEntityId = GetEntityId(_director);
            }

            if (string.IsNullOrWhiteSpace(_directorGlobalId)
                && !HasEntityId(_directorEntityId))
            {
                return !ReferenceEquals(previousDirector, _director)
                    || !string.Equals(
                        previousDirectorGlobalId,
                        _directorGlobalId,
                        StringComparison.Ordinal);
            }

            if (TryResolveDirectorByGlobalId(
                    _directorGlobalId,
                    _directorEntityId,
                    out CutsceneDirector resolvedDirector))
            {
                _director = resolvedDirector;
            }
            else
            {
                _director = null;
                _directorGlobalId = string.Empty;
                _directorEntityId = EntityId.None;
            }

            if (_director != null)
            {
                _directorGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_director).ToString();
                _directorEntityId = GetEntityId(_director);
            }

            return !ReferenceEquals(previousDirector, _director)
                || !string.Equals(
                    previousDirectorGlobalId,
                    _directorGlobalId,
                    StringComparison.Ordinal);
        }

        private static bool TryResolveDirectorByGlobalId(
            string directorGlobalId,
            EntityId directorEntityId,
            out CutsceneDirector director)
        {
            director = null;
            GlobalObjectId globalObjectId = default;
            bool hasPersistedGlobalId = !string.IsNullOrWhiteSpace(directorGlobalId)
                && GlobalObjectId.TryParse(directorGlobalId, out globalObjectId);

            if (hasPersistedGlobalId
                && TryResolveDirectorFromObject(
                    GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId),
                    out director))
            {
                return true;
            }

            if (HasEntityId(directorEntityId)
                && TryResolveDirectorFromObject(
                    EditorUtility.EntityIdToObject(directorEntityId),
                    out director))
            {
                return true;
            }

            CutsceneDirector[] candidates = Resources.FindObjectsOfTypeAll<CutsceneDirector>();

            for (int index = 0; index < candidates.Length; index++)
            {
                CutsceneDirector candidate = candidates[index];

                if (candidate == null)
                {
                    continue;
                }

                if (HasEntityId(directorEntityId)
                    && (GetEntityId(candidate).Equals(directorEntityId)
                        || GetEntityId(candidate.gameObject).Equals(directorEntityId)))
                {
                    director = candidate;
                    return true;
                }

                if (!hasPersistedGlobalId)
                {
                    continue;
                }

                string candidateGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(candidate).ToString();

                if (string.Equals(candidateGlobalId, directorGlobalId, StringComparison.Ordinal))
                {
                    director = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveDirectorFromObject(
            UnityEngine.Object source,
            out CutsceneDirector director)
        {
            if (source is CutsceneDirector resolvedDirector)
            {
                director = resolvedDirector;
                return true;
            }

            if (source is GameObject resolvedGameObject
                && resolvedGameObject.TryGetComponent(out CutsceneDirector resolvedFromGameObject))
            {
                director = resolvedFromGameObject;
                return true;
            }

            director = null;
            return false;
        }

        private static EntityId GetEntityId(UnityEngine.Object source)
        {
            return source != null ? source.GetEntityId() : EntityId.None;
        }

        private static bool HasEntityId(EntityId entityId)
        {
            return !EqualityComparer<EntityId>.Default.Equals(entityId, EntityId.None);
        }

        private void SyncDirectorBinding()
        {
            bool bindingChanged = RestoreBoundDirector();

            if (bindingChanged)
            {
                ApplyDirectorBinding();
                return;
            }

            _graphView?.RefreshRuntimeState();
            _inspectorView?.Refresh();
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode
                && state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            SyncDirectorBinding();
        }

        private void HandleNodeSelected(CutsceneGraphNodeView nodeView)
        {
            _selectedNodeIdHex = nodeView == null
                ? string.Empty
                : nodeView.Node.Id.ToHexString();
            _inspectorView?.BindSelection(_director, GetSelectedNodeId());
        }

        private void HandleGraphModified()
        {
            _inspectorView?.Refresh();
            RefreshValidationSummary();
        }

        private void HandleWindowMouseMove(MouseMoveEvent evt)
        {
            if (!CutsceneBlackboardDragAndDrop.HasActiveDrag)
            {
                UpdateDragBadge(evt.mousePosition);
                return;
            }

            _inspectorView?.UpdateCustomBlackboardDrag(evt.mousePosition);
            UpdateDragBadge(evt.mousePosition);
        }

        private void HandleWindowMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0 || !CutsceneBlackboardDragAndDrop.HasActiveDrag)
            {
                return;
            }

            _inspectorView?.TryPerformCustomBlackboardDrop(evt.mousePosition);
            CutsceneBlackboardDragAndDrop.CancelDrag();
            _inspectorView?.UpdateCustomBlackboardDrag(evt.mousePosition);
            UpdateDragBadge(evt.mousePosition);
        }

        private void UpdateDragBadge(Vector2 mousePosition)
        {
            if (_dragBadge == null || _dragBadgeLabel == null)
            {
                return;
            }

            if (!CutsceneBlackboardDragAndDrop.HasActiveDrag)
            {
                _dragBadge.style.display = DisplayStyle.None;
                return;
            }

            string entryLabel = string.IsNullOrWhiteSpace(
                CutsceneBlackboardDragAndDrop.ActiveEntryLabel)
                ? "Blackboard Variable"
                : $"@{CutsceneBlackboardDragAndDrop.ActiveEntryLabel}";
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

        private void HandleWindowDragUpdated(DragUpdatedEvent evt)
        {
            if (_inspectorView == null
                || !_inspectorView.TryHandleGlobalDrag(evt.mousePosition, performDrag: false))
            {
                return;
            }

            evt.StopImmediatePropagation();
        }

        private void HandleWindowDragPerform(DragPerformEvent evt)
        {
            if (_inspectorView == null
                || !_inspectorView.TryHandleGlobalDrag(evt.mousePosition, performDrag: true))
            {
                return;
            }

            evt.StopImmediatePropagation();
        }

        private void HandleInspectorChanged(SerializableGuid nodeId)
        {
            _graphView?.RebuildGraph(nodeId);
            HandleGraphModified();
        }

        private void HandleInspectorPreviewChanged(SerializableGuid nodeId)
        {
            _graphView?.RefreshNodePresentation(nodeId);
        }

        private SerializableGuid GetSelectedNodeId()
        {
            return string.IsNullOrWhiteSpace(_selectedNodeIdHex)
                ? SerializableGuid.Empty
                : SerializableGuid.FromHexString(_selectedNodeIdHex);
        }

        private void HandleEditorUpdate()
        {
            if (_graphView == null)
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                _graphView.RefreshRuntimeState();
                _hasRefreshedRuntimeStateDuringPlay = true;
                return;
            }

            if (!_hasRefreshedRuntimeStateDuringPlay)
            {
                return;
            }

            _hasRefreshedRuntimeStateDuringPlay = false;
            _graphView.RefreshRuntimeState();
        }

        private void HandleAutoArrangeRequested()
        {
            if (_director == null)
            {
                ShowNotification(new GUIContent("Bind a CutsceneDirector before arranging nodes."));
                return;
            }

            _graphView?.AutoArrangeNodes();
        }

        private void OpenNodeSearch(Vector2 screenPosition)
        {
            if (_director == null)
            {
                ShowNotification(new GUIContent("Bind a CutsceneDirector before creating nodes."));
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

            _searchProvider ??= CreateInstance<CutsceneNodeSearchProvider>();
            _searchProvider.Initialize(this);
            SearchWindow.Open(
                new SearchWindowContext(_pendingNodeCreationScreenPosition),
                _searchProvider);
        }

        private void OpenConnectedNodeSearch(CutsceneGraphView.ConnectedNodeCreationRequest request)
        {
            if (_director == null)
            {
                ShowNotification(new GUIContent("Bind a CutsceneDirector before creating nodes."));
                return;
            }

            _pendingNodeCreationScreenPosition = GraphViewLocalToScreenPosition(
                request.ScreenPosition);
            _hasPendingNodeCreationScreenPosition = true;
            _pendingNodeCreationGraphPosition = _graphView.contentViewContainer
                .WorldToLocal(request.ScreenPosition);
            _hasPendingNodeCreationGraphPosition = true;
            _pendingConnectionFromNodeId = request.FromNodeId;
            _pendingConnectionOutputKey = request.OutputKey;
            _hasPendingConnectionRequest = true;
            _pendingNodeCreationFromConnectionDrop = true;

            _searchProvider ??= CreateInstance<CutsceneNodeSearchProvider>();
            _searchProvider.Initialize(this);
            SearchWindow.Open(
                new SearchWindowContext(_pendingNodeCreationScreenPosition),
                _searchProvider);
        }

        private void OpenToolbarNodeSearch()
        {
            if (_graphView == null)
            {
                return;
            }

            OpenNodeSearch(_graphView.contentRect.center);
        }

        private void CreateNodeFromSearch(Type nodeType, Vector2 fallbackScreenPosition)
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

        internal void CreateNode(
            Type nodeType,
            Vector2 screenPosition,
            SerializableGuid connectFromNodeId = default,
            string connectOutputKey = null,
            bool fromConnectionDrop = false,
            Vector2? graphPositionOverride = null)
        {
            if (_director == null)
            {
                return;
            }

            CutsceneNodeBase node = CutsceneNodeCreationRegistry.CreateNode(nodeType);

            if (node == null)
            {
                return;
            }

            Vector2 graphPosition = graphPositionOverride
                ?? ScreenPointToGraphPosition(screenPosition);
            node.Position = graphPosition;

            CutsceneEditorUtility.RecordDirectorChange(
                _director,
                connectFromNodeId != default
                    ? "Add Connected Cutscene Node"
                    : "Add Cutscene Node");
            _director.Graph.AddNode(node);
            _director.Graph.EnsureNodeIds();

            if (connectFromNodeId != default)
            {
                _director.Graph.Connect(
                    connectFromNodeId,
                    string.IsNullOrWhiteSpace(connectOutputKey)
                        ? CutsceneNodePorts.Next
                        : connectOutputKey,
                    node.Id);
            }

            _selectedNodeIdHex = node.Id.ToHexString();
            _graphView.RebuildGraph(node.Id);

            if (fromConnectionDrop)
            {
                _graphView.AlignNodeInputPortToDropPosition(node.Id);
            }

            RefreshValidationSummary();
            _inspectorView?.BindSelection(_director, node.Id);
        }

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

        private void RefreshValidationSummary()
        {
            if (_validationBox == null)
            {
                return;
            }

            if (_director == null)
            {
                _validationBox.style.display = DisplayStyle.None;
                return;
            }

            IReadOnlyList<CutsceneGraphValidationIssue> issues = CutsceneGraphValidator.Validate(_director);

            if (issues.Count == 0)
            {
                _validationBox.style.display = DisplayStyle.None;
                return;
            }

            int errorCount = 0;
            int warningCount = 0;

            for (int index = 0; index < issues.Count; index++)
            {
                switch (issues[index].Severity)
                {
                    case CutsceneGraphValidationSeverity.Error:
                        errorCount++;
                        break;

                    case CutsceneGraphValidationSeverity.Warning:
                        warningCount++;
                        break;
                }
            }

            _validationBox.text = $"Validation found {errorCount} error(s) and {warningCount} warning(s). Select the director or a node to inspect details.";
            _validationBox.messageType = errorCount > 0
                ? HelpBoxMessageType.Error
                : HelpBoxMessageType.Warning;
            _validationBox.style.display = DisplayStyle.Flex;
        }

        private sealed class CutsceneNodeSearchProvider : ScriptableObject, ISearchWindowProvider
        {
            private const int LeafIndentWidth = 4;

            private CutsceneGraphWindow _window;

            public void Initialize(CutsceneGraphWindow window)
            {
                _window = window;
            }

            public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
            {
                List<SearchTreeEntry> entries = new()
                {
                    new SearchTreeGroupEntry(new GUIContent("Create Cutscene Node"), 0),
                };

                HashSet<string> addedGroups = new(StringComparer.OrdinalIgnoreCase);
                IReadOnlyList<CutsceneNodeCreationRegistry.NodeDescriptor> descriptors =
                    CutsceneNodeCreationRegistry.GetCreateableDescriptors();

                for (int index = 0; index < descriptors.Count; index++)
                {
                    CutsceneNodeCreationRegistry.NodeDescriptor descriptor = descriptors[index];
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
                        CreateLeafEntryLabel(pathSegments.Length - 1, descriptor.DisplayName),
                        descriptor.Icon))
                    {
                        level = pathSegments.Length,
                        userData = descriptor,
                    });
                }

                return entries;
            }

            private static string CreateLeafEntryLabel(int indentLevel, string label)
            {
                int indentWidth = (indentLevel < 0 ? 0 : indentLevel) * LeafIndentWidth;
                return string.Concat(new string(' ', indentWidth), label);
            }

            public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
            {
                if (searchTreeEntry.userData is not CutsceneNodeCreationRegistry.NodeDescriptor descriptor)
                {
                    return false;
                }

                _window?.CreateNodeFromSearch(descriptor.NodeType, context.screenMousePosition);
                return true;
            }
        }
    }
}