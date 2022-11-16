using T3.Editor.Gui.Interaction.Variations.Model;

namespace T3.Editor.Gui.Commands.Variations
{
    public class DeleteVariationCommand : ICommand
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
            _variationPool.Variations.Add(_originalVariation); // Warning this will change list order and NOT trigger saving the restored presets
        }

        public void Do()
        {
            _variationPool.Variations.Remove(_originalVariation);
        }
    }
}