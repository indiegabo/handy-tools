using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Actions;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    internal static class CutsceneBlackboardDragAndDrop
    {
        private const string DirectorDataKey =
            "HandyTools.Cutscenes.Blackboard.Director";
        private const string EntryIdDataKey =
            "HandyTools.Cutscenes.Blackboard.EntryId";

        private static CutsceneDirector s_activeDirector;
        private static SerializableGuid s_activeEntryId;
        private static string s_activeEntryLabel = string.Empty;

        internal static bool HasActiveDrag => s_activeDirector != null
            && s_activeEntryId != SerializableGuid.Empty;

        internal static string ActiveEntryLabel => s_activeEntryLabel;

        internal static void BeginDrag(
            CutsceneDirector director,
            CutsceneGraphBlackboardEntry entry)
        {
            if (director == null || entry == null)
            {
                return;
            }

            entry.EnsureId();
            s_activeDirector = director;
            s_activeEntryId = entry.Id;
            s_activeEntryLabel = entry.Key ?? string.Empty;
        }

        internal static void CancelDrag()
        {
            s_activeDirector = null;
            s_activeEntryId = SerializableGuid.Empty;
            s_activeEntryLabel = string.Empty;
        }

        internal static bool TryResolveDraggedEntry(
            CutsceneDirector expectedDirector,
            out CutsceneGraphBlackboardEntry entry)
        {
            entry = null;

            if (expectedDirector == null)
            {
                return false;
            }

            if (ReferenceEquals(s_activeDirector, expectedDirector)
                && s_activeEntryId != SerializableGuid.Empty)
            {
                return expectedDirector.Graph.Blackboard.TryGetEntry(s_activeEntryId, out entry);
            }

            if (!ReferenceEquals(
                    DragAndDrop.GetGenericData(DirectorDataKey),
                    expectedDirector))
            {
                return false;
            }

            string entryIdHex = DragAndDrop.GetGenericData(EntryIdDataKey) as string;

            if (string.IsNullOrWhiteSpace(entryIdHex))
            {
                return false;
            }

            SerializableGuid entryId = SerializableGuid.FromHexString(entryIdHex);

            return expectedDirector.Graph.Blackboard.TryGetEntry(entryId, out entry);
        }
    }

    internal static class CutsceneBlackboardDrawerUtility
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
        private const string ValueFieldName = "Value";
        private const string EnumValueNameFieldName = "_valueName";

        private static GUIStyle s_inlineActionButtonStyle;

        internal static event Action ValueSourceBindingCleared;

        internal static float SingleLineHeight => EditorGUIUtility.singleLineHeight;

        internal static float VerticalSpacing => EditorGUIUtility.standardVerticalSpacing;

        internal static bool TryGetBoundDirector(
            SerializedProperty property,
            out CutsceneDirector director)
        {
            director = property?.serializedObject?.targetObject as CutsceneDirector;
            return director != null;
        }

        internal static float GetVariableReferenceHeight()
        {
            return SingleLineHeight;
        }

        internal static float GetValueSourceHeight(SerializedProperty property)
        {
            if (property == null)
            {
                return SingleLineHeight;
            }

            SerializedProperty directValueProperty = property.FindPropertyRelative(
                DirectValueFieldName);
            SerializedProperty boxedValueProperty = directValueProperty
                ?.FindPropertyRelative(ValueFieldName);

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

        internal static Type ResolveExpectedValueType(
            SerializedProperty valueSourceProperty,
            FieldInfo fieldInfo)
        {
            Type serializedType = ResolveSerializedType(
                valueSourceProperty
                    .FindPropertyRelative(ExpectedValueTypeNameFieldName)
                    ?.stringValue);

            if (serializedType != null)
            {
                return serializedType;
            }

            CutsceneValueSourceTypeAttribute valueSourceTypeAttribute =
                fieldInfo?.GetCustomAttribute<CutsceneValueSourceTypeAttribute>();

            return valueSourceTypeAttribute?.ValueType;
        }

        internal static Type ResolveVariableReferenceValueType(
            SerializedProperty variableProperty)
        {
            return ResolveSerializedType(
                variableProperty
                    .FindPropertyRelative(ValueTypeNameFieldName)
                    ?.stringValue);
        }

        internal static void EnsureValueSourceExpectedType(
            SerializedProperty valueSourceProperty,
            Type expectedType)
        {
            if (expectedType == null)
            {
                return;
            }

            SerializedProperty expectedTypeNameProperty = valueSourceProperty
                .FindPropertyRelative(ExpectedValueTypeNameFieldName);
            string serializedTypeName = expectedType.AssemblyQualifiedName ?? string.Empty;

            if (!string.Equals(
                    expectedTypeNameProperty.stringValue,
                    serializedTypeName,
                    StringComparison.Ordinal))
            {
                expectedTypeNameProperty.stringValue = serializedTypeName;
            }

            SerializedProperty directValueProperty = valueSourceProperty
                .FindPropertyRelative(DirectValueFieldName);
            CutsceneGraphBlackboardValue directValue =
                directValueProperty.managedReferenceValue as CutsceneGraphBlackboardValue;

            if (CanRepresentExpectedValueType(directValue, expectedType))
            {
                return;
            }

            if (CutsceneBlackboardValueRegistry.TryCreateValue(
                    expectedType,
                    out CutsceneGraphBlackboardValue replacementValue))
            {
                directValueProperty.managedReferenceValue = replacementValue;
            }
        }

        internal static void DrawVariableReferenceField(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            Type expectedType = null)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect contentRect = HasVisibleLabel(label)
                ? EditorGUI.PrefixLabel(position, label)
                : position;
            Rect buttonRect = contentRect;
            buttonRect.width = Mathf.Max(
                0f,
                buttonRect.width - InlineActionButtonWidth - InlineActionButtonSpacing);
            Rect clearRect = GetInlineActionButtonRect(contentRect, buttonRect.xMax);

            bool hasDirector = TryGetBoundDirector(property, out CutsceneDirector director);

            using (new EditorGUI.DisabledScope(!hasDirector))
            {
                if (GUI.Button(
                        buttonRect,
                        GetVariableReferenceLabel(property, director),
                        EditorStyles.popup))
                {
                    ShowVariableReferenceMenu(property.Copy(), director, expectedType);
                }
            }

            using (new EditorGUI.DisabledScope(!IsVariableReferenceAssigned(property)))
            {
                if (DrawInlineActionButton(
                        clearRect,
                        "Clear the current blackboard variable reference."))
                {
                    ClearVariableReference(property.Copy());
                }
            }

            HandleVariableReferenceDrag(buttonRect, property, expectedType);
            EditorGUI.EndProperty();
        }

        internal static bool TryHandleUIDragUpdated(SerializedProperty property)
        {
            return TryHandleUIDrag(property, performDrag: false);
        }

        internal static bool TryHandleUIDragPerform(SerializedProperty property)
        {
            return TryHandleUIDrag(property, performDrag: true);
        }

        internal static bool TryHandleUIBlackboardDragUpdated(SerializedProperty property)
        {
            return TryHandleUIBlackboardDrag(property, performDrag: false);
        }

        internal static bool TryHandleUIBlackboardDragPerform(SerializedProperty property)
        {
            return TryHandleUIBlackboardDrag(property, performDrag: true);
        }

        internal static bool CanAcceptBlackboardDrag(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (IsValueSourceProperty(property))
            {
                Type expectedType = ResolveUIDragExpectedType(property);
                return TryGetCompatibleDraggedBlackboardEntry(property, expectedType, out _);
            }

            if (!IsVariableReferenceProperty(property))
            {
                return false;
            }

            Type variableExpectedType = ResolveVariableReferenceValueType(property);
            return TryGetCompatibleDraggedBlackboardEntry(property, variableExpectedType, out _);
        }

        internal static bool ShouldShowValueSourceDropZone(SerializedProperty property)
        {
            return property != null && !IsValueSourceInBlackboardMode(property);
        }

        internal static void DrawValueSourceField(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            Type expectedType = null)
        {
            EditorGUI.BeginProperty(position, label, property);
            EnsureValueSourceExpectedType(property, expectedType);

            GUIContent seamlessLabel = CreateSeamlessLabel(label);
            HandleValueSourceDrag(position, property, expectedType);

            if (typeof(Object).IsAssignableFrom(expectedType))
            {
                HandleDirectObjectDrag(position, property, expectedType);
            }

            if (IsValueSourceInBlackboardMode(property))
            {
                DrawBlackboardBoundValueSourceField(
                    position,
                    property,
                    seamlessLabel,
                    expectedType);
            }
            else
            {
                DrawDirectValueField(position, property, seamlessLabel, expectedType);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawDirectValueField(
            Rect position,
            SerializedProperty valueSourceProperty,
            GUIContent label,
            Type expectedType)
        {
            SerializedProperty directValueProperty = valueSourceProperty
                .FindPropertyRelative(DirectValueFieldName);

            if (expectedType == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("Unsupported type."));
                return;
            }

            EnsureValueSourceExpectedType(valueSourceProperty, expectedType);

            if (directValueProperty.managedReferenceValue
                is CutsceneGraphBlackboardUnityObjectValue objectValue)
            {
                SerializedProperty boxedValueProperty = directValueProperty
                    .FindPropertyRelative(ValueFieldName);
                Type objectType = typeof(Object).IsAssignableFrom(expectedType)
                    ? expectedType
                    : objectValue.ResolveObjectType();

                if (ShouldDrawBlackboardValueSourceDropTarget(
                        position,
                        valueSourceProperty,
                        expectedType))
                {
                    DrawBlackboardValueSourceDropTarget(position, label, expectedType);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                Object newValue = EditorGUI.ObjectField(
                    position,
                    label,
                    boxedValueProperty.objectReferenceValue,
                    objectType,
                    true);

                if (EditorGUI.EndChangeCheck())
                {
                    AssignDirectObjectValue(
                        valueSourceProperty.Copy(),
                        newValue,
                        expectedType);
                }

                return;
            }

            if (directValueProperty.managedReferenceValue
                is CutsceneGraphBlackboardEnumValue enumValue)
            {
                Type enumType = expectedType.IsEnum
                    ? expectedType
                    : enumValue.ResolveEnumType();

                if (enumType == null || !enumType.IsEnum)
                {
                    EditorGUI.LabelField(position, "No enum type configured.");
                    return;
                }

                string[] valueNames = Enum.GetNames(enumType);
                SerializedProperty valueNameProperty = directValueProperty
                    .FindPropertyRelative(EnumValueNameFieldName);
                int selectedIndex = Mathf.Max(
                    0,
                    Array.IndexOf(valueNames, valueNameProperty.stringValue));

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(
                    position,
                    label.text,
                    selectedIndex,
                    valueNames);

                if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < valueNames.Length)
                {
                    valueNameProperty.stringValue = valueNames[newIndex];
                }

                return;
            }

            SerializedProperty boxedValue = directValueProperty.FindPropertyRelative(ValueFieldName);

            if (boxedValue != null)
            {
                EditorGUI.PropertyField(position, boxedValue, label, true);
                return;
            }

            EditorGUI.PropertyField(position, directValueProperty, label, true);
        }

        private static bool TryHandleUIDrag(
            SerializedProperty property,
            bool performDrag)
        {
            if (property == null)
            {
                return false;
            }

            if (TryHandleUIBlackboardDrag(property, performDrag))
            {
                return true;
            }

            if (IsValueSourceProperty(property))
            {
                Type expectedType = ResolveUIDragExpectedType(property);

                if (expectedType != null
                    && typeof(Object).IsAssignableFrom(expectedType)
                    && TryResolveDraggedUnityObject(expectedType, out Object draggedObject)
                    && !(TryGetBoundDirector(property, out CutsceneDirector director)
                        && CutsceneBlackboardDragAndDrop.TryResolveDraggedEntry(
                            director,
                            out _)))
                {
                    if (performDrag)
                    {
                        DragAndDrop.AcceptDrag();
                        AssignDirectObjectValue(
                            property.Copy(),
                            draggedObject,
                            expectedType);
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    }

                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool TryHandleUIBlackboardDrag(
            SerializedProperty property,
            bool performDrag)
        {
            if (property == null)
            {
                return false;
            }

            if (IsValueSourceProperty(property))
            {
                Type expectedType = ResolveUIDragExpectedType(property);

                if (!TryGetCompatibleDraggedBlackboardEntry(
                        property,
                        expectedType,
                        out CutsceneGraphBlackboardEntry entry))
                {
                    return false;
                }

                if (performDrag)
                {
                    DragAndDrop.AcceptDrag();
                    AssignValueSourceBlackboardBinding(
                        property.Copy(),
                        entry,
                        expectedType);
                }
                else
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                }

                return true;
            }

            if (!IsVariableReferenceProperty(property))
            {
                return false;
            }

            Type variableExpectedType = ResolveVariableReferenceValueType(property);

            if (!TryGetCompatibleDraggedBlackboardEntry(
                    property,
                    variableExpectedType,
                    out CutsceneGraphBlackboardEntry variableEntry))
            {
                return false;
            }

            if (performDrag)
            {
                DragAndDrop.AcceptDrag();
                AssignVariableReference(property.Copy(), variableEntry);
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            }

            return true;
        }

        private static void DrawBlackboardBoundValueSourceField(
            Rect position,
            SerializedProperty valueSourceProperty,
            GUIContent label,
            Type expectedType)
        {
            SerializedProperty blackboardVariableProperty = valueSourceProperty
                .FindPropertyRelative(BlackboardVariableFieldName);
            Rect contentRect = HasVisibleLabel(label)
                ? EditorGUI.PrefixLabel(position, label)
                : position;
            Rect buttonRect = contentRect;
            buttonRect.width = Mathf.Max(
                0f,
                buttonRect.width - InlineActionButtonWidth - InlineActionButtonSpacing);
            Rect clearRect = GetInlineActionButtonRect(contentRect, buttonRect.xMax);

            bool hasDirector = TryGetBoundDirector(valueSourceProperty, out CutsceneDirector director);

            using (new EditorGUI.DisabledScope(!hasDirector))
            {
                if (GUI.Button(
                        buttonRect,
                        GetVariableReferenceLabel(blackboardVariableProperty, director),
                        EditorStyles.popup))
                {
                    ShowValueSourceBlackboardMenu(
                        valueSourceProperty.Copy(),
                        director,
                        expectedType);
                }
            }

            if (DrawInlineActionButton(
                    clearRect,
                    "Clear this blackboard binding and return to a direct value."))
            {
                ClearValueSourceBinding(valueSourceProperty.Copy());
            }
        }

        private static Rect GetInlineActionButtonRect(Rect contentRect, float primaryRectMaxX)
        {
            float buttonHeight = Mathf.Min(contentRect.height, SingleLineHeight);
            float buttonY = contentRect.y + ((contentRect.height - buttonHeight) * 0.5f);

            return new Rect(
                primaryRectMaxX + InlineActionButtonSpacing,
                buttonY,
                InlineActionButtonWidth,
                buttonHeight);
        }

        private static bool DrawInlineActionButton(Rect position, string tooltip)
        {
            return GUI.Button(
                position,
                new GUIContent("x", tooltip),
                InlineActionButtonStyle);
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

        private static void ShowVariableReferenceMenu(
            SerializedProperty property,
            CutsceneDirector director,
            Type expectedType)
        {
            GenericMenu menu = new();
            SerializableGuid currentId = ReadGuid(property.FindPropertyRelative(EntryIdFieldName));
            string currentKey = property.FindPropertyRelative(EntryKeyFieldName).stringValue;

            menu.AddItem(
                new GUIContent("None"),
                !IsVariableReferenceAssigned(property),
                () => ClearVariableReference(property.Copy()));

            if (director == null)
            {
                menu.ShowAsContext();
                return;
            }

            IReadOnlyList<CutsceneGraphBlackboardEntry> entries = director
                .Graph
                .Blackboard
                .Entries
                .Where(entry => IsEntryCompatible(entry, expectedType))
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (entries.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No compatible variables"));
                menu.ShowAsContext();
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                CutsceneGraphBlackboardEntry entry = entries[index];
                string entryLabel = $"{entry.Key} ({GetReadableTypeName(entry.Value?.GetExpectedValueType())})";
                bool isSelected = entry.Id == currentId
                    || string.Equals(entry.Key, currentKey, StringComparison.Ordinal);

                menu.AddItem(
                    new GUIContent(entryLabel),
                    isSelected,
                    () => AssignVariableReference(property.Copy(), entry));
            }

            menu.ShowAsContext();
        }

        private static void ShowValueSourceBlackboardMenu(
            SerializedProperty property,
            CutsceneDirector director,
            Type expectedType)
        {
            GenericMenu menu = new();

            menu.AddItem(
                new GUIContent("Use Direct Value"),
                !IsValueSourceInBlackboardMode(property),
                () => ClearValueSourceBinding(property.Copy()));

            if (director == null)
            {
                menu.ShowAsContext();
                return;
            }

            SerializableGuid currentId = ReadGuid(
                property
                    .FindPropertyRelative(BlackboardVariableFieldName)
                    .FindPropertyRelative(EntryIdFieldName));
            string currentKey = property
                .FindPropertyRelative(BlackboardVariableFieldName)
                .FindPropertyRelative(EntryKeyFieldName)
                .stringValue;

            IReadOnlyList<CutsceneGraphBlackboardEntry> entries = director
                .Graph
                .Blackboard
                .Entries
                .Where(entry => IsEntryCompatible(entry, expectedType))
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (entries.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No compatible variables"));
                menu.ShowAsContext();
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                CutsceneGraphBlackboardEntry entry = entries[index];
                string entryLabel = $"{entry.Key} ({GetReadableTypeName(entry.Value?.GetExpectedValueType())})";
                bool isSelected = entry.Id == currentId
                    || string.Equals(entry.Key, currentKey, StringComparison.Ordinal);

                menu.AddItem(
                    new GUIContent(entryLabel),
                    isSelected,
                    () => AssignValueSourceBlackboardBinding(
                        property.Copy(),
                        entry,
                        expectedType));
            }

            menu.ShowAsContext();
        }

        private static void HandleVariableReferenceDrag(
            Rect position,
            SerializedProperty property,
            Type expectedType)
        {
            Event currentEvent = Event.current;

            if (!position.Contains(currentEvent.mousePosition)
                || !TryGetBoundDirector(property, out CutsceneDirector director)
                || !CutsceneBlackboardDragAndDrop.TryResolveDraggedEntry(director, out CutsceneGraphBlackboardEntry entry)
                || !IsEntryCompatible(entry, expectedType))
            {
                return;
            }

            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AssignVariableReference(property.Copy(), entry);
                currentEvent.Use();
            }
        }

        private static void HandleValueSourceDrag(
            Rect position,
            SerializedProperty property,
            Type expectedType)
        {
            Event currentEvent = Event.current;

            if (!position.Contains(currentEvent.mousePosition)
                || !TryGetCompatibleDraggedBlackboardEntry(
                    property,
                    expectedType,
                    out CutsceneGraphBlackboardEntry entry))
            {
                return;
            }

            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AssignValueSourceBlackboardBinding(property.Copy(), entry, expectedType);
                currentEvent.Use();
            }
        }

        private static bool ShouldDrawBlackboardValueSourceDropTarget(
            Rect position,
            SerializedProperty property,
            Type expectedType)
        {
            Event currentEvent = Event.current;

            if (!position.Contains(currentEvent.mousePosition))
            {
                return false;
            }

            if (currentEvent.type != EventType.DragUpdated
                && currentEvent.type != EventType.DragPerform)
            {
                return false;
            }

            return TryGetCompatibleDraggedBlackboardEntry(
                property,
                expectedType,
                out _);
        }

        private static void DrawBlackboardValueSourceDropTarget(
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

        private static void HandleDirectObjectDrag(
            Rect position,
            SerializedProperty property,
            Type expectedType)
        {
            Event currentEvent = Event.current;

            if (!position.Contains(currentEvent.mousePosition)
                || expectedType == null
                || !typeof(Object).IsAssignableFrom(expectedType)
                || !TryResolveDraggedUnityObject(expectedType, out Object draggedObject))
            {
                return;
            }

            if (TryGetBoundDirector(property, out CutsceneDirector director)
                && CutsceneBlackboardDragAndDrop.TryResolveDraggedEntry(
                    director,
                    out _))
            {
                return;
            }

            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AssignDirectObjectValue(property.Copy(), draggedObject, expectedType);
                currentEvent.Use();
            }
        }

        private static bool TryGetCompatibleDraggedBlackboardEntry(
            SerializedProperty property,
            Type expectedType,
            out CutsceneGraphBlackboardEntry entry)
        {
            entry = null;

            return TryGetBoundDirector(property, out CutsceneDirector director)
                && CutsceneBlackboardDragAndDrop.TryResolveDraggedEntry(director, out entry)
                && IsEntryCompatible(entry, expectedType);
        }

        private static bool IsEntryCompatible(
            CutsceneGraphBlackboardEntry entry,
            Type expectedType)
        {
            if (entry?.Value == null || expectedType == null)
            {
                return entry != null;
            }

            return entry.Value.TryGetValue(expectedType, out _);
        }

        private static string GetVariableReferenceLabel(
            SerializedProperty property,
            CutsceneDirector director)
        {
            string entryKey = property.FindPropertyRelative(EntryKeyFieldName).stringValue;

            if (string.IsNullOrWhiteSpace(entryKey))
            {
                return "None";
            }

            if (director == null)
            {
                return entryKey;
            }

            SerializableGuid entryId = ReadGuid(property.FindPropertyRelative(EntryIdFieldName));

            if (entryId != SerializableGuid.Empty
                && director.Graph.Blackboard.TryGetEntry(entryId, out CutsceneGraphBlackboardEntry entryById))
            {
                return $"{entryById.Key} ({GetReadableTypeName(entryById.Value?.GetExpectedValueType())})";
            }

            if (director.Graph.Blackboard.TryGetEntry(entryKey, out CutsceneGraphBlackboardEntry entryByKey))
            {
                return $"{entryByKey.Key} ({GetReadableTypeName(entryByKey.Value?.GetExpectedValueType())})";
            }

            return $"{entryKey} (Missing)";
        }

        private static bool IsVariableReferenceAssigned(SerializedProperty property)
        {
            SerializableGuid entryId = ReadGuid(property.FindPropertyRelative(EntryIdFieldName));
            string entryKey = property.FindPropertyRelative(EntryKeyFieldName).stringValue;
            return entryId != SerializableGuid.Empty || !string.IsNullOrWhiteSpace(entryKey);
        }

        private static void AssignValueSourceBlackboardBinding(
            SerializedProperty property,
            CutsceneGraphBlackboardEntry entry,
            Type expectedType = null)
        {
            EnsureValueSourceExpectedType(
                property,
                expectedType ?? entry.Value?.GetExpectedValueType());
            property.FindPropertyRelative(ModeFieldName).enumValueIndex =
                (int)CutsceneValueSourceMode.Blackboard;
            AssignVariableReference(
                property.FindPropertyRelative(BlackboardVariableFieldName),
                entry);
        }

        private static void AssignDirectObjectValue(
            SerializedProperty property,
            Object value,
            Type expectedType)
        {
            if (property == null || expectedType == null)
            {
                return;
            }

            EnsureValueSourceExpectedType(property, expectedType);
            SetValueSourceMode(property, CutsceneValueSourceMode.Direct);

            SerializedProperty directValueProperty = property.FindPropertyRelative(
                DirectValueFieldName);
            SerializedProperty boxedValueProperty = directValueProperty
                ?.FindPropertyRelative(ValueFieldName);

            if (boxedValueProperty == null)
            {
                return;
            }

            boxedValueProperty.objectReferenceValue = CoerceDirectObjectValue(
                value,
                expectedType);
            ApplyPropertyChanges(property.serializedObject);
        }

        private static void ClearValueSourceBinding(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            ClearVariableReference(property.FindPropertyRelative(BlackboardVariableFieldName));
            SetValueSourceMode(property, CutsceneValueSourceMode.Direct);
            ApplyPropertyChanges(property.serializedObject);
            ValueSourceBindingCleared?.Invoke();
        }

        private static bool TryResolveDraggedUnityObject(
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

        private static Object CoerceDirectObjectValue(Object value, Type expectedType)
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

        private static bool CanRepresentExpectedValueType(
            CutsceneGraphBlackboardValue value,
            Type expectedType)
        {
            if (value == null || expectedType == null)
            {
                return false;
            }

            if (value is CutsceneGraphBlackboardUnityObjectValue objectValue)
            {
                return objectValue.GetExpectedValueType() == expectedType;
            }

            return value.CanStoreValueType(expectedType);
        }

        private static bool IsValueSourceProperty(SerializedProperty property)
        {
            return property?.FindPropertyRelative(ModeFieldName) != null
                && property.FindPropertyRelative(BlackboardVariableFieldName) != null;
        }

        private static bool IsVariableReferenceProperty(SerializedProperty property)
        {
            return property?.FindPropertyRelative(EntryIdFieldName) != null
                && property.FindPropertyRelative(EntryKeyFieldName) != null
                && property.FindPropertyRelative(BlackboardVariableFieldName) == null;
        }

        private static Type ResolveUIDragExpectedType(SerializedProperty property)
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

            SerializedProperty directValueProperty = property.FindPropertyRelative(DirectValueFieldName);
            return directValueProperty?.managedReferenceValue is CutsceneGraphBlackboardValue directValue
                ? directValue.GetExpectedValueType()
                : null;
        }

        private static void ClearVariableReference(SerializedProperty property)
        {
            SetGuid(property.FindPropertyRelative(EntryIdFieldName), SerializableGuid.Empty);
            property.FindPropertyRelative(EntryKeyFieldName).stringValue = string.Empty;
            property.FindPropertyRelative(ValueTypeNameFieldName).stringValue = string.Empty;
            ApplyPropertyChanges(property.serializedObject);
        }

        private static void AssignVariableReference(
            SerializedProperty property,
            CutsceneGraphBlackboardEntry entry)
        {
            SetGuid(property.FindPropertyRelative(EntryIdFieldName), entry.Id);
            property.FindPropertyRelative(EntryKeyFieldName).stringValue = entry.Key ?? string.Empty;
            property.FindPropertyRelative(ValueTypeNameFieldName).stringValue =
                entry.Value?.GetExpectedValueType()?.AssemblyQualifiedName ?? string.Empty;
            ApplyPropertyChanges(property.serializedObject);
        }

        private static void ApplyPropertyChanges(SerializedObject serializedObject)
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }

        private static bool IsValueSourceInBlackboardMode(SerializedProperty property)
        {
            return property != null
                && (CutsceneValueSourceMode)property
                    .FindPropertyRelative(ModeFieldName)
                    .enumValueIndex == CutsceneValueSourceMode.Blackboard;
        }

        private static void SetValueSourceMode(
            SerializedProperty property,
            CutsceneValueSourceMode mode)
        {
            property.FindPropertyRelative(ModeFieldName).enumValueIndex = (int)mode;
        }

        private static SerializableGuid ReadGuid(SerializedProperty guidProperty)
        {
            return guidProperty == null
                ? SerializableGuid.Empty
                : new SerializableGuid(
                    guidProperty.FindPropertyRelative(Part1FieldName).uintValue,
                    guidProperty.FindPropertyRelative(Part2FieldName).uintValue,
                    guidProperty.FindPropertyRelative(Part3FieldName).uintValue,
                    guidProperty.FindPropertyRelative(Part4FieldName).uintValue);
        }

        private static void SetGuid(
            SerializedProperty guidProperty,
            SerializableGuid value)
        {
            guidProperty.FindPropertyRelative(Part1FieldName).uintValue = value.Part1;
            guidProperty.FindPropertyRelative(Part2FieldName).uintValue = value.Part2;
            guidProperty.FindPropertyRelative(Part3FieldName).uintValue = value.Part3;
            guidProperty.FindPropertyRelative(Part4FieldName).uintValue = value.Part4;
        }

        private static Type ResolveSerializedType(string serializedTypeName)
        {
            return string.IsNullOrWhiteSpace(serializedTypeName)
                ? null
                : Type.GetType(serializedTypeName);
        }

        private static string GetReadableTypeName(Type type)
        {
            return type == null ? "Unknown" : type.Name;
        }

        private static GUIContent CreateSeamlessLabel(GUIContent label)
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

        private static bool HasVisibleLabel(GUIContent label)
        {
            return label != null && !string.IsNullOrWhiteSpace(label.text);
        }
    }

    [CustomPropertyDrawer(typeof(CutsceneBlackboardVariableReference))]
    public sealed class CutsceneBlackboardVariableReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            CutsceneBlackboardDrawerUtility.DrawVariableReferenceField(
                position,
                property,
                label);
        }

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            return CutsceneBlackboardDrawerUtility.GetVariableReferenceHeight();
        }
    }

    [CustomPropertyDrawer(typeof(CutsceneValueSource))]
    public sealed class CutsceneValueSourceDrawer : PropertyDrawer
    {
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            Type expectedType = CutsceneBlackboardDrawerUtility.ResolveExpectedValueType(
                property,
                fieldInfo);

            CutsceneBlackboardDrawerUtility.DrawValueSourceField(
                position,
                property,
                label,
                expectedType);
        }

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            return CutsceneBlackboardDrawerUtility.GetValueSourceHeight(property);
        }
    }

    [CustomPropertyDrawer(typeof(CutsceneSetBlackboardValuesNode.BlackboardValueAssignment))]
    public sealed class CutsceneBlackboardValueAssignmentDrawer : PropertyDrawer
    {
        private const string TargetVariableFieldName = "_targetVariable";
        private const string ValueSourceFieldName = "_valueSource";

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty targetVariableProperty = property.FindPropertyRelative(
                TargetVariableFieldName);
            SerializedProperty valueSourceProperty = property.FindPropertyRelative(
                ValueSourceFieldName);
            Type targetType = CutsceneBlackboardDrawerUtility.ResolveVariableReferenceValueType(
                targetVariableProperty);

            if (targetType != null)
            {
                CutsceneBlackboardDrawerUtility.EnsureValueSourceExpectedType(
                    valueSourceProperty,
                    targetType);
            }

            Rect variableRect = new(
                position.x,
                position.y,
                position.width,
                CutsceneBlackboardDrawerUtility.SingleLineHeight);
            Rect valueRect = new(
                position.x,
                variableRect.yMax + CutsceneBlackboardDrawerUtility.VerticalSpacing,
                position.width,
                CutsceneBlackboardDrawerUtility.GetValueSourceHeight(valueSourceProperty));

            CutsceneBlackboardDrawerUtility.DrawVariableReferenceField(
                variableRect,
                targetVariableProperty,
                new GUIContent("Variable"),
                null);
            CutsceneBlackboardDrawerUtility.DrawValueSourceField(
                valueRect,
                valueSourceProperty,
                new GUIContent("Value"),
                targetType);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty valueSourceProperty = property.FindPropertyRelative(
                ValueSourceFieldName);

            return CutsceneBlackboardDrawerUtility.SingleLineHeight
                + CutsceneBlackboardDrawerUtility.VerticalSpacing
                + CutsceneBlackboardDrawerUtility.GetValueSourceHeight(valueSourceProperty);
        }
    }
}