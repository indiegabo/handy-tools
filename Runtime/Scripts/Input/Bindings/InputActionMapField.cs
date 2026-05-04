using System.Collections;
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.Input.Bindings
{
    [Serializable]
    public class InputActionMapField
    {
        #region Inspector        

    [SerializeField]
        private InputActionAsset _inputActionAsset;

        [ValueDropdown("GetActionMapsIds")]
        [LabelText("Map")]
        [SerializeField]
        private string _mapId;

        #endregion

        #region Getters

        public string MapId => _mapId;

        #endregion

        #region Impliciting

        public static implicit operator string(InputActionMapField inputActionMapField)
        {
            return inputActionMapField.MapId;
        }

        #endregion                

        #region Callbacks

        private IEnumerable GetActionMapsIds()
        {
            if (_inputActionAsset == null) return default;

            ValueDropdownList<string> list = new ValueDropdownList<string>();

            foreach (InputActionMap map in _inputActionAsset.actionMaps)
            {
                list.Add(map.name, map.id.ToString());
            }

            return list;
        }

        #endregion
    }
}