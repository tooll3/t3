using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core
{
    public static class JsonExtensions
    {
        public static void WriteValue<T>(this JsonTextWriter writer, string name, T value) where T : struct
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WriteObject(this JsonTextWriter writer, string name, object value)
        {
            if (value != null)
            {
                writer.WritePropertyName(name);
                writer.WriteValue(value.ToString());
            }
        }
    }

    public class Json
    {
        public JsonTextWriter Writer { get; set; }
        public JsonTextReader Reader { get; set; }

        public void WriteSymbol(Symbol symbol)
        {
            Writer.WriteStartObject();

            Writer.WriteObject("Name", symbol.Name);
            Writer.WriteValue("Id", symbol.Id);
            Writer.WriteObject("Namespace", symbol.Namespace);

            WriteSymbolInputs(symbol.InputDefinitions);
            WriteSymbolChildren(symbol.Children);
            WriteConnections(symbol.Connections);
            symbol.Animator.Write(Writer);

            Writer.WriteEndObject();
        }

        private void WriteSymbolInputs(List<Symbol.InputDefinition> inputs)
        {
            Writer.WritePropertyName("Inputs");
            Writer.WriteStartArray();

            foreach (var input in inputs)
            {
                Writer.WriteStartObject();
                Writer.WriteObject("Id", input.Id);
                Writer.WriteComment(input.Name);
                Writer.WritePropertyName("DefaultValue");
                input.DefaultValue.ToJson(Writer);
                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }

        private void WriteSymbolOutputs(List<Symbol.OutputDefinition> outputs)
        {
            Writer.WritePropertyName("Outputs");
            Writer.WriteStartArray();

            foreach (var output in outputs)
            {
                Writer.WriteStartObject();
                Writer.WriteObject("Id", output.Id);
                Writer.WriteComment(output.Name);
                Writer.WritePropertyName("DefaultValue");
                // output.DefaultValue.ToJson(Writer);
                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }

        private void WriteConnections(List<Symbol.Connection> connections)
        {
            Writer.WritePropertyName("Connections");
            Writer.WriteStartArray();
            foreach (var connection in connections.OrderBy(c => c.TargetParentOrChildId.ToString() + c.TargetSlotId))
            {
                Writer.WriteStartObject();
                Writer.WriteValue("SourceParentOrChildId", connection.SourceParentOrChildId);
                Writer.WriteValue("SourceSlotId", connection.SourceSlotId);
                Writer.WriteValue("TargetParentOrChildId", connection.TargetParentOrChildId);
                Writer.WriteValue("TargetSlotId", connection.TargetSlotId);
                Writer.WriteEndObject();
            }
            Writer.WriteEndArray();
        }

        private void WriteSymbolChildren(List<SymbolChild> children)
        {
            Writer.WritePropertyName("Children");
            Writer.WriteStartArray();
            foreach (var child in children)
            {
                Writer.WriteStartObject();
                Writer.WriteValue("Id", child.Id);
                Writer.WriteComment(child.ReadableName);
                Writer.WriteValue("SymbolId", child.Symbol.Id);
                if (!string.IsNullOrEmpty(child.Name))
                {
                    Writer.WriteObject("Name", child.Name);
                }

                Writer.WritePropertyName("InputValues");
                Writer.WriteStartArray();
                foreach (var inputValueEntry in child.InputValues)
                {
                    if (inputValueEntry.Value.IsDefault)
                        continue;

                    Writer.WriteStartObject();
                    Writer.WriteValue("Id", inputValueEntry.Key);
                    Writer.WriteComment(inputValueEntry.Value.Name);
                    Writer.WriteObject("Type", inputValueEntry.Value.Value.ValueType);
                    Writer.WritePropertyName("Value");
                    inputValueEntry.Value.Value.ToJson(Writer);
                    Writer.WriteEndObject();
                }
                Writer.WriteEndArray();

                Writer.WritePropertyName("OutputData");
                Writer.WriteStartArray();
                foreach (var (id, outputData) in child.OutputData)
                {
                    Writer.WriteStartObject();
                    Writer.WriteValue("Id", id);
                    Writer.WriteObject("Type", outputData.DataType);
                    outputData.ToJson(Writer);
                    Writer.WriteEndObject();
                }

                Writer.WriteEndArray();

                Writer.WriteEndObject(); // child
            }

            Writer.WriteEndArray();
        }

        public SymbolChild ReadSymbolChild(Model model, JToken symbolChildJson)
        {
            var childId = Guid.Parse(symbolChildJson["Id"].Value<string>());
            var symbolId = Guid.Parse(symbolChildJson["SymbolId"].Value<string>());
            if (!SymbolRegistry.Entries.TryGetValue(symbolId, out var symbol))
            {
                // if the used symbol hasn't been loaded so far ensure it's loaded now
                symbol = model.ReadSymbolWithId(symbolId);
            }

            var symbolChild = new SymbolChild(symbol, childId);

            var nameToken = symbolChildJson["Name"];
            if (nameToken != null)
            {
                symbolChild.Name = nameToken.Value<string>();
            }

            foreach (var inputValue in (JArray)symbolChildJson["InputValues"])
            {
                ReadChildInputValue(symbolChild, inputValue);
            }

            foreach (var outputDataToken in (JArray)symbolChildJson["OutputData"])
            {
                ReadChildOutputData(symbolChild, outputDataToken);
            }

            return symbolChild;
        }

        private (Guid, JToken) ReadSymbolInputDefaults(JToken jsonInput)
        {
            var id = Guid.Parse(jsonInput["Id"].Value<string>());
            var jsonValue = jsonInput["DefaultValue"];
            return (id, jsonValue);
        }

        private Symbol.Connection ReadConnection(JToken jsonConnection)
        {
            var sourceInstanceId = Guid.Parse(jsonConnection["SourceParentOrChildId"].Value<string>());
            var sourceSlotId = Guid.Parse(jsonConnection["SourceSlotId"].Value<string>());
            var targetInstanceId = Guid.Parse(jsonConnection["TargetParentOrChildId"].Value<string>());
            var targetSlotId = Guid.Parse(jsonConnection["TargetSlotId"].Value<string>());

            return new Symbol.Connection(sourceInstanceId, sourceSlotId, targetInstanceId, targetSlotId);
        }

        public void ReadChildInputValue(SymbolChild symbolChild, JToken inputJson)
        {
            var id = Guid.Parse(inputJson["Id"].Value<string>());
            var jsonValue = inputJson["Value"];
            try
            {
                symbolChild.InputValues[id].Value.SetValueFromJson(jsonValue);
                symbolChild.InputValues[id].IsDefault = false;
            }
            catch
            {
                Log.Error("Failed to read input value");
            }
            
        }

        private void ReadChildOutputData(SymbolChild symbolChild, JToken json)
        {
            var id = Guid.Parse(json["Id"].Value<string>());
            // var jsonValue = jsonInput["Data"];
            symbolChild.OutputData[id].ReadFromJson(json);
        }

        public Symbol ReadSymbol(Model model)
        {
            var o = JToken.ReadFrom(Reader);
            return ReadSymbol(model, o);
        }

        public Symbol ReadSymbol(Model model, JToken o, bool allowNonOperatorInstanceType = false)
        {
            var id = Guid.Parse(o["Id"].Value<string>());
            if (SymbolRegistry.Entries.ContainsKey(id))
                return null; // symbol already in registry - nothing to do

            var name = o["Name"].Value<string>();
            var @namespace = o["Namespace"]?.Value<string>() ?? "";
            var symbolChildren = (from childJson in (JArray)o["Children"]
                                  let symbolChild = ReadSymbolChild(model, childJson)
                                  select symbolChild).ToList();
            var connections = (from c in ((JArray)o["Connections"])
                               let connection = ReadConnection(c)
                               select connection).ToList();
            var orderedInputIds = (from jsonInput in (JArray)o["Inputs"]
                                      let idAndValue = ReadSymbolInputDefaults(jsonInput)
                                      select idAndValue.Item1).ToArray();
            var inputDefaultValues = (from jsonInput in (JArray)o["Inputs"]
                                      let idAndValue = ReadSymbolInputDefaults(jsonInput)
                                      select idAndValue).ToDictionary(entry => entry.Item1, entry => entry.Item2);
            var animatorData = (JArray)o["Animator"];

            string namespaceId = id.ToString().ToLower().Replace('-', '_');
            string instanceTypeName = "T3.Operators.Types.Id_" + namespaceId + "." + name + ", Operators";
            Type instanceType = Type.GetType(instanceTypeName);
            if (instanceType == null)
            {
                if (allowNonOperatorInstanceType)
                    instanceType = typeof(object);
                else
                    throw new Exception($"The type for '{instanceTypeName}' could not be found in Operator assembly.");
            }

            var symbol = new Symbol(instanceType, id, orderedInputIds, symbolChildren)
                         {
                             Name = name,
                             Namespace = @namespace,
                         };
            symbol.Connections.AddRange(connections);

            if (animatorData != null)
            {
                symbol.Animator.Read(animatorData);
            }

            foreach (var input in symbol.InputDefinitions)
            {
                // if no entry is present just the value default is used, happens for new inputs
                if (inputDefaultValues.TryGetValue(input.Id, out var jsonDefaultValue))
                {
                    input.DefaultValue.SetValueFromJson(jsonDefaultValue);
                }
            }

            return symbol;
        }
    }
}
