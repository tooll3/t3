using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
                Writer.WriteValue("Type", inputUi.Type + $", {inputUi.Type.Assembly.GetName().Name}");
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
    }
}