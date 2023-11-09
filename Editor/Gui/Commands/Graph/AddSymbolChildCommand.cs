using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Graph
{
    public class AddSymbolChildCommand : ICommand
    {
        public string Name => "Add Symbol Child";
        public bool IsUndoable => true;
        public Guid AddedChildId => _addedChildId;

        public AddSymbolChildCommand(Symbol compositionOp, Guid symbolIdToAdd)
        {
            if (compositionOp == null)
                return;

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
            parentSymbolUi.AddChild(symbolToAdd, _addedChildId, PosOnCanvas, Size, ChildName);
        }

        // core data
        private readonly Guid _parentSymbolId;
        private readonly Guid _addedSymbolId;
        private readonly Guid _addedChildId;

        // ui data
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = SymbolChildUi.DefaultOpSize;
        public string ChildName { get; set; } = string.Empty;
    }
}