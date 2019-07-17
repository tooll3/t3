using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Graph;

namespace T3.Gui.Commands
{
    public class AddSymbolChildCommand : ICommand
    {
        public string Name => "Add Symbol Child";
        public bool IsUndoable => true;
        public Guid AddedChildId => _addedChildId;

        public AddSymbolChildCommand(Symbol compositionOp, Guid symbolIdToAdd)
        {
            _parentSymbolId = compositionOp.Id;
            _addedSymbolId = symbolIdToAdd;
            _addedChildId = Guid.NewGuid();
        }

        public void Undo()
        {
            var parentSymbolUi = SymbolUiRegistry.Entries[_parentSymbolId];
            parentSymbolUi.RemoveChild(_addedChildId);
        }

        public void Do()
        {
            var parentSymbolUi = SymbolUiRegistry.Entries[_parentSymbolId];
            var symbolToAdd = SymbolRegistry.Entries[_addedSymbolId];
            _addedChildId = parentSymbolUi.AddChild(symbolToAdd, PosOnCanvas, Size, IsVisible);
        }

        // core data
        private readonly Guid _parentSymbolId;
        private readonly Guid _addedSymbolId;
        private Guid _addedChildId;
        
        // ui data
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = GraphCanvas.DefaultOpSize;
        public bool IsVisible { get; set; } = true;
    }
}
