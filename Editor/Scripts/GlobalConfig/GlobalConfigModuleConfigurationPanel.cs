#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Editor.Modules;
using IndieGabo.HandyTools.GlobalConfig;
using IndieGabo.HandyTools.GlobalConfig.JsonTree;
using IndieGabo.HandyTools.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.GlobalConfig
{
    /// <summary>
    /// UI Toolkit configuration panel for the GlobalConfig module.
    /// </summary>
    public sealed class GlobalConfigModuleConfigurationPanel : HandyModuleConfigurationPanelBase
    {
        private Button _btnReload;
        private Button _btnSave;
        private Button _btnNewRoot;
        private ListView _rootList;
        private Label _detailHeader;
        private ScrollView _detailScroll;
        private TextField _tfAddKey;
        private Button _btnAddString;
        private Button _btnAddNumber;
        private Button _btnAddBool;

        private List<string> _rootKeys = new();
        private string _currentRoot = string.Empty;

        /// <inheritdoc />
        public override HandyModuleDescriptor Descriptor => GlobalConfigModuleDefinition.Descriptor;

        /// <inheritdoc />
        public override IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            GlobalConfigModuleDefinition.Dependencies;

        /// <inheritdoc />
        protected override void BuildPanel(VisualElement root, HandyModuleEditorContext context)
        {
            GlobalsFileUtility.EnsureGlobalsFileExists();
            Globals.LoadFromGlobals();

            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(
                "UI Toolkit/GlobalConfig/GlobalsEditorWindow"
            );

            if (visualTreeAsset == null)
            {
                root.Add(new HelpBox(
                    "GlobalConfig UI template could not be loaded from Resources/UI Toolkit/GlobalConfig/GlobalsEditorWindow.",
                    HelpBoxMessageType.Error
                ));
                return;
            }

            root.style.paddingLeft = 4f;
            root.style.paddingRight = 4f;
            root.style.paddingTop = 4f;
            root.style.paddingBottom = 4f;

            visualTreeAsset.CloneTree(root);
            CacheReferences(root);
            WireHandlers();
            RefreshRootList();
            SelectFirstRootIfAny();
        }

        private void CacheReferences(VisualElement root)
        {
            _btnReload = root.Q<Button>("btnReload");
            _btnSave = root.Q<Button>("btnSave");
            _btnNewRoot = root.Q<Button>("btnNewRoot");
            _rootList = root.Q<ListView>("rootList");
            _detailHeader = root.Q<Label>("detailHeader");
            _detailScroll = root.Q<ScrollView>("detailScroll");
            _tfAddKey = root.Q<TextField>("tfAddKey");
            _btnAddString = root.Q<Button>("btnAddString");
            _btnAddNumber = root.Q<Button>("btnAddNumber");
            _btnAddBool = root.Q<Button>("btnAddBool");
        }

        private void WireHandlers()
        {
            if (_btnReload != null)
            {
                _btnReload.clicked += () =>
                {
                    Globals.LoadFromGlobals();
                    RefreshRootList();
                    ClearDetail();
                    EditorUtility.DisplayDialog(
                        "Globals",
                        "Reloaded from Assets/Resources/globals",
                        "OK"
                    );
                };
            }

            if (_btnSave != null)
            {
                _btnSave.clicked += () =>
                {
                    Globals.SaveGlobalsToDisk();
                    EditorUtility.DisplayDialog(
                        "Globals",
                        "Saved to Assets/Resources/globals",
                        "OK"
                    );
                };
            }

            if (_btnNewRoot != null)
            {
                _btnNewRoot.clicked += OnNewRootClicked;
            }

            if (_rootList != null)
            {
                _rootList.selectionChanged += OnRootSelectionChanged;
                _rootList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            }

            if (_btnAddString != null)
            {
                _btnAddString.clicked += () =>
                {
                    TryAddChildToCurrentRoot(_tfAddKey?.value, string.Empty);
                };
            }

            if (_btnAddNumber != null)
            {
                _btnAddNumber.clicked += () =>
                {
                    TryAddChildToCurrentRoot(_tfAddKey?.value, 0);
                };
            }

            if (_btnAddBool != null)
            {
                _btnAddBool.clicked += () =>
                {
                    TryAddChildToCurrentRoot(_tfAddKey?.value, false);
                };
            }
        }

        private void RefreshRootList()
        {
            _rootKeys = CollectRootKeysInFileOrder();

            if (_rootList == null)
            {
                return;
            }

            _rootList.itemsSource = _rootKeys;
            _rootList.makeItem = () =>
            {
                VisualElement row = new();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 2f;
                row.style.paddingBottom = 2f;

                Label label = new();
                label.name = "key";
                label.style.flexGrow = 1f;

                Button removeButton = new() { text = "Remove" };
                removeButton.name = "remove";

                row.Add(label);
                row.Add(removeButton);
                return row;
            };
            _rootList.bindItem = (element, index) =>
            {
                string key = _rootKeys[index];

                Label label = element.Q<Label>("key");
                Button removeButton = element.Q<Button>("remove");

                if (label != null)
                {
                    label.text = key;
                }

                if (removeButton == null)
                {
                    return;
                }

                removeButton.clicked -= DelClicked;
                removeButton.clicked += DelClicked;

                void DelClicked()
                {
                    TryDeleteRoot(key);
                }
            };

            _rootList.Rebuild();
        }

        private static List<string> CollectRootKeysInFileOrder()
        {
            try
            {
                ObjectNode rootObject = Globals.GetTree().RootObject;
                List<string> keys = new(capacity: rootObject.Children.Count);

                foreach (string key in rootObject.Children.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(key) && key != "$")
                    {
                        keys.Add(key);
                    }
                }

                return keys;
            }
            catch
            {
                return new List<string>();
            }
        }

        private void OnRootSelectionChanged(IEnumerable<object> selection)
        {
            string first = selection.FirstOrDefault() as string;
            if (string.IsNullOrEmpty(first))
            {
                ClearDetail();
                return;
            }

            ShowDetailForRoot(first);
        }

        private void OnNewRootClicked()
        {
            string entered = TextInputWindow.Prompt("New Root", "Root name:", "root");
            if (string.IsNullOrWhiteSpace(entered))
            {
                return;
            }

            JsonValueTree tree = Globals.GetTree();
            ObjectNode rootObject = tree.RootObject;
            ObjectNode node = new(entered, rootObject);
            rootObject.Set(entered, node);

            tree.RebuildIndex();

            Globals.SaveGlobalsToDisk();
            Globals.LoadFromGlobals();

            RefreshRootList();
            SelectRoot(entered);
        }

        private void TryDeleteRoot(string key)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Root",
                $"Delete root '{key}' and all its children?",
                "Delete",
                "Cancel"
            );

            if (!confirmed)
            {
                return;
            }

            Globals.Set(key, null);
            Globals.SaveGlobalsToDisk();
            Globals.LoadFromGlobals();

            RefreshRootList();
            ClearDetail();
        }

        private void SelectFirstRootIfAny()
        {
            if (_rootKeys.Count == 0 || _rootList == null)
            {
                return;
            }

            _rootList.SetSelection(0);
        }

        private void SelectRoot(string key)
        {
            if (_rootList == null)
            {
                return;
            }

            int index = _rootKeys.IndexOf(key);
            if (index >= 0)
            {
                _rootList.SetSelection(index);
            }
        }

        private void ShowDetailForRoot(string rootKey)
        {
            _currentRoot = rootKey;

            if (_detailHeader != null)
            {
                _detailHeader.text = $"Editing: {rootKey}";
            }

            _detailScroll?.Clear();

            JsonValueTree tree = Globals.GetTree();
            ObjectNode rootObject = tree.RootObject;

            if (!rootObject.Children.TryGetValue(rootKey, out ValueNode childNode) ||
                childNode is not ObjectNode objectNode)
            {
                _detailScroll?.Add(new Label("Root not found or not an object."));
                return;
            }

            foreach (KeyValuePair<string, ValueNode> entry in objectNode.Children)
            {
                string childKey = entry.Key;
                string fullPath = $"{rootKey}.{childKey}";
                VisualElement row = BuildChildRow(fullPath, entry.Value);
                _detailScroll?.Add(row);
            }
        }

        private VisualElement BuildChildRow(string fullPath, ValueNode node)
        {
            VisualElement row = new();
            row.AddToClassList("handy-container-horizontal");

            Label keyLabel = new(fullPath);
            keyLabel.style.flexGrow = 1f;

            if (node is PrimitiveNode primitiveNode)
            {
                VisualElement editor = BuildPrimitiveEditor(fullPath, primitiveNode);
                row.Add(keyLabel);
                row.Add(editor);
            }
            else
            {
                row.Add(keyLabel);
                row.Add(new Label("[Object]"));
            }

            Button removeButton = new(() =>
            {
                Globals.Set(fullPath, null);
                OnDetailMutated();
            })
            {
                text = "Remove",
            };

            row.Add(removeButton);
            return row;
        }

        private VisualElement BuildPrimitiveEditor(string path, PrimitiveNode primitiveNode)
        {
            object value = primitiveNode.Value;

            if (value is bool booleanValue)
            {
                Toggle toggle = new() { value = booleanValue };
                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    Globals.Set(path, changeEvent.newValue);
                    OnDetailMutated(saveAndReload: false);
                });
                return toggle;
            }

            if (value is int or long or float or double)
            {
                TextField textField = new() { value = value.ToString() ?? "0" };
                textField.RegisterCallback<FocusOutEvent>(_ =>
                {
                    if (!double.TryParse(textField.value, out double number))
                    {
                        return;
                    }

                    if (long.TryParse(textField.value, out long integer64))
                    {
                        Globals.Set(path, integer64);
                    }
                    else
                    {
                        Globals.Set(path, number);
                    }

                    OnDetailMutated(saveAndReload: false);
                });
                textField.style.minWidth = 120f;
                return textField;
            }

            TextField stringField = new() { value = value?.ToString() ?? string.Empty };
            stringField.RegisterCallback<FocusOutEvent>(_ =>
            {
                Globals.Set(path, stringField.value ?? string.Empty);
                OnDetailMutated(saveAndReload: false);
            });
            stringField.style.minWidth = 160f;
            return stringField;
        }

        private void ClearDetail()
        {
            _currentRoot = string.Empty;
            if (_detailHeader != null)
            {
                _detailHeader.text = "Select a root to edit…";
            }

            _detailScroll?.Clear();
        }

        private void OnDetailMutated(bool saveAndReload = true)
        {
            if (saveAndReload)
            {
                Globals.SaveGlobalsToDisk();
                Globals.LoadFromGlobals();
            }

            RefreshRootList();
            if (string.IsNullOrEmpty(_currentRoot))
            {
                return;
            }

            ShowDetailForRoot(_currentRoot);
            SelectRoot(_currentRoot);
        }

        private void TryAddChildToCurrentRoot(string key, object value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(_currentRoot))
            {
                return;
            }

            string trimmedKey = key.Trim();
            if (string.IsNullOrEmpty(trimmedKey))
            {
                return;
            }

            Globals.Set($"{_currentRoot}.{trimmedKey}", value);
            OnDetailMutated();
        }
    }
}
#endif