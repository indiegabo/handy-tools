using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Debugging
{
    [DebugPanelSection]
    /// <summary>
    /// Debug panel section that displays an averaged frames-per-second counter.
    /// </summary>
    public class FPSSection : DebugPanelSection
    {
        private const string _templatePath = "UI/Debug Panel/Sections/DebugFPSSection_Template";

        /// <summary>
        /// Gets the display order of the section inside the panel.
        /// </summary>
        public override int OrderInPanel => -1;

        private TemplateContainer _mainContainer;
        private Label _fpsCounterLabel;

        private Dictionary<int, string> _cachedNumberStrings = new();
        private int[] _frameRateSamples;
        private int _cacheNumbersAmount = 300;
        private int _averageFromAmount = 30;
        private int _averageCounter = 0;
        private int _currentAveraged;

        /// <summary>
        /// Builds the UI Toolkit element used by the section.
        /// </summary>
        /// <returns>The root visual element of the section.</returns>
        public override VisualElement BuildSectionElement()
        {
            var templateAsset = Resources.Load<VisualTreeAsset>($"{_templatePath}");
            _mainContainer = templateAsset.CloneTree();

            Init();

            return _mainContainer;
        }

        private void Init()
        {
            _fpsCounterLabel = _mainContainer.Q<Label>("label-fps-counter");

            for (int i = 0; i < _cacheNumbersAmount; i++)
            {
                _cachedNumberStrings[i] = i.ToString();
            }
            _frameRateSamples = new int[_averageFromAmount];
        }

        private void Update()
        {
            if (!Panel.IsOpen) return;

            var currentFrame = (int)Math.Round(1f / Time.unscaledDeltaTime);
            _frameRateSamples[_averageCounter] = currentFrame;
            _fpsCounterLabel.text = $"FPS: {Time.frameCount}";

            var average = 0f;

            foreach (var frameRate in _frameRateSamples)
            {
                average += frameRate;
            }

            _currentAveraged = (int)Math.Round(average / _averageFromAmount);
            _averageCounter = (_averageCounter + 1) % _averageFromAmount;

            _fpsCounterLabel.text = _currentAveraged switch
            {
                var x when x >= 0 && x < _cacheNumbersAmount => _cachedNumberStrings[x],
                var x when x >= _cacheNumbersAmount => $"> {_cacheNumbersAmount}",
                var x when x < 0 => "< 0",
                _ => "?"
            };
        }
    }
}