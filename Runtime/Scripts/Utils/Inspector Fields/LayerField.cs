using System;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.InspectorFields
{
    [Serializable]
    /// <summary>
    /// Wraps one layer selection so it can be edited through an inspector dropdown.
    /// </summary>
    public class LayerField
    {
        [SerializeField]
        private int _index = 0;

        /// <summary>
        /// Gets or sets the selected layer index.
        /// </summary>
        public int index { get => _index; set => _index = value; }

        /// <summary>
        /// Gets the name of the selected layer.
        /// </summary>
        public string name => LayerMask.LayerToName(_index);

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
