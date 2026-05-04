using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.HandyInputSystem;
using IndieGabo.HandyTools.HandyInputSystem.Feedbacks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using System;

namespace IndieGabo.HandyTools.Editor
{
    public class FeedbackContainerWindow : EditorWindow
    {
        static readonly string TemplatePath
            = "UI Toolkit/Input/Feedbacks/FeedbackContainerWindow_Template";

        static readonly string ContainerPrefKey = "HandyTools.FeedbackContainer";

        public static void Open(FeedbackContainer feedbackContainer)
        {
            string path = AssetDatabase.GetAssetPath(feedbackContainer.GetEntityId());
            EditorPrefs.SetString(ContainerPrefKey, path);

            FeedbackContainerWindow window = GetWindow<FeedbackContainerWindow>();
            window.titleContent = new GUIContent("Feedback Container");
            window.minSize = new Vector2(760, 400);
            window.Show();
        }

        [OnOpenAsset]
        public static bool OpenAsset(int instanceID, int line)
        {
            UnityEngine.Object selectedObject = Selection.activeObject;
            if (selectedObject == null)
            {
                return false;
            }

            FeedbackContainer container = selectedObject as FeedbackContainer;

            if (container != null)
            {
                Open(container);
                return true;
            }

            return false; // we did not handle the open
        }

        private ProjectInputConfig Config => ProjectInputConfig.Get();

        private TemplateContainer _mainContainer;
        private ObjectField _fieldActionAsset;
        private VisualElement _paneWrapper;
        private VisualElement _paneOne;
        private VisualElement _paneTwo;
        private ListView _uiListMaps;
        private ListView _uiListActions;
        private ToolbarMenu _menuSchemes;
        private ToolbarButton _buttonSave;
        private FeedbackEntryElement _entryElement;

        private FeedbackContainer _feedbackContainer;
        private Dictionary<string, InputControlScheme> _schemes = new();
        private Dictionary<string, InputActionMap> _maps = new();
        private Dictionary<Guid, InputAction> _actions = new();

        private List<InputActionMap> _mapsList = new();
        private List<InputAction> _actionsList = new();

        private string _currentlySelectedMap;

        /// <summary>
        /// Key is the map name.
        /// </summary>
        private Dictionary<string, InputAction> _selectedActionsCache = new();


        public void CreateGUI()
        {
            var templateAsset = Resources.Load<VisualTreeAsset>($"{TemplatePath}");
            _mainContainer = templateAsset.CloneTree();
            _mainContainer.style.flexGrow = 1;

            if (EditorPrefs.HasKey(ContainerPrefKey))
            {
                string path = EditorPrefs.GetString(ContainerPrefKey);
                _feedbackContainer = AssetDatabase.LoadAssetAtPath<FeedbackContainer>(path);
                SetContainer(_feedbackContainer);
            }

            rootVisualElement.Add(_mainContainer);
        }

        private void SetContainer(FeedbackContainer feedbackContainer)
        {
            _feedbackContainer = feedbackContainer;

            InitializeData();

            _fieldActionAsset = _mainContainer.Q<ObjectField>("field-action-asset");
            _fieldActionAsset.objectType = typeof(FeedbackContainer);
            _fieldActionAsset.SetValueWithoutNotify(_feedbackContainer);
            _fieldActionAsset.SetEnabled(false);

            _paneWrapper = _mainContainer.Q<VisualElement>("pane-wrapper");
            _paneOne = _mainContainer.Q<VisualElement>("pane-one");
            _paneTwo = _mainContainer.Q<VisualElement>("pane-two");

            _entryElement = new FeedbackEntryElement();
            _entryElement.style.flexGrow = 1;
            _mainContainer.Q<VisualElement>("content-entries").Add(_entryElement);

            _menuSchemes = _mainContainer.Q<ToolbarMenu>("menu-schemes");
            InitializeSchemesMenu();

            _uiListMaps = _mainContainer.Q<ListView>("list-maps");
            _uiListMaps.makeItem = () => new SelectableItemElement();
            _uiListMaps.bindItem = (element, i) =>
            {
                var selectableItemElement = element as SelectableItemElement;
                selectableItemElement.Name = _mapsList[i].name;
            };
            _uiListMaps.itemsSource = _mapsList;
            _uiListMaps.selectionChanged += OnMapSelected;

            _uiListActions = _mainContainer.Q<ListView>("list-actions");
            _uiListActions.makeItem = () => new SelectableItemElement();
            _uiListActions.bindItem = (element, i) =>
            {
                var selectableItemElement = element as SelectableItemElement;
                selectableItemElement.Name = _actionsList[i].name;
            };
            _uiListActions.itemsSource = _actionsList;
            _uiListActions.selectionChanged += OnActionSelected;

            _buttonSave = _mainContainer.Q<ToolbarButton>("button-save");
            _buttonSave.clicked += SaveContainer;

            if (_mapsList.Count > 0)
            {
                _uiListMaps.SetSelection(0);
            }
        }

