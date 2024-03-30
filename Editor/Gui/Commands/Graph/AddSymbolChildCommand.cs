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
            if(!SymbolUiRegistry.TryGetSymbolUi(_parentSymbolId, out var parentSymbolUi))
            {
                Log.Warning($"Could not find symbol with id {_parentSymbolId} - was it removed?");
                return;
            }
            
            parentSymbolUi!.RemoveChild(_addedChildId);
        }

        public void Do()
        {
            if(!SymbolUiRegistry.TryGetSymbolUi(_parentSymbolId, out var parentSymbolUi))
            {
                Log.Warning($"Could not find symbol with id {_parentSymbolId} - was it removed?");
                return;
            }
            
            if(!SymbolUiRegistry.TryGetSymbolUi(_addedSymbolId, out var symbolToAdd))
            {
                Log.Warning($"Could not find symbol with id {_addedSymbolId} - was it removed?");
                return;
            }
            
            parentSymbolUi!.AddChild(symbolToAdd!.Symbol, _addedChildId, PosOnCanvas, Size, ChildName);
        }

        // core data
        private readonly Guid _parentSymbolId;
        private readonly Guid _addedSymbolId;
        private readonly Guid _addedChildId;

        // ui data
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = SymbolUi.Child.DefaultOpSize;
        public string ChildName { get; set; } = string.Empty;
    }
}