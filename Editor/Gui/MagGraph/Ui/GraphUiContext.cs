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
    public GraphUiContext(NodeSelection selector, MagGraphCanvas canvas, Instance compositionOp)
    {
        Selector = selector;
        Canvas = canvas;
        CompositionOp = compositionOp;
        ItemMovement = new MagItemMovement(this, canvas, Layout, selector);
        StateMachine = new StateMachine(this);
    }

    public readonly MagGraphCanvas Canvas;
    public readonly MagItemMovement ItemMovement;
    public readonly MagGraphLayout Layout = new();

    public readonly NodeSelection Selector;
    public readonly Instance CompositionOp;
    public readonly StateMachine StateMachine;
    public  MacroCommand? MacroCommand;
    public  ModifyCanvasElementsCommand? MoveElementsCommand;
    public MagGraphItem? ActiveItem { get; set; }
}