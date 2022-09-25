using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;


namespace T3.Core.DataTypes
{
    public class Dict<T> : Dictionary<string, T>, ICloneable, IOutputData
    {
        public Guid Id { get; set; }

        public Type DataType => typeof(Dict<T>);

        public Dict(T defaultValue)
        {
            _defaultValue = defaultValue;
        }

        public void ToJson(JsonTextWriter writer)
        {
            // TODO: unverified...
            writer.WritePropertyName("Dict");
            writer.WriteStartArray();
            foreach (var kvp in this)
            {
                writer.WriteStartObject();
                writer.WriteObject("Key", kvp.Key);
                writer.WriteObject("Val", kvp.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public void ReadFromJson(JToken json)
        {
            // TODO: unverified...
            var dict = json["Dict"];
            var keys = dict["Key"];
            var vals = dict["Val"];
            Clear();
            if (keys != null && vals != null)
            {
                while (keys.HasValues && vals.HasValues)
                {
                    var key = keys.First.ToObject<string>();
                    var val = vals.First.ToObject<T>();
                    this[key] = val;
                    keys.First.Remove();
                    vals.First.Remove();
                }
            }
        }

        public bool Assign(IOutputData outputData)
        {
            if (outputData is Dict<T> otherDict)
            {
                Clear();
                foreach (var kvp in otherDict)
                {
                    this[kvp.Key] = kvp.Value;
                }
                return true;
            }

            Log.Error($"Trying to assign output data of type '{outputData.GetType()}' to 'DictionaryCollection'.");
            return false;
        }

        public object Clone()
        {
            return TypedClone();
        }

        public Dict<T> TypedClone()
        {
            var result = new Dict<T>(_defaultValue);
            foreach (var kvp in this)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        private readonly T _defaultValue;
    }
}
