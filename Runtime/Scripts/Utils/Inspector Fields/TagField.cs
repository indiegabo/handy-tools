using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.InspectorFields
{
    [Serializable]
    /// <summary>
    /// Wraps one tag selection so it can be edited through an inspector dropdown.
    /// </summary>
    public class TagField
    {
        [BoxGroup("Tag")]
        [ValueDropdown("GetAllTags")]
        [SerializeField]
        private string _tag;

        /// <summary>
        /// Gets or sets the selected Unity tag.
        /// </summary>
        public string Tag { get => _tag; set => _tag = value; }

#if UNITY_EDITOR
        private string[] GetAllTags()
        {
            return UnityEditorInternal.InternalEditorUtility.tags;
        }
#endif

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
