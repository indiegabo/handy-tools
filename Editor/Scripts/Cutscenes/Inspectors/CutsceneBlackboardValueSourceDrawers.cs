using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Actions;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IndieGabo.HandyTools.Editor.CutscenesModule
{
    internal static class CutsceneBlackboardDragAndDrop
    {
        internal static bool HasActiveDrag => GraphBlackboardDragSession.HasActiveDrag;

        internal static string ActiveEntryLabel => GraphBlackboardDragSession.ActiveEntryLabel;

        internal static void BeginDrag(
            CutsceneDirector director,
            CutsceneGraphBlackboardEntry entry)
        {
            if (director == null || entry == null)
            {
                return;
            }

            entry.EnsureId();
            GraphBlackboardDragSession.BeginDrag(
                director,
                entry.Id,
                entry.Key ?? string.Empty);
        }

        internal static void CancelDrag()
        {
            GraphBlackboardDragSession.CancelDrag();
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

            if (!GraphBlackboardDragSession.TryGetActiveEntryId(
                    expectedDirector,
                    out SerializableGuid entryId))
            {
                return false;
            }

            return expectedDirector.Graph.Blackboard.TryGetEntry(entryId, out entry);
        }
    }

    internal static class CutsceneBlackboardDrawerUtility
    {
        private const string EntryIdFieldName = "_entryId";
        private const string EntryKeyFieldName = "_entryKey";
        private const string ValueTypeNameFieldName = "_valueTypeName";
        private const string ModeFieldName = "_mode";
        private const string BlackboardVariableFieldName = "_blackboardVariable";
        private const string DirectValueFieldName = "_directValue";
        private const string ExpectedValueTypeNameFieldName = "_expectedValueTypeName";
        private const string BoxedValueFieldName = "_value";
        private const string ValueFieldName = "Value";

        internal static event Action ValueSourceBindingCleared;

        internal static float SingleLineHeight =>
            GraphBlackboardDrawerSharedUtility.SingleLineHeight;

        internal static float VerticalSpacing =>
            GraphBlackboardDrawerSharedUtility.VerticalSpacing;

        internal static bool TryGetBoundDirector(
            SerializedProperty property,
            out CutsceneDirector director)
        {
            director = property?.serializedObject?.targetObject as CutsceneDirector;
            return director != null;
        }

        internal static float GetVariableReferenceHeight()
        {
            return GraphBlackboardDrawerSharedUtility.GetVariableReferenceHeight();
        }

        internal static float GetValueSourceHeight(SerializedProperty property)
        {
            return GraphBlackboardDrawerSharedUtility.GetValueSourceHeight(property);
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
            return GraphBlackboardDrawerSharedUtility.ResolveVariableReferenceValueType(
                variableProperty);
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
            GraphBlackboardValue directValue =
                directValueProperty.managedReferenceValue as GraphBlackboardValue;

            if (CanRepresentExpectedValueType(directValue, expectedType))
            {
                return;
            }

            if (GraphBlackboardValueRegistry.TryCreateValue(
                expectedType,
                CutsceneGraphFamily.Id,
                out GraphBlackboardValue replacementValue))
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
                buttonRect.width - GraphBlackboardDrawerSharedUtility.InlineActionButtonReservedWidth);
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
            return GraphBlackboardDrawerSharedUtility.ShouldShowValueSourceDropZone(
                property);
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
            GraphBlackboardDrawerSharedUtility.DrawDirectValueField(
                position,
                valueSourceProperty,
                label,
                expectedType,
                EnsureValueSourceExpectedType,
                AssignDirectObjectValue,
                ShouldDrawBlackboardValueSourceDropTarget);
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
                buttonRect.width - GraphBlackboardDrawerSharedUtility.InlineActionButtonReservedWidth);
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
            return GraphBlackboardDrawerSharedUtility.GetInlineActionButtonRect(
                contentRect,
                primaryRectMaxX);
        }

        private static bool DrawInlineActionButton(Rect position, string tooltip)
        {
            return GraphBlackboardDrawerSharedUtility.DrawInlineActionButton(
                position,
                tooltip);
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
            GraphBlackboardDrawerSharedUtility.DrawBlackboardValueSourceDropTarget(
                position,
                label,
                expectedType);
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
            SerializedProperty boxedValueProperty = ResolveDirectBoxedValueProperty(
                directValueProperty);

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
            return GraphBlackboardDrawerSharedUtility.TryResolveDraggedUnityObject(
                expectedType,
                out draggedObject);
        }

        private static Object CoerceDirectObjectValue(Object value, Type expectedType)
        {
            return GraphBlackboardDrawerSharedUtility.CoerceDirectObjectValue(
                value,
                expectedType);
        }

        private static bool CanRepresentExpectedValueType(
            GraphBlackboardValue value,
            Type expectedType)
        {
            if (value == null || expectedType == null)
            {
                return false;
            }

            if (value is GraphBlackboardUnityObjectValue objectValue)
            {
                return objectValue.GetExpectedValueType() == expectedType;
            }

            return value.CanStoreValueType(expectedType);
        }

        private static bool IsValueSourceProperty(SerializedProperty property)
        {
            return GraphBlackboardDrawerSharedUtility.IsValueSourceProperty(property);
        }

        private static bool IsVariableReferenceProperty(SerializedProperty property)
        {
            return GraphBlackboardDrawerSharedUtility.IsVariableReferenceProperty(property);
        }

        private static Type ResolveUIDragExpectedType(SerializedProperty property)
        {
            return GraphBlackboardDrawerSharedUtility.ResolveExpectedValueTypeFromSerializedData(
                property);
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
            GraphBlackboardDrawerSharedUtility.ApplyPropertyChanges(serializedObject);
        }

        private static bool IsValueSourceInBlackboardMode(SerializedProperty property)
        {
            return GraphBlackboardDrawerSharedUtility.IsValueSourceInBlackboardMode(
                property);
        }

        private static void SetValueSourceMode(
            SerializedProperty property,
            CutsceneValueSourceMode mode)
        {
            GraphBlackboardDrawerSharedUtility.SetValueSourceMode(
                property,
                (int)mode);
        }

        private static SerializableGuid ReadGuid(SerializedProperty guidProperty)
        {
            return GraphBlackboardDrawerSharedUtility.ReadGuid(guidProperty);
        }

        private static void SetGuid(
            SerializedProperty guidProperty,
            SerializableGuid value)
        {
            GraphBlackboardDrawerSharedUtility.SetGuid(guidProperty, value);
        }

        private static Type ResolveSerializedType(string serializedTypeName)
        {
            return GraphBlackboardDrawerSharedUtility.ResolveSerializedType(
                serializedTypeName);
        }

        private static string GetReadableTypeName(Type type)
        {
            return GraphBlackboardDrawerSharedUtility.GetReadableTypeName(type);
        }

        private static GUIContent CreateSeamlessLabel(GUIContent label)
        {
            return GraphBlackboardDrawerSharedUtility.CreateSeamlessLabel(label);
        }

        private static bool HasVisibleLabel(GUIContent label)
        {
            return GraphBlackboardDrawerSharedUtility.HasVisibleLabel(label);
        }

        private static SerializedProperty ResolveDirectBoxedValueProperty(
            SerializedProperty directValueProperty)
        {
            return directValueProperty?.FindPropertyRelative(BoxedValueFieldName)
                ?? directValueProperty?.FindPropertyRelative(ValueFieldName);
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