using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    public static class GraphBlackboardBindingDrawerUtility
    {
        private const string EntryIdFieldName = "_entryId";
        private const string EntryKeyFieldName = "_entryKey";
        private const string ValueTypeNameFieldName = "_valueTypeName";
        private const string ModeFieldName = "_mode";
        private const string BlackboardVariableFieldName = "_blackboardVariable";
        private const string DirectValueFieldName = "_directValue";
        private const string ExpectedValueTypeNameFieldName = "_expectedValueTypeName";

        internal static float GetVariableReferenceHeight()
        {
            return GraphBlackboardDrawerSharedUtility.GetVariableReferenceHeight();
        }

        internal static float GetValueSourceHeight(SerializedProperty property)
        {
            return GraphBlackboardDrawerSharedUtility.GetValueSourceHeight(property);
        }

        public static bool TryHandleUISessionUpdated(SerializedProperty property)
        {
            return TryResolveCompatibleDraggedEntry(property, out _, out _);
        }

        public static bool TryHandleUISessionPerform(SerializedProperty property)
        {
            if (!TryResolveCompatibleDraggedEntry(
                    property,
                    out GraphBlackboardEntry entry,
                    out Type expectedType))
            {
                return false;
            }

            if (GraphBlackboardDrawerSharedUtility.IsValueSourceProperty(property))
            {
                AssignValueSourceBlackboardBinding(property.Copy(), entry, expectedType);
                return true;
            }

            if (GraphBlackboardDrawerSharedUtility.IsVariableReferenceProperty(property))
            {
                AssignVariableReference(property.Copy(), entry);
                return true;
            }

            return false;
        }

        internal static Type ResolveExpectedValueType(
            SerializedProperty valueSourceProperty,
            FieldInfo fieldInfo)
        {
            Type serializedType = GraphBlackboardDrawerSharedUtility
                .ResolveExpectedValueTypeFromSerializedData(valueSourceProperty);

            if (serializedType != null)
            {
                return serializedType;
            }

            GraphValueSourceTypeAttribute attribute =
                fieldInfo?.GetCustomAttribute<GraphValueSourceTypeAttribute>();

            return attribute?.ValueType;
        }

        internal static Type ResolveVariableReferenceValueType(SerializedProperty property)
        {
            return GraphBlackboardDrawerSharedUtility.ResolveVariableReferenceValueType(property);
        }

        internal static void DrawVariableReferenceField(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            Type expectedType = null)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect contentRect = GraphBlackboardDrawerSharedUtility.HasVisibleLabel(label)
                ? EditorGUI.PrefixLabel(position, label)
                : position;
            Rect buttonRect = contentRect;
            buttonRect.width = Mathf.Max(
                0f,
                buttonRect.width - GraphBlackboardDrawerSharedUtility
                    .InlineActionButtonReservedWidth);
            Rect clearRect = GraphBlackboardDrawerSharedUtility.GetInlineActionButtonRect(
                contentRect,
                buttonRect.xMax);

            bool hasBinding = TryGetBoundGraph(property, out GraphDefinition graph);

            using (new EditorGUI.DisabledScope(!hasBinding))
            {
                if (GUI.Button(
                        buttonRect,
                        GetVariableReferenceLabel(property, graph),
                        EditorStyles.popup))
                {
                    ShowVariableReferenceMenu(property.Copy(), graph, expectedType);
                }
            }

            using (new EditorGUI.DisabledScope(!IsVariableReferenceAssigned(property)))
            {
                if (GraphBlackboardDrawerSharedUtility.DrawInlineActionButton(
                        clearRect,
                        "Clear the current graph blackboard variable reference."))
                {
                    ClearVariableReference(property.Copy());
                }
            }

            HandleVariableReferenceDrag(buttonRect, property, expectedType);
            EditorGUI.EndProperty();
        }

        internal static void DrawValueSourceField(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            Type expectedType = null)
        {
            EditorGUI.BeginProperty(position, label, property);
            EnsureValueSourceExpectedType(property, expectedType);

            GUIContent seamlessLabel = GraphBlackboardDrawerSharedUtility.CreateSeamlessLabel(
                label);
            HandleValueSourceDrag(position, property, expectedType);

            if (typeof(Object).IsAssignableFrom(expectedType))
            {
                HandleDirectObjectDrag(position, property, expectedType);
            }

            if (GraphBlackboardDrawerSharedUtility.IsValueSourceInBlackboardMode(property))
            {
                DrawBlackboardBoundValueSourceField(
                    position,
                    property,
                    seamlessLabel,
                    expectedType);
            }
            else
            {
                GraphBlackboardDrawerSharedUtility.DrawDirectValueField(
                    position,
                    property,
                    seamlessLabel,
                    expectedType,
                    EnsureValueSourceExpectedType,
                    AssignDirectObjectValue,
                    ShouldDrawBlackboardValueSourceDropTarget);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawBlackboardBoundValueSourceField(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            Type expectedType)
        {
            Rect contentRect = GraphBlackboardDrawerSharedUtility.HasVisibleLabel(label)
                ? EditorGUI.PrefixLabel(position, label)
                : position;
            Rect buttonRect = contentRect;
            buttonRect.width = Mathf.Max(
                0f,
                buttonRect.width - GraphBlackboardDrawerSharedUtility
                    .InlineActionButtonReservedWidth);
            Rect clearRect = GraphBlackboardDrawerSharedUtility.GetInlineActionButtonRect(
                contentRect,
                buttonRect.xMax);

            bool hasGraph = TryGetBoundGraph(property, out GraphDefinition graph);

            using (new EditorGUI.DisabledScope(!hasGraph))
            {
                if (GUI.Button(
                        buttonRect,
                        GetVariableReferenceLabel(
                            property.FindPropertyRelative(BlackboardVariableFieldName),
                            graph),
                        EditorStyles.popup))
                {
                    ShowValueSourceBlackboardMenu(property.Copy(), graph, expectedType);
                }
            }

            using (new EditorGUI.DisabledScope(
                       !IsVariableReferenceAssigned(
                           property.FindPropertyRelative(BlackboardVariableFieldName))))
            {
                if (GraphBlackboardDrawerSharedUtility.DrawInlineActionButton(
                        clearRect,
                        "Switch to direct-value mode and clear the graph blackboard binding."))
                {
                    ClearValueSourceBinding(property.Copy());
                }
            }
        }

        private static void EnsureValueSourceExpectedType(
            SerializedProperty valueSourceProperty,
            Type expectedType)
        {
            if (valueSourceProperty == null || expectedType == null)
            {
                return;
            }

            SerializedProperty expectedTypeNameProperty = valueSourceProperty
                .FindPropertyRelative(ExpectedValueTypeNameFieldName);
            string serializedTypeName = expectedType.AssemblyQualifiedName ?? string.Empty;

            if (!string.Equals(
                    expectedTypeNameProperty?.stringValue,
                    serializedTypeName,
                    StringComparison.Ordinal))
            {
                expectedTypeNameProperty.stringValue = serializedTypeName;
            }

            SerializedProperty directValueProperty = valueSourceProperty
                .FindPropertyRelative(DirectValueFieldName);
            GraphBlackboardValue directValue =
                directValueProperty?.managedReferenceValue as GraphBlackboardValue;

            if (CanRepresentExpectedValueType(directValue, expectedType))
            {
                return;
            }

            if (GraphBlackboardValueRegistry.TryCreateValue(
                    expectedType,
                    ResolveBoundFamilyId(valueSourceProperty),
                    out GraphBlackboardValue replacementValue))
            {
                directValueProperty.managedReferenceValue = replacementValue;
            }
        }

        private static void ShowVariableReferenceMenu(
            SerializedProperty property,
            GraphDefinition graph,
            Type expectedType)
        {
            GenericMenu menu = new();

            menu.AddItem(
                new GUIContent("None"),
                !IsVariableReferenceAssigned(property),
                () => ClearVariableReference(property.Copy()));

            IReadOnlyList<GraphBlackboardEntry> entries = graph?.Blackboard?.Entries
                ?.Where(entry => IsEntryCompatible(entry, expectedType))
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (entries == null || entries.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No compatible variables"));
                menu.ShowAsContext();
                return;
            }

            SerializableGuid currentId = GraphBlackboardDrawerSharedUtility.ReadGuid(
                property.FindPropertyRelative(EntryIdFieldName));
            string currentKey = property.FindPropertyRelative(EntryKeyFieldName).stringValue;

            for (int index = 0; index < entries.Count; index++)
            {
                GraphBlackboardEntry entry = entries[index];
                string entryLabel = $"{entry.Key} ({GraphBlackboardDrawerSharedUtility.GetReadableTypeName(entry.Value?.GetExpectedValueType())})";
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
            GraphDefinition graph,
            Type expectedType)
        {
            GenericMenu menu = new();

            menu.AddItem(
                new GUIContent("Use Direct Value"),
                !GraphBlackboardDrawerSharedUtility.IsValueSourceInBlackboardMode(property),
                () => ClearValueSourceBinding(property.Copy()));

            IReadOnlyList<GraphBlackboardEntry> entries = graph?.Blackboard?.Entries
                ?.Where(entry => IsEntryCompatible(entry, expectedType))
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (entries == null || entries.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No compatible variables"));
                menu.ShowAsContext();
                return;
            }

            SerializedProperty variableProperty = property.FindPropertyRelative(
                BlackboardVariableFieldName);
            SerializableGuid currentId = GraphBlackboardDrawerSharedUtility.ReadGuid(
                variableProperty.FindPropertyRelative(EntryIdFieldName));
            string currentKey = variableProperty.FindPropertyRelative(EntryKeyFieldName)
                .stringValue;

            for (int index = 0; index < entries.Count; index++)
            {
                GraphBlackboardEntry entry = entries[index];
                string entryLabel = $"{entry.Key} ({GraphBlackboardDrawerSharedUtility.GetReadableTypeName(entry.Value?.GetExpectedValueType())})";
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

            if (currentEvent == null
                || !position.Contains(currentEvent.mousePosition)
                || !TryResolveDraggedEntry(property, out GraphBlackboardEntry entry)
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

            if (currentEvent == null
                || !position.Contains(currentEvent.mousePosition)
                || !TryResolveDraggedEntry(property, out GraphBlackboardEntry entry)
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

            if (currentEvent == null
                || !position.Contains(currentEvent.mousePosition)
                || (currentEvent.type != EventType.DragUpdated
                    && currentEvent.type != EventType.DragPerform))
            {
                return false;
            }

            return TryResolveDraggedEntry(property, out GraphBlackboardEntry entry)
                && IsEntryCompatible(entry, expectedType);
        }

        private static void AssignValueSourceBlackboardBinding(
            SerializedProperty property,
            GraphBlackboardEntry entry,
            Type expectedType = null)
        {
            EnsureValueSourceExpectedType(
                property,
                expectedType ?? entry?.Value?.GetExpectedValueType());
            property.FindPropertyRelative(ModeFieldName).enumValueIndex =
                (int)GraphValueSourceMode.Blackboard;
            AssignVariableReference(
                property.FindPropertyRelative(BlackboardVariableFieldName),
                entry);
        }

        private static void AssignVariableReference(
            SerializedProperty property,
            GraphBlackboardEntry entry)
        {
            if (property == null)
            {
                return;
            }

            entry?.EnsureId();

            SerializedProperty entryIdProperty = property.FindPropertyRelative(EntryIdFieldName);
            SerializedProperty entryKeyProperty = property.FindPropertyRelative(EntryKeyFieldName);
            SerializedProperty valueTypeNameProperty = property.FindPropertyRelative(
                ValueTypeNameFieldName);

            GraphBlackboardDrawerSharedUtility.SetGuid(
                entryIdProperty,
                entry?.Id ?? SerializableGuid.Empty);
            entryKeyProperty.stringValue = entry?.Key ?? string.Empty;
            valueTypeNameProperty.stringValue = entry?.Value?.GetExpectedValueType()
                ?.AssemblyQualifiedName ?? string.Empty;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void ClearVariableReference(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            GraphBlackboardDrawerSharedUtility.SetGuid(
                property.FindPropertyRelative(EntryIdFieldName),
                SerializableGuid.Empty);
            property.FindPropertyRelative(EntryKeyFieldName).stringValue = string.Empty;
            property.FindPropertyRelative(ValueTypeNameFieldName).stringValue = string.Empty;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void ClearValueSourceBinding(SerializedProperty property)
        {
            if (property == null)
            {
                return;
            }

            ClearVariableReference(property.FindPropertyRelative(BlackboardVariableFieldName));
            property.FindPropertyRelative(ModeFieldName).enumValueIndex =
                (int)GraphValueSourceMode.Direct;
            property.serializedObject.ApplyModifiedProperties();
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
            property.FindPropertyRelative(ModeFieldName).enumValueIndex =
                (int)GraphValueSourceMode.Direct;

            SerializedProperty directValueProperty = property.FindPropertyRelative(
                DirectValueFieldName);
            SerializedProperty boxedValueProperty = directValueProperty?.FindPropertyRelative(
                "_value")
                ?? directValueProperty?.FindPropertyRelative("Value");

            if (boxedValueProperty == null)
            {
                return;
            }

            boxedValueProperty.objectReferenceValue =
                GraphBlackboardDrawerSharedUtility.CoerceDirectObjectValue(
                    value,
                    expectedType);
            property.serializedObject.ApplyModifiedProperties();
        }

        private static bool TryResolveDraggedEntry(
            SerializedProperty property,
            out GraphBlackboardEntry entry)
        {
            entry = null;

            return TryGetBoundHostOwner(property, out object hostOwner)
                && TryGetBoundGraph(property, out GraphDefinition graph)
                && GraphBlackboardDragSession.TryGetActiveEntryId(
                    hostOwner,
                    out SerializableGuid entryId)
                && graph.Blackboard.TryGetEntry(entryId, out entry);
        }

        private static bool TryResolveCompatibleDraggedEntry(
            SerializedProperty property,
            out GraphBlackboardEntry entry,
            out Type expectedType)
        {
            entry = null;
            expectedType = null;

            if (property == null || !TryResolveDraggedEntry(property, out entry))
            {
                return false;
            }

            if (GraphBlackboardDrawerSharedUtility.IsValueSourceProperty(property))
            {
                expectedType = GraphBlackboardDrawerSharedUtility
                    .ResolveExpectedValueTypeFromSerializedData(property);
                return IsEntryCompatible(entry, expectedType);
            }

            if (GraphBlackboardDrawerSharedUtility.IsVariableReferenceProperty(property))
            {
                expectedType = ResolveVariableReferenceValueType(property);
                return IsEntryCompatible(entry, expectedType);
            }

            return false;
        }

        private static bool TryGetBoundHostOwner(
            SerializedProperty property,
            out object hostOwner)
        {
            return TryResolveBoundContext(
                property,
                out _,
                out hostOwner,
                out _);
        }

        private static bool TryGetBoundGraph(
            SerializedProperty property,
            out GraphDefinition graph)
        {
            return TryResolveBoundContext(
                property,
                out graph,
                out _,
                out _);
        }

        private static string ResolveBoundFamilyId(SerializedProperty property)
        {
            return TryResolveBoundContext(
                    property,
                    out _,
                    out _,
                    out string familyId)
                ? familyId
                : null;
        }

        private static bool TryResolveBoundContext(
            SerializedProperty property,
            out GraphDefinition graph,
            out object hostOwner,
            out string familyId)
        {
            graph = null;
            hostOwner = null;
            familyId = null;

            object targetObject = property?.serializedObject?.targetObject;

            if (targetObject == null)
            {
                return false;
            }

            if (targetObject is IGraphPropertyBindingContextProvider provider
                && provider.TryResolveGraphPropertyBinding(
                    property.propertyPath,
                    out graph,
                    out hostOwner,
                    out familyId))
            {
                return graph != null;
            }

            if (!TryResolveGraphFromTargetObject(targetObject, out graph))
            {
                return false;
            }

            if (targetObject is IGraphHost host)
            {
                hostOwner = host.HostObject != null
                    ? host.HostObject
                    : targetObject;
                familyId = host.GraphFamilyId;
            }
            else
            {
                hostOwner = targetObject;
            }

            return true;
        }

        private static bool TryResolveGraphFromTargetObject(
            object targetObject,
            out GraphDefinition graph)
        {
            graph = null;

            if (targetObject == null)
            {
                return false;
            }

            Type targetType = targetObject.GetType();
            PropertyInfo graphProperty = targetType.GetProperty(
                "Graph",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (graphProperty != null
                && typeof(GraphDefinition).IsAssignableFrom(graphProperty.PropertyType)
                && graphProperty.GetValue(targetObject) is GraphDefinition propertyGraph)
            {
                graph = propertyGraph;
                return true;
            }

            FieldInfo graphField = targetType.GetField(
                "_graph",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (graphField != null
                && typeof(GraphDefinition).IsAssignableFrom(graphField.FieldType)
                && graphField.GetValue(targetObject) is GraphDefinition fieldGraph)
            {
                graph = fieldGraph;
                return true;
            }

            return false;
        }

        private static bool IsEntryCompatible(
            GraphBlackboardEntry entry,
            Type expectedType)
        {
            if (entry == null)
            {
                return false;
            }

            if (entry.Value == null || expectedType == null)
            {
                return true;
            }

            return entry.Value.TryGetValue(expectedType, out _);
        }

        private static string GetVariableReferenceLabel(
            SerializedProperty property,
            GraphDefinition graph)
        {
            if (property == null)
            {
                return "None";
            }

            string entryKey = property.FindPropertyRelative(EntryKeyFieldName).stringValue;

            if (string.IsNullOrWhiteSpace(entryKey))
            {
                return "None";
            }

            if (graph == null)
            {
                return entryKey;
            }

            SerializableGuid entryId = GraphBlackboardDrawerSharedUtility.ReadGuid(
                property.FindPropertyRelative(EntryIdFieldName));

            if (entryId != SerializableGuid.Empty
                && graph.Blackboard.TryGetEntry(entryId, out GraphBlackboardEntry entryById))
            {
                return FormatEntryLabel(entryById);
            }

            if (graph.Blackboard.TryGetEntry(entryKey, out GraphBlackboardEntry entryByKey))
            {
                return FormatEntryLabel(entryByKey);
            }

            return $"{entryKey} (Missing)";
        }

        private static string FormatEntryLabel(GraphBlackboardEntry entry)
        {
            if (entry == null)
            {
                return "None";
            }

            return $"{entry.Key} ({GraphBlackboardDrawerSharedUtility.GetReadableTypeName(entry.Value?.GetExpectedValueType())})";
        }

        private static bool IsVariableReferenceAssigned(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            SerializableGuid entryId = GraphBlackboardDrawerSharedUtility.ReadGuid(
                property.FindPropertyRelative(EntryIdFieldName));
            string entryKey = property.FindPropertyRelative(EntryKeyFieldName).stringValue;
            return entryId != SerializableGuid.Empty || !string.IsNullOrWhiteSpace(entryKey);
        }

        private static bool HandleDirectObjectDragGuard(Type expectedType)
        {
            return expectedType != null && typeof(Object).IsAssignableFrom(expectedType);
        }

        private static void HandleDirectObjectDrag(
            Rect position,
            SerializedProperty property,
            Type expectedType)
        {
            Event currentEvent = Event.current;

            if (currentEvent == null
                || !position.Contains(currentEvent.mousePosition)
                || !HandleDirectObjectDragGuard(expectedType)
                || !GraphBlackboardDrawerSharedUtility.TryResolveDraggedUnityObject(
                    expectedType,
                    out Object draggedObject)
                || TryResolveDraggedEntry(property, out _))
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

        private static bool CanRepresentExpectedValueType(
            GraphBlackboardValue value,
            Type expectedType)
        {
            if (value == null || expectedType == null)
            {
                return false;
            }

            return value.CanStoreValueType(expectedType)
                || value.TryGetValue(expectedType, out _);
        }
    }

    /// <summary>
    /// Draws one generic graph blackboard variable reference field.
    /// </summary>
    [CustomPropertyDrawer(typeof(GraphBlackboardVariableReference), true)]
    public sealed class GraphBlackboardVariableReferenceDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            return GraphBlackboardBindingDrawerUtility.GetVariableReferenceHeight();
        }

        /// <inheritdoc />
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            GraphBlackboardBindingDrawerUtility.DrawVariableReferenceField(
                position,
                property,
                label,
                GraphBlackboardBindingDrawerUtility.ResolveVariableReferenceValueType(property));
        }
    }

    /// <summary>
    /// Draws one generic graph value source field.
    /// </summary>
    [CustomPropertyDrawer(typeof(GraphValueSource), true)]
    public sealed class GraphValueSourceDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            return GraphBlackboardBindingDrawerUtility.GetValueSourceHeight(property);
        }

        /// <inheritdoc />
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            Type expectedType = GraphBlackboardBindingDrawerUtility.ResolveExpectedValueType(
                property,
                fieldInfo);
            GraphBlackboardBindingDrawerUtility.DrawValueSourceField(
                position,
                property,
                label,
                expectedType);
        }
    }
}