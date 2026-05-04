using IndieGabo.HandyTools.HandyServiceLocator;
using IndieGabo.HandyTools.Logger;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.SaveSystem
{
    public abstract class SavableDataBehaviour<T> : HandyBehaviour, ISavableEntity
    {
        [SerializeField]
        protected T _data;

        protected LoadedSlotService _slotHandler;
        protected SerializableGuid _guid;

        public SerializableGuid ID => _guid;
        public object SavableData
        {
            get => _data;
            set
            {
                _data = (T)value;
                OnDataBind(_data);
            }
        }
        public T Data => _data;

        public abstract object GenerateDefaultData();
        protected abstract SerializableGuid ResolveID();

        protected virtual void OnDataBind(T data) { }

        protected virtual void Awake()
        {
            if (!ServiceLocator.Global.Get(out _slotHandler))
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