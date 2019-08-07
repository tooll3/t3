using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Logging;
using T3.Core.Operator;
using static System.Int32;

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

    public class Model
    {
        protected string Path { get; } = @"..\Core\Operator\Types\";
        protected string SymbolExtension { get; } = ".t3";

        public Model()
        {
            // generic enum value from json function, must be local function
            object JsonToEnumValue<T>(JToken jsonToken) where T : struct
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

            // Register the converters from json to a specific type value
            JsonToTypeValueConverters.Entries.Add(typeof(float), jsonToken => jsonToken.Value<float>());
            JsonToTypeValueConverters.Entries.Add(typeof(int), jsonToken => jsonToken.Value<int>());
            JsonToTypeValueConverters.Entries.Add(typeof(string), jsonToken => jsonToken.Value<string>());
            JsonToTypeValueConverters.Entries.Add(typeof(Vector2), jsonToken =>
                                                                   {
                                                                       float x = jsonToken["X"].Value<float>();
                                                                       float y = jsonToken["Y"].Value<float>();
                                                                       return new Vector2(x, y);
                                                                   });
            JsonToTypeValueConverters.Entries.Add(typeof(Size2), jsonToken =>
                                                                 {
                                                                     int width = jsonToken["Width"].Value<int>();
                                                                     int height = jsonToken["Height"].Value<int>();
                                                                     return new Size2(width, height);
                                                                 });
            JsonToTypeValueConverters.Entries.Add(typeof(Format), JsonToEnumValue<Format>);
            JsonToTypeValueConverters.Entries.Add(typeof(ResourceUsage), JsonToEnumValue<ResourceUsage>);
            JsonToTypeValueConverters.Entries.Add(typeof(BindFlags), JsonToEnumValue<BindFlags>);
            JsonToTypeValueConverters.Entries.Add(typeof(CpuAccessFlags), JsonToEnumValue<CpuAccessFlags>);
            JsonToTypeValueConverters.Entries.Add(typeof(ResourceOptionFlags), JsonToEnumValue<ResourceOptionFlags>);
            JsonToTypeValueConverters.Entries.Add(typeof(List<float>), jsonToken =>
                                                                       {
                                                                           var entries = jsonToken["Values"];
                                                                           var list = new List<float>(entries.Count());
                                                                           list.AddRange(entries.Select(entry => entry.Value<float>()));

                                                                           return list;
                                                                       });

            // Register the converters from a specific type value to json
            TypeValueToJsonConverters.Entries.Add(typeof(float), (writer, obj) => writer.WriteValue((float)obj));
            TypeValueToJsonConverters.Entries.Add(typeof(int), (writer, obj) => writer.WriteValue((int)obj));
            TypeValueToJsonConverters.Entries.Add(typeof(string), (writer, value) => writer.WriteValue((string)value));
            TypeValueToJsonConverters.Entries.Add(typeof(Vector2), (writer, obj) =>
                                                                   {
                                                                       Vector2 vec = (Vector2)obj;
                                                                       writer.WriteStartObject();
                                                                       writer.WriteValue("X", vec.X);
                                                                       writer.WriteValue("Y", vec.Y);
                                                                       writer.WriteEndObject();
                                                                   });
            TypeValueToJsonConverters.Entries.Add(typeof(Size2), (writer, obj) =>
                                                                 {
                                                                     Size2 vec = (Size2)obj;
                                                                     writer.WriteStartObject();
                                                                     writer.WriteValue("Width", vec.Width);
                                                                     writer.WriteValue("Height", vec.Height);
                                                                     writer.WriteEndObject();
                                                                 });
            TypeValueToJsonConverters.Entries.Add(typeof(Format), (writer, obj) => writer.WriteValue(obj.ToString()));
            TypeValueToJsonConverters.Entries.Add(typeof(ResourceUsage), (writer, obj) => writer.WriteValue(obj.ToString()));
            TypeValueToJsonConverters.Entries.Add(typeof(BindFlags), (writer, obj) => writer.WriteValue(obj.ToString()));
            TypeValueToJsonConverters.Entries.Add(typeof(CpuAccessFlags), (writer, obj) => writer.WriteValue(obj.ToString()));
            TypeValueToJsonConverters.Entries.Add(typeof(ResourceOptionFlags), (writer, obj) => writer.WriteValue(obj.ToString()));
            TypeValueToJsonConverters.Entries.Add(typeof(List<float>), (writer, obj) =>
                                                                       {
                                                                           var list = (List<float>)obj;
                                                                           writer.WriteStartObject();
                                                                           writer.WritePropertyName("Values");
                                                                           writer.WriteStartArray();
                                                                           list.ForEach(writer.WriteValue);
                                                                           writer.WriteEndArray();
                                                                           writer.WriteEndObject();
                                                                       });

            // Register input value creators that take the relevant input attribute, extract the default value and return this with the new input value
            InputValue InputDefaultValueCreator<T>() => new InputValue<T>();
            InputValueCreators.Entries.Add(typeof(int), InputDefaultValueCreator<int>);
            InputValueCreators.Entries.Add(typeof(float), InputDefaultValueCreator<float>);
            InputValueCreators.Entries.Add(typeof(string), InputDefaultValueCreator<string>);
            InputValueCreators.Entries.Add(typeof(Size2), InputDefaultValueCreator<Size2>);
            InputValueCreators.Entries.Add(typeof(ResourceUsage), InputDefaultValueCreator<ResourceUsage>);
            InputValueCreators.Entries.Add(typeof(Format), InputDefaultValueCreator<Format>);
            InputValueCreators.Entries.Add(typeof(BindFlags), InputDefaultValueCreator<BindFlags>);
            InputValueCreators.Entries.Add(typeof(CpuAccessFlags), InputDefaultValueCreator<CpuAccessFlags>);
            InputValueCreators.Entries.Add(typeof(ResourceOptionFlags), InputDefaultValueCreator<ResourceOptionFlags>);
            InputValueCreators.Entries.Add(typeof(List<float>), () => new InputValue<List<float>>(new List<float>()));
        }

        public virtual void Load()
        {
            var symbolFiles = Directory.GetFiles(Path, $"*{SymbolExtension}");
            foreach (var symbolFile in symbolFiles)
            {
                ReadSymbolFromFile(symbolFile);
            }

            // check if there are symbols without a file, if yes add these
            var asm = typeof(Symbol).Assembly;
            var instanceTypes = (from type in asm.ExportedTypes
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
                var symbol = new Symbol(newType, Guid.NewGuid());
                SymbolRegistry.Entries.Add(symbol.Id, symbol);
                Console.WriteLine($"new added symbol: {newType}");
            }
        }

        public Symbol ReadSymbolFromFile(string symbolFile)
        {
            Log.Info($"file: {symbolFile}");
            using (var sr = new StreamReader(symbolFile))
            using (var jsonReader = new JsonTextReader(sr))
            {
                Json json = new Json();
                json.Reader = jsonReader;
                var symbol = json.ReadSymbol(this);
                if (symbol != null)
                {
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
            Json json = new Json();
            // store all symbols in corresponding files
            foreach (var symbolEntry in SymbolRegistry.Entries)
            {
                using (var sw = new StreamWriter(Path + symbolEntry.Value.Name + "_" + symbolEntry.Value.Id + SymbolExtension))
                using (var writer = new JsonTextWriter(sw))
                {
                    json.Writer = writer;
                    json.Writer.Formatting = Formatting.Indented;
                    json.WriteSymbol(symbolEntry.Value);
                }
            }
        }
    }
}