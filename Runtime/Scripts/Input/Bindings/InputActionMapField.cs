using System.Collections;
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystemModule.Bindings
{
    /// <summary>
    /// Serializes an action-map identifier selected from one InputActionAsset.
    /// </summary>
    [Serializable]
    public class InputActionMapField
    {
        #region Inspector        

        [BoxGroup("Configuration")]
        [SerializeField]
        private InputActionAsset _inputActionAsset;

        [BoxGroup("Configuration")]
        [ValueDropdown("GetActionMapsIds")]
        [LabelText("Map")]
        [SerializeField]
        private string _mapId;

        #endregion

        #region Getters

        /// <summary>
        /// Gets the serialized action-map identifier.
        /// </summary>
        public string MapId => _mapId;

        #endregion

        #region Impliciting

        /// <summary>
        /// Converts the wrapper to the stored map identifier.
        /// </summary>
        /// <param name="inputActionMapField">Wrapped map field value.</param>
        /// <returns>The serialized action-map identifier.</returns>
        public static implicit operator string(InputActionMapField inputActionMapField)
        {
            return inputActionMapField.MapId;
        }

        #endregion                

        #region Callbacks

        /// <summary>
        /// Builds the Odin dropdown list from the configured action asset.
        /// </summary>
        /// <returns>An enumerable list of action-map options.</returns>
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