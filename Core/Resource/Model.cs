using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector4 = System.Numerics.Vector4;

namespace T3.Core
{
    public static class JsonToTypeValueConverters
    {
        public static Dictionary<Type, Func<JToken, object>> Entries { get; } = new Dictionary<Type, Func<JToken, object>>();
    }

    public static class TypeValueToJsonConverters
    {
        public static Dictionary<Type, Action<JsonTextWriter, object>> Entries { get; } = new Dictionary<Type, Action<JsonTextWriter, object>>();
    }

    public static class InputValueCreators
    {
        public static Dictionary<Type, Func<InputValue>> Entries { get; } = new Dictionary<Type, Func<InputValue>>();
    }

    public static class TypeNameRegistry
    {
        public static Dictionary<Type, string> Entries { get; } = new Dictionary<Type, string>(20);
    }

    public class Command
    {
        public Action<EvaluationContext> PrepareAction;
        public Action<EvaluationContext> RestoreAction;
    }


    public class Model
    {
        public Assembly OperatorsAssembly { get; }
        protected string Path { get; } = @"Operators\Types\";
        protected string SymbolExtension { get; } = ".t3";

        public Model(Assembly operatorAssembly, bool enabledLogging)
        {
            if (enabledLogging)
                Log.AddWriter(new ConsoleWriter());

            OperatorsAssembly = operatorAssembly;

            // generic enum value from json function, must be local function
            object JsonToEnumValue<T>(JToken jsonToken) where T : struct // todo: use 7.3 and replace with enum
            {
                string value = jsonToken.Value<string>();
                if (Enum.TryParse(value, out T enumValue))
                {
                    return enumValue;
                }
                else
                {
                    return null;
                }
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

            
            // t3 core types
            RegisterType(typeof(Command), "Command",
                         () => new InputValue<Command>(null));
            RegisterType(typeof(Animation.Curve), "Curve",
                         InputDefaultValueCreator<Animation.Curve>,
                         (writer, obj) =>
                         {
                             Animation.Curve curve = (Animation.Curve)obj;
                             writer.WriteStartObject();
                             curve?.Write(writer);
                             writer.WriteEndObject();
                         },
                         jsonToken =>
                         {
                             Animation.Curve curve = new Animation.Curve();
                             if (jsonToken == null || !jsonToken.HasValues)
                             {
                                 curve.AddOrUpdateV(0, new VDefinition(){ Value = 0});
                                 curve.AddOrUpdateV(1, new VDefinition(){ Value = 1});
                             }
                             else
                             {
                                 curve.Read(jsonToken);
                             }
                             return curve;
                         });
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
                         () => new InputValue<ParticleSystem>(new ParticleSystem()));
            
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
                         () => new InputValue<RawRectangle>(new RawRectangle {Left = -100, Right = 100, Bottom = -100, Top = 100}));
            RegisterType(typeof(SharpDX.Mathematics.Interop.RawViewportF), "RawViewportF",
                         () => new InputValue<RawViewportF>(new RawViewportF
                                                            {X = 0.0f, Y = 0.0f, Width = 100.0f, Height = 100.0f, MinDepth = 0.0f, MaxDepth = 10000.0f}));
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
            
        }

        public static void RegisterType(Type type, string typeName,
                                        Func<InputValue> defaultValueCreator,
                                        Action<JsonTextWriter, object> valueToJsonConverter,
                                        Func<JToken, object> jsonToValueConverter)
        {
            RegisterType(type, typeName, defaultValueCreator);
            TypeValueToJsonConverters.Entries.Add(type, valueToJsonConverter);
            JsonToTypeValueConverters.Entries.Add(type, jsonToValueConverter);
        }

        public static void RegisterType(Type type, string typeName, Func<InputValue> defaultValueCreator)
        {
            TypeNameRegistry.Entries.Add(type, typeName);
            InputValueCreators.Entries.Add(type, defaultValueCreator);
        }

        public virtual void Load()
        {
            var symbolFiles = Directory.GetFiles(Path, $"*{SymbolExtension}");
            foreach (var symbolFile in symbolFiles)
            {
                ReadSymbolFromFile(symbolFile);
            }

            // check if there are symbols without a file, if yes add these
            var instanceTypes = (from type in OperatorsAssembly.ExportedTypes
                                 where type.IsSubclassOf(typeof(Instance))
                                 where !type.IsGenericType
                                 select type).ToList();

            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                for (int i = instanceTypes.Count - 1; i >= 0; i--)
                {
                    if (symbol.InstanceType == instanceTypes[i])
                    {
                        instanceTypes.RemoveAt(i);
                        break;
                    }
                }
            }

