using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// Provides one reusable UI Toolkit icon button with HandyTools editor styling.
    /// </summary>
    public sealed class HandyIconButton : Button
    {
        /// <summary>
        /// Gets the standard square size used by HandyTools editor icon buttons.
        /// </summary>
        public const float DefaultButtonSize = 28f;

        /// <summary>
        /// Creates one styled icon button.
        /// </summary>
        /// <param name="clickEvent">Action invoked when the button is clicked.</param>
        /// <param name="tooltip">Tooltip shown while hovering the button.</param>
        /// <param name="fallbackText">Fallback button text when no icon is found.</param>
        /// <param name="iconNames">Ordered Unity editor icon candidates.</param>
        public HandyIconButton(
            Action clickEvent,
            string tooltip = null,
            string fallbackText = null,
            params string[] iconNames)
            : base(clickEvent)
        {
            this.tooltip = tooltip ?? string.Empty;
            ApplyButtonStyle(this);
            SetIcon(fallbackText, iconNames);
        }

        /// <summary>
        /// Replaces the current icon content.
        /// </summary>
        /// <param name="fallbackText">Fallback button text when no icon is found.</param>
        /// <param name="iconNames">Ordered Unity editor icon candidates.</param>
        public void SetIcon(string fallbackText = null, params string[] iconNames)
        {
            Clear();
            text = string.Empty;

            Texture2D icon = LoadIcon(iconNames);

            if (icon == null)
            {
                text = string.IsNullOrWhiteSpace(fallbackText)
                    ? string.Empty
                    : fallbackText;
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
            Add(image);
        }

        /// <summary>
        /// Loads the first available Unity editor texture that matches the provided names.
        /// </summary>
        /// <param name="iconNames">Ordered icon lookup candidates.</param>
        /// <returns>The first resolved icon texture, or null when none exists.</returns>
        public static Texture2D LoadIcon(params string[] iconNames)
        {
            if (iconNames == null)
            {
                return null;
            }

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

        /// <summary>
        /// Applies the standard HandyTools editor icon-button styling.
        /// </summary>
        /// <param name="button">Button that should receive the styling.</param>
        private static void ApplyButtonStyle(Button button)
        {
            button.style.width = DefaultButtonSize;
            button.style.minWidth = DefaultButtonSize;
            button.style.height = DefaultButtonSize;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.justifyContent = Justify.Center;
            button.style.alignItems = Align.Center;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
        }
    }
}
