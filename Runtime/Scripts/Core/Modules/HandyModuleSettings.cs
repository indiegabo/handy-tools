using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Modules
{
    /// <summary>
    /// Stores project-level module activation settings.
    /// </summary>
    [CreateAssetMenu(
        fileName = nameof(HandyModuleSettings),
        menuName = "HandyTools/Modules/Module Settings"
    )]
    public sealed class HandyModuleSettings : ScriptableObject
    {
        private const string _resourcePath = "HandyTools/Modules/HandyModuleSettings";

#if UNITY_EDITOR
        private const string _editorAssetPath =
            "Assets/Resources/HandyTools/Modules/HandyModuleSettings.asset";
#endif

        [SerializeField] private List<HandyModuleState> _modules = new();

        private static HandyModuleSettings _instance;

        /// <summary>
        /// Gets the project module settings asset or an in-memory fallback.
        /// </summary>
        public static HandyModuleSettings Instance =>
            _instance != null ? _instance : _instance = LoadInstance();

        /// <summary>
        /// Gets whether a module descriptor is active.
        /// Required modules always resolve as active.
        /// </summary>
        /// <param name="descriptor">Module descriptor to evaluate.</param>
        /// <returns>True when the module should be loaded.</returns>
        public bool IsModuleActive(HandyModuleDescriptor descriptor)
        {
            if (descriptor.ActivationMode == HandyModuleActivationMode.Required)
            {
                return true;
            }

            HandyModuleState state = GetState(descriptor.Id);
            if (state != null)
            {
                return state.IsActive;
            }

            return descriptor.IsActiveByDefault;
        }

        /// <summary>
        /// Sets the activation flag for an optional module.
        /// </summary>
        /// <param name="moduleId">Stable module identifier.</param>
        /// <param name="isActive">Whether the module should be active.</param>
        public void SetModuleActive(string moduleId, bool isActive)
        {
            HandyModuleState state = GetState(moduleId);
            if (state == null)
            {
                _modules.Add(new HandyModuleState(moduleId, isActive));
            }
            else
            {
                state.IsActive = isActive;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }

        private HandyModuleState GetState(string moduleId)
        {
            for (int index = 0; index < _modules.Count; index++)
            {
                HandyModuleState state = _modules[index];
                if (state.ModuleId == moduleId)
                {
                    return state;
                }
            }

            return null;
        }

        private static HandyModuleSettings LoadInstance()
        {
            HandyModuleSettings asset = Resources.Load<HandyModuleSettings>(_resourcePath);

#if UNITY_EDITOR
            if (asset == null)
            {
                asset = LoadOrCreateEditorAsset();
            }
#endif

            return asset != null ? asset : CreateInstance<HandyModuleSettings>();
        }

#if UNITY_EDITOR
        private static HandyModuleSettings LoadOrCreateEditorAsset()
        {
            HandyModuleSettings asset = AssetDatabase.LoadAssetAtPath<HandyModuleSettings>(
                _editorAssetPath
            );
            if (asset != null)
            {
                return asset;
            }

            EnsureEditorFolders();
            asset = CreateInstance<HandyModuleSettings>();
            AssetDatabase.CreateAsset(asset, _editorAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void EnsureEditorFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/HandyTools"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "HandyTools");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/HandyTools/Modules"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/HandyTools", "Modules");
            }
        }
#endif
    }
}