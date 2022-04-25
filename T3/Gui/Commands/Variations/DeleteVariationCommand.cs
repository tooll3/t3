using T3.Core.Logging;
using T3.Gui.Commands;
using t3.Gui.Interaction.Variations.Model;

namespace t3.Gui.Commands.Variations
{
    public class DeleteVariationCommand : ICommand
    {
        public string Name => "Delete Variation";
        public bool IsUndoable => true;
        
        private readonly SymbolVariationPool _variationPool;
        private readonly Variation _originalVariation;
        private readonly int _index;
        
        public DeleteVariationCommand(SymbolVariationPool pool, Variation variation)
        {
            _variationPool = pool;
            _originalVariation = variation;
            _index = _variationPool.Variations.IndexOf(variation);
            if(_index == -1)
                Log.Warning("Can't find variation to delete?");
        }
        
        public void Undo()
        {
            _variationPool.Variations.Insert(_index, _originalVariation);
        }

        public void Do()
        {
            _variationPool.Variations.RemoveAt(_index);
        }
    }
}