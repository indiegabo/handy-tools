using IndieGabo.HandyTools.HandyInputSystem.Feedbacks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor
{
    [CustomEditor(typeof(FeedbackContainer))]
    public class FeedbackContainerEditor : UnityEditor.Editor
    {
        static readonly string MainTemplatePath
            = "UI Toolkit/Input/Feedbacks/FeedbackContainerInspector_Template";


        private TemplateContainer _containerMain;
        private Button _buttonOpenWindow;

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