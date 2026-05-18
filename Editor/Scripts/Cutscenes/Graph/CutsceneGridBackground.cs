using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    /// <summary>
    /// Draws one explicit major and minor grid for the cutscene graph so node
    /// placement reads as small units during authoring.
    /// </summary>
    internal sealed class CutsceneGridBackground : VisualElement
    {
        private const float MajorStep = 80f;
        private const int MinorDivisions = 5;
        private const float MinorVisibilityThreshold = 6f;

        internal const float MinorStep = MajorStep / MinorDivisions;

        private static readonly Color BackgroundColor =
            new(0.105f, 0.105f, 0.105f, 1f);

        private static readonly Color MinorGridColor =
            new(0.19f, 0.19f, 0.19f, 0.75f);

        private static readonly Color MajorGridColor =
            new(0.27f, 0.27f, 0.27f, 0.95f);

        private readonly GraphView _graphView;

        /// <summary>
        /// Creates one background drawer bound to the current graph view
        /// transform.
        /// </summary>
        /// <param name="graphView">
        /// Graph view that owns the background and its pan or zoom state.
        /// </param>
        public CutsceneGridBackground(GraphView graphView)
        {
            _graphView = graphView;

            pickingMode = PickingMode.Ignore;
            focusable = false;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
            style.backgroundColor = BackgroundColor;

            generateVisualContent += OnGenerateVisualContent;
        }

        /// <summary>
        /// Draws the grid lines for the current viewport and graph transform.
        /// </summary>
        /// <param name="context">Mesh generation context for this repaint.</param>
        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f || _graphView == null)
            {
                return;
            }

            Matrix4x4 viewTransformMatrix = _graphView.viewTransform.matrix;
            Vector3 translation = viewTransformMatrix.GetColumn(3);
            float zoom = Mathf.Max(_graphView.scale, 0.01f);
            float majorSpacing = MajorStep * zoom;
            float minorSpacing = MinorStep * zoom;

            if (minorSpacing >= MinorVisibilityThreshold)
            {
                DrawGridLines(
                    context.painter2D,
                    rect,
                    minorSpacing,
                    translation,
                    MinorGridColor);
            }

            DrawGridLines(
                context.painter2D,
                rect,
                majorSpacing,
                translation,
                MajorGridColor);
        }

        /// <summary>
        /// Draws one orthogonal grid pass using the provided spacing.
        /// </summary>
        /// <param name="painter">Painter used to emit vector lines.</param>
        /// <param name="rect">Current viewport rectangle.</param>
        /// <param name="spacing">Screen-space spacing between grid lines.</param>
        /// <param name="translation">Current content translation.</param>
        /// <param name="color">Stroke color for this grid pass.</param>
        private static void DrawGridLines(
            Painter2D painter,
            Rect rect,
            float spacing,
            Vector3 translation,
            Color color)
        {
            if (spacing <= 0f)
            {
                return;
            }

            float startX = PositiveModulo(translation.x, spacing);
            float startY = PositiveModulo(translation.y, spacing);

            painter.lineWidth = 1f;
            painter.strokeColor = color;
            painter.BeginPath();

            for (float x = startX; x <= rect.width; x += spacing)
            {
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, rect.height));
            }

            for (float y = startY; y <= rect.height; y += spacing)
            {
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(rect.width, y));
            }

            painter.Stroke();
        }

        /// <summary>
        /// Normalizes one translation remainder into the positive grid range.
        /// </summary>
        /// <param name="value">Translated axis value.</param>
        /// <param name="modulus">Spacing used by the current grid pass.</param>
        /// <returns>One positive offset inside the spacing interval.</returns>
        private static float PositiveModulo(float value, float modulus)
        {
            float remainder = value % modulus;
            return remainder < 0f ? remainder + modulus : remainder;
        }
    }
}