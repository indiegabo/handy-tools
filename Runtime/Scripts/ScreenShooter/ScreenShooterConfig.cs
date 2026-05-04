using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.ScreenShooter
{
    /// <summary>
    /// Global configuration for the runtime screenshot capturer.
    /// </summary>
    [GlobalConfig("Resources/ScreenShooter")]
    public sealed class ScreenShooterConfig : HandyGlobalConfig<ScreenShooterConfig>
    {
        private const string DefaultOutputDirectory = "Screenshots";

        #region Fields

        [SerializeField]
        private InputAction _shootInputAction = CreateDefaultShootInputAction();

        [SerializeField]
        private string _outputDirectoryPath = DefaultOutputDirectory;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the input action used to trigger screenshot capture.
        /// </summary>
        public InputAction ShootInputAction
        {
            get
            {
                EnsureShootInputAction();
                return _shootInputAction;
            }
            set => SetFieldValue(
                nameof(_shootInputAction),
                value ?? CreateDefaultShootInputAction()
            );
        }

        /// <summary>
        /// Gets or sets the configured output directory.
        /// Relative paths resolve from the project root.
        /// </summary>
        public string OutputDirectoryPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_outputDirectoryPath))
                {
                    SetFieldValue(
                        nameof(_outputDirectoryPath),
                        DefaultOutputDirectory
                    );
                }

                return _outputDirectoryPath;
            }
            set => SetFieldValue(
                nameof(_outputDirectoryPath),
                NormalizeOutputDirectoryPath(value)
            );
        }

        /// <summary>
        /// Gets the absolute output directory used by the runtime module.
        /// </summary>
        public string ResolvedOutputDirectoryPath =>
            ResolveOutputDirectoryPath(OutputDirectoryPath);

        #endregion

        #region Unity

        private void OnEnable()
        {
            EnsureShootInputAction();

            if (string.IsNullOrWhiteSpace(_outputDirectoryPath))
            {
                _outputDirectoryPath = DefaultOutputDirectory;
            }
        }

        #endregion

        #region Resolution

        /// <summary>
        /// Resolves a configured output directory to an absolute path.
        /// Relative paths resolve from the project root.
        /// </summary>
        /// <param name="configuredPath">Configured absolute or relative path.</param>
        /// <returns>An absolute output directory path.</returns>
        public static string ResolveOutputDirectoryPath(string configuredPath)
        {
            string normalizedPath = NormalizeOutputDirectoryPath(configuredPath);

            if (Path.IsPathRooted(normalizedPath))
            {
                return Path.GetFullPath(normalizedPath);
            }

            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", normalizedPath)
            );
        }

        private static string NormalizeOutputDirectoryPath(string configuredPath)
        {
            string normalizedPath = (configuredPath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return DefaultOutputDirectory;
            }

            return normalizedPath.Replace('\\', '/');
        }

        private void EnsureShootInputAction()
        {
            if (_shootInputAction != null && _shootInputAction.bindings.Count > 0)
            {
                return;
            }

            _shootInputAction = CreateDefaultShootInputAction();
        }

        private static InputAction CreateDefaultShootInputAction()
        {
            InputAction action = new(
                "Take Screenshot",
                InputActionType.Button
            );

            action.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Keyboard>/leftCtrl")
                .With("Button", "<Keyboard>/f12");

            return action;
        }

        #endregion
    }
}