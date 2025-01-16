using T3.Editor.Gui.Interaction.Variations.Model;

namespace T3.Editor.UiModel.Commands.Variations;

internal class DeleteVariationCommand : ICommand
{
    public string Name => "Delete Variation";
    public bool IsUndoable => true;
        
    private readonly SymbolVariationPool _variationPool;
    private readonly Variation _originalVariation;
        
    public DeleteVariationCommand(SymbolVariationPool pool, Variation variation)
    {
        _variationPool = pool;
        _originalVariation = variation;
    }
        
    public void Undo()
    {
        #if DEBUG

                _variationPool.AddDefaultVariation(_originalVariation);
        #else
        _variationPool.AddUserVariation(_originalVariation);
        #endif
    }

    public void Do()
    {
        #if DEBUG

                _variationPool.RemoveDefaultVariation(_originalVariation);
        #else
        _variationPool.RemoveUserVariation(_originalVariation);
        #endif
    }
}