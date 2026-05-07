using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.FSMModule
{
    public abstract class TriggerData { }

    public class FloatTriggerData : TriggerData
    {
        public float Value { get; private set; }
        public FloatTriggerData(float value)
        {
            Value = value;
        }
    }

    public class IntTriggerData : TriggerData
    {
        public int Value { get; private set; }
        public IntTriggerData(int value)
        {
            Value = value;
        }
    }

    public class StringTriggerData : TriggerData
    {
        public string Value { get; private set; }
        public StringTriggerData(string value)
        {
            Value = value;
        }
    }

    public class BoolTriggerData : TriggerData
    {
        public bool Value { get; private set; }
        public BoolTriggerData(bool value)
        {
            Value = value;
        }
    }

    public class ObjectTriggerData : TriggerData
    {
        public object Value { get; private set; }
        public ObjectTriggerData(object value)
        {
            Value = value;
        }

        public T ValueAs<T>() where T : class
        {
            return Value as T;
        }

        public bool TryValueAs<T>(out T value) where T : class
        {
            value = Value as T;
            return value != null;
        }
    }

    public class StateTriggerData : TriggerData
    {
        public IState Value { get; private set; }
        public StateTriggerData(IState value)
        {
            Value = value;
        }
    }
}