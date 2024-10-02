using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Variations
{
    public class AddPresetOrVariationCommand : ICommand
    {
        public string Name => "Add Preset";
        public bool IsUndoable => true;
        
        private readonly Symbol _symbol;
        private readonly Variation _newVariation;
        
        public AddPresetOrVariationCommand(Symbol symbol, Variation variation)
        {
            _symbol = symbol;
            _newVariation = variation;
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