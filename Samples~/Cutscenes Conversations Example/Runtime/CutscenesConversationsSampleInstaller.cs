using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.Samples.Cutscenes.Conversations
{
    /// <summary>
    /// Boots the cutscene-conversations sample scene and creates the minimal
    /// stage used to observe the authored cutscene flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutscenesConversationsSampleInstaller : MonoBehaviour
    {
        private CutsceneDirector _director;

        /// <summary>
        /// Caches the authored cutscene director stored on the sample root.
        /// </summary>
        private void Awake()
        {
            _director = GetComponent<CutsceneDirector>();
        }

        /// <summary>
        /// Ensures the runtime stage exists and starts the authored cutscene.
        /// </summary>
        private void Start()
        {
            EnsureSceneSetup();

            if (Application.isPlaying)
            {
                _director?.Play();
            }
        }

        /// <summary>
        /// Creates the camera, lighting, and simple stage markers used by the sample scene.
        /// </summary>
        private void EnsureSceneSetup()
        {
            EnsureCamera();
            EnsureLight();
            EnsureCapsuleMarker(
                "Left Actor Marker",
                new Vector3(-2.4f, 0f, 0f),
                new Color(0.31f, 0.55f, 0.82f, 1f));
            EnsureCapsuleMarker(
                "Right Actor Marker",
                new Vector3(2.4f, 0f, 0f),
                new Color(0.83f, 0.48f, 0.33f, 1f));
            EnsureCubeMarker(
                "Signal Beacon",
                new Vector3(0f, 0.5f, 2.5f),
                new Vector3(0.85f, 1f, 0.85f),
                new Color(0.76f, 0.86f, 0.36f, 1f));
        }

        /// <summary>
        /// Ensures one main camera exists for the sample scene.
        /// </summary>
        private void EnsureCamera()
        {
            Camera camera = Camera.main != null
                ? Camera.main
                : FindAnyObjectByType<Camera>();

            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.transform.SetPositionAndRotation(
                new Vector3(0f, 2.2f, -8.5f),
                Quaternion.identity);
            camera.backgroundColor = new Color(0.12f, 0.18f, 0.27f, 1f);
        }

        /// <summary>
        /// Ensures one directional light exists for the sample scene.
        /// </summary>
        private void EnsureLight()
        {
            Light light = FindAnyObjectByType<Light>();

            if (light == null)
            {
                GameObject lightObject = new("Directional Light", typeof(Light));
                light = lightObject.GetComponent<Light>();
            }

            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        /// <summary>
        /// Ensures one colored capsule marker exists under the sample root.
        /// </summary>
        /// <param name="markerName">Marker object name.</param>
        /// <param name="localPosition">Marker local position.</param>
        /// <param name="color">Marker tint color.</param>
        private void EnsureCapsuleMarker(
            string markerName,
            Vector3 localPosition,
            Color color)
        {
            Transform markerTransform = transform.Find(markerName);

            if (markerTransform == null)
            {
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                markerObject.name = markerName;
                markerObject.transform.SetParent(transform, false);
                markerTransform = markerObject.transform;
            }

            markerTransform.localPosition = localPosition;
            markerTransform.localRotation = Quaternion.identity;
            markerTransform.localScale = Vector3.one;

            if (markerTransform.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = color;
            }
        }

        /// <summary>
        /// Ensures one colored cube marker exists under the sample root.
        /// </summary>
        /// <param name="markerName">Marker object name.</param>
        /// <param name="localPosition">Marker local position.</param>
        /// <param name="localScale">Marker local scale.</param>
        /// <param name="color">Marker tint color.</param>
        private void EnsureCubeMarker(
            string markerName,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            Transform markerTransform = transform.Find(markerName);

            if (markerTransform == null)
            {
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                markerObject.name = markerName;
                markerObject.transform.SetParent(transform, false);
                markerTransform = markerObject.transform;
            }

            markerTransform.localPosition = localPosition;
            markerTransform.localRotation = Quaternion.identity;
            markerTransform.localScale = localScale;

            if (markerTransform.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = color;
            }
        }
    }
}