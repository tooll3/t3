using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Graph;

namespace T3.Gui.Commands
{
    public class CopySymbolChildCommand : ICommand
    {
        public string Name => "Add Symbol Child";
        public bool IsUndoable => true;
        public Guid AddedChildId => _addedChildId;

        public CopySymbolChildCommand(SymbolUi sourceCompositionUi, Guid symbolChildIdToCopy, SymbolUi targetCompositionUi)
        {
            _sourceSymbolId = sourceCompositionUi.Symbol.Id;
            _targetSymbolId = targetCompositionUi.Symbol.Id;
            _childIdToCopy = symbolChildIdToCopy;
            _addedChildId = Guid.NewGuid();
        }

        public void Undo()
        {
            var parentSymbolUi = SymbolUiRegistry.Entries[_targetSymbolId];
            parentSymbolUi.RemoveChild(_addedChildId);
        }

        public void Do()
        {
            var targetCompositionSymbolUi = SymbolUiRegistry.Entries[_targetSymbolId];
            var sourceCompositionSymbolUi = SymbolUiRegistry.Entries[_sourceSymbolId];
            SymbolChild symbolChildToCopy = sourceCompositionSymbolUi.Symbol.Children.Find(child => child.Id == _childIdToCopy);
            var symbolToAdd = SymbolRegistry.Entries[symbolChildToCopy.Symbol.Id];
            targetCompositionSymbolUi.AddChild(symbolToAdd, _addedChildId, PosOnCanvas, Size, IsVisible);
            var targetSymbol = targetCompositionSymbolUi.Symbol;
            SymbolChild newSymbolChild = targetSymbol.Children.Find(child => child.Id == _addedChildId);
            var newSymbolInputs = newSymbolChild.InputValues;
            foreach (var input in symbolChildToCopy.InputValues)
            {
                var newInput = newSymbolInputs[input.Key];
                newInput.Value.Assign(input.Value.Value.Clone());
                newInput.IsDefault = input.Value.IsDefault;
            }
        }

        // core data
        // private readonly Guid _sourceSymbolId;
        private readonly Guid _childIdToCopy;
        private readonly Guid _addedChildId;
        private readonly Guid _sourceSymbolId;
        private readonly Guid _targetSymbolId;

        // ui data
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = GraphCanvas.DefaultOpSize;
        public bool IsVisible { get; set; } = true;
    }
}