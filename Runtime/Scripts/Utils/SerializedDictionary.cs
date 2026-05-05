
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Provides a Unity-serializable dictionary base by mirroring keys and
    /// values into parallel serialized lists.
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type.</typeparam>
    /// <typeparam name="TValue">Dictionary value type.</typeparam>
    public abstract class SerializedDictionary<TKey, TValue>
        : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<TKey> _keyData = new List<TKey>();

        [SerializeField, HideInInspector]
        private List<TValue> _valueData = new List<TValue>();

        /// <summary>
        /// Rebuilds the runtime dictionary from the serialized key and value
        /// lists after Unity deserializes the object.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.Clear();
            for (int i = 0; i < this._keyData.Count && i < this._valueData.Count; i++)
            {
                this[this._keyData[i]] = this._valueData[i];
            }
        }

        /// <summary>
        /// Copies the runtime dictionary into parallel serialized lists before
        /// Unity writes the object.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this._keyData.Clear();
            this._valueData.Clear();

            foreach (var item in this)
            {
                this._keyData.Add(item.Key);
                this._valueData.Add(item.Value);
            }
        }
    }
}