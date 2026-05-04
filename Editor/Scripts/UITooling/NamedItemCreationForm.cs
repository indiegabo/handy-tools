using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.UITooling
{
    [UxmlElement("NamedItemCreationForm")]
    public partial class NamedItemCreationForm : VisualElement
    {
        [SerializeField]
        VisualTreeAsset _template;

        static readonly string TemplatePath
            = "UI Toolkit/Tooling/NamedItemCreationForm_Template";

        private TemplateContainer _mainContainer;

        private VisualElement _containerCreation;
        private VisualElement _containerStart;
        private Button _buttonStart;
        private Button _buttonSave;
        private Button _buttonCancel;
        private TextField _fieldName;

        private Func<string, bool> _saveValidation = null;

        public NamedItemCreationForm()
        {
            var templateAsset = Resources.Load<VisualTreeAsset>($"{TemplatePath}");
            _mainContainer = templateAsset.Instantiate();

            _containerCreation = _mainContainer.Q<VisualElement>("container-creation");

            _containerStart = _mainContainer.Q<VisualElement>("container-start");

            _buttonStart = _mainContainer.Q<Button>("button-start");
            _buttonSave = _mainContainer.Q<Button>("button-save");
            _buttonCancel = _mainContainer.Q<Button>("button-cancel");

            _fieldName = _mainContainer.Q<TextField>("field-name");

            Add(_mainContainer);
        }

        public void Initialize(Func<string, bool> saveValidation = null, LabelsStruct labels = default)
        {
            _saveValidation = saveValidation;

            _buttonStart.text = string.IsNullOrEmpty(labels.start) ? "Create" : labels.start;
            _buttonStart.clicked += () => ActivateForm(true);

            _buttonSave.text = string.IsNullOrEmpty(labels.save) ? "Save" : labels.save;
            _buttonSave.clicked += Save;

            _buttonCancel.text = string.IsNullOrEmpty(labels.cancel) ? "Cancel" : labels.cancel;
            _buttonCancel.clicked += () => ActivateForm(false);

            _fieldName.RegisterCallback<KeyUpEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    Save();
                    evt.StopPropagation();
                }

                if (evt.keyCode == KeyCode.Escape)
                {
                    ActivateForm(false);
                    evt.StopPropagation();
                }
            });

            ActivateForm(false);
        }

        private void Save()
        {
            string name = _fieldName.text;
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (_saveValidation == null)
            {
                ActivateForm(false);
                return;
            }

            if (_saveValidation(name))
            {
                _fieldName.SetValueWithoutNotify(string.Empty);
                ActivateForm(false);
            }
        }

        public void ActivateForm(bool isActive)
        {
            if (isActive)
            {
                _containerCreation.style.display = DisplayStyle.Flex;
                _containerStart.style.display = DisplayStyle.None;
                _fieldName.Focus();
            }
            else
            {
                _containerCreation.style.display = DisplayStyle.None;
                _containerStart.style.display = DisplayStyle.Flex;
                _fieldName.SetValueWithoutNotify(string.Empty);
            }
        }

        public struct LabelsStruct
        {
            public string start;
            public string cancel;
            public string save;
        }
    }
}