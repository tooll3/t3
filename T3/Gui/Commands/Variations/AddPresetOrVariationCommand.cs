using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using t3.Gui.Interaction.Presets;
using t3.Gui.Interaction.Presets.Model;

namespace T3.Gui.Commands
{
    public class AddPresetOrVariationCommand : ICommand
    {
        public string Name => "Add Preset";
        public bool IsUndoable => true;
        
        private Symbol _symbol;
        private Variation _newVariation;
        
        private readonly Dictionary<Variation, Variation> _originalDefForReferences = new Dictionary<Variation, Variation>();
        private readonly Dictionary<Variation, Variation> _newDefForReferences = new Dictionary<Variation, Variation>();

        public AddPresetOrVariationCommand(Symbol symbolUi, Variation annotation)
        {
            _symbol = symbolUi;
            _newVariation = annotation;
        }
        
        public void Do()
        {
            var list = GetVariationsList();

            if(list.Any(v => v.Id == _newVariation.Id))
            {
                Log.Warning($"Variations with id {_newVariation.Id} already exists for symbol {_symbol.Id}");
            }

            list.Add(_newVariation);
            FlagSymbolAsModified();
        }

        public void Undo()
        {
            var list = GetVariationsList();

            if(list.All(v => v.Id != _newVariation.Id))
            {
                Log.Warning($"No variations for symbol {_symbol.Name}  with id {_newVariation.Id}");
                return;
            }
            
            list.Remove(_newVariation);
            FlagSymbolAsModified();
        }
        
        private List<Variation> GetVariationsList()
        {
            var pool = VariationHandling.GetOrLoadVariations(_symbol.Id);
            return pool.Variations;
        }
        
        private void FlagSymbolAsModified()
        {
            var symbolUi = SymbolUiRegistry.Entries[_symbol.Id];
            symbolUi.FlagAsModified();
        }
    }
}