
using UnityEngine;
using UnityEngine.UIElements;
using IndieGabo.HandyTools.HandyInputSystemModule.Feedbacks;
using UnityEditor.UIElements;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using IndieGabo.HandyTools.LoggerModule;
using UnityEngine.InputSystem;
using IndieGabo.HandyTools.UITooling;

namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// UI Toolkit element used to edit one feedback entry and its animations.
    /// </summary>
    public class FeedbackEntryElement : VisualElement
    {
        static readonly string TemplatePath
            = "UI Toolkit/Input/Feedbacks/FeedbackEntryElement_Template";

        private TemplateContainer _mainContainer;
        private VisualElement _containerEntry;
        private VisualElement _containerNoEntry;

        private ObjectField _fieldStaticSprite;
        private VisualElement _staticSpritePreview;
        private Image _previewImage;
        private ListView _listViewAnimations;
        private NamedItemCreationForm _creationForm;

        private FeedbackEntry _entry;
        private string _currentControlSchemeName;

        /// <summary>
        /// Creates the visual tree used to edit one feedback entry.
        /// </summary>
        public FeedbackEntryElement()
        {
            var templateAsset = Resources.Load<VisualTreeAsset>($"{TemplatePath}");
            _mainContainer = templateAsset.Instantiate();
            _mainContainer.style.flexGrow = 1;

            _containerEntry = _mainContainer.Q<VisualElement>("container-entry");
            _containerNoEntry = _mainContainer.Q<VisualElement>("container-no-entry");

            _fieldStaticSprite = _mainContainer.Q<ObjectField>("field-static-sprite");
            _staticSpritePreview = _mainContainer.Q<VisualElement>("static-sprite-preview");
            _previewImage = new Image()
            {
                scaleMode = ScaleMode.ScaleToFit
            };
            _staticSpritePreview.Add(_previewImage);

            _listViewAnimations = _mainContainer.Q<ListView>("listview-animations");

            _creationForm = _mainContainer.Q<NamedItemCreationForm>("creation-form");
            var formLabels = new NamedItemCreationForm.LabelsStruct()
            {
                start = "Create",
                save = "Save",
                cancel = "Cancel"
            };
            _creationForm.Initialize(TryAddAnimation, formLabels);

            ActivateView(false);
            style.flexGrow = 1;
            Add(_mainContainer);
        }

        /// <summary>
        /// Loads one feedback entry into the editor element.
        /// </summary>
        /// <param name="entry">Feedback entry to edit.</param>
        public void LoadEntry(FeedbackEntry entry)
        {
            if (string.IsNullOrEmpty(_currentControlSchemeName))
            {
                HandyLogger.Error(
                    $"{nameof(FeedbackEntryElement)}",
                    "Control scheme name not set!"
                );
                ClearSchemeData();
                return;
            }

            _entry = entry;

            Sprite entrySprite = _entry.GetOrCreateSprite(_currentControlSchemeName);

            _fieldStaticSprite.objectType = typeof(Sprite);
            _fieldStaticSprite.SetValueWithoutNotify(entrySprite);
            SetPreviewImage(entrySprite);
            _fieldStaticSprite.RegisterValueChangedCallback(OnStaticSpriteChanged);

            VisualElement makeListItem()
            {
                return new FeedbackAnimationElement(data =>
                {
                    RemoveAnimation(data);
                });
            }

            void bindListItem(VisualElement e, int i)
            {
                FeedbackAnimation animation = _entry.AnimationsList[i];
                (e as FeedbackAnimationElement).LoadAnimation(animation, _currentControlSchemeName);
            }

            _listViewAnimations.makeItem = makeListItem;
            _listViewAnimations.bindItem = bindListItem;
            _listViewAnimations.itemsSource = _entry.AnimationsList;
            _listViewAnimations.Rebuild();

            ActivateView(true);
        }

        /// <summary>
        /// Sets the control scheme currently being edited.
        /// </summary>
        /// <param name="controlSchemeName">Control scheme name to target.</param>
        public void SetControlSchemeName(string controlSchemeName)
        {
            _currentControlSchemeName = controlSchemeName;
            Reload();
        }

        private void Reload()
        {
            if (_entry == null) return;
            LoadEntry(_entry);
        }

        private void OnStaticSpriteChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (_entry == null || string.IsNullOrEmpty(_currentControlSchemeName)) return;

            Sprite sprite = evt.newValue as Sprite;
            _entry.SetSprite(_currentControlSchemeName, sprite);
            SetPreviewImage(sprite);
        }

        /// <summary>
        /// Clears the currently loaded scheme-specific data from the element.
        /// </summary>
        public void ClearSchemeData()
        {
            _entry = null;
            _listViewAnimations.itemsSource = null;
            SetPreviewImage(null);
            ActivateView(false);
        }

        private void SetPreviewImage(Sprite sprite)
        {
            if (sprite == null)
            {
                _staticSpritePreview.style.display = DisplayStyle.None;
            }
            else
            {
                _staticSpritePreview.style.display = DisplayStyle.Flex;
            }

            _previewImage.sprite = sprite;
        }

        private bool TryAddAnimation(string name)
        {
            if (_entry.AnimationsList == null) return false;

            if (_entry.AnimationsList.Any(x => x.name == name))
            {
                return false;
            }

            _entry.AnimationsList.Add(new FeedbackAnimation()
            {
                name = name
            });

            _listViewAnimations.RefreshItems();
            return true;
        }

        private void RemoveAnimation(FeedbackAnimation animation)
        {
            if (_entry == null) return;
            var index = _entry.AnimationsList.IndexOf(animation);
            if (index < 0) return;

            _entry.AnimationsList.RemoveAt(index);
            _listViewAnimations.RefreshItems();
        }

        private void ActivateView(bool activate)
        {
            if (activate)
            {
                _containerEntry.style.display = DisplayStyle.Flex;
                _containerNoEntry.style.display = DisplayStyle.None;
            }
            else
            {
                _containerEntry.style.display = DisplayStyle.None;
                _containerNoEntry.style.display = DisplayStyle.Flex;
            }
        }
    }
}