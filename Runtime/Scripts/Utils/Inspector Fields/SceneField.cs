using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Utils.InspectorFields
{
    /// <summary>
    /// This is a solution to working with scene assets through Unity's inspector.
    /// It was originally found on a comment by "glitchers" at https://answers.unity.com/questions/242794/inspector-field-for-scene-asset.html#answer-1204071
    /// I moved the property drawer solution into an editor folder.
    /// </summary>
    [System.Serializable]
    public class SceneField
    {
        [BoxGroup("Scene")]
        [SerializeField]
        private Object _sceneAsset;

        [BoxGroup("Scene")]
        [SerializeField]
        private string _sceneName = "";

        public Object SceneAsset
        {
            get { return _sceneAsset; }
            set { _sceneAsset = value; }
        }

        public string SceneName
        {
            get { return _sceneName; }
            set { _sceneName = value; }
        }

        // makes it work with the existing Unity methods (LoadLevel/LoadScene)
        public static implicit operator string(SceneField sceneField)
        {
            return sceneField.SceneName;
        }

        public override string ToString()
        {
            return _sceneName;
        }

#if UNITY_EDITOR
        public static SceneField FromAsset(SceneAsset asset)
        {
            SceneField sceneField = new()
            {
                SceneAsset = asset,
                SceneName = asset.name
            };
            return sceneField;
        }
#endif
    }

}
