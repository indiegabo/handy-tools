using System;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Provides one shared set of editor helpers for serialized graph blackboard drawers.
    /// </summary>
    public static class GraphBlackboardDrawerSharedUtility
    {
        private const float InlineActionButtonWidth = 18f;
        private const float InlineActionButtonSpacing = 4f;
        private const string Part1FieldName = nameof(SerializableGuid.Part1);
        private const string Part2FieldName = nameof(SerializableGuid.Part2);
        private const string Part3FieldName = nameof(SerializableGuid.Part3);
        private const string Part4FieldName = nameof(SerializableGuid.Part4);
        private const string EntryIdFieldName = "_entryId";
        private const string EntryKeyFieldName = "_entryKey";
        private const string ValueTypeNameFieldName = "_valueTypeName";
        private const string ModeFieldName = "_mode";
        private const string BlackboardVariableFieldName = "_blackboardVariable";
        private const string DirectValueFieldName = "_directValue";
        private const string ExpectedValueTypeNameFieldName = "_expectedValueTypeName";
        private const string BoxedValueFieldName = "_value";
        private const string ValueFieldName = "Value";
        private const string EnumTypeNameFieldName = "_enumTypeName";
        private const string EnumValueNameFieldName = "_valueName";
        private const string ObjectTypeNameFieldName = "_objectTypeName";

        private static GUIStyle s_inlineActionButtonStyle;

        /// <summary>
        /// Gets one standard single-line editor height.
        /// </summary>
        public static float SingleLineHeight => EditorGUIUtility.singleLineHeight;

        /// <summary>
        /// Gets the horizontal space reserved for one inline action button and its gap.
        /// </summary>
        public static float InlineActionButtonReservedWidth =>
            InlineActionButtonWidth + InlineActionButtonSpacing;

        /// <summary>
        /// Gets one standard vertical spacing value for stacked editor fields.
        /// </summary>
        public static float VerticalSpacing => EditorGUIUtility.standardVerticalSpacing;

        /// <summary>
        /// Gets the height used by one blackboard variable reference field.
        /// </summary>
        /// <returns>The rendered field height.</returns>
        public static float GetVariableReferenceHeight()
        {
            return SingleLineHeight;
        }

        /// <summary>
        /// Gets the height used by one value-source field.
        /// </summary>
        /// <param name="property">Serialized value-source property.</param>
        /// <returns>The rendered field height.</returns>
        public static float GetValueSourceHeight(SerializedProperty property)
        {
            if (property == null)
            {
                return SingleLineHeight;
            }

            SerializedProperty directValueProperty = property.FindPropertyRelative(
                DirectValueFieldName);
            SerializedProperty boxedValueProperty = ResolveBoxedValueProperty(
                directValueProperty);

            if (boxedValueProperty == null)
            {
                return SingleLineHeight;
            }

            return Mathf.Max(
                SingleLineHeight,
                EditorGUI.GetPropertyHeight(
                    boxedValueProperty,
                    GUIContent.none,
                    includeChildren: true));
        }

        /// <summary>
        /// Resolves the cached runtime type stored by one variable reference property.
        /// </summary>
        /// <param name="variableProperty">Serialized variable-reference property.</param>
        /// <returns>The resolved runtime type when available.</returns>
        public static Type ResolveVariableReferenceValueType(SerializedProperty variableProperty)
        {
            return ResolveSerializedType(
                variableProperty
                    ?.FindPropertyRelative(ValueTypeNameFieldName)
                    ?.stringValue);
        }

        /// <summary>
        /// Resolves the expected runtime type represented by one serialized property.
        /// </summary>
        /// <param name="property">Serialized candidate property.</param>
        /// <returns>The resolved runtime type when available.</returns>
        public static Type ResolveExpectedValueTypeFromSerializedData(
            SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            if (IsVariableReferenceProperty(property))
            {
                return ResolveVariableReferenceValueType(property);
            }

            if (!IsValueSourceProperty(property))
            {
                return null;
            }

            Type serializedType = ResolveSerializedType(
                property.FindPropertyRelative(ExpectedValueTypeNameFieldName)?.stringValue);

            if (serializedType != null)
            {
                return serializedType;
            }

            SerializedProperty directValueProperty = property.FindPropertyRelative(
                DirectValueFieldName);

            if (directValueProperty == null)
            {
                return null;
            }

            SerializedProperty objectTypeNameProperty = directValueProperty.FindPropertyRelative(
                ObjectTypeNameFieldName);

            if (objectTypeNameProperty != null)
            {
                return ResolveSerializedType(objectTypeNameProperty.stringValue);
            }

            SerializedProperty enumTypeNameProperty = directValueProperty.FindPropertyRelative(
                EnumTypeNameFieldName);

            if (enumTypeNameProperty != null)
            {
                return ResolveSerializedType(enumTypeNameProperty.stringValue);
            }

            SerializedProperty boxedValueProperty = directValueProperty.FindPropertyRelative(
                BoxedValueFieldName)
                ?? directValueProperty.FindPropertyRelative(ValueFieldName);

            if (boxedValueProperty?.boxedValue != null)
            {
                return boxedValueProperty.boxedValue.GetType();
            }

            return null;
        }

        /// <summary>
        /// Gets whether one serialized property should show one explicit blackboard drop zone.
        /// </summary>
        /// <param name="property">Serialized candidate property.</param>
        /// <returns>True when the value-source is currently in direct mode.</returns>
        public static bool ShouldShowValueSourceDropZone(SerializedProperty property)
        {
            return property != null && !IsValueSourceInBlackboardMode(property);
        }

        /// <summary>
        /// Draws one direct-value editor field based on the serialized wrapper shape.
        /// </summary>
        /// <param name="position">Target drawing position.</param>
        /// <param name="valueSourceProperty">Serialized value-source property.</param>
        /// <param name="label">Field label.</param>
        /// <param name="expectedType">Expected runtime type.</param>
        /// <param name="ensureExpectedType">Callback that normalizes the current wrapper.</param>
        /// <param name="assignDirectObjectValue">Callback that stores one direct Unity object value.</param>
        /// <param name="shouldDrawBlackboardDropTarget">
        /// Optional callback that decides whether one blackboard drop target placeholder should be drawn.
        /// </param>
        public static void DrawDirectValueField(
            Rect position,
            SerializedProperty valueSourceProperty,
            GUIContent label,
            Type expectedType,
            Action<SerializedProperty, Type> ensureExpectedType,
            Action<SerializedProperty, Object, Type> assignDirectObjectValue,
            Func<Rect, SerializedProperty, Type, bool> shouldDrawBlackboardDropTarget = null)
        {
            SerializedProperty directValueProperty = valueSourceProperty
                ?.FindPropertyRelative(DirectValueFieldName);

            if (expectedType == null || directValueProperty == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("Unsupported type."));
                return;
            }

            ensureExpectedType?.Invoke(valueSourceProperty, expectedType);

            SerializedProperty boxedValueProperty = directValueProperty.FindPropertyRelative(
                BoxedValueFieldName)
                ?? directValueProperty.FindPropertyRelative(ValueFieldName);
            SerializedProperty objectTypeNameProperty = directValueProperty.FindPropertyRelative(
                ObjectTypeNameFieldName);
            SerializedProperty enumTypeNameProperty = directValueProperty.FindPropertyRelative(
                EnumTypeNameFieldName);
            SerializedProperty enumValueNameProperty = directValueProperty.FindPropertyRelative(
                EnumValueNameFieldName);

            if (objectTypeNameProperty != null && boxedValueProperty != null)
            {
                if (shouldDrawBlackboardDropTarget != null
                    && shouldDrawBlackboardDropTarget(position, valueSourceProperty, expectedType))
                {
                    DrawBlackboardValueSourceDropTarget(position, label, expectedType);
                    return;
                }

                Type objectType = typeof(Object).IsAssignableFrom(expectedType)
                    ? expectedType
                    : ResolveSerializedType(objectTypeNameProperty.stringValue) ?? typeof(Object);

                EditorGUI.BeginChangeCheck();
                Object newValue = EditorGUI.ObjectField(
                    position,
                    label,
                    boxedValueProperty.objectReferenceValue,
                    objectType,
                    true);

                if (EditorGUI.EndChangeCheck())
                {
                    assignDirectObjectValue?.Invoke(
                        valueSourceProperty.Copy(),
                        newValue,
                        expectedType);
                }

                return;
            }

            if (enumTypeNameProperty != null && enumValueNameProperty != null)
            {
                Type enumType = expectedType.IsEnum
                    ? expectedType
                    : ResolveSerializedType(enumTypeNameProperty.stringValue);

                if (enumType == null || !enumType.IsEnum)
                {
                    EditorGUI.LabelField(position, "No enum type configured.");
                    return;
                }

                string[] valueNames = Enum.GetNames(enumType);
                int selectedIndex = Mathf.Max(
                    0,
                    Array.IndexOf(valueNames, enumValueNameProperty.stringValue));

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(
                    position,
                    label.text,
                    selectedIndex,
                    valueNames);

                if (EditorGUI.EndChangeCheck()
                    && newIndex >= 0
                    && newIndex < valueNames.Length)
                {
                    enumValueNameProperty.stringValue = valueNames[newIndex];
                }

                return;
            }

            if (boxedValueProperty != null)
            {
                EditorGUI.PropertyField(position, boxedValueProperty, label, true);
                return;
            }

            EditorGUI.PropertyField(position, directValueProperty, label, true);
        }

        /// <summary>
        /// Gets the inline action-button rect that follows the primary popup rect.
        /// </summary>
        /// <param name="contentRect">Full content rect for the field.</param>
        /// <param name="primaryRectMaxX">Right edge of the primary field rect.</param>
        /// <returns>The button rect.</returns>
        public static Rect GetInlineActionButtonRect(Rect contentRect, float primaryRectMaxX)
        {
            float buttonHeight = Mathf.Min(contentRect.height, SingleLineHeight);
            float buttonY = contentRect.y + ((contentRect.height - buttonHeight) * 0.5f);

            return new Rect(
                primaryRectMaxX + InlineActionButtonSpacing,
                buttonY,
                InlineActionButtonWidth,
                buttonHeight);
        }

        /// <summary>
        /// Draws one compact inline action button.
        /// </summary>
        /// <param name="position">Button rect.</param>
        /// <param name="tooltip">Tooltip shown by the button.</param>
        /// <returns>True when the button was pressed.</returns>
        public static bool DrawInlineActionButton(Rect position, string tooltip)
        {
            return GUI.Button(
                position,
                new GUIContent("x", tooltip),
                InlineActionButtonStyle);
        }

        /// <summary>
        /// Draws one generic blackboard value-source drop target.
        /// </summary>
        /// <param name="position">Target drawing rect.</param>
        /// <param name="label">Field label.</param>
        /// <param name="expectedType">Expected runtime type.</param>
        public static void DrawBlackboardValueSourceDropTarget(
            Rect position,
            GUIContent label,
            Type expectedType)
        {
            Rect contentRect = HasVisibleLabel(label)
                ? EditorGUI.PrefixLabel(position, label)
                : position;
            string typeName = GetReadableTypeName(expectedType);

            GUI.Box(
                contentRect,
                new GUIContent($"Drop Blackboard {typeName}", label.tooltip),
                EditorStyles.objectField);
        }

        /// <summary>
        /// Attempts to resolve one dragged Unity object compatible with the expected type.
        /// </summary>
        /// <param name="expectedType">Expected runtime type.</param>
        /// <param name="draggedObject">Resolved dragged object when available.</param>
        /// <returns>True when the current drag contains one compatible Unity object.</returns>
        public static bool TryResolveDraggedUnityObject(
            Type expectedType,
            out Object draggedObject)
        {
            draggedObject = null;

            if (DragAndDrop.objectReferences == null
                || DragAndDrop.objectReferences.Length <= 0)
            {
                return false;
            }

            Object candidate = DragAndDrop.objectReferences[0];

            if (candidate == null)
            {
                return false;
            }

            Object coercedObject = CoerceDirectObjectValue(candidate, expectedType);

            if (coercedObject == null)
            {
                return false;
            }

            draggedObject = coercedObject;
            return true;
        }

        /// <summary>
        /// Coerces one Unity object into the requested expected type when possible.
        /// </summary>
        /// <param name="value">Candidate Unity object.</param>
        /// <param name="expectedType">Expected runtime type.</param>
        /// <returns>The coerced Unity object when compatible.</returns>
        public static Object CoerceDirectObjectValue(Object value, Type expectedType)
        {
            if (value == null || expectedType == null)
            {
                return null;
            }

            if (expectedType.IsInstanceOfType(value))
            {
                return value;
            }

            if (expectedType == typeof(GameObject))
            {
                return value switch
                {
                    GameObject gameObject => gameObject,
                    Component component => component.gameObject,
                    _ => null,
                };
            }

            if (typeof(Component).IsAssignableFrom(expectedType))
            {
                GameObject ownerGameObject = value switch
                {
                    GameObject gameObject => gameObject,
                    Component component => component.gameObject,
                    _ => null,
                };

                return ownerGameObject?.GetComponent(expectedType);
            }

            return null;
        }

        /// <summary>
        /// Gets whether one serialized property exposes the value-source shape.
        /// </summary>
        /// <param name="property">Serialized candidate property.</param>
        /// <returns>True when the property looks like one value-source.</returns>
        public static bool IsValueSourceProperty(SerializedProperty property)
        {
            return property?.FindPropertyRelative(ModeFieldName) != null
                && property.FindPropertyRelative(BlackboardVariableFieldName) != null;
        }

        /// <summary>
        /// Gets whether one serialized property exposes the variable-reference shape.
        /// </summary>
        /// <param name="property">Serialized candidate property.</param>
        /// <returns>True when the property looks like one variable reference.</returns>
        public static bool IsVariableReferenceProperty(SerializedProperty property)
        {
            return property?.FindPropertyRelative(EntryIdFieldName) != null
                && property.FindPropertyRelative(EntryKeyFieldName) != null
                && property.FindPropertyRelative(BlackboardVariableFieldName) == null;
        }

        /// <summary>
        /// Applies property changes and refreshes the serialized object cache.
        /// </summary>
        /// <param name="serializedObject">Serialized object to flush.</param>
        public static void ApplyPropertyChanges(SerializedObject serializedObject)
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }

        /// <summary>
        /// Gets whether one serialized value-source is currently in blackboard mode.
        /// </summary>
        /// <param name="property">Serialized value-source property.</param>
        /// <returns>True when the value-source reads from one blackboard binding.</returns>
        public static bool IsValueSourceInBlackboardMode(SerializedProperty property)
        {
            return property != null
                && property.FindPropertyRelative(ModeFieldName)?.enumValueIndex == 1;
        }

        /// <summary>
        /// Sets the current mode value on one serialized value-source property.
        /// </summary>
        /// <param name="property">Serialized value-source property.</param>
        /// <param name="modeIndex">Serialized enum index to assign.</param>
        public static void SetValueSourceMode(SerializedProperty property, int modeIndex)
        {
            SerializedProperty modeProperty = property?.FindPropertyRelative(ModeFieldName);

            if (modeProperty != null)
            {
                modeProperty.enumValueIndex = modeIndex;
            }
        }

        /// <summary>
        /// Reads one serializable guid payload from one serialized property.
        /// </summary>
        /// <param name="guidProperty">Serialized guid property.</param>
        /// <returns>The resolved serializable guid value.</returns>
        public static SerializableGuid ReadGuid(SerializedProperty guidProperty)
        {
            return guidProperty == null
                ? SerializableGuid.Empty
                : new SerializableGuid(
                    guidProperty.FindPropertyRelative(Part1FieldName).uintValue,
                    guidProperty.FindPropertyRelative(Part2FieldName).uintValue,
                    guidProperty.FindPropertyRelative(Part3FieldName).uintValue,
                    guidProperty.FindPropertyRelative(Part4FieldName).uintValue);
        }

        /// <summary>
        /// Writes one serializable guid payload into one serialized property.
        /// </summary>
        /// <param name="guidProperty">Serialized guid property.</param>
        /// <param name="value">Guid value to store.</param>
        public static void SetGuid(SerializedProperty guidProperty, SerializableGuid value)
        {
            if (guidProperty == null)
            {
                return;
            }

            guidProperty.FindPropertyRelative(Part1FieldName).uintValue = value.Part1;
            guidProperty.FindPropertyRelative(Part2FieldName).uintValue = value.Part2;
            guidProperty.FindPropertyRelative(Part3FieldName).uintValue = value.Part3;
            guidProperty.FindPropertyRelative(Part4FieldName).uintValue = value.Part4;
        }

        /// <summary>
        /// Resolves one runtime type from one serialized assembly-qualified name.
        /// </summary>
        /// <param name="serializedTypeName">Serialized type name.</param>
        /// <returns>The resolved runtime type when available.</returns>
        public static Type ResolveSerializedType(string serializedTypeName)
        {
            return string.IsNullOrWhiteSpace(serializedTypeName)
                ? null
                : Type.GetType(serializedTypeName);
        }

        /// <summary>
        /// Gets one compact readable type label.
        /// </summary>
        /// <param name="type">Runtime type to describe.</param>
        /// <returns>The readable type label.</returns>
        public static string GetReadableTypeName(Type type)
        {
            return type == null ? "Unknown" : type.Name;
        }

        /// <summary>
        /// Creates one label that hides the redundant "Source" suffix used by value-source fields.
        /// </summary>
        /// <param name="label">Original label.</param>
        /// <returns>The normalized label.</returns>
        public static GUIContent CreateSeamlessLabel(GUIContent label)
        {
            if (!HasVisibleLabel(label))
            {
                return GUIContent.none;
            }

            string labelText = label.text;

            if (labelText.EndsWith(" Source", StringComparison.Ordinal))
            {
                labelText = labelText[..^7];
            }

            return new GUIContent(labelText, label.tooltip);
        }

        /// <summary>
        /// Gets whether one GUI label should render a visible prefix.
        /// </summary>
        /// <param name="label">Candidate label.</param>
        /// <returns>True when the label contains visible text.</returns>
        public static bool HasVisibleLabel(GUIContent label)
        {
            return label != null && !string.IsNullOrWhiteSpace(label.text);
        }

        private static SerializedProperty ResolveBoxedValueProperty(
            SerializedProperty directValueProperty)
        {
            return directValueProperty?.FindPropertyRelative(BoxedValueFieldName)
                ?? directValueProperty?.FindPropertyRelative(ValueFieldName);
        }

        private static GUIStyle InlineActionButtonStyle
        {
            get
            {
                if (s_inlineActionButtonStyle != null)
                {
                    return s_inlineActionButtonStyle;
                }

                s_inlineActionButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                };

                return s_inlineActionButtonStyle;
            }
        }
    }
}