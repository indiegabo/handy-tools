using IndieGabo.HandyTools.Utils;
using UnityEngine;
using IndieGabo.HandyTools.Utils.Extensions;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.HandyInputSystem
{
    [CreateAssetMenu(fileName = "ProjectInputConfig", menuName = "HandyTools/Input/ProjectInputConfig")]
    /// <summary>
    /// Stores project-wide input configuration used by the input module.
    /// </summary>
    public class ProjectInputConfig : ScriptableObject
    {
        #region Fields

        private const string _defaultResourcesPath = "HandyTools/ProjectInputConfig";

#if UNITY_EDITOR
        private const string _defaultAssetPath =
            "Assets/Resources/HandyTools/ProjectInputConfig.asset";
#endif

        private static ProjectInputConfig _cachedInstance;

        [BoxGroup("Players")]
        [SerializeField] private int _maxNumberOfPlayers = 1;

        [BoxGroup("Players")]
        [SerializeField] private PlayerManager _playerManagerPrefab;

        #endregion

        #region Properties

        public int MaxNumberOfPlayers
        {
            get => _maxNumberOfPlayers;
#if UNITY_EDITOR
            set => this.SetAndDirty(ref _maxNumberOfPlayers, value);
#endif
        }

        public PlayerManager PlayerManagerPrefab
        {
            get => _playerManagerPrefab;
#if UNITY_EDITOR
            set => this.SetAndDirty(ref _playerManagerPrefab, value);
#endif
        }

        #endregion

        #region Providing

        /// <summary>
        /// Attempts to resolve the existing project input configuration asset
        /// from Resources without creating editor-side assets implicitly.
        /// </summary>
        /// <param name="inputConfig">
        /// The resolved project input configuration when one is available.
        /// </param>
        /// <returns>
        /// True when the project already provides a persisted configuration
        /// asset; otherwise, false.
        /// </returns>
        public static bool TryGetExisting(out ProjectInputConfig inputConfig)
        {
            if (_cachedInstance != null)
            {
                inputConfig = _cachedInstance;
                return true;
            }

            inputConfig = LoadFromResources();
            if (inputConfig == null)
            {
                return false;
            }

            _cachedInstance = inputConfig;
            return true;
        }

        public static ProjectInputConfig Get()
        {
            if (TryGetExisting(out ProjectInputConfig inputConfig))
            {
                return inputConfig;
            }

#if UNITY_EDITOR
            return GetOrCreateForEditor();
#else
            Debug.LogWarning(
                "ProjectInputConfig asset was not found. Returning a temporary runtime instance."
            );
            return CreateInstance<ProjectInputConfig>();
#endif
        }

        public static void ReloadInstance()
        {
            _cachedInstance = null;
        }

        private static ProjectInputConfig LoadFromResources()
        {
            // Try to load from default path first
            ProjectInputConfig inputConfig = Resources.Load<ProjectInputConfig>(
                _defaultResourcesPath
            );

            if (inputConfig != null) return inputConfig;

            // If not found, try alternative paths
            var path = HandyResources.GetPath("ProjectInputConfig");
            if (!string.IsNullOrEmpty(path) && path != _defaultResourcesPath)
            {
                inputConfig = Resources.Load<ProjectInputConfig>(path);
                if (inputConfig != null) return inputConfig;
            }

            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Resolves the existing project input configuration asset or creates
        /// the default editor asset when tooling explicitly requires one.
        /// </summary>
        /// <returns>
        /// The persisted project input configuration asset.
        /// </returns>
        public static ProjectInputConfig GetOrCreateForEditor()
        {
            if (TryGetExisting(out ProjectInputConfig inputConfig))
            {
                return inputConfig;
            }

            // Create new instance and save it in Editor.
            ProjectInputConfig newInputConfig = CreateInstance<ProjectInputConfig>();

            // Ensure Resources/HandyTools directory exists.
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/HandyTools"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "HandyTools");
            }

            AssetDatabase.CreateAsset(newInputConfig, _defaultAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _cachedInstance = newInputConfig;
            return newInputConfig;
        }
#endif

        #endregion

        #region Bootstrapping

        public static void Bootstrap()
        {
            if (!InputModuleDefinition.IsActive) return;

            if (!TryGetExisting(out ProjectInputConfig inputConfig))
            {
                Debug.LogWarning(
                    "Input module bootstrap skipped because ProjectInputConfig.asset "
                    + "was not found. Run the Input Starter Setup or create the "
                    + "configuration asset explicitly before enabling runtime bootstrap."
                );
                return;
            }

            Preconditions.CheckNotNull(inputConfig);
            Preconditions.CheckNotNull(
                inputConfig.PlayerManagerPrefab,
                $"{nameof(PlayerManager)} prefab is not set."
            );

            if (FindObjectsByType<PlayerManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            ).Length > 0)
            {
                return;
            }

            PlayerManager playerManager = Instantiate(inputConfig.PlayerManagerPrefab);
            DontDestroyOnLoad(playerManager.gameObject);
            playerManager.gameObject.name = $"PlayerManager";
        }

        #endregion
    }
}