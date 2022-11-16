using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace Editor.Gui.Interaction.LegacyVariations.Model
{
    public class LegacyPreset
    {
        public States State = States.InActive;
        public string Title = String.Empty;
        public Dictionary<Guid, InputValue> ValuesForGroupParameterIds = new Dictionary<Guid, InputValue>();

        public enum States
        {
            Undefined,
            InActive,
            Active,
            Modified,
            IsBlended,
        }

        public void UpdateStateIfCurrentOrModified(ParameterGroup parameterGroup, Instance activeCompositionInstance, bool isActive)
        {
            if (activeCompositionInstance == null)
                return;

            var isModified = false;
            foreach (var param in parameterGroup.Parameters)
            {
                if (param == null || !ValuesForGroupParameterIds.TryGetValue(param.Id, out var inputValue))
                    return; // Don't update state if not all parameters are defined 
                
                var instanceChild = activeCompositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == param.SymbolChildId);
                if (instanceChild == null)
                    continue;

                var input = instanceChild.Inputs.SingleOrDefault(i => i.Id == param.InputId);
                if (input == null)
                    continue;

                if (inputValue is InputValue<float> floatA && input.Input.Value is InputValue<float> floatB)
                {
                    isModified |= Math.Abs(floatA.Value - floatB.Value) > 0.0001f;
                    continue;
                }

                if (inputValue is InputValue<Vector2> vector2A && input.Input.Value is InputValue<Vector2> vector2B)
                {
                    isModified |= vector2A.Value != vector2B.Value;
                    continue;
                }

                if (inputValue is InputValue<Vector3> vector3A && input.Input.Value is InputValue<Vector3> vector3B)
                {
                    isModified |= vector3A.Value != vector3B.Value;
                    continue;
                }

                if (inputValue is InputValue<Vector4> vector4A && input.Input.Value is InputValue<Vector4> vector4B)
                {
                    isModified |= vector4A.Value != vector4B.Value;
                    continue;
                }

                if (inputValue is InputValue<int> intA && input.Input.Value is InputValue<int> intB)
                {
                    isModified |= intA.Value != intB.Value;
                    continue;
                }

                return;    // Don't update if type is unknown 
            }

            // This is useful for highlighting duplicate presets 
            if (isActive)
            {
                State = isModified ? States.Modified : States.Active;
            }
            else
            {
                State = isModified ? States.InActive : States.Active;
            }
        }

        public void ToJson(JsonTextWriter writer)
        {
            writer.WriteObject(nameof(Title), Title);
            writer.WritePropertyName("Values");
            writer.WriteStartArray();
            foreach (var (parameterId, value) in ValuesForGroupParameterIds)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("ParameterId");
                writer.WriteValue(parameterId);
                writer.WritePropertyName("Type");
                writer.WriteValue(value.ValueType.ToString());
                writer.WritePropertyName("Value");
                value.ToJson(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public static LegacyPreset FromJson(JToken reader)
        {
            var newPreset = new LegacyPreset { State = States.InActive };
            
            if (reader[ nameof(Title)] != null)
            {
                newPreset.Title = reader[nameof(Title)].Value<string>() ?? string.Empty;
            }
            
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