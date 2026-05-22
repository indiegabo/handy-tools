using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Caches presenter instances by prefab for the lifetime of the application and enforces
    /// the single-active-conversation runtime rule.
    /// </summary>
    public static class ConversationPresenterRuntimeCache
    {
        #region Constants

        private const string CacheRootObjectName = "[Conversations] Presenter Cache";

        #endregion

        #region Types

        private sealed class CachedPresenterEntry
        {
            /// <summary>
            /// Stores one cached presenter instance and its current runtime ownership.
            /// </summary>
            /// <param name="prefab">Prefab asset that produced the cached instance.</param>
            /// <param name="presenterRoot">Cached presenter composition root.</param>
            public CachedPresenterEntry(
                GameObject prefab,
                ConversationPresenterRoot presenterRoot)
            {
                Prefab = prefab;
                PresenterRoot = presenterRoot;
            }

            /// <summary>
            /// Gets the prefab asset that produced the cached presenter instance.
            /// </summary>
            public GameObject Prefab { get; }

            /// <summary>
            /// Gets or sets the cached presenter composition root.
            /// </summary>
            public ConversationPresenterRoot PresenterRoot { get; set; }

            /// <summary>
            /// Gets or sets the controller that currently owns the cached presenter.
            /// </summary>
            public MonoBehaviour ActiveOwner { get; set; }

            /// <summary>
            /// Gets whether the cached presenter is currently active.
            /// </summary>
            public bool IsInUse => ActiveOwner != null;
        }

        #endregion

        #region Fields

        private static readonly Dictionary<GameObject, CachedPresenterEntry> _entriesByPrefab =
            new();

        private static Transform _cacheRoot;

        private static MonoBehaviour _activeConversationOwner;

        private static Func<bool> _runtimeActiveEvaluator = DefaultRuntimeActiveEvaluator;

        #endregion

        #region Initialization

        /// <summary>
        /// Clears cached runtime state at the start of every play session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            DestroyCacheRoot();
            _entriesByPrefab.Clear();
            _cacheRoot = null;
            _activeConversationOwner = null;
            _runtimeActiveEvaluator = DefaultRuntimeActiveEvaluator;
        }

        #endregion

        #region Conversation Lifetime

        /// <summary>
        /// Reserves the single allowed active-conversation slot for the provided controller.
        /// </summary>
        /// <param name="owner">Controller requesting conversation activation.</param>
        public static void ReserveConversation(MonoBehaviour owner)
        {
            if (!IsRuntimeActive || owner == null)
            {
                return;
            }

            PruneDestroyedState();

            if (_activeConversationOwner != null && !ReferenceEquals(_activeConversationOwner, owner))
            {
                throw new InvalidOperationException(
                    "Conversations runtime does not allow more than one active conversation at a time. "
                    + $"'{DescribeOwner(owner)}' tried to start while '{DescribeOwner(_activeConversationOwner)}' is still active.");
            }

            _activeConversationOwner = owner;
        }

        /// <summary>
        /// Releases the active-conversation slot when it belongs to the provided controller.
        /// </summary>
        /// <param name="owner">Controller whose conversation lifetime should end.</param>
        public static void ReleaseConversation(MonoBehaviour owner)
        {
            if (!IsRuntimeActive)
            {
                return;
            }

            PruneDestroyedState();

            if (_activeConversationOwner != null && ReferenceEquals(_activeConversationOwner, owner))
            {
                _activeConversationOwner = null;
            }
        }

        #endregion

        #region Presenter Cache

        /// <summary>
        /// Resolves one cached presenter instance for the provided prefab, creating it once when
        /// no cached instance exists yet.
        /// </summary>
        /// <param name="presenterPrefab">Presenter prefab that should become active.</param>
        /// <param name="controller">Controller that should own the presenter binding.</param>
        /// <returns>The resolved cached presenter root or <c>null</c> when none is available.</returns>
        public static ConversationPresenterRoot AcquirePresenter(
            GameObject presenterPrefab,
            IConversationPlaybackController controller)
        {
            if (presenterPrefab == null || controller == null)
            {
                return null;
            }

            if (controller is MonoBehaviour owner)
            {
                return AcquirePresenter(presenterPrefab, owner, controller);
            }

            if (!IsRuntimeActive)
            {
                GameObject instance = UnityEngine.Object.Instantiate(presenterPrefab);
                instance.name = presenterPrefab.name;
                ConversationPresenterRoot presenterRoot =
                    instance.GetComponent<ConversationPresenterRoot>();

                if (presenterRoot == null)
                {
                    Debug.LogWarning(
                        $"Conversation presenter prefab '{presenterPrefab.name}' is missing ConversationPresenterRoot.");
                    return null;
                }

                presenterRoot.Bind(controller);
                return presenterRoot;
            }

            throw new InvalidOperationException(
                "Conversation presenter cache requires one owner MonoBehaviour when runtime caching is active.");
        }

        /// <summary>
        /// Resolves one cached presenter instance for the provided prefab and explicit runtime
        /// owner.
        /// </summary>
        /// <param name="presenterPrefab">Presenter prefab that should become active.</param>
        /// <param name="owner">MonoBehaviour that owns the active conversation lifetime.</param>
        /// <param name="controller">Controller that should own the presenter binding.</param>
        /// <returns>The resolved cached presenter root or <c>null</c> when none is available.</returns>
        public static ConversationPresenterRoot AcquirePresenter(
            GameObject presenterPrefab,
            MonoBehaviour owner,
            IConversationPlaybackController controller)
        {
            if (presenterPrefab == null || owner == null || controller == null)
            {
                return null;
            }

            if (!IsRuntimeActive)
            {
                GameObject instance = UnityEngine.Object.Instantiate(presenterPrefab);
                instance.name = presenterPrefab.name;
                ConversationPresenterRoot presenterRoot =
                    instance.GetComponent<ConversationPresenterRoot>();

                if (presenterRoot == null)
                {
                    Debug.LogWarning(
                        $"Conversation presenter prefab '{presenterPrefab.name}' is missing ConversationPresenterRoot.");
                    return null;
                }

                presenterRoot.Bind(controller);
                return presenterRoot;
            }

            if (owner == null)
            {
                throw new InvalidOperationException(
                    "Conversation presenter cache requires playback controllers to be MonoBehaviour instances.");
            }

            ReserveConversation(owner);
            PruneDestroyedState();

            CachedPresenterEntry entry = GetOrCreateEntry(presenterPrefab);

            if (entry == null || entry.PresenterRoot == null)
            {
                return null;
            }

            if (entry.IsInUse && !ReferenceEquals(entry.ActiveOwner, owner))
            {
                throw new InvalidOperationException(
                    "Conversations runtime does not allow more than one active presenter at a time. "
                    + $"Presenter '{presenterPrefab.name}' is already owned by '{DescribeOwner(entry.ActiveOwner)}'.");
            }

            entry.ActiveOwner = owner;

            GameObject presenterObject = entry.PresenterRoot.gameObject;
            presenterObject.transform.SetParent(EnsureCacheRoot(), false);
            entry.PresenterRoot.Bind(controller);

            if (!presenterObject.activeSelf)
            {
                presenterObject.SetActive(true);
            }

            return entry.PresenterRoot;
        }

        /// <summary>
        /// Releases one active presenter back into the runtime cache.
        /// </summary>
        /// <param name="owner">Controller releasing the presenter.</param>
        /// <param name="presenterRoot">Presenter root that should become inactive.</param>
        public static void ReleasePresenter(
            MonoBehaviour owner,
            ConversationPresenterRoot presenterRoot)
        {
            if (presenterRoot == null)
            {
                return;
            }

            if (!IsRuntimeActive)
            {
                UnityEngine.Object.DestroyImmediate(presenterRoot.gameObject);
                return;
            }

            PruneDestroyedState();

            CachedPresenterEntry entry = FindEntry(presenterRoot);

            if (entry == null)
            {
                presenterRoot.ClearBinding();
                presenterRoot.gameObject.SetActive(false);
                presenterRoot.transform.SetParent(EnsureCacheRoot(), false);
                return;
            }

            if (entry.ActiveOwner != null && !ReferenceEquals(entry.ActiveOwner, owner))
            {
                return;
            }

            entry.PresenterRoot.ClearBinding();
            entry.ActiveOwner = null;
            entry.PresenterRoot.gameObject.SetActive(false);
            entry.PresenterRoot.transform.SetParent(EnsureCacheRoot(), false);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Resolves one cached presenter entry for the provided prefab, creating it when needed.
        /// </summary>
        /// <param name="presenterPrefab">Presenter prefab asset that should be cached.</param>
        /// <returns>The resolved cached presenter entry.</returns>
        private static CachedPresenterEntry GetOrCreateEntry(GameObject presenterPrefab)
        {
            if (_entriesByPrefab.TryGetValue(presenterPrefab, out CachedPresenterEntry entry)
                && entry?.PresenterRoot != null)
            {
                return entry;
            }

            GameObject instance = UnityEngine.Object.Instantiate(
                presenterPrefab,
                EnsureCacheRoot());
            instance.name = presenterPrefab.name;
            instance.SetActive(false);

            ConversationPresenterRoot presenterRoot =
                instance.GetComponent<ConversationPresenterRoot>();

            if (presenterRoot == null)
            {
                Debug.LogWarning(
                    $"Conversation presenter prefab '{presenterPrefab.name}' is missing ConversationPresenterRoot.");
                UnityEngine.Object.Destroy(instance);
                _entriesByPrefab.Remove(presenterPrefab);
                return null;
            }

            entry = new CachedPresenterEntry(presenterPrefab, presenterRoot);
            _entriesByPrefab[presenterPrefab] = entry;
            return entry;
        }

        /// <summary>
        /// Finds the cached entry that owns the provided presenter instance.
        /// </summary>
        /// <param name="presenterRoot">Presenter root that should be resolved.</param>
        /// <returns>The cached entry when found; otherwise <c>null</c>.</returns>
        private static CachedPresenterEntry FindEntry(ConversationPresenterRoot presenterRoot)
        {
            if (presenterRoot == null)
            {
                return null;
            }

            foreach (CachedPresenterEntry entry in _entriesByPrefab.Values)
            {
                if (entry?.PresenterRoot == presenterRoot)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Ensures the persistent presenter cache root exists.
        /// </summary>
        /// <returns>The persistent cache root transform.</returns>
        private static Transform EnsureCacheRoot()
        {
            if (_cacheRoot != null)
            {
                return _cacheRoot;
            }

            GameObject cacheRootObject = GameObject.Find(CacheRootObjectName);

            if (cacheRootObject == null)
            {
                cacheRootObject = new GameObject(CacheRootObjectName);

                if (Application.isPlaying)
                {
                    UnityEngine.Object.DontDestroyOnLoad(cacheRootObject);
                }
            }

            _cacheRoot = cacheRootObject.transform;
            return _cacheRoot;
        }

        /// <summary>
        /// Gets whether the cache should execute its runtime-only behavior.
        /// </summary>
        private static bool IsRuntimeActive =>
            _runtimeActiveEvaluator?.Invoke() ?? DefaultRuntimeActiveEvaluator();

        /// <summary>
        /// Resolves the default runtime-active predicate.
        /// </summary>
        /// <returns>True when the application is currently playing.</returns>
        private static bool DefaultRuntimeActiveEvaluator()
        {
            return Application.isPlaying;
        }

        /// <summary>
        /// Destroys the persistent cache root when one already exists.
        /// </summary>
        private static void DestroyCacheRoot()
        {
            Transform existingRoot = _cacheRoot;
            _cacheRoot = null;

            GameObject cacheRootObject = existingRoot != null
                ? existingRoot.gameObject
                : GameObject.Find(CacheRootObjectName);

            if (cacheRootObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(cacheRootObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(cacheRootObject);
            }
        }

        /// <summary>
        /// Removes stale owners and destroyed cached presenters from the runtime state.
        /// </summary>
        private static void PruneDestroyedState()
        {
            if (_activeConversationOwner == null)
            {
                _activeConversationOwner = null;
            }

            List<GameObject> removedKeys = null;

            foreach (KeyValuePair<GameObject, CachedPresenterEntry> pair in _entriesByPrefab)
            {
                CachedPresenterEntry entry = pair.Value;

                if (entry == null || entry.PresenterRoot == null)
                {
                    removedKeys ??= new List<GameObject>();
                    removedKeys.Add(pair.Key);
                    continue;
                }

                if (entry.ActiveOwner == null)
                {
                    entry.ActiveOwner = null;
                }
            }

            if (removedKeys == null)
            {
                return;
            }

            for (int index = 0; index < removedKeys.Count; index++)
            {
                _entriesByPrefab.Remove(removedKeys[index]);
            }
        }

        /// <summary>
        /// Builds one readable controller label for diagnostics.
        /// </summary>
        /// <param name="owner">Controller that should be described.</param>
        /// <returns>The readable controller label.</returns>
        private static string DescribeOwner(MonoBehaviour owner)
        {
            return owner == null
                ? "Unknown Controller"
                : $"{owner.GetType().Name} on '{owner.name}'";
        }

        #endregion
    }
}