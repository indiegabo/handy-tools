using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// UI Toolkit inspector for local animation-event receivers.
    /// </summary>
    [CustomEditor(typeof(IndieGabo.HandyTools.AnimationEventsModule.AnimationEventReceiver))]
    public sealed class AnimationEventReceiverInspector : UnityEditor.Editor
    {
        #region Inspector

        /// <summary>
        /// Creates the receiver inspector using UI Toolkit controls.
        /// </summary>
        /// <returns>The root inspector visual element.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = CreateRoot();

            HelpBox helpBox = new(
                "Configure local UnityEvent callbacks that respond to "
                    + "string-based animation events emitted by states.",
                HelpBoxMessageType.Info
            );
            helpBox.style.marginBottom = 8f;
            root.Add(helpBox);

            root.Add(
                new PropertyField(
                    serializedObject.FindProperty("_animationEvents"),
                    "Animation Events"
                )
            );

            root.Bind(serializedObject);
            return root;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates the shared root container style for this inspector.
        /// </summary>
        /// <returns>Styled root visual element.</returns>
        private static VisualElement CreateRoot()
        {
            VisualElement root = new();
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 4f;
            return root;
        }

        #endregion
    }
}