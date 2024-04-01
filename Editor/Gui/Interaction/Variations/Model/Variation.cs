using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Windows.Variations;

namespace T3.Editor.Gui.Interaction.Variations.Model
{
    /// <summary>
    /// Base class for serialization of presents and snapshots
    /// </summary>/
    public class Variation : ISelectableCanvasObject
    {
        // Serialized fields...
        public Guid Id { get; init; }
        public string Title;
        public int ActivationIndex;
        public bool IsPreset;
        
        public Vector2 PosOnCanvas  { get; set; }
        public Vector2 Size  { get; set; } = VariationThumbnail.ThumbnailSize;
        public DateTime PublishedDate;
        
        // Other properties...
        public bool IsSelected { get; set; }
        public States State { get; set; } = States.InActive;
        public bool IsSnapshot => !IsPreset;

        /// <summary>
        /// Changes by SymbolChildId
        /// </summary>
        public Dictionary<Guid, Dictionary<Guid, InputValue>> ParameterSetsForChildIds;

        public static Variation FromJson(Guid symbolId, JToken jToken)
        {
            if (!SymbolRegistry.Entries.TryGetValue(symbolId, out var compositionSymbol))
                return null;

            var idToken = jToken[nameof(Id)];

            var idString = idToken?.Value<string>();
            if (idString == null)
                return null;

            var newVariation = new Variation
                                   {
                                       Id = Guid.Parse(idString),
                                       Title = jToken[nameof(Title)]?.Value<string>() ?? String.Empty,
                                       ActivationIndex = jToken[nameof(ActivationIndex)]?.Value<int>() ?? -1,
                                       IsPreset = jToken[nameof(IsPreset)]?.Value<bool>() ?? false,
                                       ParameterSetsForChildIds = new Dictionary<Guid, Dictionary<Guid, InputValue>>(),
                                       
                                   };
            
            
            var positionToken = jToken[nameof(PosOnCanvas)];
            if (positionToken != null)
            {
                newVariation.PosOnCanvas = new Vector2(positionToken["X"]?.Value<float>() ?? 0, 
                                                    positionToken["Y"]?.Value<float>() ?? 0);
            }

            
            var changesToken = (JObject)jToken[nameof(ParameterSetsForChildIds)];
            if (changesToken == null)
                return newVariation;

            foreach (var (symbolChildIdString, changes2) in changesToken)
            {
                var changeList = new Dictionary<Guid, InputValue>();
                if (changes2 is not JObject o)
                    continue;

                var symbolChildId = Guid.Parse(symbolChildIdString);

                var symbolForChanges = symbolChildId == Guid.Empty
                                           ? compositionSymbol
                                           : compositionSymbol.Children.SingleOrDefault(c => c.Id == symbolChildId)?.Symbol;

                if (symbolForChanges == null)
                {
                    //Log.Warning($"Can't find symbol {symbolChildIdString} for preset changes");
                    continue;
                }

                foreach (var (inputIdString, valueToken) in o)
                {
                    var inputId = Guid.Parse(inputIdString);
                    var input = symbolForChanges.InputDefinitions.SingleOrDefault(i => i.Id == inputId);
                    if (input == null && inputId != Guid.Empty)
                    {
                        Log.Warning($"Can't find symbol input {symbolChildIdString}/{inputId} for preset changes");
                        continue;
                    }

                    var inputValue = InputValueCreators.Entries[input.DefaultValue.ValueType]();
                    inputValue.SetValueFromJson(valueToken);
                    changeList[inputId] = inputValue;
                }

                if (changeList.Count > 0)
                {
                    newVariation.ParameterSetsForChildIds[symbolChildId] = changeList;
                }
            }

            return newVariation;
        }

        public void ToJson(JsonTextWriter writer)
        {
            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];
            
            //writer.WritePropertyName(Id.ToString());
            writer.WriteStartObject();
            {
                writer.WriteValue(nameof(Id), Id);
                writer.WriteValue(nameof(IsPreset), IsPreset);
                writer.WriteValue(nameof(ActivationIndex), ActivationIndex);
                writer.WriteObject(nameof(Title), Title);
                
                writer.WritePropertyName(nameof(PosOnCanvas));
                vec2Writer(writer, PosOnCanvas);
                
                writer.WritePropertyName(nameof(ParameterSetsForChildIds));
                writer.WriteStartObject();
                {
                    foreach (var (id, values) in ParameterSetsForChildIds)
                    {
                        writer.WritePropertyName(id.ToString());
                        writer.WriteStartObject();
                        foreach (var (inputId, value) in values)
                        {
                            writer.WritePropertyName(inputId.ToString());
                            value.ToJson(writer);
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        public override string ToString()
        {
            return $"{Title} #{ActivationIndex}";
        }
        
        public enum States
        {
            Undefined,
            InActive,
            Active,
            Modified,
            IsBlended,
        }
    }
}