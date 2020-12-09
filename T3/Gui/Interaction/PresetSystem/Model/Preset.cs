using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui.Interaction.PresetSystem.Model
{
    public class Preset
    {
        public States State = States.InActive;
        public Dictionary<Guid, InputValue> ValuesForGroupParameterIds = new Dictionary<Guid, InputValue>();

        public enum States
        {
            Undefined,
            InActive,
            Active,
            Modified,
        }

        public void ToJson(JsonTextWriter writer)
        {
            writer.WritePropertyName("Values");
            writer.WriteStartArray();
            foreach (var (parameterId, value) in ValuesForGroupParameterIds)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("ParameterId");
                writer.WriteValue(parameterId);
                writer.WritePropertyName("Type");
                writer.WriteValue( value.ValueType.ToString());
                writer.WritePropertyName("Value");
                value.ToJson(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public static Preset FromJson(JToken reader)
        {
            var newPreset = new Preset();
            newPreset.State = States.InActive;
            foreach (var presetToken in (JArray)reader["Values"])
            {
                Guid parameterId = Guid.Parse(presetToken["ParameterId"].Value<string>());
                var inputValueTypeName = presetToken.Value<string>("Type");

                if (!TypeByNameRegistry.Entries.TryGetValue(inputValueTypeName, out var type))
                {
                    Log.Error($"Preset contains undefined type {inputValueTypeName}");
                    continue;
                }

                if (type == null)
                    continue;

                var valueToken = presetToken["Value"];
                var inputValue = InputValueCreators.Entries[type]();
                inputValue.SetValueFromJson(valueToken);
                newPreset.ValuesForGroupParameterIds[parameterId] = inputValue;
            }
            
            return newPreset;
        }
    }
}