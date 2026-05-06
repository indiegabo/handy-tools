using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// UI Toolkit drawer for local animation-event response bindings.
    /// </summary>
    [CustomPropertyDrawer(typeof(IndieGabo.HandyTools.AnimationEventsModule.AnimationEventResponseBinding))]
    public sealed class AnimationEventResponseBindingDrawer : PropertyDrawer
    {
        #region Inspector

        /// <summary>
        /// Creates the property UI for one response binding entry.
        /// </summary>
        /// <param name="property">Serialized response binding property.</param>
        /// <returns>The root property visual element.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = CreateBox();

            PropertyField eventNameField = new(
                property.FindPropertyRelative("_eventName"),
                "Event Name"
            );
            eventNameField.style.marginBottom = 6f;
            root.Add(eventNameField);

            PropertyField callbacksField = new(
                property.FindPropertyRelative("_onAnimationEvent"),
                "Callbacks"
            );
            root.Add(callbacksField);

            return root;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates the shared boxed layout used by list entries.
        /// </summary>
        /// <returns>Styled root visual element.</returns>
        private static VisualElement CreateBox()
        {
            VisualElement root = new();
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;
            return root;
        }

        #endregion
    }
}