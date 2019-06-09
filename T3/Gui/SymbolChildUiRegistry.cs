using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui
{
    public static class SymbolChildUiRegistry
    {
        // symbol id -> (symbol child id -> symbol child ui entry)
        public static Dictionary<Guid, Dictionary<Guid, SymbolChildUi>> Entries { get; set; } = new Dictionary<Guid, Dictionary<Guid, SymbolChildUi>>();

        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                Log.Error($"Couldn't open File '{FilePath} for loading the symbol child ui infos.");
                return;
            }

            var vector2Converter = JsonToTypeValueConverters.Entries[typeof(Vector2)];

            using (var streamReader = new StreamReader(FilePath))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var mainObject = (JObject)JToken.ReadFrom(jsonTextReader);
                var entries = (JArray)mainObject["Entries"];
                foreach (var symbolEntry in entries)
                {
                    var symbolId = Guid.Parse(symbolEntry["SymbolId"].Value<string>());
                    var symbolChildDict = new Dictionary<Guid, SymbolChildUi>();
                    foreach (var childEntry in (JArray)symbolEntry["SymbolChildUis"])
                    {
                        var childUi = new SymbolChildUi();
                        var childId = Guid.Parse(childEntry["ChildId"].Value<string>());
                        var symbol = SymbolRegistry.Entries[symbolId];
                        childUi.SymbolChild = symbol.Children.Single(child => child.Id == childId);

                        JToken positionToken = childEntry["Position"];
                        childUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);

                        //JToken sizeToken = uiInputEntry["Size"];
                        //inputUi.Size = (Vector2)vector2Converter(sizeString);

                        symbolChildDict.Add(childId, childUi);
                    }
                    Entries.Add(symbolId, symbolChildDict);
                }
            }
        }


        public static void Save()
        {
            //todo: code here is nearly the same as in OutputUiRegistry.Save() 
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            using (var streamWriter = new StreamWriter(FilePath))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                jsonTextWriter.Formatting = Formatting.Indented;
                jsonTextWriter.WriteStartObject(); // root object 
                jsonTextWriter.WritePropertyName("Entries");
                jsonTextWriter.WriteStartArray();

                foreach (var entry in Entries.OrderBy(i => i.Key))
                {
                    var symbol = SymbolRegistry.Entries[entry.Key];
                    jsonTextWriter.WriteStartObject(); // symbol entry
                    jsonTextWriter.WriteValue("SymbolId", entry.Key);
                    jsonTextWriter.WriteComment(symbol.Name);
                    jsonTextWriter.WritePropertyName("SymbolChildUis");
                    jsonTextWriter.WriteStartArray();

                    foreach (var childEntry in entry.Value.OrderBy(i => i.Key))
                    {
                        jsonTextWriter.WriteStartObject(); // child entry
                        jsonTextWriter.WriteValue("ChildId", childEntry.Key);
                        var childUi = childEntry.Value;
                        var childName = symbol.Children.Single(child => child.Id == childEntry.Key).ReadableName;
                        jsonTextWriter.WriteComment(childName);
                        jsonTextWriter.WritePropertyName("Position");
                        vec2Writer(jsonTextWriter, childUi.PosOnCanvas);

                        //jsonTextWriter.WriteValue("Size", childUi.Size); //todo: check if needed
                        jsonTextWriter.WriteEndObject();
                    }

                    jsonTextWriter.WriteEndArray();
                    jsonTextWriter.WriteEndObject();
                }

                jsonTextWriter.WriteEndArray();
                jsonTextWriter.WriteEndObject();
            }
        }

        private static string FilePath = "SymbolChildUiRegistry.json";


    }
}