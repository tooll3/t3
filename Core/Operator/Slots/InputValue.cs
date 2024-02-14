using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.DataTypes;
using T3.Core.Model;
using T3.Core.Utils;

namespace T3.Core.Operator.Slots
{
    public abstract class InputValue
    {
        public Type ValueType;
        public abstract InputValue Clone();
        public abstract void Assign(InputValue otherValue);
        public abstract void AssignClone(InputValue otherValue);
        public abstract bool IsEditableInputReferenceType { get; }
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
            if (Value is IEditableInputType xxx)
            {
                return new InputValue<T>((T)xxx.Clone());
            }

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

        public override void AssignClone(InputValue otherValue)
        {
            if (otherValue is InputValue<T> otherTypedValue)
            {
                if (otherTypedValue.Value is IEditableInputType editableInput)
                {
                    Value = (T)editableInput.Clone();
                }
                else
                {
                    Debug.Assert(false); // trying to clone non cloneable type
                }
            }
            else
            {
                Debug.Assert(false); // trying to assign different types of input values
            }
        }

        public override bool IsEditableInputReferenceType => Value is IEditableInputType;

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

        public override string ToString()
        {
            if (ValueUtils.ToStringMethods.TryGetValue(ValueType, out var fn))
            {
                return fn(this);
            }

            return Value.ToString();
        }
        
        public T Value;


    }
}