using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils.InspectorFields
{
    [Serializable]
    public class TagField
    {
        [ValueDropdown("GetAllTags")]
        [SerializeField]
        private string _tag;

        public string Tag { get => _tag; set => _tag = value; }

#if UNITY_EDITOR
        private string[] GetAllTags()
        {
            return UnityEditorInternal.InternalEditorUtility.tags;
        }
#endif

        // makes it work with the existing Unity methods (LoadLevel/LoadScene)
        public static implicit operator string(TagField tagField)
        {
            return tagField.Tag;
        }
    }

}
