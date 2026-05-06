using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.IdentifyingModule.SceneGuids
{
    /// <summary>
    /// Resolves scene GUIDs to currently loaded GameObjects and tracks lifecycle
    /// callbacks for GUID-backed references.
    /// </summary>
    public static class GuidManager
    {
        #region Types

        /// <summary>
        /// Holds the loaded object bound to a GUID together with subscribers
        /// interested in load and unload transitions.
        /// </summary>
        private sealed class GuidRecord
        {
            /// <summary>
            /// Gets or sets the loaded GameObject currently associated with the
            /// GUID.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// Raised when a GUID becomes bound to a loaded GameObject.
            /// </summary>
            public event Action<GameObject> Added;

            /// <summary>
            /// Raised when the loaded GameObject associated with a GUID is
            /// removed from the registry.
            /// </summary>
            public event Action Removed;

            /// <summary>
            /// Registers callbacks interested in add and remove transitions.
            /// </summary>
            /// <param name="onAdded">Callback fired when the GUID resolves to a loaded object.</param>
            /// <param name="onRemoved">Callback fired when the loaded object leaves the registry.</param>
            public void Subscribe(Action<GameObject> onAdded, Action onRemoved)
            {
                if (onAdded != null)
                {
                    Added += onAdded;
                }

                if (onRemoved != null)
                {
                    Removed += onRemoved;
                }
            }

            /// <summary>
            /// Notifies listeners that the GUID is now backed by a loaded
            /// GameObject.
            /// </summary>
            /// <param name="gameObject">Resolved GameObject for the GUID.</param>
            public void NotifyAdded(GameObject gameObject)
            {
                Added?.Invoke(gameObject);
            }

            /// <summary>
            /// Notifies listeners that the loaded GameObject left the registry.
            /// </summary>
            public void NotifyRemoved()
            {
                Removed?.Invoke();
            }
        }

        #endregion

        #region State

        private static readonly Dictionary<Guid, GuidRecord> _records = new();

        #endregion

        #region API

        /// <summary>
        /// Adds or refreshes the loaded GameObject associated with a GUID.
        /// </summary>
        /// <param name="guidComponent">Scene component that owns the GUID.</param>
        /// <returns>
        /// True when the GUID could be registered or refreshed; false when the
        /// GUID collides with a different loaded GameObject.
        /// </returns>
        public static bool Add(GuidComponent guidComponent)
        {
            if (guidComponent == null)
            {
                return false;
            }

            Guid guid = guidComponent.GetGuid();
            if (guid == Guid.Empty)
            {
                return false;
            }

            if (_records.TryGetValue(guid, out GuidRecord existingRecord))
            {
                if (existingRecord.GameObject != null &&
                    existingRecord.GameObject != guidComponent.gameObject)
                {
                    return false;
                }

                existingRecord.GameObject = guidComponent.gameObject;
                existingRecord.NotifyAdded(guidComponent.gameObject);
                return true;
            }

            _records.Add(
                guid,
                new GuidRecord
                {
                    GameObject = guidComponent.gameObject
                }
            );

            return true;
        }

        /// <summary>
        /// Removes a GUID from the registry and notifies registered unload
        /// callbacks.
        /// </summary>
        /// <param name="guid">GUID removed from the registry.</param>
        public static void Remove(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return;
            }

            if (!_records.TryGetValue(guid, out GuidRecord record))
            {
                return;
            }

            record.NotifyRemoved();
            _records.Remove(guid);
        }

        /// <summary>
        /// Resolves a GUID to the currently loaded GameObject and subscribes to
        /// future add and remove transitions.
        /// </summary>
        /// <param name="guid">GUID to resolve.</param>
        /// <param name="onAddCallback">Optional callback fired when the GUID becomes loaded.</param>
        /// <param name="onRemoveCallback">Optional callback fired when the loaded object disappears.</param>
        /// <returns>The currently loaded GameObject for the GUID, or null.</returns>
        public static GameObject ResolveGuid(
            Guid guid,
            Action<GameObject> onAddCallback,
            Action onRemoveCallback
        )
        {
            if (guid == Guid.Empty)
            {
                return null;
            }

            if (!_records.TryGetValue(guid, out GuidRecord record))
            {
                record = new GuidRecord();
                _records.Add(guid, record);
            }

            record.Subscribe(onAddCallback, onRemoveCallback);
            return record.GameObject;
        }

        /// <summary>
        /// Resolves a GUID and subscribes only to unload transitions.
        /// </summary>
        /// <param name="guid">GUID to resolve.</param>
        /// <param name="onDestroyCallback">Callback fired when the loaded object disappears.</param>
        /// <returns>The currently loaded GameObject for the GUID, or null.</returns>
        public static GameObject ResolveGuid(Guid guid, Action onDestroyCallback)
        {
            return ResolveGuid(guid, null, onDestroyCallback);
        }

        /// <summary>
        /// Resolves a GUID without subscribing to lifecycle callbacks.
        /// </summary>
        /// <param name="guid">GUID to resolve.</param>
        /// <returns>The currently loaded GameObject for the GUID, or null.</returns>
        public static GameObject ResolveGuid(Guid guid)
        {
            return ResolveGuid(guid, null, null);
        }

        /// <summary>
        /// Clears all GUID bindings. This is primarily useful in editor tooling
        /// and test isolation.
        /// </summary>
        public static void Clear()
        {
            _records.Clear();
        }

        #endregion
    }
}
