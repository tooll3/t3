using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.DataTypes.Vector;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.External.Truncon.Collections;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Serialization;

// ReSharper disable AssignNullToNotNullAttribute

namespace T3.Editor.UiModel
{
    public static class SymbolUiJson
    {

        
        public static void WriteSymbolUi(SymbolUi symbolUi, JsonTextWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteValue(JsonKeys.Id, symbolUi.Symbol.Id);
            writer.WriteComment(symbolUi.Symbol.Name);

            writer.WriteObject(JsonKeys.Description, symbolUi.Description);

            WriteInputUis(symbolUi, writer);
            WriteChildUis(symbolUi, writer);
            WriteOutputUis(symbolUi, writer);
            WriteAnnotations(symbolUi, writer);
            WriteLinks(symbolUi, writer);

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
            writer.WritePropertyName(JsonKeys.SymbolChildUis);
            writer.WriteStartArray();

            foreach (var childUi in symbolUi.ChildUis.Values)
            {
                writer.WriteStartObject(); // child entry
                writer.WriteObject(JsonKeys.ChildId, childUi.Id);
                {
                    writer.WriteComment(childUi.SymbolChild.ReadableName);

                    if (childUi.Style != SymbolUi.Child.Styles.Default)
                    {
                        writer.WriteObject(JsonKeys.Style, childUi.Style);
                        if (childUi.Size != SymbolUi.Child.DefaultOpSize)
                        {
                            writer.WritePropertyName(JsonKeys.Size);
                            _vector2ToJson(writer, childUi.Size);
                        }
                    }

                    if (!string.IsNullOrEmpty(childUi.Comment))
                    {
                        writer.WriteObject(JsonKeys.Comment, childUi.Comment);
                    }
                    
                    writer.WritePropertyName(JsonKeys.Position);
                    _vector2ToJson(writer, childUi.PosOnCanvas);

                    if(childUi.SnapshotGroupIndex > 0)
                        writer.WriteObject(nameof(SymbolUi.Child.SnapshotGroupIndex), childUi.SnapshotGroupIndex);

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
                _vector2ToJson(writer, outputUi.PosOnCanvas);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteAnnotations(SymbolUi symbolUi, JsonTextWriter writer)
        {
            if (symbolUi.Annotations.Count == 0)
                return;
            writer.WritePropertyName(JsonKeys.Annotations);
            writer.WriteStartArray();

            foreach (var annotation in symbolUi.Annotations.Values)
            {
                writer.WriteStartObject();
                writer.WriteObject(JsonKeys.Id, annotation.Id);
                writer.WriteObject(JsonKeys.Title, annotation.Title);

                writer.WritePropertyName(JsonKeys.Color);
                _vector4ToJson(writer, annotation.Color.Rgba);

                writer.WritePropertyName(JsonKeys.Position);
                _vector2ToJson(writer, annotation.PosOnCanvas);

                writer.WritePropertyName(JsonKeys.Size);
                _vector2ToJson(writer, annotation.Size);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
        
        private static void WriteLinks(SymbolUi symbolUi, JsonTextWriter writer)
        {
            if (symbolUi.Links.Count == 0)
                return;
            
            writer.WritePropertyName(JsonKeys.Links);
            writer.WriteStartArray();

            foreach (var link in symbolUi.Links.Values)
            {
                writer.WriteStartObject();
                writer.WriteObject(JsonKeys.Id, link.Id);
                writer.WriteObject(JsonKeys.Title, link.Title);
                writer.WriteObject(JsonKeys.Description, link.Description);
                writer.WriteObject(JsonKeys.LinkUrl, link.Url);
                writer.WriteObject(JsonKeys.LinkType, link.Type);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
        
        
        internal static bool TryReadSymbolUiExternal(JToken mainObject, Symbol symbol, out SymbolUi symbolUi)
        {
            symbolUi = null;
            var guidString = mainObject[JsonKeys.Id].Value<string>();
            var hasGuid = Guid.TryParse(guidString, out var symbolId);

            if (!hasGuid)
            {
                Log.Warning($"Error parsing guid {guidString}");
                return false;
            }

            var success = TryReadSymbolUi(mainObject, symbol, out symbolUi);
            if(success)
                symbolUi.UpdateConsistencyWithSymbol();

            return success;
        }


        internal static bool TryReadSymbolUi(JToken mainObject, Symbol symbol, out SymbolUi symbolUi)
        {
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


            var outputDict = new OrderedDictionary<Guid, IOutputUi>();
            foreach (var uiOutputEntry in (JArray)mainObject[JsonKeys.OutputUis])
            {
                var outputIdString = uiOutputEntry[JsonKeys.OutputId].Value<string>();
                var hasOutputId = Guid.TryParse(outputIdString, out var outputId);

                if (!hasOutputId)
                {
                    Log.Warning($"Skipping UI output in {symbol.Name} {symbol.Id} for invalid output id `{outputIdString}`");
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

                    var positionToken = uiOutputEntry[JsonKeys.Position];
                    outputUi.PosOnCanvas = (Vector2)_jsonToVector2(positionToken);

                    outputDict.Add(outputId, outputUi);
                }
                else
                {
                    Log.Error($"Error creating output ui for non registered type '{type.Name}'.");
                }
            }

            var annotationDict = ReadAnnotations(mainObject);
            var linksDict = ReadLinks(mainObject);


            var symbolChildUiJson = (JArray)mainObject[JsonKeys.SymbolChildUis];
            symbolUi = new SymbolUi(symbol: symbol, 
                                    childUis: parent => CreateSymbolUiChildren(parent, symbolChildUiJson), 
                                    inputs: inputDict, 
                                    outputs: outputDict, 
                                    annotations: annotationDict, 
                                    links: linksDict, 
                                    updateConsistency: false);
            
            var descriptionEntry = mainObject[JsonKeys.Description];
            if(descriptionEntry?.Value<string>() != null)
                symbolUi.Description = descriptionEntry.Value<string>();

            return true;
        }

        private static List<SymbolUi.Child> CreateSymbolUiChildren(SymbolUi parent, IEnumerable<JToken> childJsons)
        {
            var symbolChildUis = new List<SymbolUi.Child>();
            var symbol = parent.Symbol;
            var symbolId = symbol.Id;
            foreach (var childEntry in childJsons)
            {
                var childIdString = childEntry[JsonKeys.ChildId].Value<string>();
                var hasChildId = Guid.TryParse(childIdString, out var childId);

                if (!hasChildId)
                {
                    Log.Warning($"Skipping UI child definition in {symbol.Name} {symbolId} for invalid child id `{childIdString}`");
                    continue;
                }
                
                if (!symbol.Children.TryGetValue(childId, out var symbolChild))
                {
                    Log.Warning($"Skipping UI child definition in {symbol.Name} {symbolId} for undefined child {childId}");
                    continue;
                }
                
                var childUi = new SymbolUi.Child(symbolChild, parent);
                
                if (childEntry[JsonKeys.Comment] != null)
                {
                    childUi.Comment = childEntry[JsonKeys.Comment].Value<string>();
                }

                var positionToken = childEntry[JsonKeys.Position];
                childUi.PosOnCanvas = (Vector2)_jsonToVector2(positionToken);

                if (childEntry[JsonKeys.Size] != null)
                {
                    var sizeToken = childEntry[JsonKeys.Size];
                    childUi.Size = (Vector2)_jsonToVector2(sizeToken);
                }
                
                if (childEntry[nameof(SymbolUi.Child.SnapshotGroupIndex)] != null)
                {
                    childUi.SnapshotGroupIndex = childEntry[nameof(SymbolUi.Child.SnapshotGroupIndex)].Value<int>();
                }

                var childStyleEntry = childEntry[JsonKeys.Style];
                if (childStyleEntry != null)
                {
                    childUi.Style = (SymbolUi.Child.Styles)Enum.Parse(typeof(SymbolUi.Child.Styles), childStyleEntry.Value<string>());
                }
                else
                {
                    childUi.Style = SymbolUi.Child.Styles.Default;
                }

                var conStyleEntry = childEntry[JsonKeys.ConnectionStyleOverrides];
                if (conStyleEntry != null)
                {
                    var dict = childUi.ConnectionStyleOverrides;
                    foreach (var styleEntry in (JArray)conStyleEntry)
                    {
                        var id = Guid.Parse(styleEntry[JsonKeys.Id].Value<string>());
                        var style = (SymbolUi.Child.ConnectionStyles)Enum.Parse(typeof(SymbolUi.Child.ConnectionStyles), styleEntry[JsonKeys.Style].Value<string>());
                        dict.Add(id, style);
                    }
                }

                symbolChildUis.Add(childUi);
            }

            return symbolChildUis;
        }

        private static OrderedDictionary<Guid, Annotation> ReadAnnotations(JToken token)
        {
            var annotationDict = new OrderedDictionary<Guid, Annotation>();
            var annotationsArray = (JArray)token[JsonKeys.Annotations];
            if (annotationsArray == null)
                return annotationDict;
            
            foreach (var annotationEntry in annotationsArray)
            {
                var annotation = new Annotation
                                     {
                                         Id = Guid.Parse(annotationEntry[JsonKeys.Id].Value<string>()),
                                         Title = annotationEntry[JsonKeys.Title].Value<string>(),
                                         PosOnCanvas = (Vector2)_jsonToVector2(annotationEntry[JsonKeys.Position])
                                     };

                var colorEntry = annotationEntry[JsonKeys.Color];
                if (colorEntry != null)
                {
                    annotation.Color = new Color((Vector4)_jsonToVector4(colorEntry));
                }

                annotation.Size = (Vector2)_jsonToVector2(annotationEntry[JsonKeys.Size]);
                annotationDict[annotation.Id] = annotation;
            }

            return annotationDict;
        }
        
        private static OrderedDictionary<Guid, ExternalLink> ReadLinks(JToken token)
        {
            var linkDict = new OrderedDictionary<Guid, ExternalLink>();
            var linksArray = (JArray)token[JsonKeys.Links];
            if (linksArray == null)
                return linkDict;
            
            foreach (var linkEntry in linksArray)
            {
                if (!Enum.TryParse<ExternalLink.LinkTypes>(linkEntry[JsonKeys.LinkType]?.Value<string>(), out var type))
                    type = ExternalLink.LinkTypes.Other;
                
                var link = new ExternalLink
                               {
                                   Id = Guid.Parse(linkEntry[JsonKeys.Id].Value<string>()),
                                   Title = linkEntry[JsonKeys.Title]?.Value<string>(),
                                   Description = linkEntry[JsonKeys.Description]?.Value<string>(),
                                   Url = linkEntry[JsonKeys.LinkUrl]?.Value<string>(),
                                   Type =  type,
                               };

                linkDict[link.Id] = link;
            }
            return linkDict;
        }
        

        private readonly struct JsonKeys
        {
            public const string InputUis = nameof(InputUis);
            public const string InputId = nameof(InputId);
            public const string OutputUis = nameof(OutputUis);
            public const string OutputId = nameof(OutputId);
            public const string SymbolChildUis = nameof(SymbolChildUis);
            public const string ChildId = nameof(ChildId);
            public const string Position = nameof(Position);
            public const string Annotations = nameof(Annotations);
            public const string Links = nameof(Links);
            public const string Comment = nameof(Comment);
            public const string Id = nameof(Id);
            public const string Title = nameof(Title);
            public const string Color = nameof(Color);
            public const string Size = nameof(Size);
            public const string Description = nameof(Description);
            public const string Style = nameof(Style);
            public const string ConnectionStyleOverrides = nameof(ConnectionStyleOverrides);
            public const string LinkType = nameof(LinkType);
            public const string LinkUrl = nameof(LinkUrl);
        }
        
        private static readonly Func<JToken, object> _jsonToVector2 = JsonToTypeValueConverters.Entries[typeof(Vector2)];
        private static readonly Func<JToken, object> _jsonToVector4 = JsonToTypeValueConverters.Entries[typeof(Vector4)];
        private static readonly Action<JsonTextWriter, object> _vector2ToJson = TypeValueToJsonConverters.Entries[typeof(Vector2)];
        private static readonly Action<JsonTextWriter, object> _vector4ToJson = TypeValueToJsonConverters.Entries[typeof(Vector4)];
    }
}