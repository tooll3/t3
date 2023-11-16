using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.ResearchCanvas.Model;

public class SnapGraphItem : ISelectableCanvasObject
{
    public Guid Id { get; init; }
    public readonly Type PrimaryType = typeof(float);
    public Vector2 PosOnCanvas { get; set; }
    public Vector2 Size { get; set; }
    public bool IsSelected { get; }
    public SnapGroup SnapGroup;
    public float UnitHeight => VisibleInputSockets.Count + VisibleOutputSockets.Count + 1;

    public override string ToString()
    {
        return SymbolChild.ReadableName;
    }

    // Caching definition references
    public SymbolUi SymbolUi;
    public SymbolChild SymbolChild;
    public SymbolChildUi SymbolChildUi;
    public Instance Instance;

    public readonly List<InSocket> VisibleInputSockets = new(4);
    public readonly List<OutSocket> VisibleOutputSockets = new(1);

    // public Vector2 GetConnectionTargetPosition(Symbol.Connection connection)
    // {
    //     // Todo: Check zero index for primary inputs
    //     for (var index = 0; index < VisibleInputSockets.Count; index++)
    //     {
    //         var input = VisibleInputSockets[index];
    //         if(input.Input.Id == connection.TargetSlotId)
    //             return SymbolChildUi.PosOnCanvas + new Vector2(0, index * GridSize.Y);
    //             
    //     }
    //
    //     Debug.Assert(false, "Target input should have been visible?");
    //     return Vector2.Zero;
    // }
    //
    // public Vector2 GetConnectionSourcePosition(Symbol.Connection connection)
    // {
    //     // TODO: implement
    //     return Vector2.Zero;
    // }

    public struct InSocket
    {
        public IInputSlot Input;
        public IInputUi InputUi;
    }

    public struct OutSocket
    {
        public ISlot Output;
        public IOutputUi OutputUi;
    }

    public static readonly Vector2 GridSize = new Vector2(80, 20);
}