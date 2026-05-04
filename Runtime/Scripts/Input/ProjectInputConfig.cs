using IndieGabo.HandyTools.Utils;
using UnityEngine;
using Sirenix.Utilities;
using UnityEngine.AddressableAssets;
using IndieGabo.HandyTools.Utils.Extensions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.HandyInputSystem
{
    [CreateAssetMenu(fileName = "ProjectInputConfig", menuName = "HandyTools/Input/ProjectInputConfig")]
    public class ProjectInputConfig : ScriptableObject
    {
        #region Fields

        private static ProjectInputConfig _cachedInstance;

        [SerializeField] private int _maxNumberOfPlayers = 1;
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

        public static ProjectInputConfig Get()
        {
            if (_cachedInstance == null)
            {
                _cachedInstance = GetFromResources();
            }
            return _cachedInstance;
        }

        public static void ReloadInstance()
        {
            _cachedInstance = null;
        }

        public static ProjectInputConfig GetFromResources()
        {
            // Try to load from default path first
            var defaultPath = "HandyTools/ProjectInputConfig";
            var inputConfig = Resources.Load<ProjectInputConfig>(defaultPath);

            if (inputConfig != null) return inputConfig;

            // If not found, try alternative paths
            var path = HandyResources.GetPath("ProjectInputConfig");
            if (!string.IsNullOrEmpty(path) && path != defaultPath)
            {
                inputConfig = Resources.Load<ProjectInputConfig>(path);
                if (inputConfig != null) return inputConfig;
            }

#if UNITY_EDITOR
            // Create new instance and save it in Editor
            var newInputConfig = CreateInstance<ProjectInputConfig>();

            // Ensure Resources/HandyTools directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/HandyTools"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "HandyTools");
            }

            AssetDatabase.CreateAsset(newInputConfig, $"Assets/Resources/HandyTools/ProjectInputConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newInputConfig;
#else
            // Runtime fallback - create non-persistent instance
            Debug.LogWarning($"ProjectInputConfig asset not found at path '{path}' or '{defaultPath}'. Creating temporary runtime instance.");
            return CreateInstance<ProjectInputConfig>();
#endif
        }

        #endregion

        #region Bootstrapping

        public static void Bootstrap()
        {
            if (!InputModuleDefinition.IsActive) return;

            var inputConfig = Get();
            Preconditions.CheckNotNull(inputConfig);
            Preconditions.CheckNotNull(
                inputConfig.PlayerManagerPrefab,
                $"{nameof(PlayerManager)} prefab is not set."
            );

            var playerManager = Instantiate(inputConfig.PlayerManagerPrefab);
            DontDestroyOnLoad(playerManager.gameObject);
            playerManager.gameObject.name = $"PlayerManager";
        }

        #endregion
    }
}