using System;
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

        [SerializeField]
        private InputActionAsset _inputActionAsset;

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

    }
}