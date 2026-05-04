using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Debugging
{
    [DebugPanelSection]
    public class DebugSettingsSection : DebugPanelSection
    {
        private const string TemplatePath = "UI/Debug Panel/Sections/DebugSettingsSection_Template";

        public override int OrderInPanel => 9999;

        private TemplateContainer _mainContainer;
        private Foldout _foldout;
        private Toggle _toggleVsync;
        private DropdownField _dropdownFPSRate;

        private DebugSettingsConfig Config => DebugSettingsConfig.Instance;
        private Dictionary<string, int> _fpsRates = new();

        public override VisualElement BuildSectionElement()
        {
            var templateAsset = Resources.Load<VisualTreeAsset>($"{TemplatePath}");
            _mainContainer = templateAsset.CloneTree();

            Init();

            return _mainContainer;
        }

        private void Init()
        {
            _foldout = _mainContainer.Q<Foldout>();
            _foldout.SetValueWithoutNotify(false);

            _toggleVsync = _mainContainer.Q<Toggle>("toggle-vsync");
            _toggleVsync.SetValueWithoutNotify(Config.VSyncOn);
            _toggleVsync.RegisterValueChangedCallback(OnToggleVsyncValueChanged);
            SetVerticalSync(Config.VSyncOn);


            _dropdownFPSRate = _mainContainer.Q<DropdownField>("dropdown-fps-rate");
            _dropdownFPSRate.choices.Clear();

            _dropdownFPSRate.choices.Add("60 FPS");
            _fpsRates.Add("60 FPS", 60);
            _dropdownFPSRate.choices.Add("30 FPS");
            _fpsRates.Add("30 FPS", 30);
            _dropdownFPSRate.choices.Add("No Constraints");
            _fpsRates.Add("No Constraints", -1);

            _dropdownFPSRate.SetValueWithoutNotify(Config.FpsLimit);
            _dropdownFPSRate.RegisterValueChangedCallback(OnFSPValueChanged);

            SetFPSLimit(_fpsRates[Config.FpsLimit]);
        }

        private void OnFSPValueChanged(ChangeEvent<string> evt)
        {
            int value = _fpsRates[evt.newValue];
            Config.FpsLimit = evt.newValue;
            SetFPSLimit(value);
        }

        private void OnToggleVsyncValueChanged(ChangeEvent<bool> evt)
        {
            Config.VSyncOn = evt.newValue;
            SetVerticalSync(Config.VSyncOn);
        }

        private void SetVerticalSync(bool value) => QualitySettings.vSyncCount = value ? 1 : 0;
        private void SetFPSLimit(int value) => Application.targetFrameRate = value;
    }
}