        private void Update()
        {
            if (_paneWrapper == null || _paneOne == null) return;

            float paneWrapperHeight = _paneWrapper.resolvedStyle.height;
            if (paneWrapperHeight <= 0f) return;

            _paneOne.style.height = paneWrapperHeight;
            // _paneTwo.style.height = paneWrapperHeight;
        }

        private void OnDestroy()
        {
            SaveContainer();
            EditorPrefs.DeleteKey(ContainerPrefKey);
        }

        private void InitializeData()
        {
            foreach (var scheme in _feedbackContainer.ActionAsset.controlSchemes)
            {
                _schemes.Add(scheme.name, scheme);
            }

            _mapsList = _feedbackContainer.ActionAsset.actionMaps.ToList();
            foreach (var map in _mapsList)
            {
                _maps.Add(map.name, map);

                foreach (var action in map.actions)
                {
                    _actions.Add(action.id, action);
                }
            }
        }

        /// <summary>
        /// Must be called after <see cref="InitializeData"/>
        /// </summary>
        private void InitializeSchemesMenu()
        {
            _menuSchemes.menu.ClearItems();

            foreach (InputControlScheme scheme in _schemes.Values)
            {
                _menuSchemes.menu.AppendAction(
                    scheme.name,
                    (action) =>
                    {
                        _menuSchemes.text = scheme.name;
                        ChangeControlScheme(scheme);
                    }
                );
            }

            string fallbackName = FeedbackEntry.FallbackSchemeControlName;
            fallbackName = char.ToUpper(fallbackName[0]) + fallbackName[1..];
            _menuSchemes.menu.AppendAction(
                fallbackName,
                (action) =>
                {
                    _menuSchemes.text = fallbackName;
                    ChangeControlScheme(fallbackName);
                }
            );

            if (_schemes.Count > 0)
            {
                InputControlScheme scheme = _schemes.First().Value;
                ChangeControlScheme(scheme);
                _menuSchemes.text = scheme.name;
            }
            else
            {
                ChangeControlScheme(fallbackName);
                _menuSchemes.text = fallbackName;
            }
        }

        private void OnMapSelected(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null || selectedItems.Count() == 0)
            {
                _entryElement.ClearSchemeData();
                return;
            }

            InputActionMap selectedMap = selectedItems.First() as InputActionMap;
            _currentlySelectedMap = selectedMap.name;
            ActivateMap(selectedMap);
        }

        private void ActivateMap(InputActionMap map)
        {
            _actionsList.Clear();

            if (map != null)
            {
                foreach (var action in map.actions)
                {
                    _actionsList.Add(action);
                }
            }

            if (_actionsList.Count == 0)
            {
                _selectedActionsCache.Remove(_currentlySelectedMap);
                _uiListActions.Rebuild();
                return;
            }

            if (_selectedActionsCache.TryGetValue(
                _currentlySelectedMap,
                out InputAction selectedAction
            ))
            {
                int actionIndex = _actionsList.IndexOf(selectedAction);
                _uiListActions.SetSelection(actionIndex);
                DisplayData(selectedAction);
            }
            else
            {
                _selectedActionsCache.Remove(_currentlySelectedMap);
                _uiListActions.SetSelection(0);
                DisplayData(_actionsList[0]);
            }

            _uiListActions.Rebuild();
        }

        private void OnActionSelected(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null || selectedItems.Count() == 0) return;

            InputAction selectedAction = selectedItems.First() as InputAction;

            _selectedActionsCache[_currentlySelectedMap] = selectedAction;
            DisplayData(selectedAction);
        }

        private void ChangeControlScheme(InputControlScheme controlScheme)
        {
            ChangeControlScheme(controlScheme.name);
        }

        private void ChangeControlScheme(string controlSchemeName)
        {
            _entryElement.SetControlSchemeName(controlSchemeName);
        }

        private void DisplayData(InputAction action)
        {
            if (action == null)
            {
                _entryElement.ClearSchemeData();
                return;
            }

            FeedbackEntry entry = _feedbackContainer.GetOrCreateEntry(action.id);
            _entryElement.LoadEntry(entry);
        }

        private void SaveContainer()
        {
            if (_feedbackContainer != null)
            {
                _buttonSave.SetEnabled(false);
                EditorUtility.SetDirty(_feedbackContainer);
                AssetDatabase.SaveAssetIfDirty(_feedbackContainer);
                _buttonSave.SetEnabled(true);
            }
        }
    }
}
