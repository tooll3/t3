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
    public class UiJson : Json
    {
        public void WriteSymbolUi(SymbolUi symbolUi)
        {
            Writer.WriteStartObject();

            Writer.WriteValue("Id", symbolUi.Symbol.Id);
            Writer.WriteComment(symbolUi.Symbol.Name);

            WriteInputUis(symbolUi);
            WriteChildUis(symbolUi);
            WriteOutputUis(symbolUi);

            Writer.WriteEndObject();
        }

        public void WriteInputUis(SymbolUi symbolUi)
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            Writer.WritePropertyName("InputUis");
            Writer.WriteStartArray();
            foreach (var inputEntry in symbolUi.InputUis.OrderBy(i => i.Key))
            {
                var symbolInput = symbolUi.Symbol.InputDefinitions.SingleOrDefault(inputDef => inputDef.Id == inputEntry.Key);
                if (symbolInput == null)
                {
                    Log.Info($"In '{symbolUi.Symbol.Name}': Didn't found input definition for InputUi, skipping this one. This can happen if an input got removed.");
                    continue;
                }

                Writer.WriteStartObject(); // input entry
                Writer.WriteValue("InputId", inputEntry.Key);
                Writer.WriteComment(symbolInput.Name);
                var inputUi = inputEntry.Value;
                Writer.WriteValue("Type", $"{inputUi.Type}, {inputUi.Type.Assembly.GetName().Name}");
                Writer.WritePropertyName("Position");
                vec2Writer(Writer, inputUi.PosOnCanvas);

                //jsonTextWriter.WriteValue("Size", inputUi.Size); //todo: check if needed
                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }

        public void WriteChildUis(SymbolUi symbolUi)
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            Writer.WritePropertyName("SymbolChildUis");
            Writer.WriteStartArray();

            foreach (var childUi in symbolUi.ChildUis)
            {
                Writer.WriteStartObject(); // child entry
                Writer.WriteValue("ChildId", childUi.Id);
                Writer.WriteComment(childUi.SymbolChild.ReadableName);
                Writer.WritePropertyName("Position");
                vec2Writer(Writer, childUi.PosOnCanvas);

                //Writer.WriteValue("Size", childUi.Size); //todo: check if needed
                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }

        public void WriteOutputUis(SymbolUi symbolUi)
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            Writer.WritePropertyName("OutputUis");
            Writer.WriteStartArray();

            foreach (var outputEntry in symbolUi.OutputUis.OrderBy(i => i.Key))
            {
                Writer.WriteStartObject(); // output entry
                Writer.WriteValue("OutputId", outputEntry.Key);
                var outputName = symbolUi.Symbol.OutputDefinitions.Single(outputDef => outputDef.Id == outputEntry.Key).Name;
                Writer.WriteComment(outputName);
                var outputUi = outputEntry.Value;
                Writer.WriteValue("Type", outputUi.Type + $", {outputUi.Type.Assembly.GetName().Name}");
                Writer.WritePropertyName("Position");
                vec2Writer(Writer, outputUi.PosOnCanvas);

                //Writer.WriteValue("Size", outputUi.Size); //todo: check if needed
                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }


        public SymbolUi ReadSymbolUi(string filePath)
        {
            var vector2Converter = JsonToTypeValueConverters.Entries[typeof(Vector2)];

            using (var streamReader = new StreamReader(filePath))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var mainObject = (JObject)JToken.ReadFrom(jsonTextReader);
                var symbolId = Guid.Parse(mainObject["Id"].Value<string>());

                var inputDict = new Dictionary<Guid, IInputUi>();
                foreach (var uiInputEntry in (JArray)mainObject["InputUis"])
                {
                    var inputId = Guid.Parse(uiInputEntry["InputId"].Value<string>());
                    var typeName = uiInputEntry["Type"].Value<string>();
                    Type type = Type.GetType(typeName);
                    if (type == null)
                    {
                        Console.WriteLine($"type not available: {typeName}");
                    }
                    else if (InputUiFactory.Entries.TryGetValue(type, out var inputCreator))
                    {
                        var inputUi = inputCreator(inputId);
                        JToken positionToken = uiInputEntry["Position"];
                        inputUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);
                        //JToken sizeToken = uiInputEntry["Size"];
                        //inputUi.Size = (Vector2)vector2Converter(sizeString);

                        inputDict.Add(inputId, inputUi);
                    }
                    else
                    {
                        Log.Error($"Error creating input ui for non registered type '{typeName}'.");
                    }
                }

                var symbol = SymbolRegistry.Entries[symbolId];
                var symbolChildUis = new List<SymbolChildUi>();
                foreach (var childEntry in (JArray)mainObject["SymbolChildUis"])
                {
                    var childUi = new SymbolChildUi();
                    var childId = Guid.Parse(childEntry["ChildId"].Value<string>());
                    childUi.SymbolChild = symbol.Children.Single(child => child.Id == childId);

                    JToken positionToken = childEntry["Position"];
                    childUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);

                    //JToken sizeToken = uiInputEntry["Size"];
                    //inputUi.Size = (Vector2)vector2Converter(sizeString);

                    symbolChildUis.Add(childUi);
                }

                var outputDict = new Dictionary<Guid, IOutputUi>();
                foreach (var uiOutputEntry in (JArray)mainObject["OutputUis"])
                {
                    var outputId = Guid.Parse(uiOutputEntry["OutputId"].Value<string>());
                    var typeName = uiOutputEntry["Type"].Value<string>();
                    Type type = Type.GetType(typeName);
                    if (type == null)
                    {
                        Console.WriteLine($"type not available: {typeName}");
                    }
                    else if (OutputUiFactory.Entries.TryGetValue(type, out var outputCreator))
                    {
                        var outputUi = outputCreator(outputId);
                        JToken positionToken = uiOutputEntry["Position"];
                        outputUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);
                        //JToken sizeToken = uiOutputEntry["Size"];
                        //outputUi.Size = (Vector2)vector2Converter(sizeString);

                        outputDict.Add(outputId, outputUi);
                    }
                    else
                    {
                        Log.Error($"Error creating output ui for non registered type '{typeName}'.");
                    }
                }

                return new SymbolUi(symbol, symbolChildUis, inputDict, outputDict);
            }

        }
    }
}