using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.FSMModule;
using IndieGabo.HandyTools.FSMModule.Registering;
using IndieGabo.HandyTools.FSMModule.Implementations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    /// <summary>
    /// Renders the current-state and history views for the state visualizer window.
    /// </summary>
    public class StateVisualizer
    {
        private const string CCProStateFullName =
            "IndieGabo.HandyTools.FSMModule.CCPro.CCProState";

        private const string ScriptableCCProStateFullName =
            "IndieGabo.HandyTools.FSMModule.CCPro.ScriptableCCProState";

        private const float MinNodeWidth = 110f;
        private const float MaxNodeWidth = 260f;
        private const float MinOwnerNodeWidth = 220f;
        private const float MaxOwnerNodeWidth = 420f;
        private const float NodeHeight = 66f;
        private const float HorizontalSpacing = 36f;
        private const float VerticalSpacing = 124f;

        #region Fields

        private readonly VisualElement _root;
        private readonly StatesGraphView _graphView;
        private readonly VisualElement _graphContainer;
        private readonly VisualElement _currentStateView;
        private readonly VisualElement _historyView;
        private readonly Button _currentStateButton;
        private readonly Button _historyButton;
        private readonly Label _currentStateSummaryLabel;
        private readonly Label _currentStateHelpLabel;
        private readonly Label _historySummaryLabel;
        private readonly Label _historyHelpLabel;
        private readonly ScrollView _historyScrollView;

        private readonly Dictionary<Type, float> _subtreeWidths = new();
        private readonly Dictionary<Type, float> _nodeWidths = new();

        private FSMBrain _machine;
        private ViewMode _viewMode;

        private Session _session;

        #endregion

        #region Types

        /// <summary>
        /// Defines the active view mode shown by the visualizer.
        /// </summary>
        private enum ViewMode
        {
            CurrentState,
            History
        }

        /// <summary>
        /// Represents a single type inside the inheritance tree used by the graph.
        /// </summary>
        private sealed class StateTreeNode
        {
            public StateTreeNode(Type stateType)
            {
                StateType = stateType;
                Children = new List<StateTreeNode>();
            }

            public Type StateType { get; }

            public List<StateTreeNode> Children { get; }

            public StateTreeNode Parent { get; set; }
        }

        #endregion

        #region Getters

        public VisualElement Root => _root;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the visualizer UI and its supporting graph view.
        /// </summary>
        public StateVisualizer()
        {
            VisualTreeAsset tree = Resources.Load<VisualTreeAsset>("UI Toolkit/FSM/UI Documents/StateVisualizerUI");

            if (tree == null)
            {
                _root = new VisualElement();
                _root.style.flexGrow = 1f;
                _root.Add(new Label("StateVisualizerUI could not be loaded."));
                return;
            }

            TemplateContainer template = tree.CloneTree();
            template.style.flexGrow = 1f;

            StyleSheet styleSheet = Resources.Load<StyleSheet>(
                "UI Toolkit/FSM/Styles/handy-fsm-state-visualizer-styles");

            if (styleSheet != null)
            {
                template.styleSheets.Add(styleSheet);
            }

            _root = template;
            _graphContainer = _root.Q<VisualElement>("graph-container");
            _currentStateView = _root.Q<VisualElement>("current-state-view");
            _historyView = _root.Q<VisualElement>("history-view");
            _currentStateButton = _root.Q<Button>("current-state-button");
            _historyButton = _root.Q<Button>("history-button");
            _currentStateSummaryLabel = _root.Q<Label>("current-state-summary-label");
            _currentStateHelpLabel = _root.Q<Label>("current-state-help-label");
            _historySummaryLabel = _root.Q<Label>("history-summary-label");
            _historyHelpLabel = _root.Q<Label>("history-help-label");
            _historyScrollView = _root.Q<ScrollView>("history-scroll-view");

            _graphView = new StatesGraphView();
            _graphContainer.Add(_graphView);

            _currentStateButton.clicked += () => SetViewMode(ViewMode.CurrentState);
            _historyButton.clicked += () => SetViewMode(ViewMode.History);

            SetViewMode(ViewMode.CurrentState);
            Refresh();
        }

        #endregion

        #region Flow

        /// <summary>
        /// Updates the machine inspected by the visualizer.
        /// </summary>
        /// <param name="machine">The machine currently selected in the window.</param>
        public void SetMachine(FSMBrain machine)
        {
            _machine = machine;
            Refresh();
        }

        /// <summary>
        /// Loads a new play-session history into the visualizer.
        /// </summary>
        /// <param name="session">The session to display.</param>
        public void LoadSession(Session session)
        {
            _session = session;
            Refresh();
        }

        /// <summary>
        /// Clears the currently loaded session.
        /// </summary>
        public void Dismiss()
        {
            _session = null;
            Refresh();
        }

        /// <summary>
        /// Clears the currently loaded session.
        /// </summary>
        public void ClearSession()
        {
            Dismiss();
        }

        /// <summary>
        /// Registers a new active state in the current session and refreshes the views.
        /// </summary>
        /// <param name="state">The state that has just become active.</param>
        public void RegisterState(IState state)
        {
            if (_session != null && state != null)
            {
                _session.Register(
                    state,
                    _machine != null
                        ? _machine.LastTransitionReport
                        : global::IndieGabo.HandyTools.FSMModule.StateTransitionReport.Unknown);
            }

            Refresh();
        }

        /// <summary>
        /// Switches between the current-state and history views.
        /// </summary>
        /// <param name="viewMode">The view that should become visible.</param>
        private void SetViewMode(ViewMode viewMode)
        {
            _viewMode = viewMode;
            _currentStateView.style.display =
                viewMode == ViewMode.CurrentState ? DisplayStyle.Flex : DisplayStyle.None;
            _historyView.style.display =
                viewMode == ViewMode.History ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateToolbarState();
        }

        /// <summary>
        /// Refreshes all dynamic content shown by the visualizer.
        /// </summary>
        private void Refresh()
        {
            RefreshCurrentStateView();
            RefreshHistoryView();
        }

        /// <summary>
        /// Updates the toolbar buttons to match the selected view.
        /// </summary>
        private void UpdateToolbarState()
        {
            _currentStateButton.EnableInClassList(
                "visualizer-toolbar-button--active",
                _viewMode == ViewMode.CurrentState);

            _historyButton.EnableInClassList(
                "visualizer-toolbar-button--active",
                _viewMode == ViewMode.History);
        }

        /// <summary>
        /// Rebuilds the current-state graph and summary text.
        /// </summary>
        private void RefreshCurrentStateView()
        {
            if (_graphView == null)
                return;

            _graphView.ClearGraph();

            if (_machine == null)
            {
                _currentStateSummaryLabel.text = "Select an FSMBrain to inspect.";
                _currentStateHelpLabel.text =
                    "In play mode the window will highlight the active path.";
                return;
            }

            List<Type> loadedStateTypes = ResolveLoadedStateTypes();

            if (loadedStateTypes.Count == 0)
            {
                _currentStateSummaryLabel.text =
                    "No states could be resolved for this FSMBrain.";
                _currentStateHelpLabel.text =
                    "Loaded scriptable states or generic runtime states will appear here.";
                return;
            }

            HashSet<Type> currentPathTypes = ResolveCurrentPathTypes(_machine.CurrentState);
            List<StateTreeNode> roots = BuildForest(loadedStateTypes);

            BuildGraph(
                roots,
                currentPathTypes,
                _machine.CurrentState?.GetType(),
                _machine.Owner != null ? _machine.Owner.name : _machine.name);

            string currentStateName = _machine.CurrentState == null
                ? "None"
                : _machine.CurrentState.DisplayName;

            string currentPath = BuildCurrentPathLabel(_machine.CurrentState);
            string previewMode = _machine.IsInitialized ? "runtime" : "preview";

            _currentStateSummaryLabel.text =
                $"Owner: {_machine.Owner.name} | Current: {currentStateName} | " +
                $"Mode: {previewMode}";

            _currentStateHelpLabel.text = _machine.CurrentState == null
                ? "The machine is not running. The graph shows the available inheritance tree."
                : $"Active path: {currentPath}";
        }

        /// <summary>
        /// Rebuilds the history list for the current play session.
        /// </summary>
        private void RefreshHistoryView()
        {
            if (_historyScrollView == null)
                return;

            _historyScrollView.Clear();

            if (_session == null || _session.Records == null || _session.Records.Count == 0)
            {
                _historySummaryLabel.text = "No transitions recorded for the last captured session.";
                _historyHelpLabel.text =
                    "Enable Save History on the FSMBrain debug section to capture a session. " +
                    "Transition reasons will be added when the runtime is instrumented.";
                return;
            }

            _historySummaryLabel.text =
                $"Recorded transitions: {_session.Records.Count}";

            _historyHelpLabel.text =
                "This view shows the last captured session for the selected machine, " +
                "including the explicit reason for each state change.";

            for (int index = 0; index < _session.Records.Count; index++)
            {
                Record record = _session.Records[index];
                _historyScrollView.Add(CreateHistoryRow(record, index + 1));
            }
        }

        /// <summary>
        /// Creates a single history row for the recorded transition list.
        /// </summary>
        /// <param name="record">The transition record to display.</param>
        /// <param name="index">The 1-based position in the session history.</param>
        /// <returns>A visual element representing the record.</returns>
        private VisualElement CreateHistoryRow(Record record, int index)
        {
            VisualElement row = new();
            row.AddToClassList("history-row");

            Label indexLabel = new(index.ToString("00"));
            indexLabel.AddToClassList("history-row-index");

            string fromName = ResolveRecordStateName(record, true);
            string toName = ResolveRecordStateName(record, false);

            Label transitionLabel = new($"{fromName} -> {toName}");
            transitionLabel.AddToClassList("history-row-transition");

            Label metadataLabel = new(BuildHistoryMetadata(record));
            metadataLabel.AddToClassList("history-row-meta");

            row.Add(indexLabel);
            row.Add(transitionLabel);
            row.Add(metadataLabel);
            return row;
        }

        /// <summary>
        /// Resolves the display name stored in a transition record.
        /// </summary>
        /// <param name="record">The record being displayed.</param>
        /// <param name="fromState">Whether the source state should be resolved.</param>
        /// <returns>The label shown in the history row.</returns>
        private static string ResolveRecordStateName(Record record, bool fromState)
        {
            if (record == null)
                return "None";

            if (fromState)
            {
                if (record.FromState != null)
                    return record.FromState.DisplayName;

                if (record.FromScriptable != null)
                    return record.FromScriptable.DisplayName;

                return "Start";
            }

            if (record.State != null)
                return record.State.DisplayName;

            if (record.ScriptableState != null)
                return record.ScriptableState.DisplayName;

            return "None";
        }

        /// <summary>
        /// Builds the metadata label shown for a history record.
        /// </summary>
        /// <param name="record">The transition record being displayed.</param>
        /// <returns>The formatted history metadata label.</returns>
        private static string BuildHistoryMetadata(Record record)
        {
            if (record == null)
            {
                return "Reason: Unknown";
            }

            string reasonLabel = FormatTransitionReason(record.TransitionReason);

            if (string.IsNullOrEmpty(record.TransitionMessage))
            {
                return $"Reason: {reasonLabel}";
            }

            return $"Reason: {reasonLabel} | Message: {record.TransitionMessage}";
        }

        /// <summary>
        /// Formats a transition-reason enum for the history UI.
        /// </summary>
        /// <param name="transitionReason">The reason recorded for the transition.</param>
        /// <returns>The user-facing reason label.</returns>
        private static string FormatTransitionReason(StateTransitionReason transitionReason)
        {
            return transitionReason switch
            {
                StateTransitionReason.InitialEntry => "Initial entry",
                StateTransitionReason.VoluntaryExit => "Voluntary exit",
                StateTransitionReason.Interrupted => "Interrupted by transition evaluation",
                StateTransitionReason.ExternalRequest => "Requested externally",
                StateTransitionReason.ConditionTransition => "Condition transitation",
                StateTransitionReason.NaturalTransition => "Natural transition",
                StateTransitionReason.ErrorTransition => "Error transition",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Resolves the set of state types available to the machine.
        /// </summary>
        /// <returns>The distinct resolved state types.</returns>
        private List<Type> ResolveLoadedStateTypes()
        {
            if (_machine == null)
                return new List<Type>();

            if (_machine.IsInitialized)
            {
                return _machine
                    .States.GetAllStates()
                    .Where(state => state != null)
                    .Select(state => state.GetType())
                    .Distinct()
                    .OrderBy(type => type.Name)
                    .ToList();
            }

            HashSet<Type> previewTypes = new();
            SerializedObject serializedMachine = new(_machine);

            AddScriptableStateTypes(serializedMachine, previewTypes);
            AddGenericRuntimeStateTypes(previewTypes);

            return previewTypes.OrderBy(type => type.Name).ToList();
        }

        /// <summary>
        /// Adds serialized scriptable state types configured on the machine.
        /// </summary>
        /// <param name="serializedMachine">The serialized representation of the machine.</param>
        /// <param name="stateTypes">The target set that receives the discovered types.</param>
        private static void AddScriptableStateTypes(
            SerializedObject serializedMachine,
            ISet<Type> stateTypes)
        {
            SerializedProperty defaultStateProperty =
                serializedMachine.FindProperty("_defaultScriptableState");

            if (defaultStateProperty?.objectReferenceValue is ScriptableState defaultState)
            {
                stateTypes.Add(defaultState.GetType());
            }

            SerializedProperty statesProperty =
                serializedMachine.FindProperty("_scriptableStates");

            if (statesProperty == null || !statesProperty.isArray)
                return;

            for (int index = 0; index < statesProperty.arraySize; index++)
            {
                SerializedProperty element = statesProperty.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue is ScriptableState scriptableState)
                {
                    stateTypes.Add(scriptableState.GetType());
                }
            }
        }

        /// <summary>
        /// Adds generic runtime state types discovered from the machine base class.
        /// </summary>
        /// <param name="stateTypes">The target set that receives the discovered types.</param>
        private void AddGenericRuntimeStateTypes(ISet<Type> stateTypes)
        {
            if (!TryGetGenericBaseStateType(_machine.GetType(), out Type baseStateType))
                return;

            IEnumerable<Type> derivedTypes = baseStateType.Assembly
                .GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    baseStateType.IsAssignableFrom(type));

            foreach (Type derivedType in derivedTypes)
            {
                stateTypes.Add(derivedType);
            }
        }

        /// <summary>
        /// Resolves the generic state base type from a generic HandyTools FSM brain.
        /// </summary>
        /// <param name="machineType">The brain type being inspected.</param>
        /// <param name="baseStateType">The discovered state base type.</param>
        /// <returns>True when a generic HandyTools FSM brain was found.</returns>
        private static bool TryGetGenericBaseStateType(
            Type machineType,
            out Type baseStateType)
        {
            Type currentType = machineType;

            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType)
                {
                    Type genericDefinition = currentType.GetGenericTypeDefinition();

                    if (genericDefinition == typeof(GenericHandyFSMBrain<>) ||
                        genericDefinition == typeof(GenericHandyFSMBrain<,>))
                    {
                        baseStateType = currentType.GetGenericArguments()[0];
                        return true;
                    }
                }

                currentType = currentType.BaseType;
            }

            baseStateType = null;
            return false;
        }

        /// <summary>
        /// Resolves the full path of types that lead to the current active state.
        /// </summary>
        /// <param name="currentState">The active state instance.</param>
        /// <returns>The set of types that belong to the active inheritance path.</returns>
        private static HashSet<Type> ResolveCurrentPathTypes(IState currentState)
        {
            HashSet<Type> pathTypes = new();

            if (currentState == null)
                return pathTypes;

            foreach (Type stateType in BuildStateChain(currentState.GetType()))
            {
                pathTypes.Add(stateType);
            }

            return pathTypes;
        }

        /// <summary>
        /// Formats the active inheritance path for the summary text.
        /// </summary>
        /// <param name="currentState">The active state instance.</param>
        /// <returns>The formatted path label.</returns>
        private static string BuildCurrentPathLabel(IState currentState)
        {
            if (currentState == null)
                return "No active state";

            IReadOnlyList<Type> chain = BuildStateChain(currentState.GetType());
            List<string> formattedTitles = new(chain.Count);

            for (int index = 0; index < chain.Count; index++)
            {
                Type parentType = index > 0 ? chain[index - 1] : null;
                formattedTitles.Add(FormatStateTitle(chain[index], parentType));
            }

            return string.Join(
                " > ",
                formattedTitles);
        }

        /// <summary>
        /// Builds the tree of inheritance nodes that should appear in the graph.
        /// </summary>
        /// <param name="stateTypes">The resolved loaded state types.</param>
        /// <returns>The ordered list of forest roots.</returns>
        private static List<StateTreeNode> BuildForest(IEnumerable<Type> stateTypes)
        {
            Dictionary<Type, StateTreeNode> nodeMap = new();
            List<StateTreeNode> roots = new();

            foreach (Type stateType in stateTypes)
            {
                IReadOnlyList<Type> chain = BuildStateChain(stateType);
                StateTreeNode parentNode = null;

                foreach (Type chainType in chain)
                {
                    if (!nodeMap.TryGetValue(chainType, out StateTreeNode treeNode))
                    {
                        treeNode = new StateTreeNode(chainType);
                        nodeMap.Add(chainType, treeNode);
                    }

                    if (parentNode == null)
                    {
                        if (!roots.Contains(treeNode))
                        {
                            roots.Add(treeNode);
                        }
                    }
                    else if (!parentNode.Children.Contains(treeNode))
                    {
                        treeNode.Parent = parentNode;
                        parentNode.Children.Add(treeNode);
                    }

                    parentNode = treeNode;
                }
            }

            SortForest(roots);
            return roots;
        }

        /// <summary>
        /// Sorts every node in the forest by type name.
        /// </summary>
        /// <param name="roots">The forest roots that should be sorted.</param>
        private static void SortForest(List<StateTreeNode> roots)
        {
            roots.Sort((left, right) =>
                string.Compare(left.StateType.Name, right.StateType.Name, StringComparison.Ordinal));

            foreach (StateTreeNode root in roots)
            {
                root.Children.Sort((left, right) =>
                    string.Compare(left.StateType.Name, right.StateType.Name, StringComparison.Ordinal));

                SortForest(root.Children);
            }
        }

        /// <summary>
        /// Builds the inheritance chain for a concrete state type.
        /// </summary>
        /// <param name="stateType">The concrete type being inspected.</param>
        /// <returns>The ordered chain from root-most derived type to the leaf type.</returns>
        private static IReadOnlyList<Type> BuildStateChain(Type stateType)
        {
            List<Type> chain = new();
            Type currentType = stateType;

            while (currentType != null &&
                   currentType != typeof(State) &&
                   currentType != typeof(ScriptableState))
            {
                if (!ShouldSkipBaseStateType(currentType))
                {
                    chain.Add(currentType);
                }

                currentType = currentType.BaseType;
            }

            chain.Reverse();
            return chain;
        }

        /// <summary>
        /// Determines whether a state type should be hidden from the visualizer inheritance graph.
        /// </summary>
        /// <param name="stateType">The state type being evaluated.</param>
        /// <returns>True when the type is a non-informative framework base node.</returns>
        private static bool ShouldSkipBaseStateType(Type stateType)
        {
            return stateType == typeof(State) ||
                stateType == typeof(ScriptableState) ||
                string.Equals(
                    stateType.FullName,
                    CCProStateFullName,
                    StringComparison.Ordinal) ||
                string.Equals(
                    stateType.FullName,
                    ScriptableCCProStateFullName,
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// Draws the resolved inheritance tree inside the graph view.
        /// </summary>
        /// <param name="roots">The forest roots that define the graph.</param>
        /// <param name="currentPathTypes">The active path types that should be highlighted.</param>
        /// <param name="currentStateType">The currently active concrete state type.</param>
        /// <param name="ownerName">The owner name displayed at the top of the graph.</param>
        private void BuildGraph(
            List<StateTreeNode> roots,
            HashSet<Type> currentPathTypes,
            Type currentStateType,
            string ownerName)
        {
            if (roots.Count == 0)
                return;

            _subtreeWidths.Clear();
            _nodeWidths.Clear();
            Dictionary<Type, StateNode> nodeLookup = new();
            List<StatesGraphView.ConnectionData> connections = new();
            float totalWidth = MeasureForest(roots);
            float ownerNodeWidth = ResolveTitleWidth(ownerName, true);

            StateNode ownerNode = new(
                ownerName,
                StateNode.NodeKind.Owner,
                false,
                true,
                ownerNodeWidth);

            ownerNode.SetPosition(new Rect(
                (totalWidth - ownerNodeWidth) * 0.5f,
                0f,
                ownerNodeWidth,
                NodeHeight));

            _graphView.AddElement(ownerNode);

            float left = 0f;
            foreach (StateTreeNode root in roots)
            {
                float rootWidth = _subtreeWidths[root.StateType];
                CreateBranch(
                    root,
                    left,
                    VerticalSpacing,
                    currentPathTypes,
                    currentStateType,
                    nodeLookup,
                    connections);

                connections.Add(CreateConnection(
                    ownerNode,
                    nodeLookup[root.StateType],
                    currentPathTypes.Contains(root.StateType)));

                left += rootWidth + HorizontalSpacing;
            }

            float graphHeight = Mathf.Max(
                ownerNode.GetPosition().yMax,
                nodeLookup.Values.Max(node => node.GetPosition().yMax)) + 64f;

            float graphWidth = Mathf.Max(totalWidth, ownerNodeWidth) + 64f;

            _graphView.SetConnections(connections, graphWidth, graphHeight);

            _graphView.schedule.Execute(_graphView.FitAllNodes).ExecuteLater(1);
        }

        /// <summary>
        /// Creates the nodes and edges of a tree branch recursively.
        /// </summary>
        /// <param name="branch">The current branch node.</param>
        /// <param name="left">The left-most available position for the subtree.</param>
        /// <param name="top">The top position for the node.</param>
        /// <param name="currentPathTypes">The active path types that should be highlighted.</param>
        /// <param name="currentStateType">The currently active concrete state type.</param>
        /// <param name="nodeLookup">The map of already created graph nodes.</param>
        private void CreateBranch(
            StateTreeNode branch,
            float left,
            float top,
            HashSet<Type> currentPathTypes,
            Type currentStateType,
            IDictionary<Type, StateNode> nodeLookup,
            IList<StatesGraphView.ConnectionData> connections)
        {
            float branchWidth = _subtreeWidths[branch.StateType];
            bool isCurrent = currentStateType == branch.StateType;
            bool isPath = currentPathTypes.Contains(branch.StateType);
            float nodeWidth = ResolveBranchNodeWidth(branch);

            StateNode.NodeKind nodeKind = isCurrent
                ? StateNode.NodeKind.Current
                : isPath
                    ? StateNode.NodeKind.Path
                    : StateNode.NodeKind.Neutral;

            StateNode node = new(
                FormatStateTitle(branch.StateType, branch.Parent?.StateType),
                nodeKind,
                true,
                branch.Children.Count > 0,
                nodeWidth);

            node.SetPosition(new Rect(
                left + (branchWidth - nodeWidth) * 0.5f,
                top,
                nodeWidth,
                NodeHeight));

            _graphView.AddElement(node);
            nodeLookup.Add(branch.StateType, node);

            float childLeft = left;
            foreach (StateTreeNode child in branch.Children)
            {
                float childWidth = _subtreeWidths[child.StateType];

                CreateBranch(
                    child,
                    childLeft,
                    top + VerticalSpacing,
                    currentPathTypes,
                    currentStateType,
                    nodeLookup,
                    connections);

                bool highlightEdge = currentPathTypes.Contains(branch.StateType) &&
                    currentPathTypes.Contains(child.StateType);

                connections.Add(CreateConnection(
                    node,
                    nodeLookup[child.StateType],
                    highlightEdge));

                childLeft += childWidth + HorizontalSpacing;
            }
        }

        /// <summary>
        /// Connects two graph nodes using a highlighted or neutral edge.
        /// </summary>
        /// <param name="fromNode">The node placed closer to the graph root.</param>
        /// <param name="toNode">The node placed closer to the leaves.</param>
        /// <param name="highlight">Whether the edge belongs to the active path.</param>
        private static StatesGraphView.ConnectionData CreateConnection(
            StateNode fromNode,
            StateNode toNode,
            bool highlight)
        {
            Color edgeColor = highlight
                ? new Color(0.74f, 0.42f, 0.92f)
                : new Color(0.46f, 0.47f, 0.56f);

            return new StatesGraphView.ConnectionData(
                fromNode.OutputPort,
                toNode.InputPort,
                edgeColor);
        }

        /// <summary>
        /// Measures the total width required by the forest.
        /// </summary>
        /// <param name="roots">The forest roots to measure.</param>
        /// <returns>The total width in graph space.</returns>
        private float MeasureForest(IReadOnlyList<StateTreeNode> roots)
        {
            float totalWidth = 0f;

            for (int index = 0; index < roots.Count; index++)
            {
                totalWidth += MeasureBranch(roots[index]);

                if (index < roots.Count - 1)
                {
                    totalWidth += HorizontalSpacing;
                }
            }

            return totalWidth;
        }

        /// <summary>
        /// Measures the width required by a single branch.
        /// </summary>
        /// <param name="branch">The branch being measured.</param>
        /// <returns>The width in graph space.</returns>
        private float MeasureBranch(StateTreeNode branch)
        {
            if (_subtreeWidths.TryGetValue(branch.StateType, out float cachedWidth))
            {
                return cachedWidth;
            }

            float nodeWidth = ResolveBranchNodeWidth(branch);

            if (branch.Children.Count == 0)
            {
                _subtreeWidths[branch.StateType] = nodeWidth;
                return nodeWidth;
            }

            float childrenWidth = 0f;

            for (int index = 0; index < branch.Children.Count; index++)
            {
                childrenWidth += MeasureBranch(branch.Children[index]);

                if (index < branch.Children.Count - 1)
                {
                    childrenWidth += HorizontalSpacing;
                }
            }

            float measuredWidth = Mathf.Max(nodeWidth, childrenWidth);
            _subtreeWidths[branch.StateType] = measuredWidth;
            return measuredWidth;
        }

        /// <summary>
        /// Resolves the rendered width for a single tree node.
        /// </summary>
        /// <param name="branch">The branch whose node width should be measured.</param>
        /// <returns>The node width in graph space.</returns>
        private float ResolveBranchNodeWidth(StateTreeNode branch)
        {
            if (_nodeWidths.TryGetValue(branch.StateType, out float cachedWidth))
            {
                return cachedWidth;
            }

            float width = ResolveTitleWidth(
                FormatStateTitle(branch.StateType, branch.Parent?.StateType),
                false);

            _nodeWidths[branch.StateType] = width;
            return width;
        }

        /// <summary>
        /// Formats a state title to be readable inside the visualizer graph.
        /// </summary>
        /// <param name="stateType">The state type being displayed.</param>
        /// <param name="parentType">The direct parent state type in the inheritance graph.</param>
        /// <returns>The formatted title.</returns>
        private static string FormatStateTitle(Type stateType, Type parentType)
        {
            if (stateType == null)
            {
                return string.Empty;
            }

            string rawTitle = stateType.Name;

            if (parentType != null &&
                rawTitle.StartsWith(parentType.Name, StringComparison.Ordinal) &&
                rawTitle.Length > parentType.Name.Length)
            {
                rawTitle = rawTitle.Substring(parentType.Name.Length);
            }

            string formattedTitle = ObjectNames.NicifyVariableName(rawTitle);

            return string.IsNullOrWhiteSpace(formattedTitle)
                ? stateType.Name
                : formattedTitle;
        }

        /// <summary>
        /// Estimates a width that better fits the rendered title.
        /// </summary>
        /// <param name="title">The title displayed inside the node.</param>
        /// <param name="isOwner">Whether the title belongs to the owner node.</param>
        /// <returns>The estimated width in graph space.</returns>
        private static float ResolveTitleWidth(string title, bool isOwner)
        {
            if (string.IsNullOrEmpty(title))
            {
                return isOwner ? MinOwnerNodeWidth : MinNodeWidth;
            }

            float baseWidth = title.Length * (isOwner ? 10.5f : 8.25f) + 34f;

            return isOwner
                ? Mathf.Clamp(baseWidth, MinOwnerNodeWidth, MaxOwnerNodeWidth)
                : Mathf.Clamp(baseWidth, MinNodeWidth, MaxNodeWidth);
        }

        #endregion
    }
}