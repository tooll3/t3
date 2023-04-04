using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Stats;
using Buffer = SharpDX.Direct3D11.Buffer;
using Point = T3.Core.DataTypes.Point;
using Vector4 = System.Numerics.Vector4;

// ReSharper disable RedundantNameQualifier

namespace T3.Core.Resource
{
    public static class JsonToTypeValueConverters
    {
        public static Dictionary<Type, Func<JToken, object>> Entries { get; } = new();
    }

    public static class TypeValueToJsonConverters
    {
        public static Dictionary<Type, Action<JsonTextWriter, object>> Entries { get; } = new();
    }

    public static class InputValueCreators
    {
        public static Dictionary<Type, Func<InputValue>> Entries { get; } = new();
    }

    public static class TypeNameRegistry
    {
        public static Dictionary<Type, string> Entries { get; } = new(20);
    }

    public class Model
    {
        
        public Assembly OperatorsAssembly { get; }

        public Model(Assembly operatorAssembly)
        {
            
            
            OperatorsAssembly = operatorAssembly;

            // generic enum value from json function, must be local function
            object JsonToEnumValue<T>(JToken jsonToken) where T : struct // todo: use 7.3 and replace with enum
            {
                var value = jsonToken.Value<string>();

                if (Enum.TryParse(value, out T enumValue))
                {
                    return enumValue;
                }

                return null;
            }

            InputValue InputDefaultValueCreator<T>() => new InputValue<T>();

            // build-in default types
            RegisterType(typeof(bool), "bool",
                         InputDefaultValueCreator<bool>,
                         (writer, obj) => writer.WriteValue((bool)obj),
                         jsonToken => jsonToken.Value<bool>());
            RegisterType(typeof(int), "int",
                         InputDefaultValueCreator<int>,
                         (writer, obj) => writer.WriteValue((int)obj),
                         jsonToken => jsonToken.Value<int>());
            RegisterType(typeof(float), "float",
                         InputDefaultValueCreator<float>,
                         (writer, obj) => writer.WriteValue((float)obj),
                         jsonToken => jsonToken.Value<float>());
            RegisterType(typeof(string), "string",
                         () => new InputValue<string>(string.Empty),
                         (writer, value) => writer.WriteValue((string)value),
                         jsonToken => jsonToken.Value<string>());

            // system types
            RegisterType(typeof(System.Collections.Generic.List<float>), "List<float>",
                         () => new InputValue<List<float>>(new List<float>()),
                         (writer, obj) =>
                         {
                             var list = (List<float>)obj;
                             writer.WriteStartObject();
                             writer.WritePropertyName("Values");
                             writer.WriteStartArray();
                             list.ForEach(writer.WriteValue);
                             writer.WriteEndArray();
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             var entries = jsonToken["Values"];
                             var list = new List<float>(entries.Count());
                             list.AddRange(entries.Select(entry => entry.Value<float>()));

                             return list;
                         });
            RegisterType(typeof(System.Collections.Generic.List<string>), "List<string>",
                         () => new InputValue<List<string>>(new List<string>()),
                         (writer, obj) =>
                         {
                             var list = (List<string>)obj;
                             writer.WriteStartObject();
                             writer.WritePropertyName("Values");
                             writer.WriteStartArray();
                             list.ForEach(writer.WriteValue);
                             writer.WriteEndArray();
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             var entries = jsonToken["Values"];
                             var list = new List<string>(entries.Count());
                             list.AddRange(entries.Select(entry => entry.Value<string>()));
                             return list;
                         });
            RegisterType(typeof(System.Numerics.Quaternion), "Quaternion",
                         () => new InputValue<System.Numerics.Quaternion>(System.Numerics.Quaternion.Identity),
                         (writer, obj) =>
                         {
                             var quaternion = (System.Numerics.Quaternion)obj;
                             writer.WriteStartObject();
                             writer.WriteValue("X", quaternion.X);
                             writer.WriteValue("Y", quaternion.Y);
                             writer.WriteValue("Z", quaternion.Z);
                             writer.WriteValue("W", quaternion.W);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             float x = jsonToken["X"].Value<float>();
                             float y = jsonToken["Y"].Value<float>();
                             float z = jsonToken["Z"].Value<float>();
                             float w = jsonToken["W"].Value<float>();
                             return new System.Numerics.Quaternion(x, y, z, w);
                         });
            RegisterType(typeof(System.Numerics.Vector2), "Vector2",
                         InputDefaultValueCreator<System.Numerics.Vector2>,
                         (writer, obj) =>
                         {
                             var vec = (System.Numerics.Vector2)obj;
                             writer.WriteStartObject();
                             writer.WriteValue("X", vec.X);
                             writer.WriteValue("Y", vec.Y);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             float x = jsonToken["X"].Value<float>();
                             float y = jsonToken["Y"].Value<float>();
                             return new System.Numerics.Vector2(x, y);
                         });
            RegisterType(typeof(System.Numerics.Vector3), "Vector3",
                         InputDefaultValueCreator<System.Numerics.Vector3>,
                         (writer, obj) =>
                         {
                             var vec = (System.Numerics.Vector3)obj;
                             writer.WriteStartObject();
                             writer.WriteValue("X", vec.X);
                             writer.WriteValue("Y", vec.Y);
                             writer.WriteValue("Z", vec.Z);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             float x = jsonToken["X"].Value<float>();
                             float y = jsonToken["Y"].Value<float>();
                             float z = jsonToken["Z"].Value<float>();
                             return new System.Numerics.Vector3(x, y, z);
                         });
            RegisterType(typeof(System.Numerics.Vector4), "Vector4",
                         () => new InputValue<Vector4>(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
                         (writer, obj) =>
                         {
                             var vec = (Vector4)obj;
                             writer.WriteStartObject();
                             writer.WriteValue("X", vec.X);
                             writer.WriteValue("Y", vec.Y);
                             writer.WriteValue("Z", vec.Z);
                             writer.WriteValue("W", vec.W);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             float x = jsonToken["X"].Value<float>();
                             float y = jsonToken["Y"].Value<float>();
                             float z = jsonToken["Z"].Value<float>();
                             float w = jsonToken["W"].Value<float>();
                             return new Vector4(x, y, z, w);
                         });
            RegisterType(typeof(System.Text.StringBuilder), "StringBuilder",
                         () => new InputValue<StringBuilder>(new StringBuilder()));

            RegisterType(typeof(DateTime), "DateTime",
                         () => new InputValue<DateTime>(new DateTime()));

            // t3 core types
            RegisterType(typeof(BufferWithViews), "BufferWithViews",
                         () => new InputValue<BufferWithViews>(null));

            RegisterType(typeof(Command), "Command",
                         () => new InputValue<Command>(null));
            RegisterType(typeof(Curve), "Curve",
                         InputDefaultValueCreator<Curve>,
                         (writer, obj) =>
                         {
                             Curve curve = (Curve)obj;
                             writer.WriteStartObject();
                             curve?.Write(writer);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             Curve curve = new Curve();
                             if (jsonToken == null || !jsonToken.HasValues)
                             {
                                 curve.AddOrUpdateV(0, new VDefinition() { Value = 0 });
                                 curve.AddOrUpdateV(1, new VDefinition() { Value = 1 });
                             }
                             else
                             {
                                 curve.Read(jsonToken);
                             }

                             return curve;
                         });
            RegisterType(typeof(T3.Core.Operator.GizmoVisibility), "GizmoVisibility",
                         InputDefaultValueCreator<T3.Core.Operator.GizmoVisibility>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<T3.Core.Operator.GizmoVisibility>);
            RegisterType(typeof(DataTypes.Gradient), "Gradient",
                         InputDefaultValueCreator<Gradient>,
                         (writer, obj) =>
                         {
                             Gradient gradient = (Gradient)obj;
                             writer.WriteStartObject();
                             gradient?.Write(writer);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             Gradient gradient = new Gradient();
                             if (jsonToken == null || !jsonToken.HasValues)
                             {
                                 gradient = new Gradient();
                             }
                             else
                             {
                                 gradient.Read(jsonToken);
                             }

                             return gradient;
                         });
            RegisterType(typeof(ParticleSystem), "ParticleSystem",
                         () => new InputValue<ParticleSystem>(null));

            RegisterType(typeof(Point[]), "Point",
                         () => new InputValue<Point[]>());
            RegisterType(typeof(RenderTargetReference), "RenderTargetRef",
                         () => new InputValue<RenderTargetReference>());
            RegisterType(typeof(Object), "Object",
                         () => new InputValue<Object>());
            RegisterType(typeof(StructuredList), "StructuredList",
                         () => new InputValue<StructuredList>());
            RegisterType(typeof(Texture3dWithViews), "Texture3dWithViews",
                         () => new InputValue<Texture3dWithViews>(new Texture3dWithViews()));
            RegisterType(typeof(MeshBuffers), "MeshBuffers",
                         () => new InputValue<MeshBuffers>(null));

            // sharpdx types
            RegisterType(typeof(SharpDX.Direct3D.PrimitiveTopology), "PrimitiveTopology",
                         InputDefaultValueCreator<PrimitiveTopology>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<PrimitiveTopology>);
            RegisterType(typeof(SharpDX.Direct3D11.BindFlags), "BindFlags",
                         InputDefaultValueCreator<BindFlags>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<BindFlags>);
            RegisterType(typeof(SharpDX.Direct3D11.BlendOperation), "BlendOperation",
                         InputDefaultValueCreator<BlendOperation>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<BlendOperation>);
            RegisterType(typeof(SharpDX.Direct3D11.BlendOption), "BlendOption",
                         InputDefaultValueCreator<BlendOption>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<BlendOption>);
            RegisterType(typeof(SharpDX.Direct3D11.BlendState), "BlendState",
                         () => new InputValue<BlendState>(null));
            RegisterType(typeof(SharpDX.Direct3D11.Buffer), "Buffer",
                         () => new InputValue<Buffer>(null));
            RegisterType(typeof(SharpDX.Direct3D11.ColorWriteMaskFlags), "ColorWriteMaskFlags",
                         InputDefaultValueCreator<ColorWriteMaskFlags>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<ColorWriteMaskFlags>);
            RegisterType(typeof(SharpDX.Direct3D11.Comparison), "Comparison",
                         InputDefaultValueCreator<Comparison>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<Comparison>);
            RegisterType(typeof(SharpDX.Direct3D11.ComputeShader), "ComputeShader",
                         () => new InputValue<ComputeShader>(null));
            RegisterType(typeof(SharpDX.Direct3D11.CpuAccessFlags), "CpuAccessFlags",
                         InputDefaultValueCreator<CpuAccessFlags>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<CpuAccessFlags>);
            RegisterType(typeof(SharpDX.Direct3D11.CullMode), "CullMode",
                         InputDefaultValueCreator<CullMode>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<CullMode>);
            RegisterType(typeof(SharpDX.Direct3D11.DepthStencilState), "DepthStencilState",
                         () => new InputValue<DepthStencilState>(null));
            RegisterType(typeof(SharpDX.Direct3D11.DepthStencilView), "DepthStencilView",
                         () => new InputValue<DepthStencilView>(null));
            RegisterType(typeof(SharpDX.Direct3D11.FillMode), "FillMode",
                         InputDefaultValueCreator<FillMode>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<FillMode>);
            RegisterType(typeof(SharpDX.Direct3D11.Filter), "Filter",
                         InputDefaultValueCreator<Filter>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<Filter>);
            RegisterType(typeof(SharpDX.Direct3D11.GeometryShader), "GeometryShader",
                         () => new InputValue<GeometryShader>(null));
            RegisterType(typeof(SharpDX.Direct3D11.InputLayout), "InputLayout",
                         () => new InputValue<InputLayout>(null));
            RegisterType(typeof(SharpDX.Direct3D11.PixelShader), "PixelShader",
                         () => new InputValue<PixelShader>(null));
            RegisterType(typeof(SharpDX.Direct3D11.RenderTargetBlendDescription), "RenderTargetBlendDescription",
                         () => new InputValue<RenderTargetBlendDescription>());
            RegisterType(typeof(SharpDX.Direct3D11.RasterizerState), "RasterizerState",
                         () => new InputValue<RasterizerState>(null));
            RegisterType(typeof(SharpDX.Direct3D11.RenderTargetView), "RenderTargetView",
                         () => new InputValue<RenderTargetView>(null));
            RegisterType(typeof(SharpDX.Direct3D11.ResourceOptionFlags), "ResourceOptionFlags",
                         InputDefaultValueCreator<ResourceOptionFlags>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<ResourceOptionFlags>);
            RegisterType(typeof(SharpDX.Direct3D11.ResourceUsage), "ResourceUsage",
                         InputDefaultValueCreator<ResourceUsage>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<ResourceUsage>);
            RegisterType(typeof(SharpDX.Direct3D11.SamplerState), "SamplerState",
                         () => new InputValue<SamplerState>(null));
            RegisterType(typeof(SharpDX.Direct3D11.ShaderResourceView), "ShaderResourceView",
                         () => new InputValue<ShaderResourceView>(null));
            RegisterType(typeof(SharpDX.Direct3D11.Texture2D), "Texture2D",
                         () => new InputValue<Texture2D>(null));
            RegisterType(typeof(SharpDX.Direct3D11.Texture3D), "Texture3D",
                         () => new InputValue<Texture3D>(null));
            RegisterType(typeof(SharpDX.Direct3D11.TextureAddressMode), "TextureAddressMode",
                         InputDefaultValueCreator<TextureAddressMode>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<TextureAddressMode>);
            RegisterType(typeof(SharpDX.Direct3D11.UnorderedAccessView), "UnorderedAccessView",
                         () => new InputValue<UnorderedAccessView>(null));
            RegisterType(typeof(SharpDX.Direct3D11.UnorderedAccessViewBufferFlags), "UnorderedAccessViewBufferFlags",
                         InputDefaultValueCreator<UnorderedAccessViewBufferFlags>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<UnorderedAccessViewBufferFlags>);
            RegisterType(typeof(SharpDX.Direct3D11.VertexShader), "VertexShader",
                         () => new InputValue<VertexShader>(null));
            RegisterType(typeof(SharpDX.DXGI.Format), "Format",
                         InputDefaultValueCreator<Format>,
                         (writer, obj) => writer.WriteValue(obj.ToString()),
                         JsonToEnumValue<Format>);
            RegisterType(typeof(SharpDX.Int3), "Int3",
                         InputDefaultValueCreator<Int3>,
                         (writer, obj) =>
                         {
                             Int3 vec = (Int3)obj;
                             writer.WriteStartObject();
                             writer.WriteValue("X", vec.X);
                             writer.WriteValue("Y", vec.Y);
                             writer.WriteValue("Z", vec.Z);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             int x = jsonToken["X"].Value<int>();
                             int y = jsonToken["Y"].Value<int>();
                             int z = jsonToken["Z"].Value<int>();
                             return new Int3(x, y, z);
                         });
            RegisterType(typeof(SharpDX.Mathematics.Interop.RawRectangle), "RawRectangle",
                         () => new InputValue<RawRectangle>(new RawRectangle { Left = -100, Right = 100, Bottom = -100, Top = 100 }));
            RegisterType(typeof(SharpDX.Mathematics.Interop.RawViewportF), "RawViewportF",
                         () => new InputValue<RawViewportF>(new RawViewportF
                                                                { X = 0.0f, Y = 0.0f, Width = 100.0f, Height = 100.0f, MinDepth = 0.0f, MaxDepth = 10000.0f }));
            RegisterType(typeof(SharpDX.Size2), "Size2",
                         InputDefaultValueCreator<Size2>,
                         (writer, obj) =>
                         {
                             Size2 vec = (Size2)obj;
                             writer.WriteStartObject();
                             writer.WriteValue("Width", vec.Width);
                             writer.WriteValue("Height", vec.Height);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             int width = jsonToken["Width"].Value<int>();
                             int height = jsonToken["Height"].Value<int>();
                             return new Size2(width, height);
                         });
            RegisterType(typeof(SharpDX.Vector4[]), "Vector4[]",
                         () => new InputValue<SharpDX.Vector4[]>(Array.Empty<SharpDX.Vector4>()));
            RegisterType(typeof(Dict<float>), "Dict<float>",
                         () => new InputValue<Dict<float>>());

            _updateCounter = new OpUpdateCounter();
        }

        private static void RegisterType(Type type, string typeName,
                                         Func<InputValue> defaultValueCreator,
                                         Action<JsonTextWriter, object> valueToJsonConverter,
                                         Func<JToken, object> jsonToValueConverter)
        {
            RegisterType(type, typeName, defaultValueCreator);
            TypeValueToJsonConverters.Entries.Add(type, valueToJsonConverter);
            JsonToTypeValueConverters.Entries.Add(type, jsonToValueConverter);
        }

        private static void RegisterType(Type type, string typeName, Func<InputValue> defaultValueCreator)
        {
            TypeNameRegistry.Entries.Add(type, typeName);
            InputValueCreators.Entries.Add(type, defaultValueCreator);
        }

        private Dictionary<Guid, JsonFileResult<Symbol>> _symbolJsonTokens = new();

        public virtual void Load(bool enableLog)
        {
            var symbolFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolExtension}", SearchOption.AllDirectories);

            _symbolJsonTokens = symbolFiles.AsParallel()
                                           .Select(JsonFileResult<Symbol>.ReadAndCreate)
                                           .ToDictionary(x => x.Guid, x => x);
            
            _ = _symbolJsonTokens.AsParallel()
                                 .Select(TryReadSymbolFromJsonFileResult)
                                 .ToList(); // Execute and bring back to main thread

            foreach (var idJsonTokenPair in _symbolJsonTokens )
            {
                var jsonResult = idJsonTokenPair.Value;
                if (jsonResult.ObjectWasSet)
                {
                    SymbolRegistry.Entries.TryAdd(jsonResult.Object.Id, jsonResult.Object);
                }
            }

            // Check if there are symbols without a file, if yes add these
            var instanceTypesWithoutFile = (from type in OperatorsAssembly.ExportedTypes
                                 where type.IsSubclassOf(typeof(Instance))
                                 where !type.IsGenericType
                                 select type).ToHashSet();

            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                instanceTypesWithoutFile.Remove(symbol.InstanceType);
            }

