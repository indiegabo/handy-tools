using IndieGabo.HandyTools.IdentifyingModule.SceneGuids;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.IdentifyingModule.SceneGuids
{
    /// <summary>
    /// Draws <see cref="GuidReference"/> fields as object selectors backed by
    /// <see cref="GuidComponent"/> instances.
    /// </summary>
    [CustomPropertyDrawer(typeof(GuidReference))]
    public sealed class GuidReferenceDrawer : PropertyDrawer
    {
        #region GUI

        private static readonly GUIContent SceneLabel = new(
            "Containing Scene",
            "Scene asset expected to contain the referenced object."
        );

        private static readonly GUIContent ClearLabel = new(
            "Clear",
            "Remove the stored GUID reference."
        );

        #endregion

        /// <summary>
        /// Gets the total height required by the GUID field and the read-only
        /// scene line below it.
        /// </summary>
        /// <param name="property">Serialized GUID reference property.</param>
        /// <param name="label">Label provided by the inspector.</param>
        /// <returns>The full property height.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight * 2f) +
                EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary>
        /// Draws the GUID reference selector and its read-only scene metadata.
        /// </summary>
        /// <param name="position">Inspector rectangle assigned to the property.</param>
        /// <param name="property">Serialized GUID reference property.</param>
        /// <param name="label">Label provided by the inspector.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty guidProperty = property.FindPropertyRelative("serializedGuid");
            SerializedProperty nameProperty = property.FindPropertyRelative("cachedName");
            SerializedProperty sceneProperty = property.FindPropertyRelative("cachedScene");

            Rect objectFieldRect = position;
            objectFieldRect.height = EditorGUIUtility.singleLineHeight;

            Rect sceneFieldRect = objectFieldRect;
            sceneFieldRect.y +=
                EditorGUIUtility.singleLineHeight +
                EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.BeginProperty(position, label, property);

            DrawReferenceField(
                objectFieldRect,
                label,
                guidProperty,
                nameProperty,
                sceneProperty
            );
            DrawSceneField(sceneFieldRect, sceneProperty);
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws the primary object selector or the unloaded-reference state.
        /// </summary>
        /// <param name="position">Rect of the main object field.</param>
        /// <param name="label">Property label.</param>
        /// <param name="guidProperty">Serialized GUID byte array property.</param>
        /// <param name="nameProperty">Cached target name property.</param>
        /// <param name="sceneProperty">Cached scene asset property.</param>
        private static void DrawReferenceField(
            Rect position,
            GUIContent label,
            SerializedProperty guidProperty,
            SerializedProperty nameProperty,
            SerializedProperty sceneProperty
        )
        {
            Rect contentRect = EditorGUI.PrefixLabel(
                position,
                GUIUtility.GetControlID(FocusType.Passive),
                label
            );

            System.Guid currentGuid = ReadGuid(guidProperty);
            GameObject currentObject = currentGuid == System.Guid.Empty
                ? null
                : GuidManager.ResolveGuid(currentGuid);
            GuidComponent currentComponent = currentObject != null
                ? currentObject.GetComponent<GuidComponent>()
                : null;

            if (currentGuid != System.Guid.Empty && currentComponent == null)
            {
                DrawUnloadedReferenceState(contentRect, nameProperty, guidProperty, sceneProperty);
                return;
            }

            GuidComponent selectedComponent = EditorGUI.ObjectField(
                contentRect,
                currentComponent,
                typeof(GuidComponent),
                true
            ) as GuidComponent;

            if (selectedComponent == currentComponent)
            {
                UpdateMetadata(nameProperty, sceneProperty, selectedComponent);
                return;
            }

            if (selectedComponent == null)
            {
                ClearReference(guidProperty, nameProperty, sceneProperty);
                return;
            }

            WriteGuid(guidProperty, selectedComponent.GetGuid());
            UpdateMetadata(nameProperty, sceneProperty, selectedComponent);
        }

        /// <summary>
        /// Draws the unloaded-reference fallback with a clear button.
        /// </summary>
        /// <param name="position">Rect of the main object field.</param>
        /// <param name="nameProperty">Cached target name property.</param>
        /// <param name="guidProperty">Serialized GUID byte array property.</param>
        /// <param name="sceneProperty">Cached scene asset property.</param>
        private static void DrawUnloadedReferenceState(
            Rect position,
            SerializedProperty nameProperty,
            SerializedProperty guidProperty,
            SerializedProperty sceneProperty
        )
        {
            const float clearButtonWidth = 55f;

            Rect labelRect = position;
            labelRect.xMax -= clearButtonWidth;

            Rect buttonRect = position;
            buttonRect.xMin = labelRect.xMax;

            bool previousEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.LabelField(
                labelRect,
                new GUIContent(
                    nameProperty.stringValue,
                    "The referenced object is currently not loaded."
                ),
                EditorStyles.objectField
            );
            GUI.enabled = previousEnabled;

            if (GUI.Button(buttonRect, ClearLabel, EditorStyles.miniButton))
            {
                ClearReference(guidProperty, nameProperty, sceneProperty);
            }
        }

        /// <summary>
        /// Draws the read-only scene field below the main selector.
        /// </summary>
        /// <param name="position">Rect for the scene field.</param>
        /// <param name="sceneProperty">Serialized cached scene property.</param>
        private static void DrawSceneField(Rect position, SerializedProperty sceneProperty)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.indentLevel++;
            EditorGUI.ObjectField(
                position,
                SceneLabel,
                sceneProperty.objectReferenceValue,
                typeof(SceneAsset),
                false
            );
            EditorGUI.indentLevel--;
            GUI.enabled = previousEnabled;
        }

        /// <summary>
        /// Reads the GUID stored in the serialized byte array property.
        /// </summary>
        /// <param name="guidProperty">Serialized GUID byte array property.</param>
        /// <returns>The stored GUID, or <see cref="System.Guid.Empty"/>.</returns>
        private static System.Guid ReadGuid(SerializedProperty guidProperty)
        {
            byte[] bytes = new byte[16];
            int arraySize = Mathf.Min(guidProperty.arraySize, bytes.Length);

            for (int index = 0; index < arraySize; index++)
            {
                bytes[index] = (byte)guidProperty.GetArrayElementAtIndex(index).intValue;
            }

            return new System.Guid(bytes);
        }

        /// <summary>
        /// Writes a GUID into the serialized byte array property.
        /// </summary>
        /// <param name="guidProperty">Serialized GUID byte array property.</param>
        /// <param name="guid">GUID to persist.</param>
        private static void WriteGuid(SerializedProperty guidProperty, System.Guid guid)
        {
            byte[] bytes = guid == System.Guid.Empty
                ? System.Array.Empty<byte>()
                : guid.ToByteArray();

            guidProperty.arraySize = bytes.Length;
            for (int index = 0; index < bytes.Length; index++)
            {
                guidProperty.GetArrayElementAtIndex(index).intValue = bytes[index];
            }
        }

        /// <summary>
        /// Updates cached editor metadata for the selected GUID target.
        /// </summary>
        /// <param name="nameProperty">Cached target name property.</param>
        /// <param name="sceneProperty">Cached scene asset property.</param>
        /// <param name="component">Selected GUID component.</param>
        private static void UpdateMetadata(
            SerializedProperty nameProperty,
            SerializedProperty sceneProperty,
            GuidComponent component
        )
        {
            if (component == null)
            {
                nameProperty.stringValue = string.Empty;
                sceneProperty.objectReferenceValue = null;
                return;
            }

            nameProperty.stringValue = component.name;
            string scenePath = component.gameObject.scene.path;
            sceneProperty.objectReferenceValue = string.IsNullOrWhiteSpace(scenePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        /// <summary>
        /// Clears the stored GUID and its cached editor metadata.
        /// </summary>
        /// <param name="guidProperty">Serialized GUID byte array property.</param>
        /// <param name="nameProperty">Cached target name property.</param>
        /// <param name="sceneProperty">Cached scene asset property.</param>
        private static void ClearReference(
            SerializedProperty guidProperty,
            SerializedProperty nameProperty,
            SerializedProperty sceneProperty
        )
        {
            WriteGuid(guidProperty, System.Guid.Empty);
            nameProperty.stringValue = string.Empty;
            sceneProperty.objectReferenceValue = null;
        }
    }
}