using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace IndieGabo.HandyTools.Scenes.Samples
{
    /// <summary>
    /// Loads one HandyScene metadata payload during play mode, writes one
    /// console line for each serialized field found in its resolved sections,
    /// and mirrors the same values into one runtime canvas overlay.
    /// </summary>
    public sealed class HandySceneMetadataConsoleLogger : MonoBehaviour
    {
        #region Constants

        private const string OverlayCanvasName = "__HandySceneMetadataCanvas";
        private const string OverlayPanelName = "MetadataPanel";
        private const string OverlayTextName = "MetadataText";

        #endregion

        #region Fields

        [SerializeField]
        private HandySceneReference _handyScene;

        [SerializeField]
        private bool _showRuntimeOverlay = true;

        [SerializeField]
        private int _overlayFontSize = 22;

        [SerializeField]
        private Color _overlayTextColor = Color.white;

        [SerializeField]
        private Vector2 _overlayPanelSize = new(720f, 420f);

        [SerializeField]
        private Vector2 _overlayPanelOffset = new(24f, -24f);

        private Text _overlayText;

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Creates or resolves the runtime overlay before the sample starts
        /// reading HandyScene metadata.
        /// </summary>
        private void Awake()
        {
            if (!_showRuntimeOverlay)
            {
                return;
            }

            _overlayText = GetOrCreateOverlayText();
        }

        /// <summary>
        /// Loads the selected HandyScene metadata and logs every serialized
        /// field value from each resolved SceneExtender payload.
        /// </summary>
        private void Start()
        {
            if (_handyScene == null || !_handyScene.IsAssigned)
            {
                string warning = "The sample logger requires one HandyScene reference.";
                Debug.LogWarning(warning, this);
                RenderOverlay(warning);
                return;
            }

            List<string> outputLines = new();
            outputLines.Add($"HandyScene: {_handyScene.SceneName}");
            outputLines.Add($"Path: {_handyScene.SceneAssetPath}");
            outputLines.Add(string.Empty);

            LogSection<LevelMapping>(outputLines);
            LogSection<SceneTravelProfile>(outputLines);
            RenderOverlay(BuildOverlayText(outputLines));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Resolves one sample section by type and logs all of its serialized
        /// fields when available.
        /// </summary>
        /// <typeparam name="TSection">Requested sample section type.</typeparam>
        /// <param name="outputLines">Shared overlay output lines.</param>
        private void LogSection<TSection>(List<string> outputLines)
            where TSection : SceneExtender
        {
            if (!_handyScene.TryGetSection(out TSection section))
            {
                string warning =
                    $"Could not resolve the HandyScene section '{typeof(TSection).Name}' " +
                    $"from '{_handyScene.SceneAssetPath}'. Apply the metadata " +
                    "changes before entering play mode.";
                Debug.LogWarning(warning, this);

                outputLines?.Add($"[{typeof(TSection).Name}]");
                outputLines?.Add("  missing");
                outputLines?.Add(string.Empty);
                return;
            }

            LogSectionFields(section, outputLines);
        }

        /// <summary>
        /// Logs every serialized field declared by one resolved SceneExtender
        /// instance.
        /// </summary>
        /// <param name="section">Resolved SceneExtender instance.</param>
        /// <param name="outputLines">Shared overlay output lines.</param>
        private void LogSectionFields(
            SceneExtender section,
            List<string> outputLines)
        {
            if (section == null)
            {
                return;
            }

            outputLines?.Add($"[{section.GetType().Name}]");

            List<FieldInfo> fields = GetSerializableFields(section.GetType());
            if (fields.Count == 0)
            {
                outputLines?.Add("  <no serialized fields>");
                outputLines?.Add(string.Empty);
                return;
            }

            for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
            {
                FieldInfo field = fields[fieldIndex];
                object fieldValue = field.GetValue(section);
                string formattedFieldName = FormatFieldName(field.Name);
                string formattedFieldValue = FormatFieldValue(fieldValue);

                outputLines?.Add($"  {formattedFieldName}: {formattedFieldValue}");

                Debug.Log(
                    $"HandyScene '{_handyScene.SceneName}' | " +
                    $"{section.GetType().Name}.{formattedFieldName} = " +
                    $"{formattedFieldValue}",
                    this);
            }

            outputLines?.Add(string.Empty);
        }

        /// <summary>
        /// Builds the final multiline overlay string from the collected sample
        /// metadata lines.
        /// </summary>
        /// <param name="outputLines">Collected overlay output lines.</param>
        /// <returns>The final overlay text.</returns>
        private static string BuildOverlayText(IReadOnlyList<string> outputLines)
        {
            StringBuilder builder = new();
            builder.AppendLine("HandyScene Runtime Metadata");
            builder.AppendLine(string.Empty);

            if (outputLines != null)
            {
                for (int index = 0; index < outputLines.Count; index++)
                {
                    builder.AppendLine(outputLines[index] ?? string.Empty);
                }
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Writes the provided text into the runtime overlay when the sample
        /// overlay is enabled.
        /// </summary>
        /// <param name="overlayContent">Text to render on screen.</param>
        private void RenderOverlay(string overlayContent)
        {
            if (!_showRuntimeOverlay || _overlayText == null)
            {
                return;
            }

            _overlayText.text = overlayContent ?? string.Empty;
        }

        /// <summary>
        /// Resolves or creates the sample runtime overlay text.
        /// </summary>
        /// <returns>The overlay text component.</returns>
        private Text GetOrCreateOverlayText()
        {
            Canvas canvas = GetOrCreateOverlayCanvas();
            RectTransform panel = GetOrCreateOverlayPanel(canvas.transform);
            return GetOrCreateOverlayLabel(panel);
        }

        /// <summary>
        /// Resolves or creates the dedicated screen-space canvas used by the
        /// sample runtime overlay.
        /// </summary>
        /// <returns>The resolved canvas component.</returns>
        private static Canvas GetOrCreateOverlayCanvas()
        {
            GameObject canvasObject = GameObject.Find(OverlayCanvasName);
            if (canvasObject == null)
            {
                canvasObject = new GameObject(
                    OverlayCanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        /// <summary>
        /// Resolves or creates the sample metadata panel.
        /// </summary>
        /// <param name="canvasTransform">Parent canvas transform.</param>
        /// <returns>The resolved panel transform.</returns>
        private RectTransform GetOrCreateOverlayPanel(Transform canvasTransform)
        {
            Transform panelTransform = canvasTransform.Find(OverlayPanelName);
            GameObject panelObject = panelTransform != null
                ? panelTransform.gameObject
                : new GameObject(
                    OverlayPanelName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            if (panelTransform == null)
            {
                panelObject.transform.SetParent(canvasTransform, false);
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = _overlayPanelOffset;
            panelRect.sizeDelta = _overlayPanelSize;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);
            panelImage.raycastTarget = false;

            return panelRect;
        }

        /// <summary>
        /// Resolves or creates the text component used by the runtime overlay.
        /// </summary>
        /// <param name="panelTransform">Parent panel transform.</param>
        /// <returns>The resolved text component.</returns>
        private Text GetOrCreateOverlayLabel(RectTransform panelTransform)
        {
            Transform labelTransform = panelTransform.Find(OverlayTextName);
            GameObject labelObject = labelTransform != null
                ? labelTransform.gameObject
                : new GameObject(
                    OverlayTextName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text),
                    typeof(Outline));

            if (labelTransform == null)
            {
                labelObject.transform.SetParent(panelTransform, false);
            }

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 18f);
            labelRect.offsetMax = new Vector2(-18f, -18f);

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = _overlayFontSize;
            label.color = _overlayTextColor;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            Outline outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);

            return label;
        }

        /// <summary>
        /// Collects the fields that Unity serializes for one SceneExtender
        /// hierarchy.
        /// </summary>
        /// <param name="sectionType">Concrete section type.</param>
        /// <returns>The serializable fields in declaration order.</returns>
        private static List<FieldInfo> GetSerializableFields(Type sectionType)
        {
            List<FieldInfo> fields = new();
            Type currentType = sectionType;

            while (currentType != null && typeof(SceneExtender).IsAssignableFrom(currentType))
            {
                FieldInfo[] declaredFields = currentType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                Array.Sort(
                    declaredFields,
                    (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));

                for (int fieldIndex = 0;
                    fieldIndex < declaredFields.Length;
                    fieldIndex++)
                {
                    FieldInfo field = declaredFields[fieldIndex];
                    if (IsUnitySerializedField(field))
                    {
                        fields.Add(field);
                    }
                }

                currentType = currentType.BaseType;
            }

            return fields;
        }

        /// <summary>
        /// Gets whether one reflected field participates in Unity
        /// serialization.
        /// </summary>
        /// <param name="field">Reflected field to inspect.</param>
        /// <returns>True when the field is serialized by Unity.</returns>
        private static bool IsUnitySerializedField(FieldInfo field)
        {
            if (field == null || field.IsStatic || field.IsNotSerialized)
            {
                return false;
            }

            if (field.IsPublic)
            {
                return true;
            }

            return Attribute.IsDefined(field, typeof(SerializeField))
                || Attribute.IsDefined(field, typeof(SerializeReference));
        }

        /// <summary>
        /// Formats one field name for the sample debug output.
        /// </summary>
        /// <param name="fieldName">Backing field name.</param>
        /// <returns>Display-friendly field name.</returns>
        private static string FormatFieldName(string fieldName)
        {
            return string.IsNullOrWhiteSpace(fieldName)
                ? string.Empty
                : fieldName.TrimStart('_');
        }

        /// <summary>
        /// Formats one serialized field value for console output.
        /// </summary>
        /// <param name="fieldValue">Field value to format.</param>
        /// <returns>String representation used in the debug log.</returns>
        private static string FormatFieldValue(object fieldValue)
        {
            return fieldValue switch
            {
                null => "null",
                UnityEngine.Object unityObject => unityObject.name,
                _ => fieldValue.ToString() ?? string.Empty,
            };
        }

        #endregion
    }
}