using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// Provides one reusable UI Toolkit button that copies text into the host
    /// operating system clipboard.
    /// </summary>
    public sealed class HandyClipboardCopyButton : Button
    {
        private const float DefaultButtonSize = 28f;

        private readonly Func<string> _valueProvider;
        private readonly string _defaultTooltip;
        private readonly string _emptyTooltip;
        private readonly string _copiedTooltip;

        /// <summary>
        /// Creates one clipboard-copy button backed by a text provider.
        /// </summary>
        /// <param name="valueProvider">Returns the current text to copy.</param>
        /// <param name="defaultTooltip">Tooltip shown when copying is available.</param>
        /// <param name="emptyTooltip">Tooltip shown when there is nothing to copy.</param>
        public HandyClipboardCopyButton(
            Func<string> valueProvider,
            string defaultTooltip = null,
            string emptyTooltip = null)
        {
            _valueProvider = valueProvider
                ?? throw new ArgumentNullException(nameof(valueProvider));
            _defaultTooltip = string.IsNullOrWhiteSpace(defaultTooltip)
                ? "Copy to clipboard"
                : defaultTooltip;
            _emptyTooltip = string.IsNullOrWhiteSpace(emptyTooltip)
                ? "Nothing to copy"
                : emptyTooltip;
            _copiedTooltip = "Copied to clipboard";

            clicked += HandleClicked;
            ApplyButtonStyle(this);
            RefreshState();
        }

        /// <summary>
        /// Refreshes the current enabled state and tooltip from the provider value.
        /// </summary>
        public void RefreshState()
        {
            bool hasValue = TryGetCopyValue(out _);
            SetEnabled(hasValue);
            tooltip = hasValue ? _defaultTooltip : _emptyTooltip;
        }

        /// <summary>
        /// Copies the current provider value into the host operating system clipboard.
        /// </summary>
        private void HandleClicked()
        {
            if (!TryGetCopyValue(out string value))
            {
                RefreshState();
                return;
            }

            EditorGUIUtility.systemCopyBuffer = value;
            tooltip = _copiedTooltip;
        }

        /// <summary>
        /// Resolves the current copyable value from the provider.
        /// </summary>
        /// <param name="value">Resolved non-empty value when available.</param>
        /// <returns>True when a copyable value exists.</returns>
        private bool TryGetCopyValue(out string value)
        {
            value = _valueProvider.Invoke()?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Applies the standard HandyTools icon-button styling for clipboard copy actions.
        /// </summary>
        /// <param name="button">Button to style.</param>
        private static void ApplyButtonStyle(Button button)
        {
            button.text = string.Empty;
            button.style.width = DefaultButtonSize;
            button.style.minWidth = DefaultButtonSize;
            button.style.height = DefaultButtonSize;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;

            Texture2D icon = LoadIcon(
                "Clipboard",
                "d_Clipboard",
                "TreeEditor.Duplicate",
                "d_TreeEditor.Duplicate");

            if (icon == null)
            {
                button.text = "Copy";
                return;
            }

            Image image = new()
            {
                image = icon,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };
            image.style.width = 16f;
            image.style.height = 16f;
            button.Add(image);
        }

        /// <summary>
        /// Loads the first available Unity editor texture matching the provided icon names.
        /// </summary>
        /// <param name="iconNames">Ordered icon lookup candidates.</param>
        /// <returns>Resolved texture when one exists.</returns>
        private static Texture2D LoadIcon(params string[] iconNames)
        {
            for (int index = 0; index < iconNames.Length; index++)
            {
                string iconName = iconNames[index];

                if (string.IsNullOrWhiteSpace(iconName))
                {
                    continue;
                }

                Texture2D texture = EditorGUIUtility.FindTexture(iconName);

                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }
    }
}
