using System;
using IndieGabo.HandyTools.AnimationEventsModule;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// Provides shared editor helpers for animation bus event selection and
    /// serialized-property synchronization.
    /// </summary>
    internal static class AnimatorBusEventEditorUtility
    {
        #region Field Names

        internal const string EventPathFieldName = "_eventPath";
        internal const string EventPayloadFieldName = "_eventPayload";
        internal const string EventReferenceFieldName = "_eventReference";
        internal const string EventTypeNameFieldName = "_eventTypeName";

        #endregion

        #region Public API

        /// <summary>
        /// Resolves the selected metadata from one serialized trigger entry.
        /// </summary>
        /// <param name="triggerProperty">Serialized trigger property.</param>
        /// <returns>Resolved metadata when available.</returns>
        internal static AnimatorBusEventMetadata GetSelectedMetadata(
            SerializedProperty triggerProperty
        )
        {
            SerializedProperty referenceProperty = triggerProperty
                .FindPropertyRelative(EventReferenceFieldName);

            SerializedProperty eventPathProperty = referenceProperty
                .FindPropertyRelative(EventPathFieldName);

            SerializedProperty eventTypeNameProperty = referenceProperty
                .FindPropertyRelative(EventTypeNameFieldName);

            if (!string.IsNullOrWhiteSpace(eventTypeNameProperty.stringValue))
            {
                Type resolvedType = Type.GetType(
                    eventTypeNameProperty.stringValue,
                    throwOnError: false
                );

                if (resolvedType != null
                    && AnimatorBusEventRegistry.TryGetMetadata(
                        resolvedType,
                        out AnimatorBusEventMetadata typeMetadata
                    ))
                {
                    return typeMetadata;
                }
            }

            return AnimatorBusEventRegistry.TryGetMetadata(
                eventPathProperty.stringValue,
                out AnimatorBusEventMetadata pathMetadata
            )
                ? pathMetadata
                : null;
        }

        /// <summary>
        /// Applies one selected event metadata entry to a serialized trigger.
        /// </summary>
        /// <param name="triggerProperty">Serialized trigger property.</param>
        /// <param name="metadata">Selected metadata entry.</param>
        internal static void ApplySelection(
            SerializedProperty triggerProperty,
            AnimatorBusEventMetadata metadata
        )
        {
            SerializedProperty referenceProperty = triggerProperty
                .FindPropertyRelative(EventReferenceFieldName);

            SerializedProperty eventPathProperty = referenceProperty
                .FindPropertyRelative(EventPathFieldName);

            SerializedProperty eventTypeNameProperty = referenceProperty
                .FindPropertyRelative(EventTypeNameFieldName);

            SerializedProperty payloadProperty = triggerProperty
                .FindPropertyRelative(EventPayloadFieldName);

            if (metadata == null)
            {
                eventPathProperty.stringValue = string.Empty;
                eventTypeNameProperty.stringValue = string.Empty;
                payloadProperty.managedReferenceValue = null;
                triggerProperty.serializedObject.ApplyModifiedProperties();
                triggerProperty.serializedObject.Update();
                return;
            }

            eventPathProperty.stringValue = metadata.Path;
            eventTypeNameProperty.stringValue =
                metadata.EventType.AssemblyQualifiedName ?? string.Empty;

            object currentPayload = payloadProperty.managedReferenceValue;
            if (currentPayload == null || currentPayload.GetType() != metadata.EventType)
            {
                payloadProperty.managedReferenceValue = Activator.CreateInstance(
                    metadata.EventType,
                    nonPublic: true
                );
            }

            triggerProperty.serializedObject.ApplyModifiedProperties();
            triggerProperty.serializedObject.Update();
        }

        /// <summary>
        /// Builds the hierarchical menu path shown in the event picker.
        /// </summary>
        /// <param name="metadata">Selected metadata entry.</param>
        /// <returns>Slash-delimited menu path.</returns>
        internal static string GetMenuPath(AnimatorBusEventMetadata metadata)
        {
            return metadata.Path.Replace('.', '/');
        }

        #endregion
    }
}