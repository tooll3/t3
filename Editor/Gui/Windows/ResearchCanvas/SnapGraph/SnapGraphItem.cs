using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

public class SnapGraphItem : ISelectableCanvasObject
{
    public Guid Id { get; init; }
    public readonly Type PrimaryType = typeof(float);
    public Vector2 PosOnCanvas { get; set; }
    public Vector2 Size { get; set; }
    public bool IsSelected => NodeSelection.IsNodeSelected(SymbolChildUi);
    public SnapGroup SnapGroup;
    public float UnitHeight => VisibleInputSockets.Count + VisibleOutputSockets.Count + 1;

    public override string ToString()
    {
        return SymbolChild.ReadableName;
    }

    public SymbolUi SymbolUi;
    public SymbolChild SymbolChild;
    public SymbolChildUi SymbolChildUi;
    public Instance Instance;

    public readonly List<InSocket> VisibleInputSockets = new(4);
    public readonly List<OutSocket> VisibleOutputSockets = new(1);
    
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