            foreach (var newType in instanceTypesWithoutFile)
            {
                var typeNamespace = newType.Namespace;
                if (string.IsNullOrWhiteSpace(typeNamespace))
                {
                    Log.Error($"Null or empty namespace of type {newType.Name}");
                    continue;
                } 
                
                var @namespace = _innerNamespace.Replace(newType.Namespace ?? string.Empty, "").ToLower();
                var idFromNamespace = _idFromNamespace
                                     .Match(newType.Namespace ?? string.Empty).Value
                                     .Replace('_', '-');

                Debug.Assert(!string.IsNullOrWhiteSpace(idFromNamespace));
                var symbol = new Symbol(newType, Guid.Parse(idFromNamespace))
                                 {
                                     Namespace = @namespace,
                                     Name = newType.Name
                                 };

                var added = SymbolRegistry.Entries.TryAdd(symbol.Id, symbol);
                if (!added)
                {
                    Log.Error($"Ignoring redefinition symbol {symbol.Name}. Please fix multiple definitions in Operators/Types/ folder");
                    continue;
                }
                
                if(enableLog)
                    Log.Debug($"new added symbol: {newType}");
            }

            (bool success, Symbol symbol) TryReadSymbolFromJsonFileResult(KeyValuePair<Guid, JsonFileResult<Symbol>> idJsonResultPair)
            {
                var jsonInfo = idJsonResultPair.Value;
                var success = SymbolJson.TryReadSymbol(this, jsonInfo.Guid, jsonInfo.JToken, out var symbol, allowNonOperatorInstanceType: false);
                if (success)
                {
                    // This jsonInfo.Object can be read/assigned outside of this thread, so we should lock it 
                    lock (jsonInfo)
                    {
                        jsonInfo.Object = symbol;
                    }
                }
                else
                {
                    Log.Warning($"Failed to load symbol {jsonInfo.Guid}");
                }

                return (success, symbol);
            }
        }

