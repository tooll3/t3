using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;

namespace t3.Gui.Interaction.Presets.Model
{
    /// <summary>
    /// Base class for serialization 
    /// </summary>
    public class VariationModel
    {
        public Guid Id;
        public string Title;
        public int ActivationIndex;
        public bool IsPreset;
        public DateTime PublishedDate;

        /// <summary>
        /// Changes by SymbolChildId
        /// </summary>
        public Dictionary<Guid, Dictionary<Guid, InputValue>> InputValuesForChildIds;

        public static VariationModel FromJson(Guid symbolId, JToken jToken)
        {
            if (!SymbolRegistry.Entries.TryGetValue(symbolId, out var compositionSymbol))
                return null;
            
            var newVariation = new VariationModel
                                   {
                                       Id = Guid.Parse(jToken["Id"].Value<string>()),
                                       Title = jToken["Title"].Value<string>(),
                                       ActivationIndex = jToken["ActivationIndex"].Value<int>(),
                                       IsPreset = jToken["IsPreset"].Value<bool>(),
                                   };

            newVariation.InputValuesForChildIds = new Dictionary<Guid, Dictionary<Guid, InputValue>>();

            var changesToken = (JObject)jToken["InputValuesForChildIds"];
            if (changesToken == null)
                return newVariation;
            
            foreach (var (symbolChildIdString, changes2)  in changesToken)
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
                    Log.Warning($"Can't find symbol {symbolChildIdString} for preset changes");
                    continue;
                }
                
                foreach (var (inputIdString, valueToken)  in o)
                {
                    var inputId = Guid.Parse(inputIdString);
                    var input = symbolForChanges.InputDefinitions.SingleOrDefault(i => i.Id == inputId);
                    if (input == null)
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
                     newVariation.InputValuesForChildIds[symbolChildId] = changeList;
                }
            }

            return newVariation;
        }
        
        public override string ToString()
        {
            return $"{Title} #{ActivationIndex}";
        }
    }
}