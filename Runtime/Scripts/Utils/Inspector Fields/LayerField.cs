using System.Collections;
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.InspectorFields
{
    [Serializable]
    public class LayerField
    {
        [ValueDropdown("GetAllLayers")]
        [OnValueChanged("OnIndexValueChange")]
        [LabelText("Layer")]
        [SerializeField]
        private int _index = 0;

        private string _name;

        public int index { get => _index; set => _index = value; }
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

        // makes it work with the existing Unity methods (LoadLevel/LoadScene)
        public static implicit operator int(LayerField layerField)
        {
            return layerField.index;
        }
    }

}