        private readonly Regex _innerNamespace = new Regex(@".Id_(\{){0,1}[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12}(\}){0,1}",
                                                           RegexOptions.IgnoreCase);

        private readonly Regex _idFromNamespace = new Regex(@"(\{){0,1}[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12}(\}){0,1}",
                                                            RegexOptions.IgnoreCase);

        internal bool TryReadSymbolWithId(Guid symbolId, out Symbol symbol)
        {
            var hasJsonInfo = _symbolJsonTokens.TryGetValue(symbolId, out var jsonInfo); // todo: TryGet
            if (!hasJsonInfo)
            {
                symbol = null;
                return false;
            }

            if (jsonInfo.Object is null)
            {
                var success = SymbolJson.TryReadSymbol(this, jsonInfo.Guid, jsonInfo.JToken, out symbol, false);
                if (!success)
                    return false;
                
                jsonInfo.Object = symbol;

                return true;
            }

            symbol = jsonInfo.Object;
            return true;
        }
        
        public virtual void SaveAll()
        {
            ResourceFileWatcher.DisableOperatorFileWatcher(); // don't update ops if file is written during save
            
            // todo: this sounds like a dangerous step. we should overwrite these files by default and can check which files are not overwritten to delete others?
            RemoveAllSymbolFiles(); 
            SortAllSymbolSourceFiles();
            SaveSymbolDefinitionAndSourceFiles(SymbolRegistry.Entries.Values);

            ResourceFileWatcher.EnableOperatorFileWatcher();
        }

