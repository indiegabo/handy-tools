using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Identifying.SceneGuids
{
    /// <summary>
    /// Stores a scene-object GUID and resolves it to a loaded GameObject when
    /// the target is available.
    /// </summary>
    [Serializable]
    public class GuidReference : ISerializationCallbackReceiver
    {
        #region State

        private GameObject _cachedReference;
        private bool _isCacheSet;
        private Guid _guid;

        [SerializeField]
        private byte[] serializedGuid;

#if UNITY_EDITOR
        [SerializeField]
        private string cachedName;

        [SerializeField]
        private SceneAsset cachedScene;
#endif

        private Action<GameObject> _guidAddedDelegate;
        private Action _guidRemovedDelegate;

        #endregion

        #region Events

        /// <summary>
        /// Raised when the stored GUID resolves to a loaded GameObject.
        /// </summary>
        public event Action<GameObject> OnGuidAdded = delegate (GameObject go) { };

        /// <summary>
        /// Raised when the currently loaded GameObject for the stored GUID is
        /// removed.
        /// </summary>
        public event Action OnGuidRemoved = delegate () { };

        #endregion

        #region Properties

        /// <summary>
        /// Resolves the stored GUID to a loaded GameObject.
        /// </summary>
        public GameObject gameObject
        {
            get
            {
                if (_isCacheSet)
                {
                    return _cachedReference;
                }

                _cachedReference = GuidManager.ResolveGuid(
                    _guid,
                    _guidAddedDelegate,
                    _guidRemovedDelegate
                );
                _isCacheSet = true;
                return _cachedReference;
            }

            private set { }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty GUID reference.
        /// </summary>
        public GuidReference()
        {
            InitializeDelegates();
        }

        /// <summary>
        /// Creates a GUID reference bound to the provided scene GUID component.
        /// </summary>
        /// <param name="target">Target component that owns the GUID.</param>
        public GuidReference(GuidComponent target)
        {
            InitializeDelegates();

            if (target == null)
            {
                return;
            }

            _guid = target.GetGuid();
            serializedGuid = _guid == Guid.Empty
                ? Array.Empty<byte>()
                : _guid.ToByteArray();

#if UNITY_EDITOR
            CacheEditorMetadata(target.gameObject);
#endif
        }

        #endregion

        #region Callbacks

        private void HandleGuidAdded(GameObject targetObject)
        {
            _cachedReference = targetObject;
            _isCacheSet = true;
            OnGuidAdded(targetObject);

#if UNITY_EDITOR
            CacheEditorMetadata(targetObject);
#endif
        }

        private void HandleGuidRemoved()
        {
            _cachedReference = null;
            _isCacheSet = false;
            OnGuidRemoved();
        }

        /// <summary>
        /// Serializes the current GUID into a Unity-friendly byte array.
        /// </summary>
        public void OnBeforeSerialize()
        {
            serializedGuid = _guid == Guid.Empty
                ? Array.Empty<byte>()
                : _guid.ToByteArray();
        }

        /// <summary>
        /// Restores runtime state from serialized bytes.
        /// </summary>
        public void OnAfterDeserialize()
        {
            _cachedReference = null;
            _isCacheSet = false;
            _guid = serializedGuid != null && serializedGuid.Length == 16
                ? new Guid(serializedGuid)
                : Guid.Empty;
            InitializeDelegates();
        }

        #endregion

        #region Helpers

        private void InitializeDelegates()
        {
            _guidAddedDelegate = HandleGuidAdded;
            _guidRemovedDelegate = HandleGuidRemoved;
        }

#if UNITY_EDITOR
        private void CacheEditorMetadata(GameObject targetObject)
        {
            if (targetObject == null)
            {
                cachedName = string.Empty;
                cachedScene = null;
                return;
            }

            cachedName = targetObject.name;
            string scenePath = targetObject.scene.path;
            cachedScene = string.IsNullOrWhiteSpace(scenePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }
#endif

        #endregion
    }
}
