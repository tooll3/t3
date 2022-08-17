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
using T3.Gui.Graph;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;
using Truncon.Collections;

namespace T3.Gui
{
    public class SymbolUiJson : SymbolJson
    {
        public void WriteSymbolUi(SymbolUi symbolUi)
        {
            Writer.WriteStartObject();

            Writer.WriteObject("Id", symbolUi.Symbol.Id);
            Writer.WriteComment(symbolUi.Symbol.Name);

            Writer.WriteObject("Description", symbolUi.Description);
            
            WriteInputUis(symbolUi);
            WriteChildUis(symbolUi);
            WriteOutputUis(symbolUi);
            WriteAnnotations(symbolUi);

            Writer.WriteEndObject();
        }

        public void WriteInputUis(SymbolUi symbolUi)
        {
            Writer.WritePropertyName("InputUis");
            Writer.WriteStartArray();

            foreach (var inputEntry in symbolUi.InputUis)
            {
                var symbolInput = symbolUi.Symbol.InputDefinitions.SingleOrDefault(inputDef => inputDef.Id == inputEntry.Key);
                if (symbolInput == null)
                {
                    Log.Info($"In '{symbolUi.Symbol.Name}': Didn't found input definition for InputUi, skipping this one. This can happen if an input got removed.");
                    continue;
                }

                Writer.WriteStartObject(); // input entry
                Writer.WriteObject("InputId", inputEntry.Key);
                Writer.WriteComment(symbolInput.Name);
                var inputUi = inputEntry.Value;
                inputUi.Write(Writer);
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
                Writer.WriteObject("ChildId", childUi.Id);
                {
                    Writer.WriteComment(childUi.SymbolChild.ReadableName);

                    if (childUi.Style != SymbolChildUi.Styles.Default)
                        Writer.WriteObject("Style", childUi.Style);

                    if (childUi.Size != SymbolChildUi.DefaultOpSize)
                    {
                        Writer.WritePropertyName("Size");
                        vec2Writer(Writer, childUi.Size);
                    }

                    Writer.WritePropertyName("Position");
                    vec2Writer(Writer, childUi.PosOnCanvas);

                    if (childUi.ConnectionStyleOverrides.Count > 0)
                    {
                        Writer.WritePropertyName("ConnectionStyleOverrides");
                        Writer.WriteStartArray();
                        foreach (var (key, value) in childUi.ConnectionStyleOverrides)
                        {
                            Writer.WriteStartObject();
                            Writer.WriteObject("Id", key);
                            Writer.WriteObject("Style", value);
                            Writer.WriteEndObject();
                        }
                        Writer.WriteEndArray();
                    }
                }
                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }

        public void WriteOutputUis(SymbolUi symbolUi)
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            Writer.WritePropertyName("OutputUis");
            Writer.WriteStartArray();

            foreach (var outputEntry in symbolUi.OutputUis)
            {
                Writer.WriteStartObject(); // output entry
                Writer.WriteObject("OutputId", outputEntry.Key);
                var outputName = symbolUi.Symbol.OutputDefinitions.Single(outputDef => outputDef.Id == outputEntry.Key).Name;
                Writer.WriteComment(outputName);
                var outputUi = outputEntry.Value;
                Writer.WritePropertyName("Position");
                vec2Writer(Writer, outputUi.PosOnCanvas);

                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }
        
        
        
        public void WriteAnnotations(SymbolUi symbolUi)
        {
            if (symbolUi.Annotations.Count == 0)
                return;
            
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];
            var vec4Writer = TypeValueToJsonConverters.Entries[typeof(Vector4)];
            Writer.WritePropertyName("Annotations");
            Writer.WriteStartArray();

            foreach (var annotation in symbolUi.Annotations.Values)
            {
                Writer.WriteStartObject(); 
                Writer.WriteObject("Id", annotation.Id);
                Writer.WriteObject("Title", annotation.Title);
                
                Writer.WritePropertyName("Color");
                vec4Writer(Writer, annotation.Color.Rgba);
                
                Writer.WritePropertyName("Position");
                vec2Writer(Writer, annotation.PosOnCanvas);
                
                Writer.WritePropertyName("Size");
                vec2Writer(Writer, annotation.Size);
                Writer.WriteEndObject();
            }

            Writer.WriteEndArray();
        }        
        
        public SymbolUi ReadSymbolUi(string filePath)
        {
            using (var streamReader = new StreamReader(filePath))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return ReadSymbolUi(jsonTextReader);
            }
        }

        public static SymbolUi ReadSymbolUi(JsonTextReader jsonTextReader)
        {
            try
            {
                var mainObject = JToken.ReadFrom(jsonTextReader);
                return ReadSymbolUi(mainObject);
            }
            catch (System.TypeInitializationException e)
            {
                Log.Error("Failed to initialize type from json: " + e + "\nLine-Number" + jsonTextReader.LineNumber);
            }
            catch (Exception e)
            {
                Log.Error("Can't read json " + e);
            }

            return null;
        }

