using System.Collections;
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.InspectorFields
{
    [Serializable]
    /// <summary>
    /// Wraps one layer selection so it can be edited through an inspector dropdown.
    /// </summary>
    public class LayerField
    {
        [BoxGroup("Layer")]
        [ValueDropdown("GetAllLayers")]
        [OnValueChanged("OnIndexValueChange")]
        [LabelText("Layer")]
        [SerializeField]
        private int _index = 0;

        private string _name;

        /// <summary>
        /// Gets or sets the selected layer index.
        /// </summary>
        public int index { get => _index; set => _index = value; }

        /// <summary>
        /// Gets the name of the selected layer.
        /// </summary>
        public string name => _name;

        private void OnIndexValueChange(int index)
        {
            _name = LayerMask.LayerToName(index);
        }

        private IEnumerable GetAllLayers()
        {
            ValueDropdownList<int> list = new ValueDropdownList<int>();
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName)) continue;
                list.Add(layerName, i);
            }
            return list;
        }

        /// <summary>
        /// Converts the wrapper to the selected layer index.
        /// </summary>
        /// <param name="layerField">Wrapper instance to convert.</param>
        public static implicit operator int(LayerField layerField)
        {
            return layerField.index;
        }
    }

}
