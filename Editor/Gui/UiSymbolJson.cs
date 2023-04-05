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
using T3.Core.Resource;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using Truncon.Collections;

// ReSharper disable AssignNullToNotNullAttribute

namespace T3.Editor.Gui
{
    public static class SymbolUiJson
    {
        public static void WriteSymbolUi(SymbolUi symbolUi, JsonTextWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteObject(JsonKeys.Id, symbolUi.Symbol.Id);
            writer.WriteComment(symbolUi.Symbol.Name);

            writer.WriteObject(JsonKeys.Description, symbolUi.Description);

            WriteInputUis(symbolUi, writer);
            WriteChildUis(symbolUi, writer);
            WriteOutputUis(symbolUi, writer);
            WriteAnnotations(symbolUi, writer);

            writer.WriteEndObject();
        }

        private static void WriteInputUis(SymbolUi symbolUi, JsonTextWriter writer)
        {
            writer.WritePropertyName(JsonKeys.InputUis);
            writer.WriteStartArray();

            foreach (var inputEntry in symbolUi.InputUis)
            {
                var symbolInput = symbolUi.Symbol.InputDefinitions.SingleOrDefault(inputDef => inputDef.Id == inputEntry.Key);
                if (symbolInput == null)
                {
                    Log.Info($"In '{symbolUi.Symbol.Name}': Didn't found input definition for InputUi, skipping this one. This can happen if an input got removed.");
                    continue;
                }

                writer.WriteStartObject(); // input entry
                writer.WriteObject(JsonKeys.InputId, inputEntry.Key);
                writer.WriteComment(symbolInput.Name);
                var inputUi = inputEntry.Value;
                inputUi.Write(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteChildUis(SymbolUi symbolUi, JsonTextWriter writer)
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            writer.WritePropertyName(JsonKeys.SymbolChildUis);
            writer.WriteStartArray();

            foreach (var childUi in symbolUi.ChildUis)
            {
                writer.WriteStartObject(); // child entry
                writer.WriteObject(JsonKeys.ChildId, childUi.Id);
                {
                    writer.WriteComment(childUi.SymbolChild.ReadableName);

                    if (childUi.Style != SymbolChildUi.Styles.Default)
                        writer.WriteObject(JsonKeys.Style, childUi.Style);

                    if (childUi.Size != SymbolChildUi.DefaultOpSize)
                    {
                        writer.WritePropertyName(JsonKeys.Size);
                        vec2Writer(writer, childUi.Size);
                    }

                    writer.WritePropertyName(JsonKeys.Position);
                    vec2Writer(writer, childUi.PosOnCanvas);

                    if (childUi.ConnectionStyleOverrides.Count > 0)
                    {
                        writer.WritePropertyName(JsonKeys.ConnectionStyleOverrides);
                        writer.WriteStartArray();
                        foreach (var (key, value) in childUi.ConnectionStyleOverrides)
                        {
                            writer.WriteStartObject();
                            writer.WriteObject(JsonKeys.Id, key);
                            writer.WriteObject(JsonKeys.Style, value);
                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                    }
                }
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteOutputUis(SymbolUi symbolUi, JsonTextWriter writer)
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];

            writer.WritePropertyName(JsonKeys.OutputUis);
            writer.WriteStartArray();

            foreach (var outputEntry in symbolUi.OutputUis)
            {
                writer.WriteStartObject(); // output entry
                writer.WriteObject(JsonKeys.OutputId, outputEntry.Key);
                var outputName = symbolUi.Symbol.OutputDefinitions.Single(outputDef => outputDef.Id == outputEntry.Key).Name;
                writer.WriteComment(outputName);
                var outputUi = outputEntry.Value;
                writer.WritePropertyName(JsonKeys.Position);
                vec2Writer(writer, outputUi.PosOnCanvas);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteAnnotations(SymbolUi symbolUi, JsonTextWriter writer)
        {
            if (symbolUi.Annotations.Count == 0)
                return;

            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];
            var vec4Writer = TypeValueToJsonConverters.Entries[typeof(Vector4)];
            writer.WritePropertyName(JsonKeys.Annotations);
            writer.WriteStartArray();

            foreach (var annotation in symbolUi.Annotations.Values)
            {
                writer.WriteStartObject();
                writer.WriteObject(JsonKeys.Id, annotation.Id);
                writer.WriteObject(JsonKeys.Title, annotation.Title);

                writer.WritePropertyName(JsonKeys.Color);
                vec4Writer(writer, annotation.Color.Rgba);

                writer.WritePropertyName(JsonKeys.Position);
                vec2Writer(writer, annotation.PosOnCanvas);

                writer.WritePropertyName(JsonKeys.Size);
                vec2Writer(writer, annotation.Size);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
        
        internal static bool TryReadSymbolUi(JToken mainObject, out SymbolUi symbolUi)
        {
            symbolUi = null;
            var guidString = mainObject[JsonKeys.Id].Value<string>();
            var hasGuid = Guid.TryParse(guidString, out var symbolId);

            if (!hasGuid)
            {
                Log.Warning($"Error parsing guid {guidString}");
                return false;
            }

            return TryReadSymbolUi(mainObject, symbolId, out symbolUi);
        }

        internal static bool TryReadSymbolUi(JToken mainObject, Guid symbolId, out SymbolUi symbolUi)
        {
            var vector2Converter = JsonToTypeValueConverters.Entries[typeof(Vector2)];
            var vector4Converter = JsonToTypeValueConverters.Entries[typeof(Vector4)];

            var symbol = SymbolRegistry.Entries[symbolId];

            var inputDict = new OrderedDictionary<Guid, IInputUi>();
            foreach (JToken uiInputEntry in (JArray)mainObject[JsonKeys.InputUis])
            {
                Guid inputId;
                try
                {
                    inputId = Guid.Parse(uiInputEntry[JsonKeys.InputId].Value<string>() ?? string.Empty);
                }
                catch
                {
                    Log.Error("Skipping input with invalid symbolChildUi id");
                    symbolUi = null;
                    return false;
                }

                var inputDefinition = symbol.InputDefinitions.SingleOrDefault(def => def.Id == inputId);
                if (inputDefinition == null)
                {
                    Log.Warning($"Found input entry in ui file for symbol '{symbol.Name}', but no corresponding input in symbol. " +
                                $"Assuming that the input was removed and ignoring the ui information.");
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
            foreach (var childEntry in (JArray)mainObject[JsonKeys.SymbolChildUis])
            {
                var childUi = new SymbolChildUi();
                var childIdString = childEntry[JsonKeys.ChildId].Value<string>();
                var hasChildId = Guid.TryParse(childIdString, out var childId);

                if (!hasChildId)
                {
                    Log.Warning($"Skipping UI child definition in {symbol.Name} {symbolId} for invalid child id `{childIdString}`");
                    continue;
                }
                
                childUi.SymbolChild = symbol.Children.SingleOrDefault(child => child.Id == childId);
                if (childUi.SymbolChild == null)
                {
                    Log.Warning($"Skipping UI child definition in {symbol.Name} {symbolId} for undefined child {childId}");
                    continue;
                }

                JToken positionToken = childEntry[JsonKeys.Position];
                childUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);

                if (childEntry[JsonKeys.Size] != null)
                {
                    JToken sizeToken = childEntry[JsonKeys.Size];
                    childUi.Size = (Vector2)vector2Converter(sizeToken);
                }

                var childStyleEntry = childEntry[JsonKeys.Style];
                if (childStyleEntry != null)
                {
                    childUi.Style = (SymbolChildUi.Styles)Enum.Parse(typeof(SymbolChildUi.Styles), childStyleEntry.Value<string>());
                }
                else
                {
                    childUi.Style = SymbolChildUi.Styles.Default;
                }

                var conStyleEntry = childEntry[JsonKeys.ConnectionStyleOverrides];
                if (conStyleEntry != null)
                {
                    var dict = childUi.ConnectionStyleOverrides;
                    foreach (var styleEntry in (JArray)conStyleEntry)
                    {
                        var id = Guid.Parse(styleEntry[JsonKeys.Id].Value<string>());
                        var style = (SymbolChildUi.ConnectionStyles)Enum.Parse(typeof(SymbolChildUi.ConnectionStyles), styleEntry[JsonKeys.Style].Value<string>());
                        dict.Add(id, style);
                    }
                }

                symbolChildUis.Add(childUi);
            }

            var outputDict = new OrderedDictionary<Guid, IOutputUi>();
            foreach (var uiOutputEntry in (JArray)mainObject[JsonKeys.OutputUis])
            {
                var outputIdString = uiOutputEntry[JsonKeys.OutputId].Value<string>();
                var hasOutputId = Guid.TryParse(outputIdString, out var outputId);

                if (!hasOutputId)
                {
                    Log.Warning($"Skipping UI output in {symbol.Name} {symbolId} for invalid output id `{outputIdString}`");
                    continue;
                }
                
                var outputDefinition = symbol.OutputDefinitions.SingleOrDefault(def => def.Id == outputId);
                if (outputDefinition == null)
                {
                    Log.Warning($"Found output entry in ui file for symbol '{symbol.Name}', but no corresponding output in symbol. " +
                                $"Assuming that the output was removed and ignoring the ui information.");
                    continue;
                }

                var type = outputDefinition.ValueType;
                if (OutputUiFactory.Entries.TryGetValue(type, out var outputCreator))
                {
                    var outputUi = outputCreator();
                    outputUi.OutputDefinition = symbol.OutputDefinitions.First(def => def.Id == outputId);

                    JToken positionToken = uiOutputEntry[JsonKeys.Position];
                    outputUi.PosOnCanvas = (Vector2)vector2Converter(positionToken);

                    outputDict.Add(outputId, outputUi);
                }
                else
                {
                    Log.Error($"Error creating output ui for non registered type '{type.Name}'.");
                }
            }

            var annotationDict = new OrderedDictionary<Guid, Annotation>();
            var annotationsArray = (JArray)mainObject[JsonKeys.Annotations];
            if (annotationsArray != null)
            {
                foreach (var annotationEntry in annotationsArray)
                {
                    var annotation = new Annotation
                                         {
                                             Id = Guid.Parse(annotationEntry[JsonKeys.Id].Value<string>()),
                                             Title = annotationEntry[JsonKeys.Title].Value<string>(),
                                             PosOnCanvas = (Vector2)vector2Converter(annotationEntry[JsonKeys.Position])
                                         };

                    var colorEntry = annotationEntry[JsonKeys.Color];
                    if (colorEntry != null)
                    {
                        annotation.Color = new Color((Vector4)vector4Converter(colorEntry));
                    }

                    annotation.Size = (Vector2)vector2Converter(annotationEntry[JsonKeys.Size]);
                    annotationDict[annotation.Id] = annotation;
                }
            }

            symbolUi = new SymbolUi(symbol, symbolChildUis, inputDict, outputDict, annotationDict)
                                  {
                                      Description = mainObject[JsonKeys.Description]?.Value<string>()
                                  };
            return true;
        }

        private readonly struct JsonKeys
        {
            public const string InputUis = "InputUis";
            public const string InputId = "InputId";
            public const string OutputUis = "OutputUis";
            public const string OutputId = "OutputId";
            public const string SymbolChildUis = "SymbolChildUis";
            public const string ChildId = "ChildId";
            public const string Position = "Position";
            public const string Annotations = "Annotations";
            public const string Id = "Id";
            public const string Title = "Title";
            public const string Color = "Color";
            public const string Size = "Size";
            public const string Description = "Description";
            public const string Style = "Style";
            public const string ConnectionStyleOverrides = "ConnectionStyleOverrides";
        }
    }
}