using UnityEngine;
using Sirenix.OdinInspector;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Utils.Identifying
{
    public class IdentifiableScriptableObject : SerializedScriptableObject
    {
        #region Inspector

        [BoxGroup("Identification")]
        [LabelText("ID")]
        [LabelWidth(100)]
        [ReadOnly]
        [SerializeField]
        private Identifier _identifier;

#if UNITY_EDITOR
        [BoxGroup("Identification")]
        [Button("GenerateID")]
        private void GenerateID()
        {
            var path = AssetDatabase.GetAssetPath(this);
            _identifier = new Identifier();
            _identifier.Initialize(AssetDatabase.AssetPathToGUID(path));
        }
#endif

        #endregion

        #region  Getters

        public Identifier Identifier => _identifier;
        public string ID => _identifier.ToGuidString();

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            GenerateID();
        }
#endif
        #endregion
    }
}