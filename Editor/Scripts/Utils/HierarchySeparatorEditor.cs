using IndieGabo.HandyTools.Utils;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor.Utils
{
    [CustomEditor(typeof(HierarchySeparator))]
    [CanEditMultipleObjects]
    /// <summary>
    /// Custom inspector for hierarchy separator styling values.
    /// </summary>
    public class HierarchySeparatorEditor : UnityEditor.Editor
    {
        private SerializedProperty _outlineSize;
        private SerializedProperty _outlineColor;
        private SerializedProperty _barColor;
        private SerializedProperty _textColor;

        /// <summary>
        /// Resolves the serialized properties used by the inspector.
        /// </summary>
        public void OnEnable()
        {
            _outlineSize = serializedObject.FindProperty("m_OutlineSize");
            _outlineColor = serializedObject.FindProperty("m_OutlineColor");
            _barColor = serializedObject.FindProperty("m_BarColor");
            _textColor = serializedObject.FindProperty("m_TextColor");
        }

        /// <summary>
        /// Draws the custom inspector for the hierarchy separator component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_outlineSize);
            EditorGUILayout.PropertyField(_outlineColor);
            EditorGUILayout.PropertyField(_barColor);
            EditorGUILayout.PropertyField(_textColor);

            serializedObject.ApplyModifiedProperties();
        }
    }
}