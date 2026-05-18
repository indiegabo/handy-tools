using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule.Samples
{
    /// <summary>
    /// Maintains one discrete grid position and an optional persistent trail
    /// that visualizes the traversed path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommandPatternSampleGridActor : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _visualScale = new(0.8f, 0.8f, 0.8f);

        [SerializeField]
        private Color _visualColor = new(0.96f, 0.47f, 0.18f, 1f);

        [SerializeField]
        private Color _trailColor = new(0.96f, 0.78f, 0.2f, 1f);

        [SerializeField]
        private float _trailWidth = 0.12f;

        [SerializeField]
        private Vector2Int _initialGridPosition = Vector2Int.zero;

        [SerializeField]
        private float _cellSize = 1f;

        [SerializeField]
        private LineRenderer _trailLine;

        [SerializeField]
        private Transform _visualRoot;

        private readonly List<Vector3> _trailPoints = new();

        /// <summary>
        /// Gets the current discrete grid position.
        /// </summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>
        /// Gets the authored cell size.
        /// </summary>
        public float CellSize => _cellSize;

        private void Awake()
        {
            EnsureVisualRoot();
            EnsureTrailLine();
            SnapToGrid(_initialGridPosition, appendTrailPoint: true);
        }

        /// <summary>
        /// Moves the actor by one discrete delta.
        /// </summary>
        /// <param name="delta">Discrete grid delta.</param>
        public void MoveBy(Vector2Int delta)
        {
            SnapToGrid(GridPosition + delta, appendTrailPoint: true);
        }

        /// <summary>
        /// Moves the actor to one explicit grid position.
        /// </summary>
        /// <param name="gridPosition">Target grid position.</param>
        /// <param name="appendTrailPoint">Whether the new point extends the trail.</param>
        public void SnapToGrid(Vector2Int gridPosition, bool appendTrailPoint)
        {
            GridPosition = gridPosition;
            transform.position = new Vector3(
                gridPosition.x * _cellSize,
                transform.position.y,
                gridPosition.y * _cellSize);

            if (appendTrailPoint)
            {
                AppendTrailPoint(transform.position);
            }
        }

        /// <summary>
        /// Clears the persistent trail and seeds it with the current position.
        /// </summary>
        public void ResetTrail()
        {
            _trailPoints.Clear();
            AppendTrailPoint(transform.position);
        }

        /// <summary>
        /// Restores the actor to its initial grid position and resets the
        /// persistent trail to that origin point.
        /// </summary>
        public void ResetToInitialState()
        {
            SnapToGrid(_initialGridPosition, appendTrailPoint: false);
            ResetTrail();
        }

        private void AppendTrailPoint(Vector3 point)
        {
            int pointCount = _trailPoints.Count;
            if (pointCount > 0
                && IsSamePoint(_trailPoints[pointCount - 1], point))
            {
                ApplyTrail();
                return;
            }

            if (pointCount > 1
                && IsSamePoint(_trailPoints[pointCount - 2], point))
            {
                _trailPoints.RemoveAt(pointCount - 1);
                ApplyTrail();
                return;
            }

            _trailPoints.Add(point);
            ApplyTrail();
        }

        private static bool IsSamePoint(Vector3 left, Vector3 right)
        {
            return Vector3.SqrMagnitude(left - right) <= 0.0001f;
        }

        private void ApplyTrail()
        {
            if (_trailLine == null)
            {
                return;
            }

            _trailLine.positionCount = _trailPoints.Count;
            _trailLine.SetPositions(_trailPoints.ToArray());
        }

        private void EnsureVisualRoot()
        {
            if (_visualRoot != null)
            {
                return;
            }

            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObject.name = "Visual";
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localPosition = Vector3.up * 0.5f;
            visualObject.transform.localScale = _visualScale;

            Renderer renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = CreateRuntimeMaterial(_visualColor);
                renderer.sharedMaterial = material;
            }

            _visualRoot = visualObject.transform;
        }

        private void EnsureTrailLine()
        {
            if (_trailLine == null)
            {
                _trailLine = GetComponent<LineRenderer>();
            }

            if (_trailLine == null)
            {
                _trailLine = gameObject.AddComponent<LineRenderer>();
            }

            _trailLine.useWorldSpace = true;
            _trailLine.loop = false;
            _trailLine.widthMultiplier = _trailWidth;
            _trailLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trailLine.receiveShadows = false;
            _trailLine.sharedMaterial = CreateRuntimeMaterial(_trailColor);
            _trailLine.startColor = _trailColor;
            _trailLine.endColor = _trailColor;
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            Material material = new(shader)
            {
                color = color,
            };

            material.hideFlags = HideFlags.HideAndDontSave;
            return material;
        }
    }
}