
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools
{
    public class SingleHandyBehaviour<T0> : HandyBehaviour where T0 : MonoBehaviour
    {
        #region Static

        public static T0 Instance { get; private set; }

        public static bool InstanceUnavailable
        {
            get
            {
                bool unavailable = Instance == null;

                if (unavailable)
                {
                    Debug.LogWarning(
                        $"[SingleHandyBehaviour] {typeof(T0).Name} failed a singleton instance validation."
                    );
                }

                return unavailable;
            }
        }

        #endregion

        #region Inspector

        [Tooltip("Keeps this object alive when a new scene is loaded.")]
        [SerializeField]
        private bool _persistent = true;

        [Tooltip("Logs an error when another singleton instance is already active.")]
        [SerializeField]
        private bool _alertAboutOtherInstances = true;

        #endregion

        #region Fields

        private bool _loaded;

        private UnityAction _singletonLoaded;

        #endregion

        #region Behaviour

        protected void Awake()
        {
            LoadSingleton();

            if (!_loaded) return;

            LoadSingletonActions();
            _singletonLoaded?.Invoke();
        }

        #endregion

        #region Singleton

        private void LoadSingleton()
        {
            T0 currentInstance = Instance;
            T0 thisInstance = this as T0;

            if (currentInstance != null && currentInstance != thisInstance)
            {
                if (_alertAboutOtherInstances)
                {
                    Debug.LogError(
                        $"[SingleHandyBehaviour] {name} Awake interrupted because another instance is already active.",
                        this
                    );
                }

                Destroy(gameObject);

                return;
            }

            Instance = thisInstance;
            _loaded = true;

            if (_persistent)
                DontDestroyOnLoad(thisInstance);
        }

        #endregion

        #region Actions

        /// <summary>
        /// Loads methods as actions to be called during the state's lifecycle.
        /// </summary>
        private void LoadSingletonActions()
        {
            System.Type stateType = this.GetType();
            bool found = false;

            while (!found && stateType != typeof(SingleHandyBehaviour<T0>))
            {
                MethodInfo mi;
                mi = stateType.GetMethod("OnSingletonLoaded", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (mi != null)
                    _singletonLoaded = Delegate.CreateDelegate(typeof(UnityAction), this, mi) as UnityAction;

                if (_singletonLoaded != null)
                {
                    found = true;
                }
                else
                {
                    stateType = stateType.BaseType;
                }
            }
        }

        #endregion
    }
}