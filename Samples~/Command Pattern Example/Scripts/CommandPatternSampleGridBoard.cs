using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule.Samples
{
    /// <summary>
    /// Builds a lightweight visible grid with line renderers for the sample
    /// movement board.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommandPatternSampleGridBoard : MonoBehaviour
    {
        [SerializeField]
        private int _halfWidth = 4;

        [SerializeField]
        private int _halfHeight = 4;

        [SerializeField]
        private float _cellSize = 1f;

        [SerializeField]
        private float _lineWidth = 0.04f;

        [SerializeField]
        private Color _lineColor = new(0.22f, 0.27f, 0.31f, 1f);

        private readonly List<LineRenderer> _lineRenderers = new();

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                RebuildGrid();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                ClearGrid();
            }
        }

        private void OnValidate()
        {
            _halfWidth = Mathf.Max(0, _halfWidth);
            _halfHeight = Mathf.Max(0, _halfHeight);
            _cellSize = Mathf.Max(0.01f, _cellSize);
            _lineWidth = Mathf.Max(0.001f, _lineWidth);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _lineColor;

            int verticalLineCount = (_halfWidth * 2) + 1;
            int horizontalLineCount = (_halfHeight * 2) + 1;

            for (int x = 0; x < verticalLineCount; x++)
            {
                float xPosition = (x - _halfWidth) * _cellSize;
                Vector3 start = new(xPosition, 0f, -_halfHeight * _cellSize);
                Vector3 end = new(xPosition, 0f, _halfHeight * _cellSize);
                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y < horizontalLineCount; y++)
            {
                float zPosition = (y - _halfHeight) * _cellSize;
                Vector3 start = new(-_halfWidth * _cellSize, 0f, zPosition);
                Vector3 end = new(_halfWidth * _cellSize, 0f, zPosition);
                Gizmos.DrawLine(start, end);
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        /// <summary>
        /// Recreates the procedural grid lines.
        /// </summary>
        public void RebuildGrid()
        {
            ClearGrid();

            int verticalLineCount = (_halfWidth * 2) + 1;
            int horizontalLineCount = (_halfHeight * 2) + 1;

            for (int x = 0; x < verticalLineCount; x++)
            {
                float xPosition = (x - _halfWidth) * _cellSize;
                Vector3 start = new(xPosition, 0f, -_halfHeight * _cellSize);
                Vector3 end = new(xPosition, 0f, _halfHeight * _cellSize);
                _lineRenderers.Add(CreateLineRenderer($"Grid Vertical {x}", start, end));
            }

            for (int y = 0; y < horizontalLineCount; y++)
            {
                float zPosition = (y - _halfHeight) * _cellSize;
                Vector3 start = new(-_halfWidth * _cellSize, 0f, zPosition);
                Vector3 end = new(_halfWidth * _cellSize, 0f, zPosition);
                _lineRenderers.Add(CreateLineRenderer($"Grid Horizontal {y}", start, end));
            }
        }

        private LineRenderer CreateLineRenderer(
            string objectName,
            Vector3 start,
            Vector3 end)
        {
            GameObject lineObject = new(objectName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = false;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.widthMultiplier = _lineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.material = CreateGridMaterial();
            lineRenderer.startColor = _lineColor;
            lineRenderer.endColor = _lineColor;
            return lineRenderer;
        }

        private Material CreateGridMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            Material material = new(shader)
            {
                color = _lineColor,
            };

            material.hideFlags = HideFlags.HideAndDontSave;
            return material;
        }

        private void ClearGrid()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                Transform child = transform.GetChild(index);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _lineRenderers.Clear();
        }
    }
}