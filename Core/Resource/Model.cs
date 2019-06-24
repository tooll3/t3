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
        public static Dictionary<Type, Func<InputAttribute, InputValue>> Entries { get; } = new Dictionary<Type, Func<InputAttribute, InputValue>>();
    }

    public class Model
    {
        protected string Path { get; } = @"..\Core\Operator\Types\";
        protected string SymbolExtension { get; } = ".t3";

        public Model()
        {
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

            // Register input value creators that take the relevant input attribute, extract the default value and return this with the new input value
            InputValue IntInputValueCreator(InputAttribute inputAttribute) => new InputValue<int>(((IntInputAttribute)inputAttribute).DefaultValue);
            InputValue FloatInputValueCreator(InputAttribute inputAttribute) => new InputValue<float>(((FloatInputAttribute)inputAttribute).DefaultValue);
            InputValue StringInputValueCreator(InputAttribute inputAttribute) => new InputValue<string>(((StringInputAttribute)inputAttribute).DefaultValue);
            InputValue Size2InputValueCreator(InputAttribute inputAttribute) => new InputValue<Size2>(((Size2InputAttribute)inputAttribute).DefaultValue);
            InputValue ResourceUsageInputValueCreator(InputAttribute inputAttribute) => new InputValue<ResourceUsage>(((ResourceUsageInputAttribute)inputAttribute).DefaultValue);
            InputValue FormatInputValueCreator(InputAttribute inputAttribute) => new InputValue<Format>(((FormatInputAttribute)inputAttribute).DefaultValue);
            InputValueCreators.Entries.Add(typeof(int), IntInputValueCreator);
            InputValueCreators.Entries.Add(typeof(float), FloatInputValueCreator);
            InputValueCreators.Entries.Add(typeof(string), StringInputValueCreator);
            InputValueCreators.Entries.Add(typeof(Size2), Size2InputValueCreator);
            InputValueCreators.Entries.Add(typeof(ResourceUsage), ResourceUsageInputValueCreator);
            InputValueCreators.Entries.Add(typeof(Format), FormatInputValueCreator);
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
