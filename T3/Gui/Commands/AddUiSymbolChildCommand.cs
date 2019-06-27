using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Graph;

namespace T3.Gui.Commands
{
    public class AddUiSymbolChildCommand : Core.Commands.AddSymbolChildCommand
    {
        public AddUiSymbolChildCommand(Symbol compositionOp, Guid symbolIdToAdd)
            : base(compositionOp, symbolIdToAdd)
        {
        }

        public override void Undo()
        {
            SymbolChildUiRegistry.Entries[_parentSymbolId].Remove(AddedInstanceId);

            base.Undo();
        }

        public override void Do()
        {
            base.Do();

            var uiEntriesForChildrenOfSymbol = SymbolChildUiRegistry.Entries[_parentSymbolId];
            var parentSymbol = SymbolRegistry.Entries[_parentSymbolId];
            uiEntriesForChildrenOfSymbol.Add(AddedInstanceId, new SymbolChildUi
                                                              {
                                                                  SymbolChild = parentSymbol.Children.Find(entry => entry.Id == AddedInstanceId),
                                                                  PosOnCanvas = PosOnCanvas,
                                                                  Size = Size,
                                                                  IsVisible = IsVisible
                                                              });
        }

        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = GraphCanvas.DefaultOpSize;
        public bool IsVisible { get; set; } = true;
    }
}
