using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.FSMModule
{
    public class TriggersProvider
    {
        protected FSMBrain _brain;
        protected List<UnityAction> _datalessActions = new();
        protected List<UnityAction<TriggerData>> _dataActions = new();
        protected Dictionary<string, List<UnityAction>> _datalessTriggers = new();
        protected Dictionary<string, List<UnityAction<TriggerData>>> _dataTriggers = new();

        public TriggersProvider(FSMBrain brain)
        {
            _brain = brain;
        }

        public void Squeeze(string key)
        {
            if (!_datalessTriggers.TryGetValue(key, out List<UnityAction> callbacks))
            {
                return;
            }

            _datalessActions.Clear();
            _datalessActions.AddRange(callbacks);

            for (int index = 0; index < _datalessActions.Count; index++)
            {
                _datalessActions[index]?.Invoke();
            }
        }

        public void Squeeze(string key, TriggerData value)
        {
            if (!_dataTriggers.TryGetValue(
                    key,
                    out List<UnityAction<TriggerData>> callbacks))
            {
                return;
            }

            _dataActions.Clear();
            _dataActions.AddRange(callbacks);

            for (int index = 0; index < _dataActions.Count; index++)
            {
                _dataActions[index]?.Invoke(value);
            }
        }

        public void RegisterCallback(string key, UnityAction action)
        {
            if (!_datalessTriggers.TryGetValue(key, out List<UnityAction> callbacks))
            {
                callbacks = new List<UnityAction>();
                _datalessTriggers.Add(key, callbacks);
            }

            callbacks.Add(action);
        }

        public void UnregisterCallback(string key, UnityAction action)
        {
            if (!_datalessTriggers.TryGetValue(key, out List<UnityAction> callbacks))
            {
                return;
            }

            callbacks.Remove(action);

            if (callbacks.Count == 0)
            {
                _datalessTriggers.Remove(key);
            }
        }

        public void RegisterCallback(string key, UnityAction<TriggerData> action)
        {
            if (!_dataTriggers.TryGetValue(
                    key,
                    out List<UnityAction<TriggerData>> callbacks))
            {
                callbacks = new List<UnityAction<TriggerData>>();
                _dataTriggers.Add(key, callbacks);
            }

            callbacks.Add(action);
        }

        public void UnregisterCallback(string key, UnityAction<TriggerData> action)
        {
            if (!_dataTriggers.TryGetValue(
                    key,
                    out List<UnityAction<TriggerData>> callbacks))
            {
                return;
            }

            callbacks.Remove(action);

            if (callbacks.Count == 0)
            {
                _dataTriggers.Remove(key);
            }
        }
    }
}