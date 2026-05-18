using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    internal static class CutsceneEditorUtility
    {
        public static void RecordDirectorChange(CutsceneDirector director, string actionName)
        {
            if (director == null)
            {
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                Undo.RecordObject(director, actionName);
            }

            MarkDirectorDirty(director);
        }

        public static void MarkDirectorDirty(CutsceneDirector director)
        {
            if (director == null)
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                return;
            }

            EditorUtility.SetDirty(director);

            if (director.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            }
        }

        public static CutsceneDirector GetSelectedDirector()
        {
            return Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponent<CutsceneDirector>();
        }

        public static SerializedProperty FindNodeProperty(
            SerializedObject serializedObject,
            SerializableGuid nodeId)
        {
            if (serializedObject == null)
            {
                return null;
            }

            SerializedProperty nodesProperty = serializedObject.FindProperty("_graph._nodes");

            if (nodesProperty == null || !nodesProperty.isArray)
            {
                return null;
            }

            for (int index = 0; index < nodesProperty.arraySize; index++)
            {
                SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(index);
                SerializedProperty idProperty = nodeProperty.FindPropertyRelative("_id");

                if (SerializableGuidEquals(idProperty, nodeId))
                {
                    return nodeProperty;
                }
            }

            return null;
        }

        private static bool SerializableGuidEquals(
            SerializedProperty guidProperty,
            SerializableGuid value)
        {
            if (guidProperty == null)
            {
                return false;
            }

            return guidProperty.FindPropertyRelative(nameof(SerializableGuid.Part1)).uintValue == value.Part1
                && guidProperty.FindPropertyRelative(nameof(SerializableGuid.Part2)).uintValue == value.Part2
                && guidProperty.FindPropertyRelative(nameof(SerializableGuid.Part3)).uintValue == value.Part3
                && guidProperty.FindPropertyRelative(nameof(SerializableGuid.Part4)).uintValue == value.Part4;
        }
    }
}