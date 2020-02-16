using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace T3.Core.Operator
{
    public abstract class InputValue
    {
        public Type ValueType;
        public abstract InputValue Clone();
        public abstract void Assign(InputValue otherValue);
        public abstract void ToJson(JsonTextWriter writer);
        public abstract void SetValueFromJson(JToken json);
    }

    public class InputValue<T> : InputValue
    {
        public InputValue() : this(default)
        {
        }

        public InputValue(T value)
        {
            Value = value;
            ValueType = typeof(T);
        }

        public override InputValue Clone()
        {
            return new InputValue<T>(Value);
        }

        public override void Assign(InputValue otherValue)
        {
            if (otherValue is InputValue<T> otherTypedValue)
            {
                Value = otherTypedValue.Value;
            }
            else
            {
                Debug.Assert(false); // trying to assign different types of input values
            }
        }

        public override void ToJson(JsonTextWriter writer)
        {
            if (TypeValueToJsonConverters.Entries.TryGetValue(ValueType, out var converterFunc))
            {
                converterFunc(writer, Value);
            }
        }

        public override void SetValueFromJson(Newtonsoft.Json.Linq.JToken json)
        {
            if (JsonToTypeValueConverters.Entries.TryGetValue(ValueType, out var converterFunc))
            {
                Value = (T)converterFunc(json);
            }
            else
            {
                //Log.Error($"Trying to read a json value for type '{ValueType}' but no converter registered in JsonToTypeValueConverters. Skipping value reading.");
            }
        }

        public T Value;
    }
}