        protected static void SaveSymbolDefinitionAndSourceFiles(IEnumerable<Symbol> valueCollection)
        {
            foreach (var symbol in valueCollection)
            {
                var filepath = BuildFilepathForSymbol(symbol, SymbolExtension);

                using (var sw = new StreamWriter(filepath))
                using (var writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
                {
                    SymbolJson.WriteSymbol(symbol, writer);
                }

                if (!string.IsNullOrEmpty(symbol.PendingSource))
                {
                    WriteSymbolSourceToFile(symbol);
                }
            }
        }

        private static void SortAllSymbolSourceFiles()
        {
            // Move existing source files to correct namespace folder
            var sourceFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SourceExtension}", SearchOption.AllDirectories);
            foreach (var sourceFilePath in sourceFiles)
            {
                var classname = Path.GetFileNameWithoutExtension(sourceFilePath);
                var symbol = SymbolRegistry.Entries.Values.SingleOrDefault(s => s.Name == classname);
                if (symbol == null)
                {
                    Log.Warning($"Skipping unregistered source file {sourceFilePath}");
                    continue;
                }

                var targetFilepath = BuildFilepathForSymbol(symbol, SourceExtension);
                if (sourceFilePath == targetFilepath)
                    continue;

                Log.Debug($" Moving {sourceFilePath} -> {targetFilepath} ...");
                try
                {
                    File.Move(sourceFilePath, targetFilepath);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to write source file '" + sourceFilePath + "': " + e);
                }
            }
        }

