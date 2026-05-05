using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// UI Toolkit element that displays one selectable named item.
    /// </summary>
    public class SelectableItemElement : VisualElement
    {
        static readonly string TemplatePath
            = "UI Toolkit/Input/SelectableItem_Template";

        private TemplateContainer _mainContainer;
        private Label _labelName;

        private string _name;

        /// <summary>
        /// Gets or sets the display name rendered by the element.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _labelName.text = _name;
            }
        }

        /// <summary>
        /// Creates the visual tree for the selectable item element.
        /// </summary>
        public SelectableItemElement()
        {
            var templateAsset = Resources.Load<VisualTreeAsset>($"{TemplatePath}");
            _mainContainer = templateAsset.CloneTree();

            _labelName = _mainContainer.Q<Label>("label-name");


            Add(_mainContainer);
        }
    }
}