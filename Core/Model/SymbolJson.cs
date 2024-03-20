using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.SystemUi;
using T3.Serialization;

// ReSharper disable AssignNullToNotNullAttribute

namespace T3.Core.Model
{
    public static class SymbolJson
    {
        #region writing
        public static void WriteSymbol(Symbol symbol, JsonTextWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteValue(JsonKeys.Id, symbol.Id);
            writer.WriteComment(symbol.Name);

            WriteSymbolInputs(symbol.InputDefinitions, writer);
            WriteSymbolChildren(symbol.Children, writer);
            WriteConnections(symbol.Connections, writer);
            symbol.PlaybackSettings?.WriteToJson(writer);
            symbol.Animator.Write(writer);

            writer.WriteEndObject();
        }

        private static void WriteSymbolInputs(List<Symbol.InputDefinition> inputs, JsonTextWriter writer)
        {
            writer.WritePropertyName(JsonKeys.Inputs);
            writer.WriteStartArray();

            foreach (var input in inputs)
            {
                writer.WriteStartObject();
                writer.WriteObject(JsonKeys.Id, input.Id);
                writer.WriteComment(input.Name);
                writer.WritePropertyName(JsonKeys.DefaultValue);
                input.DefaultValue.ToJson(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteConnections(List<Symbol.Connection> connections, JsonTextWriter writer)
        {
            writer.WritePropertyName(JsonKeys.Connections);
            writer.WriteStartArray();
            foreach (var connection in connections.OrderBy(c => c.TargetParentOrChildId.ToString() + c.TargetSlotId))
            {
                writer.WriteStartObject();
                writer.WriteValue(JsonKeys.SourceParentOrChildId, connection.SourceParentOrChildId);
                writer.WriteValue(JsonKeys.SourceSlotId, connection.SourceSlotId);
                writer.WriteValue(JsonKeys.TargetParentOrChildId, connection.TargetParentOrChildId);
                writer.WriteValue(JsonKeys.TargetSlotId, connection.TargetSlotId);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteSymbolChildren(List<SymbolChild> children, JsonTextWriter writer)
        {
            writer.WritePropertyName(JsonKeys.Children);
            writer.WriteStartArray();
            foreach (var child in children)
            {
                writer.WriteStartObject();
                writer.WriteValue(JsonKeys.Id, child.Id);
                writer.WriteComment(child.ReadableName);
                if (child.IsBypassed)
                {
                    writer.WriteValue(JsonKeys.IsBypassed, child.IsBypassed);
                }

                writer.WriteValue(JsonKeys.SymbolId, child.Symbol.Id);
                if (!string.IsNullOrEmpty(child.Name))
                {
                    writer.WriteObject(JsonKeys.SymbolChildName, child.Name);
                }

                writer.WritePropertyName(JsonKeys.InputValues);
                writer.WriteStartArray();
                foreach (var (id, inputValue) in child.Inputs)
                {
                    if (inputValue.IsDefault)
                        continue;

                    writer.WriteStartObject();
                    writer.WriteValue(JsonKeys.Id, id);
                    writer.WriteComment(inputValue.Name);
                    writer.WriteObject(JsonKeys.Type, inputValue.Value.ValueType);
                    writer.WritePropertyName(JsonKeys.Value);
                    inputValue.Value.ToJson(writer);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WritePropertyName(JsonKeys.Outputs);
                writer.WriteStartArray();

                foreach (var (id, output) in child.Outputs)
                {
                    if (output.DirtyFlagTrigger != output.OutputDefinition.DirtyFlagTrigger || output.OutputData != null || output.IsDisabled)
                    {
                        writer.WriteStartObject();
                        writer.WriteValue(JsonKeys.Id, id);
                        writer.WriteComment(child.ReadableName);

                        if (output.OutputData != null)
                        {
                            writer.WritePropertyName(JsonKeys.OutputData);
                            writer.WriteStartObject();
                            writer.WriteObject(JsonKeys.Type, output.OutputData.DataType);
                            output.OutputData.ToJson(writer);
                            writer.WriteEndObject();
                        }

                        if (output.DirtyFlagTrigger != output.OutputDefinition.DirtyFlagTrigger)
                        {
                            writer.WriteObject(JsonKeys.DirtyFlagTrigger, output.DirtyFlagTrigger);
                        }

                        if (output.IsDisabled)
                        {
                            writer.WriteValue(JsonKeys.IsDisabled, output.IsDisabled);
                        }

                        writer.WriteEndObject();
                    }
                }

                writer.WriteEndArray();

                writer.WriteEndObject(); // child
            }

            writer.WriteEndArray();
        }
        #endregion

        #region reading
        public static bool TryReadAndApplySymbolChildren(SymbolReadResult symbolReadResult)
        {
            var childrenJson = symbolReadResult.ChildrenJsonArray;

            if (childrenJson.Length == 0)
                return true;

            var parent = symbolReadResult.Symbol;
            var success = true;

            List<SymbolChild> children = new(childrenJson.Length); // todo: ordered dictionary
            foreach (var childJson in childrenJson)
            {
                var gotChild = TryReadSymbolChild(in childJson, out var symbolChild);
                success &= gotChild;

                if (!gotChild)
                    continue;

                children.Add(symbolChild);
                symbolChild.Parent = parent;
            }

            parent.SetChildren(children, setChildrensParent: false);

            if (symbolReadResult.AnimatorJsonData != null)
                parent.Animator.Read(symbolReadResult.AnimatorJsonData, parent);

            return success;
        }

        private static bool TryReadSymbolChild(in JsonChildResult childJsonResult, out SymbolChild child)
        {
            // If the used symbol hasn't been loaded so far ensure it's loaded now
            var haveChildSymbolDefinition = SymbolRegistry.Entries.TryGetValue(childJsonResult.SymbolId, out var symbol);
            if (!haveChildSymbolDefinition)
            {
                Log.Warning($"Error loading symbol child {childJsonResult.SymbolId}");
                child = null;
                return false;
            }

            child = new SymbolChild(symbol, childJsonResult.ChildId, null);

            var symbolChildJson = childJsonResult.Json;
            var nameToken = symbolChildJson[JsonKeys.SymbolChildName]?.Value<string>();
            if (nameToken != null)
            {
                child.Name = nameToken;
            }

            var isBypassedJson = symbolChildJson[JsonKeys.IsBypassed];
            if (isBypassedJson != null)
            {
                child.IsBypassed = isBypassedJson.Value<bool>();
            }

            foreach (var inputValue in (JArray)symbolChildJson[JsonKeys.InputValues])
            {
                ReadChildInputValue(child, inputValue);
            }

            foreach (var outputJson in (JArray)symbolChildJson[JsonKeys.Outputs])
            {
                var outputId = Guid.Parse(outputJson[JsonKeys.Id].Value<string>());
                var outputDataJson = outputJson[JsonKeys.OutputData];
                if (outputDataJson != null)
                {
                    ReadChildOutputData(child, outputId, outputDataJson);
                }

                var dirtyFlagJson = outputJson[JsonKeys.DirtyFlagTrigger];
                if (dirtyFlagJson != null)
                {
                    child.Outputs[outputId].DirtyFlagTrigger = (DirtyFlagTrigger)Enum.Parse(typeof(DirtyFlagTrigger), dirtyFlagJson.Value<string>());
                }

                var isDisabledJson = outputJson[JsonKeys.IsDisabled];
                if (isDisabledJson != null)
                {
                    if (child.Outputs.TryGetValue(outputId, out var output))
                        output.IsDisabled = isDisabledJson.Value<bool>();
                }
            }

            return true;
        }

        private static (Guid, JToken) ReadSymbolInputDefaults(JToken jsonInput)
        {
            var id = Guid.Parse(jsonInput[JsonKeys.Id].Value<string>());
            var jsonValue = jsonInput[JsonKeys.DefaultValue];
            return (id, jsonValue);
        }

        private static Symbol.Connection ReadConnection(JToken jsonConnection)
        {
            var sourceInstanceId = Guid.Parse(jsonConnection[JsonKeys.SourceParentOrChildId].Value<string>());
            var sourceSlotId = Guid.Parse(jsonConnection[JsonKeys.SourceSlotId].Value<string>());
            var targetInstanceId = Guid.Parse(jsonConnection[JsonKeys.TargetParentOrChildId].Value<string>());
            var targetSlotId = Guid.Parse(jsonConnection[JsonKeys.TargetSlotId].Value<string>());

            return new Symbol.Connection(sourceInstanceId, sourceSlotId, targetInstanceId, targetSlotId);
        }

        private static void ReadChildInputValue(SymbolChild symbolChild, JToken inputJson)
        {
            var id = Guid.Parse(inputJson[JsonKeys.Id].Value<string>());
            var jsonValue = inputJson[JsonKeys.Value];
            var gotInput = symbolChild.Inputs.TryGetValue(id, out var input);
            if (!gotInput)
            {
                Log.Warning($"Skipping definition of obsolete input in [{symbolChild.Symbol.Name}]: " + id);
                return;
            }

            try
            {
                input.Value.SetValueFromJson(jsonValue);
                input.IsDefault = false;
            }
            catch
            {
                Log.Error($"Failed to read input value ({input.DefaultValue.ValueType}) for {symbolChild.Symbol.Id}: " + jsonValue);
            }
        }

        private static void ReadChildOutputData(SymbolChild symbolChild, Guid outputId, JToken json)
        {
            if (json[JsonKeys.Type] == null)
                return;

            if (!symbolChild.Outputs.TryGetValue(outputId, out var output))
            {
                Log.Warning("Skipping definition of obsolete output " + outputId);
                return;
            }

            output.OutputData.ReadFromJson(json);
        }

        public static SymbolReadResult ReadSymbolRoot(in Guid id, JToken jToken, Type instanceType, SymbolPackage package)
        {
            // Read symbol with Id - dictionary of Guid-JToken?

            var childrenJsonArray = (JArray)jToken[JsonKeys.Children];
            JsonChildResult[] childrenJsons = new JsonChildResult[childrenJsonArray.Count];
            for (int i = 0; i < childrenJsonArray.Count; i++)
            {
                childrenJsons[i] = new JsonChildResult(childrenJsonArray[i]);
            }

            var connections = new List<Symbol.Connection>();
            var connectionsJson = ((JArray)jToken[JsonKeys.Connections]);
            var hasConnections = connectionsJson.Count > 0;
            if (hasConnections)
            {
                ObtainConnections(connectionsJson, connections);
            }

            var inputJsonArray = (JArray)jToken[JsonKeys.Inputs];
            var inputDefaults = inputJsonArray
                               .Select(ReadSymbolInputDefaults).ToArray();

            var orderedInputIds = inputDefaults
                                 .Select(idAndValue => idAndValue.Item1).ToArray();

            var inputDefaultValues = new Dictionary<Guid, JToken>();
            foreach (var idAndValue in inputDefaults)
            {
                inputDefaultValues[idAndValue.Item1] = idAndValue.Item2;
            }

            var symbol = package.CreateSymbol(instanceType, id, orderedInputIds);

            if (hasConnections)
                symbol.Connections.AddRange(connections);

            foreach (var input in symbol.InputDefinitions)
            {
                // If no entry is present just the value default is used, happens for new inputs
                if (inputDefaultValues.TryGetValue(input.Id, out var jsonDefaultValue))
                {
                    input.DefaultValue.SetValueFromJson(jsonDefaultValue);
                }
            }

            symbol.PlaybackSettings = PlaybackSettings.ReadFromJson(jToken, package);

            var animatorData = (JArray)jToken[JsonKeys.Animator];
            return new SymbolReadResult(symbol, childrenJsons, animatorData);
        }

        private static void ObtainConnections(JArray connectionsJson, List<Symbol.Connection> connections)
        {
            foreach (var c in connectionsJson)
            {
                var connection = ReadConnection(c);
                connections.Add(connection);
            }
        }
        #endregion

        public readonly struct JsonKeys
        {
            public const string Connections = "Connections";
            public const string SourceParentOrChildId = "SourceParentOrChildId";
            public const string SourceSlotId = "SourceSlotId";
            public const string TargetParentOrChildId = "TargetParentOrChildId";
            public const string TargetSlotId = "TargetSlotId";
            public const string Children = "Children";
            public const string Id = "Id";
            public const string SymbolChildName = "Name";
            public const string SymbolId = "SymbolId";
            public const string InputValues = "InputValues";
            public const string OutputData = "OutputData";
            public const string Type = "Type";
            public const string IsBypassed = "IsBypassed";
            public const string DirtyFlagTrigger = "DirtyFlagTrigger";
            public const string IsDisabled = "IsDisabled";
            public const string DefaultValue = "DefaultValue";
            public const string Value = "Value";
            public const string Inputs = "Inputs";
            public const string Outputs = "Outputs";
            public const string Animator = "Animator";
        }

        public readonly record struct SymbolReadResult(Symbol Symbol, JsonChildResult[] ChildrenJsonArray, JArray AnimatorJsonData);

        public readonly struct JsonChildResult
        {
            public readonly Guid SymbolId;
            public readonly Guid ChildId;
            public readonly JToken Json;

            public JsonChildResult(JToken json)
            {
                // todo: handle failure
                var symbolIdString = json[JsonKeys.SymbolId].Value<string>();
                _ = Guid.TryParse(symbolIdString, out SymbolId);

                var childIdString = json[JsonKeys.Id].Value<string>();
                _ = Guid.TryParse(childIdString, out ChildId);

                Json = json;
            }
        }

        public static JsonLoadSettings LoadSettings { get; } = new()
                                                                   {
                                                                       CommentHandling = CommentHandling.Ignore,
                                                                       DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                                                                       LineInfoHandling = LineInfoHandling.Ignore
                                                                   };
    }
}