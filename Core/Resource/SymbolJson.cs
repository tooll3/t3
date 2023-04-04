using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    
    public static class SymbolJson
    {
        #region writing
        public static void WriteSymbol(Symbol symbol, JsonTextWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteObject(JsonKeys.Name, symbol.Name);
            writer.WriteValue(JsonKeys.Id, symbol.Id);
            writer.WriteObject(JsonKeys.Namespace, symbol.Namespace);

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
                writer.WriteValue(JsonKeys.SymbolId, child.Symbol.Id);
                if (!string.IsNullOrEmpty(child.Name))
                {
                    writer.WriteObject(JsonKeys.Name, child.Name);
                }

                writer.WritePropertyName(JsonKeys.InputValues);
                writer.WriteStartArray();
                foreach (var (id, inputValue) in child.InputValues)
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
        private static bool TryReadSymbolChild(Model model, JToken symbolChildJson, out SymbolChild symbolChild)
        {
            var childId = Guid.Parse(symbolChildJson[JsonKeys.Id].Value<string>());
            var symbolId = Guid.Parse(symbolChildJson[JsonKeys.SymbolId].Value<string>());
            if (!SymbolRegistry.Entries.TryGetValue(symbolId, out var symbol))
            {
                // If the used symbol hasn't been loaded so far ensure it's loaded now
                var loaded = model.TryReadSymbolWithId(symbolId, out symbol);
                if (!loaded)
                {
                    Log.Warning($"Error loading symbol child {symbolId}");
                    symbolChild = null;
                    return false;
                }
            }

            symbolChild = new SymbolChild(symbol, childId, null);

            var nameToken = symbolChildJson[JsonKeys.Name]?.Value<string>();
            if (nameToken != null)
            {
                symbolChild.Name = nameToken;
            }

            foreach (var inputValue in (JArray)symbolChildJson[JsonKeys.InputValues])
            {
                ReadChildInputValue(symbolChild, inputValue);
            }

            foreach (var outputJson in (JArray)symbolChildJson[JsonKeys.Outputs])
            {
                var outputId = Guid.Parse(outputJson[JsonKeys.Id].Value<string>());
                var outputDataJson = outputJson[JsonKeys.OutputData];
                if (outputDataJson != null)
                {
                    ReadChildOutputData(symbolChild, outputId, outputDataJson);
                }

                var dirtyFlagJson = outputJson[JsonKeys.DirtyFlagTrigger];
                if (dirtyFlagJson != null)
                {
                    symbolChild.Outputs[outputId].DirtyFlagTrigger = (DirtyFlagTrigger)Enum.Parse(typeof(DirtyFlagTrigger), dirtyFlagJson.Value<string>());
                }

                var isDisabledJson = outputJson[JsonKeys.IsDisabled];
                if (isDisabledJson != null)
                {
                    symbolChild.Outputs[outputId].IsDisabled = isDisabledJson.Value<bool>();
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
            if (json[JsonKeys.Type] != null)
            {
                symbolChild.Outputs[outputId].OutputData.ReadFromJson(json);
            }
        }

        public static bool TryReadSymbol(Model model, JToken jToken, out Symbol symbol, bool allowNonOperatorInstanceType)
        {
            var guidString = jToken[JsonKeys.Id].Value<string>();
            var hasId = Guid.TryParse(guidString, out var guid);

            if (!hasId)
            {
                Log.Error($"Failed to parse guid in symbol json: `{guidString}`");
                symbol = null;
                return false;
            }

            return TryReadSymbol(model, guid, jToken, out symbol, allowNonOperatorInstanceType);
        }

        internal static bool TryReadSymbol(Model model, Guid id, JToken jToken, out Symbol symbol, bool allowNonOperatorInstanceType)
        {
            // read symbol with Id - dictionary of Guid-JToken?
            var name = jToken[JsonKeys.Name].Value<string>();
            
            var symbolChildren = new List<SymbolChild>();
            var missingSymbolChildIds = new HashSet<Guid>();

            foreach (var childJson in ((JArray)jToken[JsonKeys.Children]))
            {
                ObtainSymbolChild(model, childJson, symbolChildren, name, missingSymbolChildIds);
            }

            var connections = new List<Symbol.Connection>();
            var connectionsJson = ((JArray)jToken[JsonKeys.Connections]);
            var hasConnections = connectionsJson.Count > 0;
            if (hasConnections)
            {
                ObtainConnections(connectionsJson, name, missingSymbolChildIds, connections);
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

            var namespaceId = CreateGuidNamespaceString(id);
            var instanceTypeName = $"T3.Operators.Types.Id_{namespaceId}.{name}, Operators, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var instanceType = !allowNonOperatorInstanceType ? GetOperatorInstanceType(instanceTypeName) : GetAnyInstanceType(instanceTypeName);

            var @namespace = jToken[JsonKeys.Namespace]?.Value<string>() ?? string.Empty;
            symbol = new Symbol(instanceType, id, orderedInputIds, symbolChildren)
                         {
                             Name = name,
                             Namespace = @namespace,
                         };
            
            if(hasConnections)
                symbol.Connections.AddRange(connections);

            var animatorData = (JArray)jToken[JsonKeys.Animator];
            if (animatorData != null)
            {
                symbol.Animator.Read(animatorData, symbol);
            }

            foreach (var input in symbol.InputDefinitions)
            {
                // If no entry is present just the value default is used, happens for new inputs
                if (inputDefaultValues.TryGetValue(input.Id, out var jsonDefaultValue))
                {
                    input.DefaultValue.SetValueFromJson(jsonDefaultValue);
                }
            }

            symbol.PlaybackSettings = PlaybackSettings.ReadFromJson(jToken);
            return true;
        }

        private static void ObtainSymbolChild(Model model, JToken childJson, List<SymbolChild> symbolChildrenList, string name, HashSet<Guid> missingSymbolChildIds)
        {
            var hasSymbolChild = TryReadSymbolChild(model, childJson, out var symbolChild);

            if (hasSymbolChild)
            {
                symbolChildrenList.Add(symbolChild);
                return;
            }

            var idJson = childJson[JsonKeys.Id];
            if (idJson is null)
            {
                Log.Error($"Symbol child {name} has no `{JsonKeys.Id}` entry in its json");
                return;
            }

            var childIdString = idJson.Value<string>();
            var hasChildId = Guid.TryParse(childIdString, out var childId) && childId != Guid.Empty;

            if (!hasChildId)
            {
                Log.Error($"Skipping child of undefined type {childIdString} in {name}");
                return;
            }
            
            missingSymbolChildIds.Add(childId);
        }
        
        // returns an id with valid C# namespace characters ('_' instead of '-') and all-lowercase
        static string CreateGuidNamespaceString(Guid guid)
        {
            var idSpan = guid.ToString().AsSpan();
            Span<char> namespaceIdSpan = stackalloc char[idSpan.Length];
            idSpan.ToLowerInvariant(namespaceIdSpan);

            int index;
            while ((index = namespaceIdSpan.IndexOf('-')) != -1)
            {
                namespaceIdSpan[index] = '_';
            }

            return new string(namespaceIdSpan);
        }

        // Method for when allowNonOpInstanceType = true
        static Type GetOperatorInstanceType(string typeName)
        {
            var thisType = Type.GetType(typeName);
                
            if (thisType is not null)
                return thisType;
                
            MessageBox.Show($"Definition '{typeName}' is missing in Operator.dll.\nPlease try to rebuild your solution.");
            Application.Exit();
            Application.ExitThread();

            return null;
        }
        
        // Method for when allowNonOpInstanceType = false
        static Type GetAnyInstanceType(string typeName) => Type.GetType(typeName) ?? typeof(object);
        
        private static void ObtainConnections(JArray connectionsJson, string name, HashSet<Guid> missingSymbolChildIds, List<Symbol.Connection> connections)
        {
            foreach (var c in connectionsJson)
            {
                var connection = ReadConnection(c);

                var undefinedType = missingSymbolChildIds.Contains(connection.TargetParentOrChildId)
                                    || missingSymbolChildIds.Contains(connection.SourceParentOrChildId);

                if (undefinedType)
                {
                    Log.Warning($"Skipping connection to child of undefined type in {name}");
                    continue;
                }

                connections.Add(connection);
            }
        }

        #endregion
        
        internal readonly struct JsonKeys
        {
            public const string Connections = "Connections";
            public const string SourceParentOrChildId = "SourceParentOrChildId";
            public const string SourceSlotId = "SourceSlotId";
            public const string TargetParentOrChildId = "TargetParentOrChildId";
            public const string TargetSlotId = "TargetSlotId";
            public const string Children = "Children";
            public const string Id = "Id";
            public const string SymbolId = "SymbolId";
            public const string Name = "Name";
            public const string Namespace = "Namespace";
            public const string InputValues = "InputValues";
            public const string OutputData = "OutputData";
            public const string Type = "Type";
            public const string DirtyFlagTrigger = "DirtyFlagTrigger";
            public const string IsDisabled = "IsDisabled";
            public const string DefaultValue = "DefaultValue";
            public const string Value = "Value";
            public const string Inputs = "Inputs";
            public const string Outputs = "Outputs";
            public const string Animator = "Animator";
        }

        public static JsonLoadSettings LoadSettings { get; } = new()
                                                                   {
                                                                       CommentHandling = CommentHandling.Ignore,
                                                                       DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                                                                       LineInfoHandling = LineInfoHandling.Ignore
                                                                   };
    }
}