        private static void RemoveAllSymbolFiles()
        {
            // Remove all old t3 files before storing to get rid off invalid ones
            var symbolFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolExtension}", SearchOption.AllDirectories);
            foreach (var symbolFilePath in symbolFiles)
            {
                try
                {
                    File.Delete(symbolFilePath);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to deleted file '" + symbolFilePath + "': " + e);
                }
            }
        }

        // public void SaveModifiedSymbol(Symbol symbol)
        // {
        //     RemoveObsoleteSymbolFiles(symbol);
        //
        //     var symbolJson = new SymbolJson();
        //     
        //     var filepath = BuildFilepathForSymbol(symbol, SymbolExtension);
        //
        //     using (var sw = new StreamWriter(filepath))
        //     using (var writer = new JsonTextWriter(sw))
        //     {
        //         symbolJson.Writer = writer;
        //         symbolJson.Writer.Formatting = Formatting.Indented;
        //         symbolJson.WriteSymbol(symbol);
        //     }
        //
        //     if (!string.IsNullOrEmpty(symbol.PendingSource))
        //     {
        //         WriteSymbolSourceToFile(symbol);
        //     }
        // }

        private static void RemoveDeprecatedSymbolFiles(Symbol symbol)
        {
            if (string.IsNullOrEmpty(symbol.DeprecatedSourcePath))
                return;

            foreach (var fileExtension in OperatorFileExtensions)
            {
                var sourceFilepath = Path.Combine(OperatorTypesFolder, symbol.DeprecatedSourcePath + "_" + symbol.Id + fileExtension);
                try
                {
                    File.Delete(sourceFilepath);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to deleted file '" + sourceFilepath + "': " + e);
                }
            }

            symbol.DeprecatedSourcePath = String.Empty;
        }


        private static void WriteSymbolSourceToFile(Symbol symbol)
        {
            var sourcePath = BuildFilepathForSymbol(symbol, SourceExtension);
            using (var sw = new StreamWriter(sourcePath))
            {
                sw.Write(symbol.PendingSource);
            }

            // Remove old source file and its entry in project
            if (!string.IsNullOrEmpty(symbol.DeprecatedSourcePath))
            {
                if (symbol.DeprecatedSourcePath == Model.BuildFilepathForSymbol(symbol, SourceExtension))
                {
                    Log.Warning($"Attempted to deprecated valid source file: {symbol.DeprecatedSourcePath}");
                    symbol.DeprecatedSourcePath = string.Empty;
                    return;
                }
                File.Delete(symbol.DeprecatedSourcePath);

                // Adjust path of file resource
                ResourceManager.RenameOperatorResource(symbol.DeprecatedSourcePath, sourcePath);

                symbol.DeprecatedSourcePath = string.Empty;
            }
            symbol.PendingSource = null;
        }

        #region File path handling
        private static string GetSubDirectoryFromNamespace(string symbolNamespace)
        {
            var trimmed = symbolNamespace.Trim().Replace(".", "\\");
            return trimmed;
        }
        
        private static string BuildAndCreateFolderFromNamespace(string symbolNamespace)
        {
            var directory = Path.Combine(OperatorTypesFolder, GetSubDirectoryFromNamespace(symbolNamespace));
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        public static string BuildFilepathForSymbol(Symbol symbol, string extension)
        {
            var dir = BuildAndCreateFolderFromNamespace(symbol.Namespace);
            return extension == SourceExtension
                       ? Path.Combine(dir, symbol.Name + extension)
                       : Path.Combine(dir, symbol.Name + "_" + symbol.Id + extension);
        }
        #endregion

        private static OpUpdateCounter _updateCounter;

        public const string SourceExtension = ".cs";
        private const string SymbolExtension = ".t3";
        protected const string SymbolUiExtension = ".t3ui";
        public const string OperatorTypesFolder = @"Operators\Types\";

        private static readonly List<string> OperatorFileExtensions = new()
                                                                           {
                                                                               SymbolExtension,
                                                                               SymbolUiExtension,
                                                                               SourceExtension,
                                                                           };
    }
}