using System;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace IndieGabo.HandyTools.IdentifyingModule.SceneGuids
{
    /// <summary>
    /// Assigns a stable scene GUID to a GameObject instance and keeps that GUID
    /// registered while the object is loaded.
    /// </summary>
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class GuidComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region State

        private Guid _guid = Guid.Empty;

        [FoldoutGroup("Guid")]
        [ReadOnly]
        [SerializeField]
        private byte[] _serializedGuid;

        #endregion

        #region Public API

        /// <summary>
        /// Gets whether the component currently owns a non-empty GUID.
        /// </summary>
        /// <returns>True when a GUID has been assigned.</returns>
        public bool IsGuidAssigned()
        {
            return _guid != Guid.Empty;
        }

        /// <summary>
        /// Gets the stable GUID for this component, generating one when needed.
        /// </summary>
        /// <returns>The current non-empty GUID.</returns>
        public Guid GetGuid()
        {
            EnsureGuidLoaded();
            return _guid;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureRegisteredGuid();
        }

        private void OnEnable()
        {
            EnsureRegisteredGuid();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (IsAssetOnDisk())
            {
                ClearGuid();
                return;
            }
#endif

            EnsureRegisteredGuid();
        }

        private void Reset()
        {
            EnsureRegisteredGuid();
        }

        /// <summary>
        /// Converts the current GUID into Unity-serializable bytes before save.
        /// Prefab assets keep an empty GUID so instances can generate their own.
        /// </summary>
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (IsAssetOnDisk())
            {
                _serializedGuid = Array.Empty<byte>();
                _guid = Guid.Empty;
                return;
            }
#endif

            EnsureGuidLoaded();
            _serializedGuid = _guid == Guid.Empty
                ? Array.Empty<byte>()
                : _guid.ToByteArray();
        }

        /// <summary>
        /// Restores the runtime GUID state from serialized bytes.
        /// </summary>
        public void OnAfterDeserialize()
        {
            EnsureGuidLoaded();
        }

        private void OnDestroy()
        {
            GuidManager.Remove(_guid);
        }

        #endregion

        #region Internal Helpers

        private void EnsureRegisteredGuid()
        {
#if UNITY_EDITOR
            if (IsAssetOnDisk())
            {
                ClearGuid();
                return;
            }
#endif

            EnsureGuidLoaded();
            if (_guid == Guid.Empty)
            {
                AssignNewGuid();
            }

            for (int attempt = 0; attempt < 4; attempt++)
            {
                if (GuidManager.Add(this))
                {
                    return;
                }

                AssignNewGuid();
            }

            Debug.LogError(
                $"[{nameof(GuidComponent)}] Failed to register a unique GUID for {name}.",
                this
            );
        }

        private void EnsureGuidLoaded()
        {
            if (_guid != Guid.Empty)
            {
                return;
            }

            if (_serializedGuid == null || _serializedGuid.Length != 16)
            {
                return;
            }

            _guid = new Guid(_serializedGuid);
        }

        private void AssignNewGuid()
        {
            _guid = Guid.NewGuid();
            _serializedGuid = _guid.ToByteArray();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);

            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(this))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
#endif
        }

        private void ClearGuid()
        {
            GuidManager.Remove(_guid);
            _guid = Guid.Empty;
            _serializedGuid = Array.Empty<byte>();
        }

#if UNITY_EDITOR
        private bool IsAssetOnDisk()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(this) || EditorUtility.IsPersistent(this))
            {
                return true;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            return prefabStage != null;
        }

#endif

        #endregion
    }
}
