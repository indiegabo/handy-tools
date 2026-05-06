using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.ScreenShooterModule
{
    /// <summary>
    /// Creates the runtime screenshot capturer when the module is active.
    /// </summary>
    public static class ScreenShooterBootstrapper
    {
        /// <summary>
        /// Creates the persistent runtime screenshot capturer.
        /// </summary>
        public static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<ScreenShooter>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            ScreenShooter screenShooter = new GameObject("ScreenShooter").AddComponent<ScreenShooter>();
            Object.DontDestroyOnLoad(screenShooter);
        }
    }

    /// <summary>
    /// Captures screenshots when the runtime shortcut is pressed.
    /// </summary>
    public class ScreenShooter : MonoBehaviour
    {
        private InputAction _shootAction;
        private bool _enabledShootAction;

        private ScreenShooterConfig _config;

        /// <summary>
        /// Gets the absolute screenshot output directory configured for the module.
        /// </summary>
        public static string ScreenshotDirectoryPath =>
            ScreenShooterConfig.Instance.ResolvedOutputDirectoryPath;

        private void Awake()
        {
            Initialize(ScreenShooterConfig.Instance);
        }

        private void OnDestroy()
        {
            UnbindShootAction();
        }

        /// <summary>
        /// Initializes the runtime capturer with the resolved project config.
        /// </summary>
        /// <param name="config">Resolved runtime module configuration.</param>
        public void Initialize(ScreenShooterConfig config)
        {
            _config = config;
            BindShootAction(config?.ShootInputAction);
        }

        private void BindShootAction(InputAction action)
        {
            UnbindShootAction();

            _shootAction = action;
            if (_shootAction == null)
            {
                Debug.LogWarning(
                    $"[{nameof(ScreenShooter)}] No screenshot input action was configured."
                );
                return;
            }

            _shootAction.performed += OnShootActionPerformed;

            if (_shootAction.enabled)
            {
                return;
            }

            _shootAction.Enable();
            _enabledShootAction = true;
        }

        private void UnbindShootAction()
        {
            if (_shootAction == null)
            {
                return;
            }

            _shootAction.performed -= OnShootActionPerformed;

            if (_enabledShootAction)
            {
                _shootAction.Disable();
            }

            _shootAction = null;
            _enabledShootAction = false;
        }

        private void OnShootActionPerformed(InputAction.CallbackContext context)
        {
            Shoot();
        }

        /// <summary>
        /// Captures one screenshot to the project Screenshots folder.
        /// </summary>
        public void Shoot()
        {
            string currentTime = System.DateTime.UtcNow.ToString("dd-MM-yyyy_HH-mm-ss");

            try
            {
                string screenshotDir = _config != null
                    ? _config.ResolvedOutputDirectoryPath
                    : ScreenshotDirectoryPath;

                if (!Directory.Exists(screenshotDir))
                {
                    Directory.CreateDirectory(screenshotDir);
                }

                string screenshotPath = Path.Combine(screenshotDir, $"{currentTime}.png");
                ScreenCapture.CaptureScreenshot(screenshotPath);
                Debug.Log($"[ScreenShooter] Screenshot saved at {screenshotPath}", this);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[ScreenShooter] Screenshot failed: {exception}", this);
            }
        }
    }
}