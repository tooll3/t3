#nullable enable
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.MagGraph.Ui.Interaction;
using T3.Editor.Gui.MagGraph.Ui.Interaction.States;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed class GraphUiContext
{
    public GraphUiContext(NodeSelection selection, MagGraphCanvas canvas, Instance compositionOp)
    {
        Selection = selection;
        Canvas = canvas;
        CompositionOp = compositionOp;
        ItemMovement = new MagItemMovement(this, canvas, Layout, selection);
        StateMachine = new StateMachine(this);
    }

    public readonly MagGraphCanvas Canvas;
    public readonly MagItemMovement ItemMovement;
    public readonly MagGraphLayout Layout = new();

    public readonly NodeSelection Selection;
    public readonly Instance CompositionOp;
    public readonly StateMachine StateMachine;
    public static MacroCommand? MacroCommand;
    public static ModifyCanvasElementsCommand? _moveElementsCommand;
    public MagGraphItem? LastHoveredItem { get; set; }
}