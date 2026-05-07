using System;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.InspectorFields
{
    [Serializable]
    /// <summary>
    /// Wraps one tag selection so it can be edited through an inspector dropdown.
    /// </summary>
    public class TagField
    {
        [SerializeField]
        private string _tag;

        /// <summary>
        /// Gets or sets the selected Unity tag.
        /// </summary>
        public string Tag { get => _tag; set => _tag = value; }

        /// <summary>
        /// Converts the wrapper to the selected tag string.
        /// </summary>
        /// <param name="tagField">Wrapper instance to convert.</param>
        public static implicit operator string(TagField tagField)
        {
            return tagField.Tag;
        }
    }

}
