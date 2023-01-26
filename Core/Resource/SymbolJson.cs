using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Audio;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

// ReSharper disable AssignNullToNotNullAttribute

namespace T3.Core.Resource
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

    public class SymbolJson
    {
        public JsonTextWriter Writer { get; set; }
        public JsonTextReader Reader { get; init; }

        #region writing
        public void WriteSymbol(Symbol symbol)
        {
            Writer.WriteStartObject();

            Writer.WriteObject("Name", symbol.Name);
            Writer.WriteValue("Id", symbol.Id);
            Writer.WriteObject("Namespace", symbol.Namespace);

            WriteSymbolInputs(symbol.InputDefinitions);
            WriteSymbolChildren(symbol.Children);
            WriteConnections(symbol.Connections);
            WriteSoundSettings(symbol.SoundSettings);
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
                foreach (var (id, inputValue) in child.InputValues)
                {
                    if (inputValue.IsDefault)
                        continue;

                    Writer.WriteStartObject();
                    Writer.WriteValue("Id", id);
                    Writer.WriteComment(inputValue.Name);
                    Writer.WriteObject("Type", inputValue.Value.ValueType);
                    Writer.WritePropertyName("Value");
                    inputValue.Value.ToJson(Writer);
                    Writer.WriteEndObject();
                }

                Writer.WriteEndArray();

                Writer.WritePropertyName("Outputs");
                Writer.WriteStartArray();

                foreach (var (id, output) in child.Outputs)
                {
                    if (output.DirtyFlagTrigger != output.OutputDefinition.DirtyFlagTrigger || output.OutputData != null || output.IsDisabled)
                    {
                        Writer.WriteStartObject();
                        Writer.WriteValue("Id", id);
                        Writer.WriteComment(child.ReadableName);

                        if (output.OutputData != null)
                        {
                            Writer.WritePropertyName("OutputData");
                            Writer.WriteStartObject();
                            Writer.WriteObject("Type", output.OutputData.DataType);
                            output.OutputData.ToJson(Writer);
                            Writer.WriteEndObject();
                        }

                        if (output.DirtyFlagTrigger != output.OutputDefinition.DirtyFlagTrigger)
                        {
                            Writer.WriteObject("DirtyFlagTrigger", output.DirtyFlagTrigger);
                        }

                        if (output.IsDisabled)
                        {
                            Writer.WriteValue("IsDisabled", output.IsDisabled);
                        }

                        Writer.WriteEndObject();
                    }
                }

                Writer.WriteEndArray();

                Writer.WriteEndObject(); // child
            }

            Writer.WriteEndArray();
        }

        private void WriteSoundSettings(SoundSettings soundSettings)
        {
            Writer.WriteValue("HasSettings", soundSettings.HasSettings);
            
            // Write audio clips
            var audioClips = soundSettings.AudioClips;
            if (audioClips == null || audioClips.Count == 0)
                return;

            
            Writer.WritePropertyName("AudioClips");
            Writer.WriteStartArray();
            foreach (var audioClip in audioClips)
            {
                audioClip.ToJson(Writer);
            }

            Writer.WriteEndArray();
        }
        #endregion

        #region reading
        private static SymbolChild ReadSymbolChild(Model model, JToken symbolChildJson)
        {
            var childId = Guid.Parse(symbolChildJson["Id"].Value<string>());
            var symbolId = Guid.Parse(symbolChildJson["SymbolId"].Value<string>());
            if (!SymbolRegistry.Entries.TryGetValue(symbolId, out var symbol))
            {
                // If the used symbol hasn't been loaded so far ensure it's loaded now
                symbol = model.ReadSymbolWithId(symbolId);
            }

            if (symbol == null)
            {
                Log.Warning($"Failed to load symbol {symbolId}.");
                return null;
            }

            var symbolChild = new SymbolChild(symbol, childId, null);

            var nameToken = symbolChildJson["Name"];
            if (nameToken != null)
            {
                symbolChild.Name = nameToken.Value<string>();
            }

            foreach (var inputValue in (JArray)symbolChildJson["InputValues"])
            {
                ReadChildInputValue(symbolChild, inputValue);
            }

            foreach (var outputJson in (JArray)symbolChildJson["Outputs"])
            {
                var outputId = Guid.Parse(outputJson["Id"].Value<string>());
                var outputDataJson = outputJson["OutputData"];
                if (outputDataJson != null)
                {
                    ReadChildOutputData(symbolChild, outputId, outputDataJson);
                }

                var dirtyFlagJson = outputJson["DirtyFlagTrigger"];
                if (dirtyFlagJson != null)
                {
                    symbolChild.Outputs[outputId].DirtyFlagTrigger = (DirtyFlagTrigger)Enum.Parse(typeof(DirtyFlagTrigger), dirtyFlagJson.Value<string>());
                }

                var isDisabledJson = outputJson["IsDisabled"];
                if (isDisabledJson != null)
                {
                    symbolChild.Outputs[outputId].IsDisabled = isDisabledJson.Value<bool>();
                }
            }

            return symbolChild;
        }

        private static (Guid, JToken) ReadSymbolInputDefaults(JToken jsonInput)
        {
            var id = Guid.Parse(jsonInput["Id"].Value<string>());
            var jsonValue = jsonInput["DefaultValue"];
            return (id, jsonValue);
        }

        private static Symbol.Connection ReadConnection(JToken jsonConnection)
        {
            var sourceInstanceId = Guid.Parse(jsonConnection["SourceParentOrChildId"].Value<string>());
            var sourceSlotId = Guid.Parse(jsonConnection["SourceSlotId"].Value<string>());
            var targetInstanceId = Guid.Parse(jsonConnection["TargetParentOrChildId"].Value<string>());
            var targetSlotId = Guid.Parse(jsonConnection["TargetSlotId"].Value<string>());

            return new Symbol.Connection(sourceInstanceId, sourceSlotId, targetInstanceId, targetSlotId);
        }

        private static void ReadChildInputValue(SymbolChild symbolChild, JToken inputJson)
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

        private static void ReadChildOutputData(SymbolChild symbolChild, Guid outputId, JToken json)
        {
            if (json["Type"] != null)
            {
                symbolChild.Outputs[outputId].OutputData.ReadFromJson(json);
            }
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
            var symbolChildren = new List<SymbolChild>();

            var missingSymbolChildIds = new HashSet<Guid>();
            var missingSymbolsIds = new HashSet<Guid>();

            foreach (var childJson in ((JArray)o["Children"]))
            {
                SymbolChild symbolChild = ReadSymbolChild(model, childJson);
                if (symbolChild == null)
                {
                    var childId = Guid.Parse((childJson["Id"] ?? "").Value<string>() ?? string.Empty);
                    var symbolId = Guid.Parse((childJson["SymbolId"] ?? "").Value<string>() ?? string.Empty);
                    Log.Warning($"Skipping child of undefined type {symbolId} in {name}");

                    if (childId != Guid.Empty)
                        missingSymbolChildIds.Add(childId);

                    if (symbolId != Guid.Empty)
                        missingSymbolsIds.Add(symbolId);
                }
                else
                {
                    symbolChildren.Add(symbolChild);
                }
            }

            var connections = new List<Symbol.Connection>();
            foreach (var c in ((JArray)o["Connections"]))
            {
                Symbol.Connection connection = ReadConnection(c);
                if (connection == null)
                {
                    Log.Warning($"Skipping invalid connection in {name}");
                }
                else if (missingSymbolChildIds.Contains(connection.TargetParentOrChildId)
                         || missingSymbolChildIds.Contains(connection.SourceParentOrChildId)
                    )
                {
                    Log.Warning("Skipping connection to child of undefined type");
                }
                else
                {
                    connections.Add(connection);
                }
            }

            var orderedInputIds = (from jsonInput in (JArray)o["Inputs"]
                                   let idAndValue = ReadSymbolInputDefaults(jsonInput)
                                   select idAndValue.Item1).ToArray();

            var inputDefaultValues = (from jsonInput in (JArray)o["Inputs"]
                                      let idAndValue = ReadSymbolInputDefaults(jsonInput)
                                      select idAndValue).ToDictionary(entry => entry.Item1, entry => entry.Item2);
            var animatorData = (JArray)o["Animator"];

            var namespaceId = id.ToString().ToLower().Replace('-', '_');
            var instanceTypeName = $"T3.Operators.Types.Id_{namespaceId}.{name}, Operators, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var instanceType = Type.GetType(instanceTypeName);
            if (instanceType == null)
            {
                if (allowNonOperatorInstanceType)
                {
                    instanceType = typeof(object);
                }
                else
                {
                    MessageBox.Show($"Definition '{instanceTypeName}' is missing in Operator.dll.\nPlease try to rebuild your solution.");
                    Application.Exit();
                    Application.ExitThread();
                }
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
                // If no entry is present just the value default is used, happens for new inputs
                if (inputDefaultValues.TryGetValue(input.Id, out var jsonDefaultValue))
                {
                    input.DefaultValue.SetValueFromJson(jsonDefaultValue);
                }
            }

            // Read sound settings

            var jSettingsToken = o["HasSettings"];
            var hasSettings = jSettingsToken != null && jSettingsToken.Value<bool>();
            
            symbol.SoundSettings = new SoundSettings
                                       {
                                           HasSettings = hasSettings
                                       };

            var jAudioClipArray = (JArray)o[nameof(Symbol.SoundSettings.AudioClips)];
            if (jAudioClipArray == null)
                return symbol;

            foreach (var c in jAudioClipArray)
            {
                var clip = AudioClip.FromJson(c);
                symbol.SoundSettings.AudioClips.Add(clip);
            }

            return symbol;
        }
        #endregion
    }
}