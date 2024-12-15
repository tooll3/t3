#nullable enable
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// Handles snapping to input and output connections
/// </summary>
internal static class InputSnapper
{
    public static void Update(GraphUiContext context)
    {
        BestInputMatch = _bestInputMatchForCurrentFrame;
        _bestInputMatchForCurrentFrame = new InputMatch();
        
        if (context.StateMachine.CurrentState != GraphStates.HoldingConnectionEnd)
            return;
        

        if (BestInputMatch.Item != null)
        {
            // TODO: Make beautiful
            ImGui.GetWindowDrawList().AddCircle(context.Canvas.TransformPosition(BestInputMatch.Anchor.PositionOnCanvas), 20, Color.Red);
        }
    }

    public static void RegisterAsPotentialTargetInput(GraphUiContext context, MagGraphItem item, MagGraphItem.AnchorPoint inputAnchor)
    {
        var posOnScreen = context.Canvas.TransformPosition(inputAnchor.PositionOnCanvas);
        var distance = Vector2.Distance(posOnScreen, ImGui.GetMousePos());
        if (distance < _bestInputMatchForCurrentFrame.Distance)
        {
            _bestInputMatchForCurrentFrame = new InputMatch(item, inputAnchor, distance);
        }
    }

    public static bool TryToReconnect(GraphUiContext context)
    {
        if (BestInputMatch.Item == null)
            return false;

        if (context.TempConnections.Count == 0)
            return false;

        if (context.MacroCommand == null)
        {
            context.StartMacroCommand("Create connection");
        }

        //Debug.Assert(context.MacroCommand != null);

        var didSomething = false;
        foreach (var c in context.TempConnections)
        {
            // Create connection
            var connectionToAdd = new Symbol.Connection(c.SourceItem.Id,
                                                        c.SourceOutput.Id,
                                                        BestInputMatch.Item.Id,
                                                        BestInputMatch.Anchor.SlotId);

            if (Structure.CheckForCycle(context.CompositionOp.Symbol, connectionToAdd))
            {
                Log.Debug("Sorry, this connection would create a cycle.");
                continue;
            }
            
            context.MacroCommand!.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                            connectionToAdd,
                                                                            c.MultiInputIndex));
            didSomething = true;
        }

        return didSomething;
    }
    
    public sealed record InputMatch(MagGraphItem? Item = null, 
                                    MagGraphItem.AnchorPoint Anchor = default, 
                                    float Distance = OutputSnapper.SnapThreshold);

    private static InputMatch _bestInputMatchForCurrentFrame = new();
    public static InputMatch BestInputMatch = new();
}