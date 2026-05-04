using IndieGabo.HandyTools.Identifying.SceneGuids;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor.Identifying.SceneGuids
{
    /// <summary>
    /// Displays the resolved GUID owned by a <see cref="GuidComponent"/>.
    /// </summary>
    [CustomEditor(typeof(GuidComponent))]
    public sealed class GuidComponentDrawer : UnityEditor.Editor
    {
        private GuidComponent _guidComponent;

        /// <summary>
        /// Draws the default inspector together with the resolved runtime GUID.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (_guidComponent == null)
            {
                _guidComponent = (GuidComponent)target;
            }

            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Guid", _guidComponent.GetGuid().ToString());
        }
    }
}