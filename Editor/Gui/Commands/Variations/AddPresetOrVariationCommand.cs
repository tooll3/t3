using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Variations;

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
        var pool = VariationHandling.GetOrLoadVariations(_symbol.Id);

        if (pool.AllVariations.Any(v => v.Id == _newVariation.Id))
        {
            Log.Warning($"Variations with id {_newVariation.Id} already exists for symbol {_symbol.Id}");
            return;
        }

        #if DEBUG

                pool.AddDefaultVariation(_newVariation);
        #else
        pool.AddUserVariation(_newVariation);
        #endif
            
        FlagSymbolAsModified();
    }

    public void Undo()
    {
        var pool = VariationHandling.GetOrLoadVariations(_symbol.Id);

        if (pool.AllVariations.All(v => v.Id != _newVariation.Id))
        {
            Log.Warning($"No variations for symbol {_symbol.Name}  with id {_newVariation.Id}");
            return;
        }


        #if DEBUG

                pool.RemoveDefaultVariation(_newVariation);
        #else
        pool.RemoveUserVariation(_newVariation);
        #endif
        FlagSymbolAsModified();
    }

    private void FlagSymbolAsModified()
    {
        if (!SymbolUiRegistry.TryGetSymbolUi(_symbol.Id, out var symbolUi))
        {
            Log.Warning($"Could not find symbol with id {_symbol.Id} - was it removed?");
            return;
        }
            
        symbolUi!.FlagAsModified();
    }
}