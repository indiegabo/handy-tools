using IndieGabo.HandyTools.HandyInputSystem.Feedbacks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor
{
    [CustomEditor(typeof(FeedbackContainer))]
    /// <summary>
    /// Custom inspector that opens the dedicated feedback container window.
    /// </summary>
    public class FeedbackContainerInspector : UnityEditor.Editor
    {
        static readonly string MainTemplatePath
            = "UI Toolkit/Input/Feedbacks/FeedbackContainerInspector_Template";


        private TemplateContainer _containerMain;
        private Button _buttonOpenWindow;

        /// <summary>
        /// Creates the custom inspector UI for the feedback container asset.
        /// </summary>
        /// <returns>The root inspector visual element.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            _containerMain
                = Resources.Load<VisualTreeAsset>($"{MainTemplatePath}").Instantiate();

            _buttonOpenWindow = _containerMain.Q<Button>("button-open-window");
            _buttonOpenWindow.clicked += () =>
            {
                FeedbackContainerWindow.Open(target as FeedbackContainer);
            };

            return _containerMain;
        }
    }
}