            foreach (var newType in instanceTypes)
            {
                string idFromNamespace = _idFromNamespace.Match(newType.Namespace ?? string.Empty).Value.Replace('_', '-');
                Debug.Assert(!string.IsNullOrEmpty(idFromNamespace));
                var symbol = new Symbol(newType, Guid.Parse(idFromNamespace));
                SymbolRegistry.Entries.Add(symbol.Id, symbol);
                Console.WriteLine($"new added symbol: {newType}");
            }
        }

        private readonly Regex _idFromNamespace = new Regex(@"(\{){0,1}[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12}(\}){0,1}",
                                                            RegexOptions.IgnoreCase);

        public Symbol ReadSymbolFromFile(string symbolFile)
        {
            Log.Info($"file: {symbolFile}");
            using (var sr = new StreamReader(symbolFile))
            using (var jsonReader = new JsonTextReader(sr))
            {
                Json json = new Json {Reader = jsonReader};
                var symbol = json.ReadSymbol(this);
                if (symbol != null)
                {
                    symbol.SourcePath = Path + symbol.Name + ".cs";
                    SymbolRegistry.Entries.Add(symbol.Id, symbol);
                }

                return symbol;
            }
        }

        public Symbol ReadSymbolWithId(Guid id)
        {
            var symbolFile = Directory.GetFiles(Path, $"*{id}*{SymbolExtension}").FirstOrDefault();
            if (symbolFile == null)
            {
                Log.Error($"Could not find symbol file containing the id '{id}'");
                return null;
            }

            var symbol = ReadSymbolFromFile(symbolFile);
            return symbol;
        }

        public virtual void Save()
        {
            ResourceManager.Instance().DisableOperatorFileWatcher(); // don't update ops if file is written during save
            
            // remove all old t3 files before storing to get rid off invalid ones
            DirectoryInfo di = new DirectoryInfo(Path);
            FileInfo[] files = di.GetFiles("*" + SymbolExtension).ToArray();
            foreach (FileInfo file in files)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to deleted " + file + e);
                }
                
            }

            Json json = new Json();
            // store all symbols in corresponding files
            foreach (var (_, symbol) in SymbolRegistry.Entries)
            {
                using (var sw = new StreamWriter(Path + symbol.Name + "_" + symbol.Id + SymbolExtension))
                using (var writer = new JsonTextWriter(sw))
                {
                    json.Writer = writer;
                    json.Writer.Formatting = Formatting.Indented;
                    json.WriteSymbol(symbol);
                }

                if (!string.IsNullOrEmpty(symbol.PendingSource))
                {
                    WriteSymbolSourceToFile(symbol);
                }
            }

            ResourceManager.Instance().EnableOperatorFileWatcher();
        }
        
        private void WriteSymbolSourceToFile(Symbol symbol)
        {
            string sourcePath = Path + symbol.Name + ".cs";
            using (var sw = new StreamWriter(sourcePath))
            {
                sw.Write(symbol.PendingSource);
            }

            if (!string.IsNullOrEmpty(symbol.DeprecatedSourcePath))
            {
                // remove old source file and its entry in project 
                RemoveSourceFileFromProject(symbol.DeprecatedSourcePath);
                File.Delete(symbol.DeprecatedSourcePath);
                symbol.DeprecatedSourcePath = string.Empty;
            }

            if (string.IsNullOrEmpty(symbol.SourcePath))
            {
                symbol.SourcePath = sourcePath;
                AddSourceFileToProject(sourcePath);
            }

            symbol.PendingSource = null;
        }
        
        /// <summary>
        /// Inserts an entry like...
        /// 
        ///      <Compile Include="Types\GfxPipelineExample.cs" />
        /// 
        /// ... to the project file.
        /// </summary>
        public static void AddSourceFileToProject(string newSourceFilePath)
        {
            var path = System.IO.Path.GetDirectoryName(newSourceFilePath);
            var newFileName = System.IO.Path.GetFileName(newSourceFilePath);
            var directoryInfo = new DirectoryInfo(path).Parent;
            if (directoryInfo == null)
            {
                Log.Error("Can't find project file folder for " + newSourceFilePath);
                return;
            }

            var parentPath = directoryInfo.FullName;
            var projectFilePath = System.IO.Path.Combine(parentPath, "Operators.csproj");

            if (!File.Exists(projectFilePath))
            {
                Log.Error("Can't find project file in " + projectFilePath);
                return;
            }

            var orgLine = "<ItemGroup>\r\n    <Compile Include";
            var newLine = $"<ItemGroup>\r\n    <Compile Include=\"Types\\{newFileName}\" />\r\n    <Compile Include";
            var newContent = File.ReadAllText(projectFilePath).Replace(orgLine, newLine);
            File.WriteAllText(projectFilePath, newContent);
        }

        public static void RemoveSourceFileFromProject(string sourceFilePath)
        {
            var path = System.IO.Path.GetDirectoryName(sourceFilePath);
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            var directoryInfo = new DirectoryInfo(path).Parent;
            if (directoryInfo == null)
            {
                Log.Error("Can't find project file folder for " + sourceFilePath);
                return;
            }

            var parentPath = directoryInfo.FullName;
            var projectFilePath = System.IO.Path.Combine(parentPath, "Operators.csproj");

            if (!File.Exists(projectFilePath))
            {
                Log.Error("Can't find project file in " + projectFilePath);
                return;
            }

            var orgLine = $"    <Compile Include=\"Types\\{fileName}\" />\r\n";
            var newContent = File.ReadAllText(projectFilePath).Replace(orgLine, string.Empty);
            File.WriteAllText(projectFilePath, newContent);
        }
    }
}