        public static SymbolUi ReadSymbolUi(JToken mainObject)
        {
            var vector2Converter = JsonToTypeValueConverters.Entries[typeof(Vector2)];
            var vector4Converter = JsonToTypeValueConverters.Entries[typeof(Vector4)];
            
            var symbolId = Guid.Parse(mainObject["Id"].Value<string>());
            var symbol = SymbolRegistry.Entries[symbolId];
            

            var inputDict = new OrderedDictionary<Guid, IInputUi>();
            foreach (JToken uiInputEntry in (JArray)mainObject["InputUis"])
            {
                var inputId = Guid.Parse(uiInputEntry["InputId"].Value<string>());
                var inputDefinition = symbol.InputDefinitions.SingleOrDefault(def => def.Id == inputId);
                if (inputDefinition == null)
                {
                    Log.Warning($"Found input entry in ui file for symbol '{symbol.Name}', but no corresponding input in symbol. Assuming that the input was removed and ignoring the ui information.");
                    continue;
                }

                var type = inputDefinition.DefaultValue.ValueType;
                if (InputUiFactory.Entries.TryGetValue(type, out var inputCreator))
                {
                    // get the symbol input definition
                    var inputUi = inputCreator();
                    inputUi.InputDefinition = inputDefinition;
                    inputUi.Read(uiInputEntry);
                    inputDict.Add(inputId, inputUi);
                }
                else
                {
                    Log.Error($"Error creating input ui for non registered type '{type.Name}'.");
                }
            }

            var symbolChildUis = new List<SymbolChildUi>();
            foreach (var childEntry in (JArray)mainObject["SymbolChildUis"])
            {
                var childUi = new SymbolChildUi();
                var childId = Guid.Parse(childEntry["ChildId"].Value<string>());
                childUi.SymbolChild = symbol.Children.SingleOrDefault(child => child.Id == childId);
                if (childUi.SymbolChild == null)
                {
                    Log.Warning($"Skipping UI child definition in {symbol.Name} {symbolId} for undefined child {childId}");
                    continue;
                }

                JToken positionToken = childEntry["Position"];
                childUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);

                if (childEntry["Size"] != null)
                {
                    JToken sizeToken = childEntry["Size"];
                    childUi.Size = (Vector2)vector2Converter(sizeToken);
                }

                if (childEntry["Style"] != null)
                {
                    childUi.Style = (SymbolChildUi.Styles)Enum.Parse(typeof(SymbolChildUi.Styles), childEntry["Style"].Value<string>());
                }
                else
                {
                    childUi.Style = SymbolChildUi.Styles.Default;
                }

                var conStyleEntry = childEntry["ConnectionStyleOverrides"];
                if (conStyleEntry != null)
                {
                    var dict = childUi.ConnectionStyleOverrides;
                    foreach (var styleEntry in (JArray)conStyleEntry)
                    {
                        Guid id = Guid.Parse(styleEntry["Id"].Value<string>());
                        var style = (SymbolChildUi.ConnectionStyles)Enum.Parse(typeof(SymbolChildUi.ConnectionStyles), styleEntry["Style"].Value<string>());
                        dict.Add(id, style);
                    }
                }

                symbolChildUis.Add(childUi);
            }

            var outputDict = new OrderedDictionary<Guid, IOutputUi>();
            foreach (var uiOutputEntry in (JArray)mainObject["OutputUis"])
            {
                var outputId = Guid.Parse(uiOutputEntry["OutputId"].Value<string>());
                var outputDefinition = symbol.OutputDefinitions.SingleOrDefault(def => def.Id == outputId);
                if (outputDefinition == null)
                {
                    Log.Warning($"Found output entry in ui file for symbol '{symbol.Name}', but no corresponding output in symbol. Assuming that the output was removed and ignoring the ui information.");
                    continue;
                }

                var type = outputDefinition.ValueType;
                if (OutputUiFactory.Entries.TryGetValue(type, out var outputCreator))
                {
                    var outputUi = outputCreator();
                    outputUi.OutputDefinition = symbol.OutputDefinitions.Single(def => def.Id == outputId);

                    JToken positionToken = uiOutputEntry["Position"];
                    outputUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);

                    outputDict.Add(outputId, outputUi);
                }
                else
                {
                    Log.Error($"Error creating output ui for non registered type '{type.Name}'.");
                }
            }
            
            var annotationDict = new OrderedDictionary<Guid, Annotation>();
            var annotationsArray = (JArray)mainObject["Annotations"];
            if (annotationsArray != null)
            {
                foreach (var annotationEntry in annotationsArray)
                {
                    var annotation = new Annotation();
                    
                    annotation.Id = Guid.Parse(annotationEntry["Id"].Value<string>());
                    annotation.Title = annotationEntry["Title"].Value<string>();
                    annotation.PosOnCanvas = (Vector2)vector2Converter(annotationEntry["Position"]);
                    if (annotationEntry["Color"] != null)
                    {
                        annotation.Color =  new Color((Vector4)vector4Converter(annotationEntry["Color"]));
                    } 
                    
                    annotation.Size = (Vector2)vector2Converter(annotationEntry["Size"]);
                    annotationDict[annotation.Id] = annotation;
                }
            }
            

            var newSymbolUi = new SymbolUi(symbol, symbolChildUis, inputDict, outputDict, annotationDict);
            newSymbolUi.Description = mainObject["Description"]?.Value<string>();
            return newSymbolUi;
        }
    }
}