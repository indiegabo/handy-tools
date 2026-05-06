
using UnityEngine;
using UnityEngine.UIElements;
using IndieGabo.HandyTools.HandyInputSystemModule.Feedbacks;
using UnityEditor.UIElements;
using UnityEditor;
using System;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// UI Toolkit element used to edit one feedback animation entry.
    /// </summary>
    public class FeedbackAnimationElement : VisualElement
    {
        static readonly string TemplatePath
            = "UI Toolkit/Input/Feedbacks/FeedbackAnimationElement_Template";

        private TemplateContainer _mainContainer;

        private FeedbackAnimation _animation;
        private string _controlSchemeName;
        private FeedbackAnimationData _animationData;

        private Foldout _foldout;
        private TextField _fieldName;
        private ObjectField _fieldPrefab;
        private ListView _listViewSprites;

        private UnityAction<FeedbackAnimation> _onDeleteAction;

        /// <summary>
        /// Creates the visual tree used to edit one feedback animation.
        /// </summary>
        /// <param name="onDeleAction">Callback invoked when the animation is deleted.</param>
        public FeedbackAnimationElement(UnityAction<FeedbackAnimation> onDeleAction)
        {
            _onDeleteAction = onDeleAction;
            var templateAsset = Resources.Load<VisualTreeAsset>($"{TemplatePath}");
            _mainContainer = templateAsset.Instantiate();

            _foldout = _mainContainer.Q<Foldout>("foldout");
            GenerateDeleteButton(_foldout);

            _fieldName = _mainContainer.Q<TextField>("field-name");
            _fieldName.RegisterValueChangedCallback(OnNameChanged);

            _fieldPrefab = _mainContainer.Q<ObjectField>("field-prefab");
            _fieldPrefab.RegisterValueChangedCallback(OnPrefabChanged);

            _listViewSprites = _mainContainer.Q<ListView>("listview-sprites");

            Add(_mainContainer);
        }

        /// <summary>
        /// Loads one animation and the targeted control scheme data into the element.
        /// </summary>
        /// <param name="animation">Animation to edit.</param>
        /// <param name="controlSchemeName">Control scheme currently being edited.</param>
        public void LoadAnimation(FeedbackAnimation animation, string controlSchemeName)
        {
            _animation = animation;
            _controlSchemeName = controlSchemeName;
            _animationData = _animation.GetAnimationData(controlSchemeName);

            _foldout.value = false;
            ChangeFoldoutText(_animation.name);
            _fieldName.SetValueWithoutNotify(_animation.name);
            _fieldPrefab.SetValueWithoutNotify(_animationData.prefab);

            _listViewSprites.makeItem = () =>
            {
                var objectField = new ObjectField()
                {
                    objectType = typeof(Sprite)
                };
                return objectField;
            };

            _listViewSprites.bindItem = (element, i) =>
            {
                var objectField = element as ObjectField;
                objectField.objectType = typeof(Sprite);
                objectField.SetValueWithoutNotify(_animationData.sprites[i]);

                objectField.RegisterValueChangedCallback(evt =>
                {
                    _animationData.sprites[i] = evt.newValue as Sprite;
                });
            };

            _listViewSprites.itemsSource = _animationData.sprites;


            _listViewSprites.Rebuild();
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            if (_animation == null) return;
            string stripped = evt.newValue.Replace(" ", "");
            _fieldName.SetValueWithoutNotify(stripped);
            _animation.name = stripped;
            ChangeFoldoutText(_animation.name);
        }

        private void OnPrefabChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (_animation == null) return;
            GameObject prefab = evt.newValue as GameObject;
            _animationData.prefab = prefab;
        }


        private void ChangeFoldoutText(string text)
        {
            _foldout.text = !string.IsNullOrEmpty(text) ? text : "Animation";
        }

        private Button GenerateDeleteButton(Foldout foldout)
        {
            Toggle toggle = foldout.Q<Toggle>();
            Button deleteButton = new()
            {
                text = "Delete",
            };

            deleteButton.style.justifyContent = Justify.Center;
            deleteButton.style.paddingBottom = 5;
            deleteButton.style.paddingLeft = 5;
            deleteButton.style.paddingRight = 5;
            deleteButton.style.paddingTop = 5;

            deleteButton.clicked += () =>
            {
                if (_animation == null) return;
                _onDeleteAction.Invoke(_animation);
            };

            toggle.Insert(1, deleteButton);

            return deleteButton;
        }
    }
}