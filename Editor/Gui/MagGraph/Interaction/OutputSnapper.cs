#nullable enable
using System.Diagnostics;
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
internal static class OutputSnapper
{
    public static void Update(GraphUiContext context)
    {
        if (context.StateMachine.CurrentState != GraphStates.HoldingConnectionBeginning)
            return;

        BestOutputMatch = _bestOutputMatchForCurrentFrame;
        _bestOutputMatchForCurrentFrame = new OutputMatch();

        if (BestOutputMatch.Item != null)
        {
            ImGui.GetWindowDrawList().AddCircle(context.Canvas.TransformPosition(BestOutputMatch.Anchor.PositionOnCanvas), 20, Color.Red);
        }
    }

    public static void RegisterAsPotentialTargetOutput(GraphUiContext context, MagGraphItem item, MagGraphItem.AnchorPoint outputAnchor)
    {
        var posOnScreen = context.Canvas.TransformPosition(outputAnchor.PositionOnCanvas);
        var distance = Vector2.Distance(posOnScreen, ImGui.GetMousePos());
        if (distance < _bestOutputMatchForCurrentFrame.Distance)
        {
            _bestOutputMatchForCurrentFrame = new OutputMatch(item, outputAnchor, distance);
        }
    }

    public static bool TryToReconnect(GraphUiContext context)
    {
        if (BestOutputMatch.Item == null)
            return false;

        if (context.TempConnections.Count == 0)
            return false;

        Debug.Assert(context.MacroCommand != null);

        var didSomething = false;
        foreach (var c in context.TempConnections)
        {
            // Create connection
            var connectionToAdd = new Symbol.Connection(BestOutputMatch.Item.Id,
                                                        BestOutputMatch.Anchor.SlotId,
                                                        c.TargetItem.Id,
                                                        c.TargetInput.Id);

            if (Structure.CheckForCycle(context.CompositionOp.Symbol, connectionToAdd))
            {
                Log.Debug("Sorry, this connection would create a cycle.");
                continue;
            }
            
            context.MacroCommand.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                            connectionToAdd,
                                                                            c.MultiInputIndex));
            didSomething = true;
        }

        return didSomething;
    }

    public const float SnapThreshold = 100;

    public sealed record OutputMatch(MagGraphItem? Item = null, MagGraphItem.AnchorPoint Anchor = default, float Distance = SnapThreshold);

    private static OutputMatch _bestOutputMatchForCurrentFrame = new();
    public static OutputMatch BestOutputMatch = new();
}