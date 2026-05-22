using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Stores project-level runtime settings for Conversations export loading and cache behavior.
    /// </summary>
    [CreateAssetMenu(
        fileName = nameof(ConversationRuntimeSettings),
        menuName = "HandyTools/Conversations/Runtime Settings")]
    public sealed class ConversationRuntimeSettings : ScriptableObject
    {
        #region Constants

        private const string ResourcePath = "HandyTools/Conversations/ConversationRuntimeSettings";

#if UNITY_EDITOR
        private const string EditorAssetPath =
            "Assets/Resources/HandyTools/Conversations/ConversationRuntimeSettings.asset";
#endif

        #endregion

        #region Fields

        [SerializeField]
        private ConversationLoadingStrategy _loadingStrategy =
            ConversationLoadingStrategy.StreamingAssetsOnly;

        [SerializeField]
        private int _cacheCapacity = 64;

        [SerializeField]
        private string _streamingAssetsRootOverride = string.Empty;

        [SerializeField]
        private string _alternateLocalizationRootFolderName = "alternate-localizations";

        [SerializeField]
        private string _localeOverride = string.Empty;

        [SerializeField]
        private InputActionReference _fallbackContinueAction;

        [SerializeField]
        private InputActionReference _fallbackCancelAction;

        [SerializeField]
        private InputActionReference _fallbackSkipAction;

        private static ConversationRuntimeSettings _instance;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the shared project runtime settings asset or an in-memory fallback.
        /// </summary>
        public static ConversationRuntimeSettings Instance =>
            _instance != null ? _instance : _instance = LoadInstance();

        /// <summary>
        /// Gets the configured runtime loading strategy.
        /// </summary>
        public ConversationLoadingStrategy LoadingStrategy => _loadingStrategy;

        /// <summary>
        /// Gets the configured cache capacity used by the default runtime loader.
        /// </summary>
        public int CacheCapacity => System.Math.Max(1, _cacheCapacity);

        /// <summary>
        /// Gets the optional StreamingAssets root override used by the default catalog provider.
        /// </summary>
        public string StreamingAssetsRootOverride => _streamingAssetsRootOverride ?? string.Empty;

        /// <summary>
        /// Gets the root folder name used for alternate-localization overlays under StreamingAssets.
        /// </summary>
        public string AlternateLocalizationRootFolderName =>
            _alternateLocalizationRootFolderName ?? string.Empty;

        /// <summary>
        /// Gets the optional locale override used for alternate-localization overlay lookup.
        /// </summary>
        public string LocaleOverride => _localeOverride ?? string.Empty;

        /// <summary>
        /// Gets the fallback continue action used when one table does not assign its own action.
        /// </summary>
        public InputActionReference FallbackContinueAction => _fallbackContinueAction;

        /// <summary>
        /// Gets the fallback cancel action used when one table does not assign its own action.
        /// </summary>
        public InputActionReference FallbackCancelAction => _fallbackCancelAction;

        /// <summary>
        /// Gets the fallback skip action used when one table does not assign its own action.
        /// </summary>
        public InputActionReference FallbackSkipAction => _fallbackSkipAction;

        #endregion

        #region Public API

        /// <summary>
        /// Sets the runtime loading strategy.
        /// </summary>
        /// <param name="loadingStrategy">Requested loading strategy.</param>
        public void SetLoadingStrategy(ConversationLoadingStrategy loadingStrategy)
        {
            _loadingStrategy = loadingStrategy;
            SaveIfNeeded();
        }

        /// <summary>
        /// Sets the cache capacity used by the default runtime loader.
        /// </summary>
        /// <param name="cacheCapacity">Requested cache capacity.</param>
        public void SetCacheCapacity(int cacheCapacity)
        {
            _cacheCapacity = System.Math.Max(1, cacheCapacity);
            SaveIfNeeded();
        }

        /// <summary>
        /// Sets the optional StreamingAssets root override used by the default catalog provider.
        /// </summary>
        /// <param name="streamingAssetsRootOverride">Absolute or StreamingAssets-relative root override.</param>
        public void SetStreamingAssetsRootOverride(string streamingAssetsRootOverride)
        {
            _streamingAssetsRootOverride = streamingAssetsRootOverride?.Trim() ?? string.Empty;
            SaveIfNeeded();
        }

        /// <summary>
        /// Sets the root folder name used for alternate-localization overlays.
        /// </summary>
        /// <param name="alternateLocalizationRootFolderName">Overlay root folder name.</param>
        public void SetAlternateLocalizationRootFolderName(
            string alternateLocalizationRootFolderName)
        {
            _alternateLocalizationRootFolderName =
                alternateLocalizationRootFolderName?.Trim() ?? string.Empty;
            SaveIfNeeded();
        }

        /// <summary>
        /// Sets the optional locale override used for alternate-localization overlays.
        /// </summary>
        /// <param name="localeOverride">Locale override used by runtime lookup.</param>
        public void SetLocaleOverride(string localeOverride)
        {
            _localeOverride = localeOverride?.Trim() ?? string.Empty;
            SaveIfNeeded();
        }

        /// <summary>
        /// Sets the fallback continue action used when one table does not assign its own action.
        /// </summary>
        /// <param name="fallbackContinueAction">Fallback continue action.</param>
        public void SetFallbackContinueAction(InputActionReference fallbackContinueAction)
        {
            _fallbackContinueAction = fallbackContinueAction;
            SaveIfNeeded();
        }

        /// <summary>
        /// Sets the fallback cancel action used when one table does not assign its own action.
        /// </summary>
        /// <param name="fallbackCancelAction">Fallback cancel action.</param>
        public void SetFallbackCancelAction(InputActionReference fallbackCancelAction)
        {
            _fallbackCancelAction = fallbackCancelAction;
            SaveIfNeeded();
        }

        /// <summary>
        /// Sets the fallback skip action used when one table does not assign its own action.
        /// </summary>
        /// <param name="fallbackSkipAction">Fallback skip action.</param>
        public void SetFallbackSkipAction(InputActionReference fallbackSkipAction)
        {
            _fallbackSkipAction = fallbackSkipAction;
            SaveIfNeeded();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Loads the shared project runtime settings asset.
        /// </summary>
        /// <returns>The loaded asset or an in-memory fallback.</returns>
        private static ConversationRuntimeSettings LoadInstance()
        {
            ConversationRuntimeSettings asset = Resources.Load<ConversationRuntimeSettings>(
                ResourcePath);

#if UNITY_EDITOR
            if (asset == null)
            {
                asset = LoadOrCreateEditorAsset();
            }
#endif

            return asset != null ? asset : CreateInstance<ConversationRuntimeSettings>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Loads or creates the editor-side runtime settings asset under Resources.
        /// </summary>
        /// <returns>The loaded or created settings asset.</returns>
        private static ConversationRuntimeSettings LoadOrCreateEditorAsset()
        {
            ConversationRuntimeSettings asset =
                AssetDatabase.LoadAssetAtPath<ConversationRuntimeSettings>(EditorAssetPath);

            if (asset != null)
            {
                return asset;
            }

            EnsureEditorFolders();
            asset = CreateInstance<ConversationRuntimeSettings>();
            AssetDatabase.CreateAsset(asset, EditorAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>
        /// Ensures the Resources folder chain used by the settings asset exists.
        /// </summary>
        private static void EnsureEditorFolders()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/HandyTools");
            EnsureFolder("Assets/Resources/HandyTools/Conversations");
        }

        /// <summary>
        /// Ensures one project-relative folder exists.
        /// </summary>
        /// <param name="folderPath">Project-relative folder path.</param>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int separatorIndex = folderPath.LastIndexOf('/');

            if (separatorIndex <= 0)
            {
                return;
            }

            string parentFolder = folderPath.Substring(0, separatorIndex);
            string folderName = folderPath[(separatorIndex + 1)..];

            EnsureFolder(parentFolder);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        /// <summary>
        /// Persists the settings asset after one editor-side change.
        /// </summary>
        private void SaveIfNeeded()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#else
        /// <summary>
        /// No-op persistence hook outside the editor.
        /// </summary>
        private void SaveIfNeeded()
        {
        }
#endif

        #endregion
    }
}