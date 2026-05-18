using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    public sealed class CutsceneRuntimeStateStore
    {
        private readonly Dictionary<SerializableGuid, Dictionary<string, object>> _nodeState = new();

        public T GetOrCreate<T>(SerializableGuid nodeId, string key, Func<T> factory)
        {
            if (TryGet(nodeId, key, out T existingValue))
            {
                return existingValue;
            }

            T createdValue = factory();
            Set(nodeId, key, createdValue);
            return createdValue;
        }

        public bool TryGet<T>(SerializableGuid nodeId, string key, out T value)
        {
            value = default;

            if (!_nodeState.TryGetValue(nodeId, out Dictionary<string, object> values))
            {
                return false;
            }

            if (!values.TryGetValue(key, out object boxedValue) || boxedValue is not T typedValue)
            {
                return false;
            }

            value = typedValue;
            return true;
        }

        public void Set<T>(SerializableGuid nodeId, string key, T value)
        {
            if (!_nodeState.TryGetValue(nodeId, out Dictionary<string, object> values))
            {
                values = new Dictionary<string, object>(StringComparer.Ordinal);
                _nodeState[nodeId] = values;
            }

            values[key] = value;
        }

        public void Remove(SerializableGuid nodeId, string key)
        {
            if (_nodeState.TryGetValue(nodeId, out Dictionary<string, object> values))
            {
                values.Remove(key);
            }
        }

        public void ClearNode(SerializableGuid nodeId)
        {
            _nodeState.Remove(nodeId);
        }

        public void Clear()
        {
            _nodeState.Clear();
        }
    }
}