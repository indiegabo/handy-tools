using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.LoggerModule;
using IndieGabo.HandyTools.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.SaveSystemModule
{
    /// <summary>
    /// Base behaviour for scene entities that persist typed save data through
    /// the loaded slot service.
    /// </summary>
    /// <typeparam name="T">Persisted data type.</typeparam>
    public abstract class SavableDataBehaviour<T> : HandyBehaviour, ISavableEntity
    {
        [BoxGroup("Save Data")]
        [SerializeField]
        protected T _data;

        protected LoadedSlotService _slotHandler;
        protected SerializableGuid _guid;

        /// <summary>
        /// Gets the stable identifier used by the save system.
        /// </summary>
        public SerializableGuid ID => _guid;

        /// <summary>
        /// Gets or sets the boxed persisted data payload.
        /// </summary>
        public object SavableData
        {
            get => _data;
            set
            {
                _data = (T)value;
                OnDataBind(_data);
            }
        }

        /// <summary>
        /// Gets the strongly typed persisted data payload.
        /// </summary>
        public T Data => _data;

        /// <summary>
        /// Creates the default payload used when no save entry exists yet.
        /// </summary>
        /// <returns>A new default data instance.</returns>
        public abstract object GenerateDefaultData();

        /// <summary>
        /// Resolves the stable identifier used to store this entity.
        /// </summary>
        /// <returns>The resolved entity identifier.</returns>
        protected abstract SerializableGuid ResolveID();

        /// <summary>
        /// Reacts to boxed data assignment after the payload has been bound.
        /// </summary>
        /// <param name="data">New typed payload.</param>
        protected virtual void OnDataBind(T data) { }

        /// <summary>
        /// Resolves services, computes the entity identifier, and registers the
        /// entity in the loaded slot service.
        /// </summary>
        protected virtual void Awake()
        {
            if (!ServiceLocator.TryGet(out _slotHandler))
            {
                HandyLogger.Error(
                    "SavableDataBehaviour",
                    $"No {nameof(LoadedSlotService)} found",
                    this
                );
                return;
            }

            _guid = ResolveID();

            if (_slotHandler != null)
                _slotHandler.Bind(this);
        }

        /// <summary>
        /// Persists the current payload and unregisters the entity from the
        /// loaded slot service.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_slotHandler != null)
            {
                _slotHandler.PersistData(this);
                _slotHandler.Unbind(this);
            }
        }
    }
}