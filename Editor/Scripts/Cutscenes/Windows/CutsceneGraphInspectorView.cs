using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Meta;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.Editor.CutscenesModule.Validation;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    /// <summary>
    /// UI Toolkit inspector surface hosted by the cutscene graph window.
    /// Shows one director summary when no node is selected and shows the
    /// selected node serialized fields otherwise.
    /// </summary>
    public sealed class CutsceneGraphInspectorView : VisualElement
    {
        private const string BaseFieldLabelClassName = "unity-base-field__label";
        private const string BaseFieldInputClassName = "unity-base-field__input";
        private const string LiveCommentTextRelayClassName =
            "handytools-cutscene-live-comment-text-relay";
        private const float CompactPropertyLabelWidth = 72f;
        private const float CompactPropertyLabelGap = 6f;

        private readonly struct DragRelayField
        {
            public DragRelayField(PropertyField field, string propertyPath)
            {
                Field = field;
                PropertyPath = propertyPath;
            }

            public PropertyField Field { get; }

            public string PropertyPath { get; }
        }

        private readonly struct BlackboardDropZoneField
        {
            public BlackboardDropZoneField(VisualElement dropZone, string propertyPath)
            {
                DropZone = dropZone;
                PropertyPath = propertyPath;
            }

            public VisualElement DropZone { get; }

            public string PropertyPath { get; }
        }

        private static readonly Color DropZoneIdleColor = new(0.32f, 0.32f, 0.32f, 0.95f);
        private static readonly Color DropZoneReadyColor = new(0.25f, 0.40f, 0.29f, 0.98f);
        private static readonly Color DropZoneHoverColor = new(0.31f, 0.55f, 0.36f, 1f);

        private readonly ScrollView _content;
        private readonly List<DragRelayField> _dragRelayFields = new();
        private readonly List<BlackboardDropZoneField> _blackboardDropZones = new();
        private readonly DeferredGraphUiActionDispatcher _inspectorChangedDispatcher;
        private readonly DeferredGraphUiActionDispatcher _refreshDispatcher;

        private CutsceneDirector _director;
        private SerializableGuid _selectedNodeId;
        private bool _isRefreshing;
        private bool _hasPendingLiveCommentPreview;
        private string _pendingLiveCommentPropertyPath;
        private string _pendingLiveCommentValue;

        /// <summary>
        /// Raised after one node field change is committed.
        /// </summary>
        public event Action<SerializableGuid> InspectorChanged;

        /// <summary>
        /// Raised when one comment node text field changes and the graph node
        /// preview should refresh without rebuilding the whole graph.
        /// </summary>
        public event Action<SerializableGuid> InspectorPreviewChanged;

        /// <summary>
        /// Creates the hosted UI Toolkit hierarchy.
        /// </summary>
        public CutsceneGraphInspectorView()
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1f;
            style.marginTop = 8f;
            style.minWidth = 0f;

            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.style.flexGrow = 1f;
            _content.style.minWidth = 0f;
            _content.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _content.contentContainer.style.minWidth = 0f;
            _content.contentContainer.style.flexShrink = 1f;
            Add(_content);

            _inspectorChangedDispatcher = new DeferredGraphUiActionDispatcher(this);
            _refreshDispatcher = new DeferredGraphUiActionDispatcher(this);

            RegisterCallback<DragUpdatedEvent>(
                HandleRootDragUpdated,
                TrickleDown.TrickleDown);
            RegisterCallback<DragPerformEvent>(
                HandleRootDragPerform,
                TrickleDown.TrickleDown);
            RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }

        /// <summary>
        /// Binds the inspector to the current director and selected node.
        /// </summary>
        /// <param name="director">Director that owns the graph.</param>
        /// <param name="selectedNodeId">Currently selected node id.</param>
        public void BindSelection(
            CutsceneDirector director,
            SerializableGuid selectedNodeId)
        {
            _director = director;
            _selectedNodeId = selectedNodeId;
            Refresh();
        }

        /// <summary>
        /// Attempts to route one drag event coming from a higher-level UI
        /// container into the hovered inspector property field.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        /// <param name="performDrag">True to perform the drop; false to update feedback only.</param>
        /// <returns>True when the drag was recognized by one hovered field.</returns>
        internal bool TryHandleGlobalDrag(
            Vector2 mousePosition,
            bool performDrag)
        {
            if (!worldBound.Contains(mousePosition)
                || !TryGetHoveredRelayProperty(mousePosition, out SerializedProperty property))
            {
                return false;
            }

            bool handled = performDrag
                ? CutsceneBlackboardDrawerUtility.TryHandleUIDragPerform(property)
                : CutsceneBlackboardDrawerUtility.TryHandleUIDragUpdated(property);

            if (handled && performDrag)
            {
                HandleRelayedDragMutation();
            }

            return handled;
        }

        /// <summary>
        /// Updates the explicit blackboard drop-zone affordances for the
        /// current internal drag session.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        internal void UpdateCustomBlackboardDrag(Vector2 mousePosition)
        {
            RefreshBlackboardDropZoneStyles(mousePosition);
        }

        /// <summary>
        /// Rebuilds the inspector after one value source leaves blackboard mode
        /// through the custom IMGUI clear action.
        /// </summary>
        private void HandleValueSourceBindingCleared()
        {
            if (_director == null || _selectedNodeId == SerializableGuid.Empty)
            {
                return;
            }

            _refreshDispatcher.Dispatch(Refresh);
        }

        /// <summary>
        /// Subscribes to drawer-driven inspector refresh notifications when the
        /// inspector is attached to one panel.
        /// </summary>
        /// <param name="evt">Attach event payload.</param>
        private void HandleAttachToPanel(AttachToPanelEvent evt)
        {
            CutsceneBlackboardDrawerUtility.ValueSourceBindingCleared -=
                HandleValueSourceBindingCleared;
            CutsceneBlackboardDrawerUtility.ValueSourceBindingCleared +=
                HandleValueSourceBindingCleared;
        }

        /// <summary>
        /// Removes drawer-driven refresh notifications when the inspector is no
        /// longer attached to one panel.
        /// </summary>
        /// <param name="evt">Detach event payload.</param>
        private void HandleDetachFromPanel(DetachFromPanelEvent evt)
        {
            CutsceneBlackboardDrawerUtility.ValueSourceBindingCleared -=
                HandleValueSourceBindingCleared;
        }

        /// <summary>
        /// Attempts to complete one internal blackboard drag by releasing the
        /// mouse over one explicit CutsceneValueSource drop zone.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        /// <returns>True when one hovered drop zone accepted the binding.</returns>
        internal bool TryPerformCustomBlackboardDrop(Vector2 mousePosition)
        {
            if (!TryGetHoveredBlackboardDropZone(
                    mousePosition,
                    out _,
                    out SerializedProperty property)
                || !CutsceneBlackboardDrawerUtility.TryHandleUIBlackboardDragPerform(property))
            {
                RefreshBlackboardDropZoneStyles(mousePosition);
                return false;
            }

            HandleRelayedDragMutation();
            RefreshBlackboardDropZoneStyles(mousePosition);
            return true;
        }

        /// <summary>
        /// Rebuilds the hosted inspector UI from the current selection.
        /// </summary>
        public void Refresh()
        {
            _isRefreshing = true;

            try
            {
                _content.Unbind();
                _content.Clear();
                _dragRelayFields.Clear();
                _blackboardDropZones.Clear();

                if (_director == null)
                {
                    _content.Add(new HelpBox(
                        "Bind a CutsceneDirector from the toolbar or select one in the Hierarchy.",
                        HelpBoxMessageType.Info));
                    return;
                }

                IReadOnlyList<CutsceneGraphValidationIssue> issues =
                    CutsceneGraphValidator.Validate(_director);

                if (_selectedNodeId == SerializableGuid.Empty)
                {
                    BuildDirectorSummary(issues);
                    return;
                }

                SerializedObject serializedDirector = new(_director);
                serializedDirector.UpdateIfRequiredOrScript();

                SerializedProperty nodeProperty = CutsceneEditorUtility.FindNodeProperty(
                    serializedDirector,
                    _selectedNodeId);

                if (nodeProperty == null)
                {
                    _selectedNodeId = SerializableGuid.Empty;
                    BuildDirectorSummary(issues);
                    return;
                }

                BuildSelectedNodeInspector(serializedDirector, nodeProperty, issues);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// Builds the read-only director summary state.
        /// </summary>
        /// <param name="issues">Current validation issues.</param>
        private void BuildDirectorSummary(IReadOnlyList<CutsceneGraphValidationIssue> issues)
        {
            _content.Add(CreateHeaderLabel("Director"));
            _content.Add(CreateValueRow("Title", _director.Title));
            _content.Add(CreateValueRow("Runtime Status", _director.RuntimeStatus.ToString()));

            if (!string.IsNullOrWhiteSpace(_director.RuntimeFailureReason))
            {
                _content.Add(new HelpBox(
                    _director.RuntimeFailureReason,
                    HelpBoxMessageType.Warning));
            }

            _content.Add(CreateHeaderLabel("Validation"));
            AddIssueList(issues, SerializableGuid.Empty);
        }

        /// <summary>
        /// Builds the selected node property list.
        /// </summary>
        /// <param name="serializedDirector">Serialized director wrapper.</param>
        /// <param name="nodeProperty">Serialized property for the selected node.</param>
        /// <param name="issues">Current validation issues.</param>
        private void BuildSelectedNodeInspector(
            SerializedObject serializedDirector,
            SerializedProperty nodeProperty,
            IReadOnlyList<CutsceneGraphValidationIssue> issues)
        {
            if (_director.Graph.TryGetNode(_selectedNodeId, out CutsceneNodeBase node))
            {
                _content.Add(CreateHeaderLabel(node.DisplayTitle));

                string summary = node.GetSummary();

                if (!string.IsNullOrWhiteSpace(summary))
                {
                    HelpBox summaryBox = new(summary, HelpBoxMessageType.None);
                    summaryBox.style.marginBottom = 8f;
                    _content.Add(summaryBox);
                }
            }

            VisualElement fieldsContainer = new();
            fieldsContainer.style.flexDirection = FlexDirection.Column;
            fieldsContainer.style.flexGrow = 1f;

            SerializedProperty iterator = nodeProperty.Copy();
            SerializedProperty endProperty = nodeProperty.GetEndProperty();
            int childDepth = nodeProperty.depth + 1;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren)
                && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;

                if (iterator.depth != childDepth || !ShouldDrawSelectedNodeField(iterator))
                {
                    continue;
                }

                fieldsContainer.Add(CreateInspectorFieldElement(iterator.Copy()));
            }

            _content.Add(fieldsContainer);
            _content.Bind(serializedDirector);
            ApplyCompactPropertyFieldLayout(fieldsContainer);
            RegisterCommentLiveTextRelays(fieldsContainer);
            schedule.Execute(() => ApplyCompactPropertyFieldLayout(fieldsContainer))
                .ExecuteLater(0);
            schedule.Execute(() => RegisterCommentLiveTextRelays(fieldsContainer))
                .ExecuteLater(0);
            fieldsContainer.RegisterCallback<SerializedPropertyChangeEvent>(
                _ => HandleNodePropertyChanged());

            _content.Add(CreateHeaderLabel("Validation"));
            AddIssueList(issues, _selectedNodeId);
        }

        /// <summary>
        /// Applies the side effects required after one selected node field
        /// changes through UI Toolkit binding.
        /// </summary>
        private void HandleNodePropertyChanged()
        {
            if (_isRefreshing
                || _inspectorChangedDispatcher.HasPendingAction
                || _director == null
                || _selectedNodeId == SerializableGuid.Empty)
            {
                return;
            }

            SerializedObject serializedDirector = new(_director);
            serializedDirector.UpdateIfRequiredOrScript();

            SerializedProperty nodeProperty = CutsceneEditorUtility.FindNodeProperty(
                serializedDirector,
                _selectedNodeId);

            if (nodeProperty == null)
            {
                return;
            }

            SerializedProperty titleProperty = nodeProperty.FindPropertyRelative("_title");

            if (titleProperty != null
                && string.IsNullOrWhiteSpace(titleProperty.stringValue)
                && _director.Graph.TryGetNode(_selectedNodeId, out CutsceneNodeBase resolvedNode))
            {
                titleProperty.stringValue = resolvedNode.DisplayTitle;
            }

            bool hasSerializedChanges = serializedDirector.ApplyModifiedProperties();

            if (!hasSerializedChanges)
            {
                return;
            }

            _director.Graph.EnsureNodeIds();
            CutsceneEditorUtility.MarkDirectorDirty(_director);

            SerializableGuid changedNodeId = _selectedNodeId;
            _inspectorChangedDispatcher.Dispatch(() =>
            {
                if (_director == null || changedNodeId == SerializableGuid.Empty)
                {
                    return;
                }

                InspectorChanged?.Invoke(changedNodeId);
            });
        }

        /// <summary>
        /// Relays drag-and-drop events through UI Toolkit so custom blackboard
        /// drawers can still accept drops when the hosted IMGUI control does not
        /// become the drag target.
        /// </summary>
        /// <param name="field">Property field rendered by the inspector.</param>
        /// <param name="propertyPath">Serialized property path bound to the field.</param>
        private void RegisterBlackboardDragRelay(
            PropertyField field,
            string propertyPath)
        {
            if (field == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            field.RegisterCallback<DragUpdatedEvent>(
                evt =>
                {
                    if (!TryGetInspectorProperty(propertyPath, out SerializedProperty property)
                        || !CutsceneBlackboardDrawerUtility.TryHandleUIDragUpdated(property))
                    {
                        return;
                    }

                    evt.StopImmediatePropagation();
                },
                TrickleDown.TrickleDown);

            field.RegisterCallback<DragPerformEvent>(
                evt =>
                {
                    if (!TryGetInspectorProperty(propertyPath, out SerializedProperty property)
                        || !CutsceneBlackboardDrawerUtility.TryHandleUIDragPerform(property))
                    {
                        return;
                    }

                    evt.StopImmediatePropagation();
                    HandleRelayedDragMutation();
                },
                TrickleDown.TrickleDown);
        }

        /// <summary>
        /// Creates the visual element used to render one node property row.
        /// Cutscene value sources receive one dedicated blackboard drop target
        /// rendered beside the serialized field.
        /// </summary>
        /// <param name="property">Property being rendered.</param>
        /// <returns>The composed property row.</returns>
        private VisualElement CreateInspectorFieldElement(SerializedProperty property)
        {
            PropertyField field = new(property);
            field.userData = property.propertyPath;
            field.style.flexGrow = 1f;
            field.style.flexShrink = 1f;
            field.style.minWidth = 0f;
            field.style.maxWidth = new StyleLength(Length.Percent(100));
            field.style.marginBottom = 0f;
            RegisterBlackboardDragRelay(field, property.propertyPath);
            _dragRelayFields.Add(new DragRelayField(field, property.propertyPath));

            if (!IsValueSourceProperty(property))
            {
                field.style.marginBottom = 4f;
                return field;
            }

            if (!CutsceneBlackboardDrawerUtility.ShouldShowValueSourceDropZone(property))
            {
                field.style.marginBottom = 4f;
                return field;
            }

            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4f;
            row.style.minWidth = 0f;
            row.style.flexGrow = 1f;
            row.style.flexShrink = 1f;
            row.style.maxWidth = new StyleLength(Length.Percent(100));
            row.style.overflow = Overflow.Hidden;

            row.Add(CreateValueSourceDropZone(property.propertyPath));
            row.Add(field);
            return row;
        }

        /// <summary>
        /// Registers immediate text relays for the comment node title/body so
        /// the graph node preview refreshes while the user types.
        /// </summary>
        /// <param name="container">Container that hosts the generated property fields.</param>
        private void RegisterCommentLiveTextRelays(VisualElement container)
        {
            if (container == null
                || !_director.Graph.TryGetNode(_selectedNodeId, out CutsceneNodeBase node)
                || node is not CutsceneCommentNode)
            {
                return;
            }

            List<PropertyField> propertyFields = container.Query<PropertyField>().ToList();

            for (int index = 0; index < propertyFields.Count; index++)
            {
                PropertyField propertyField = propertyFields[index];

                if (propertyField == null
                    || propertyField.userData is not string propertyPath
                    || !ShouldRelayCommentTextPreview(propertyPath))
                {
                    continue;
                }

                TextField textField = propertyField.Q<TextField>();

                if (textField == null
                    || textField.ClassListContains(LiveCommentTextRelayClassName))
                {
                    continue;
                }

                textField.isDelayed = false;
                textField.AddToClassList(LiveCommentTextRelayClassName);
                textField.RegisterValueChangedCallback(
                    evt => ScheduleCommentPreviewRefresh(propertyPath, evt.newValue));
            }
        }

        /// <summary>
        /// Returns whether the provided property path drives the comment node
        /// visual text shown inside the graph.
        /// </summary>
        /// <param name="propertyPath">Serialized property path.</param>
        /// <returns>True when the property affects live comment text.</returns>
        private static bool ShouldRelayCommentTextPreview(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
            {
                return false;
            }

            return propertyPath.EndsWith("._title", StringComparison.Ordinal)
                || propertyPath.EndsWith("._body", StringComparison.Ordinal);
        }

        /// <summary>
        /// Schedules one lightweight comment node preview refresh using the
        /// latest typed text value.
        /// </summary>
        /// <param name="propertyPath">Serialized property path being edited.</param>
        /// <param name="value">Latest text value authored in the field.</param>
        private void ScheduleCommentPreviewRefresh(string propertyPath, string value)
        {
            if (_isRefreshing
                || _director == null
                || _selectedNodeId == SerializableGuid.Empty
                || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            _pendingLiveCommentPropertyPath = propertyPath;
            _pendingLiveCommentValue = value ?? string.Empty;

            if (_hasPendingLiveCommentPreview)
            {
                return;
            }

            _hasPendingLiveCommentPreview = true;

            schedule.Execute(() => FlushCommentPreviewRefresh())
                .ExecuteLater(0);
        }

        /// <summary>
        /// Applies the latest pending comment text edit through the serialized
        /// director wrapper and notifies the graph to refresh only that node.
        /// </summary>
        private void FlushCommentPreviewRefresh()
        {
            _hasPendingLiveCommentPreview = false;

            if (_director == null
                || _selectedNodeId == SerializableGuid.Empty
                || string.IsNullOrWhiteSpace(_pendingLiveCommentPropertyPath))
            {
                return;
            }

            SerializedObject serializedDirector = new(_director);
            serializedDirector.UpdateIfRequiredOrScript();

            SerializedProperty property = serializedDirector.FindProperty(
                _pendingLiveCommentPropertyPath);

            if (property == null || property.propertyType != SerializedPropertyType.String)
            {
                return;
            }

            if (!string.Equals(
                    property.stringValue,
                    _pendingLiveCommentValue,
                    StringComparison.Ordinal))
            {
                property.stringValue = _pendingLiveCommentValue;
                serializedDirector.ApplyModifiedProperties();
                _director.Graph.EnsureNodeIds();
                CutsceneEditorUtility.MarkDirectorDirty(_director);
            }

            InspectorPreviewChanged?.Invoke(_selectedNodeId);
        }

        /// <summary>
        /// Tightens the default UI Toolkit label spacing for node inspector
        /// property fields so short labels do not waste horizontal space.
        /// </summary>
        /// <param name="container">Container that hosts the generated property fields.</param>
        private static void ApplyCompactPropertyFieldLayout(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            List<PropertyField> propertyFields = container.Query<PropertyField>().ToList();

            for (int index = 0; index < propertyFields.Count; index++)
            {
                PropertyField propertyField = propertyFields[index];

                if (propertyField == null)
                {
                    continue;
                }

                Label labelElement = propertyField.Q<Label>(className: BaseFieldLabelClassName);

                if (labelElement != null)
                {
                    labelElement.style.minWidth = CompactPropertyLabelWidth;
                    labelElement.style.width = CompactPropertyLabelWidth;
                    labelElement.style.maxWidth = CompactPropertyLabelWidth;
                    labelElement.style.marginRight = CompactPropertyLabelGap;
                    labelElement.style.flexShrink = 0f;
                }

                VisualElement inputElement = propertyField.Q(className: BaseFieldInputClassName);

                if (inputElement == null)
                {
                    continue;
                }

                inputElement.style.marginLeft = 0f;
                inputElement.style.flexGrow = 1f;
                inputElement.style.flexShrink = 1f;
                inputElement.style.minWidth = 0f;
            }
        }

        /// <summary>
        /// Creates one explicit UI Toolkit drop affordance beside one
        /// CutsceneValueSource field.
        /// </summary>
        /// <param name="propertyPath">Serialized property path bound to the value source.</param>
        /// <returns>The configured drop target element.</returns>
        private VisualElement CreateValueSourceDropZone(string propertyPath)
        {
            VisualElement dropZone = new();
            dropZone.tooltip =
                "Drop a compatible blackboard variable here to bind this source.";
            dropZone.style.width = 20f;
            dropZone.style.height = 18f;
            dropZone.style.minWidth = 20f;
            dropZone.style.marginRight = 6f;
            dropZone.style.flexShrink = 0f;
            dropZone.style.alignSelf = Align.Center;
            dropZone.style.justifyContent = Justify.Center;
            dropZone.style.alignItems = Align.Center;
            dropZone.style.backgroundColor = new StyleColor(DropZoneIdleColor);
            dropZone.style.borderLeftWidth = 1f;
            dropZone.style.borderRightWidth = 1f;
            dropZone.style.borderTopWidth = 1f;
            dropZone.style.borderBottomWidth = 1f;
            dropZone.style.borderLeftColor = new Color(0.46f, 0.46f, 0.46f, 0.95f);
            dropZone.style.borderRightColor = new Color(0.46f, 0.46f, 0.46f, 0.95f);
            dropZone.style.borderTopColor = new Color(0.46f, 0.46f, 0.46f, 0.95f);
            dropZone.style.borderBottomColor = new Color(0.46f, 0.46f, 0.46f, 0.95f);
            dropZone.style.borderTopLeftRadius = 6f;
            dropZone.style.borderTopRightRadius = 6f;
            dropZone.style.borderBottomLeftRadius = 6f;
            dropZone.style.borderBottomRightRadius = 6f;

            _blackboardDropZones.Add(new BlackboardDropZoneField(dropZone, propertyPath));

            dropZone.RegisterCallback<DragUpdatedEvent>(
                evt =>
                {
                    if (!TryGetInspectorProperty(propertyPath, out SerializedProperty property)
                        || !CutsceneBlackboardDrawerUtility.TryHandleUIBlackboardDragUpdated(
                            property))
                    {
                        return;
                    }

                    evt.StopImmediatePropagation();
                },
                TrickleDown.TrickleDown);

            dropZone.RegisterCallback<DragPerformEvent>(
                evt =>
                {
                    if (!TryGetInspectorProperty(propertyPath, out SerializedProperty property)
                        || !CutsceneBlackboardDrawerUtility.TryHandleUIBlackboardDragPerform(
                            property))
                    {
                        return;
                    }

                    evt.StopImmediatePropagation();
                    HandleRelayedDragMutation();
                },
                TrickleDown.TrickleDown);

            return dropZone;
        }

        /// <summary>
        /// Resolves one fresh serialized property from the currently bound
        /// director for UI Toolkit drag relays.
        /// </summary>
        /// <param name="propertyPath">Serialized property path inside the director.</param>
        /// <param name="property">Resolved property when available.</param>
        /// <returns>True when the property exists.</returns>
        private bool TryGetInspectorProperty(
            string propertyPath,
            out SerializedProperty property)
        {
            property = null;

            if (_director == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return false;
            }

            SerializedObject serializedDirector = new(_director);
            serializedDirector.UpdateIfRequiredOrScript();
            property = serializedDirector.FindProperty(propertyPath);
            return property != null;
        }

        /// <summary>
        /// Commits a node change applied through a drag relay and refreshes the
        /// graph inspector state.
        /// </summary>
        private void HandleRelayedDragMutation()
        {
            if (_director == null || _selectedNodeId == SerializableGuid.Empty)
            {
                return;
            }

            _director.Graph.EnsureNodeIds();
            CutsceneEditorUtility.MarkDirectorDirty(_director);

            SerializableGuid changedNodeId = _selectedNodeId;
            Refresh();

            _inspectorChangedDispatcher.Dispatch(() =>
            {
                if (_director == null || changedNodeId == SerializableGuid.Empty)
                {
                    return;
                }

                InspectorChanged?.Invoke(changedNodeId);
            });
        }

        /// <summary>
        /// Handles drag updates at the inspector root so blackboard drops still
        /// work when nested controls do not receive the UI Toolkit drag event.
        /// </summary>
        /// <param name="evt">Current drag update event.</param>
        private void HandleRootDragUpdated(DragUpdatedEvent evt)
        {
            if (!TryHandleGlobalDrag(evt.mousePosition, performDrag: false))
            {
                return;
            }

            evt.StopImmediatePropagation();
        }

        /// <summary>
        /// Handles drag performs at the inspector root so blackboard drops still
        /// work when nested controls do not receive the UI Toolkit drag event.
        /// </summary>
        /// <param name="evt">Current drag perform event.</param>
        private void HandleRootDragPerform(DragPerformEvent evt)
        {
            if (!TryHandleGlobalDrag(evt.mousePosition, performDrag: true))
            {
                return;
            }

            evt.StopImmediatePropagation();
        }

        /// <summary>
        /// Resolves the serialized property associated with the property field
        /// currently under the provided mouse position.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        /// <param name="property">Resolved property when available.</param>
        /// <returns>True when one relay field is hovered and the property exists.</returns>
        private bool TryGetHoveredRelayProperty(
            Vector2 mousePosition,
            out SerializedProperty property)
        {
            property = null;

            for (int index = 0; index < _dragRelayFields.Count; index++)
            {
                DragRelayField relayField = _dragRelayFields[index];

                if (relayField.Field == null
                    || !relayField.Field.visible
                    || !relayField.Field.enabledInHierarchy
                    || !relayField.Field.worldBound.Contains(mousePosition))
                {
                    continue;
                }

                return TryGetInspectorProperty(relayField.PropertyPath, out property);
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve one explicit blackboard drop zone under the
        /// current mouse position together with its serialized property.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        /// <param name="propertyPath">Serialized property path bound to the hovered drop zone.</param>
        /// <param name="property">Resolved property when available.</param>
        /// <returns>True when a hovered drop zone exists and the property resolves.</returns>
        private bool TryGetHoveredBlackboardDropZone(
            Vector2 mousePosition,
            out string propertyPath,
            out SerializedProperty property)
        {
            propertyPath = null;
            property = null;

            for (int index = 0; index < _blackboardDropZones.Count; index++)
            {
                BlackboardDropZoneField dropZoneField = _blackboardDropZones[index];

                if (dropZoneField.DropZone == null
                    || !dropZoneField.DropZone.visible
                    || !dropZoneField.DropZone.enabledInHierarchy
                    || !dropZoneField.DropZone.worldBound.Contains(mousePosition))
                {
                    continue;
                }

                propertyPath = dropZoneField.PropertyPath;
                return TryGetInspectorProperty(propertyPath, out property);
            }

            return false;
        }

        /// <summary>
        /// Refreshes the explicit blackboard drop-zone highlight state for the
        /// current internal drag session.
        /// </summary>
        /// <param name="mousePosition">Mouse position in panel space.</param>
        private void RefreshBlackboardDropZoneStyles(Vector2 mousePosition)
        {
            for (int index = 0; index < _blackboardDropZones.Count; index++)
            {
                BlackboardDropZoneField dropZoneField = _blackboardDropZones[index];

                if (dropZoneField.DropZone == null)
                {
                    continue;
                }

                Color backgroundColor = DropZoneIdleColor;

                if (CutsceneBlackboardDragAndDrop.HasActiveDrag
                    && TryGetInspectorProperty(
                        dropZoneField.PropertyPath,
                        out SerializedProperty property)
                    && CutsceneBlackboardDrawerUtility.CanAcceptBlackboardDrag(property))
                {
                    backgroundColor = dropZoneField.DropZone.worldBound.Contains(mousePosition)
                        ? DropZoneHoverColor
                        : DropZoneReadyColor;
                }

                dropZoneField.DropZone.style.backgroundColor =
                    new StyleColor(backgroundColor);
            }
        }

        /// <summary>
        /// Determines whether one serialized property represents a
        /// CutsceneValueSource instance.
        /// </summary>
        /// <param name="property">Candidate property.</param>
        /// <returns>True when the property exposes the serialized value-source shape.</returns>
        private static bool IsValueSourceProperty(SerializedProperty property)
        {
            return property?.FindPropertyRelative("_mode") != null
                && property.FindPropertyRelative("_blackboardVariable") != null
                && property.FindPropertyRelative("_directValue") != null;
        }

        /// <summary>
        /// Adds the relevant validation issues to the inspector.
        /// </summary>
        /// <param name="issues">Current validation issues.</param>
        /// <param name="nodeId">Target node filter.</param>
        private void AddIssueList(
            IReadOnlyList<CutsceneGraphValidationIssue> issues,
            SerializableGuid nodeId)
        {
            List<CutsceneGraphValidationIssue> filteredIssues = new();

            for (int index = 0; index < issues.Count; index++)
            {
                CutsceneGraphValidationIssue issue = issues[index];

                if (nodeId != SerializableGuid.Empty
                    && issue.NodeId != SerializableGuid.Empty
                    && issue.NodeId != nodeId)
                {
                    continue;
                }

                filteredIssues.Add(issue);
            }

            if (filteredIssues.Count == 0)
            {
                _content.Add(new HelpBox("No validation issues.", HelpBoxMessageType.None));
                return;
            }

            for (int index = 0; index < filteredIssues.Count; index++)
            {
                CutsceneGraphValidationIssue issue = filteredIssues[index];
                _content.Add(new HelpBox(issue.Message, ToMessageType(issue.Severity)));
            }
        }

        /// <summary>
        /// Creates one section header label.
        /// </summary>
        /// <param name="text">Header text.</param>
        /// <returns>The configured label.</returns>
        private static Label CreateHeaderLabel(string text)
        {
            Label label = new(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 6f;
            label.style.marginTop = 2f;
            return label;
        }

        /// <summary>
        /// Creates one compact read-only key/value row.
        /// </summary>
        /// <param name="label">Row label.</param>
        /// <param name="value">Row value.</param>
        /// <returns>The configured row container.</returns>
        private static VisualElement CreateValueRow(string label, string value)
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4f;

            Label labelElement = new($"{label}:");
            labelElement.style.minWidth = 110f;
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(labelElement);

            Label valueElement = new(value ?? string.Empty);
            valueElement.style.flexGrow = 1f;
            row.Add(valueElement);
            return row;
        }

        /// <summary>
        /// Determines whether one node serialized field should be shown in the
        /// inspector.
        /// </summary>
        /// <param name="property">Candidate property.</param>
        /// <returns>True when the field should be drawn.</returns>
        private static bool ShouldDrawSelectedNodeField(SerializedProperty property)
        {
            return property.name switch
            {
                "_id" => false,
                "_position" => false,
                _ => true,
            };
        }

        /// <summary>
        /// Maps validation severity to the UI Toolkit help box style.
        /// </summary>
        /// <param name="severity">Validation severity.</param>
        /// <returns>Equivalent help box message type.</returns>
        private static HelpBoxMessageType ToMessageType(CutsceneGraphValidationSeverity severity)
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