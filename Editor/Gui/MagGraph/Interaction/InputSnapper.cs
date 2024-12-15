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
            ImGui.GetWindowDrawList().AddCircle(context.Canvas.TransformPosition(BestInputMatch.PosOnScreen), 20, Color.Red);
        }
    }

    public static void RegisterAsPotentialTargetInput(MagGraphItem item, Vector2 posOnScreen, Guid slotId,
                                                      InputSnapTypes inputSnapType = InputSnapTypes.Normal, int multiInputIndex = 0)
    {
        var distance = Vector2.Distance(posOnScreen, ImGui.GetMousePos());
        if (distance < _bestInputMatchForCurrentFrame.Distance)
        {
            _bestInputMatchForCurrentFrame = new InputMatch(item, slotId, posOnScreen, inputSnapType, multiInputIndex, distance);
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

        Debug.Assert(context.TempConnections.Count == 1);

        var tempConnection = context.TempConnections[0];

        // TODO: Use snap type...
        // Create connection
        var connectionToAdd = new Symbol.Connection(tempConnection.SourceItem.Id,
                                                    tempConnection.SourceOutput.Id,
                                                    BestInputMatch.Item.Id,
                                                    BestInputMatch.SlotId);

        if (Structure.CheckForCycle(context.CompositionOp.Symbol, connectionToAdd))
        {
            Log.Debug("Sorry, this connection would create a cycle.");
            return false;
        }

        var multiInputIndex = BestInputMatch.MultiInputIndex;
        if (BestInputMatch.InputSnapType == InputSnapTypes.InsertAfterMultiInput)
        {
            context.MacroCommand!.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                             connectionToAdd,
                                                                             multiInputIndex + 1));
        }
        else if (BestInputMatch.InputSnapType == InputSnapTypes.ReplaceMultiInput)
        {
            context.MacroCommand!.AddAndExecCommand(new DeleteConnectionCommand(context.CompositionOp.Symbol,
                                                                                connectionToAdd,
                                                                                multiInputIndex));

            context.MacroCommand!.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                             connectionToAdd,
                                                                             multiInputIndex));
        }
        else
        {
            context.MacroCommand!.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                             connectionToAdd,
                                                                             multiInputIndex ));
        }

        return true;
    }

    public enum InputSnapTypes
    {
        Normal,
        InsertBeforeMultiInput,
        ReplaceMultiInput,
        InsertAfterMultiInput,
    }

    public sealed record InputMatch(
        MagGraphItem? Item = null,
        Guid SlotId = default,
        Vector2 PosOnScreen = default,
        InputSnapTypes InputSnapType = InputSnapTypes.Normal,
        int MultiInputIndex = 0,
        float Distance = OutputSnapper.SnapThreshold);

    private static InputMatch _bestInputMatchForCurrentFrame = new();
    public static InputMatch BestInputMatch = new();
}