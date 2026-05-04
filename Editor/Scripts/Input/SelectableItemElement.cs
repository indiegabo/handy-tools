using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor
{
    public class SelectableItemElement : VisualElement
    {
        static readonly string TemplatePath
            = "UI Toolkit/Input/SelectableItem_Template";

        private TemplateContainer _mainContainer;
        private Label _labelName;

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _labelName.text = _name;
            }
        }

        public SelectableItemElement()
        {
            var templateAsset = Resources.Load<VisualTreeAsset>($"{TemplatePath}");
            _mainContainer = templateAsset.CloneTree();

            _labelName = _mainContainer.Q<Label>("label-name");


            Add(_mainContainer);
        }
    }
}