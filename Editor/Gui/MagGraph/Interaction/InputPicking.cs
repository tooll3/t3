using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// Things related to picking a hidden input after dropping a node or connection onto an item
/// </summary>
internal static class InputPicking
{
    internal static bool TryInitializeInputSelectionPickerForDraggedItem(GraphUiContext context)
    {
        if (context.ActiveSourceItem == null)
            return false;
        
        foreach (var otherItem in context.Layout.Items.Values)
        {
            if (otherItem == context.ActiveSourceItem)
                continue;
            
            if (!otherItem.Area.Contains(context.PeekAnchorInCanvas))
                continue;
            
            context.ItemForInputSelection = otherItem;
            context.ShouldAttemptToSnapToInput = true;
            context.PeekAnchorInCanvas = new Vector2(context.ActiveSourceItem.Area.Max.X - MagGraphItem.GridSize.Y * 0.25f,
                                                     context.ActiveSourceItem.Area.Min.Y + MagGraphItem.GridSize.Y * 0.5f);
            return true;
        }
        
        return false;
    }
    
    internal static bool TryInitializeAtPosition(GraphUiContext context, Vector2 peekPosInCanvas)
    {
        if (context.ActiveSourceItem == null)
            return false;
        
        foreach (var otherItem in context.Layout.Items.Values)
        {
            if (otherItem == context.ActiveSourceItem)
                continue;
            
            if (!otherItem.Area.Contains(peekPosInCanvas))
                continue;
            
            context.ItemForInputSelection = otherItem;
            context.PeekAnchorInCanvas = peekPosInCanvas;
            context.ShouldAttemptToSnapToInput = false;
            context.StartOrContinueMacroCommand("Connect operators");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// After snapping picking an hidden input field for connection, this
    /// method can be called to...
    /// - move the dragged items to the snapped position
    /// - move all other relevant snapped items down
    /// - create the connection
    /// </summary>
    private static void TryConnectHiddenInput(GraphUiContext context, IInputUi targetInputUi)
    {
        var composition = context.CompositionOp;
        
        Debug.Assert(context.ActiveSourceItem != null && context.ItemForInputSelection != null);
        Debug.Assert(context.MacroCommand != null);
        Debug.Assert(context.ItemForInputSelection.Variant == MagGraphItem.Variants.Operator); // This will bite us later...
        
        if (context.ActiveSourceItem.OutputLines.Length == 0)
        {
            Log.Warning("no visible output to connect?");
            return;
        }
        
        // Create connection
        var connectionToAdd = new Symbol.Connection(context.ActiveSourceItem.Id,
                                                    context.ActiveSourceItem.OutputLines[0].Id,
                                                    context.ItemForInputSelection.Id,
                                                    targetInputUi.Id);
        
        if (Structure.CheckForCycle(composition.Symbol, connectionToAdd))
        {
            Log.Debug("Sorry, this connection would create a cycle.");
            return;
        }

        var inputConnectionCount = context.CompositionOp.Symbol.Connections.Count(c => c.TargetParentOrChildId == context.ItemForInputSelection.Id
                                                                                       && c.TargetSlotId == targetInputUi.Id);
        
        
         context.MacroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                         connectionToAdd,
                                                                         inputConnectionCount));
        
        
        // Find insertion index
        var inputLineIndex = 0;
        foreach (var input in context.ItemForInputSelection.Instance!.Inputs)
        {
            if (input.Id == targetInputUi.InputDefinition.Id)
                break;
            
            if (inputLineIndex < context.ItemForInputSelection.InputLines.Length
                && input.Id == context.ItemForInputSelection.InputLines[inputLineIndex].Input.Id)
                inputLineIndex++;
        }
        
        if (inputLineIndex > 0)
            MagItemMovement.MoveSnappedItemsVertically(context,
                                                       MagItemMovement.CollectSnappedItems(context.ItemForInputSelection),
                                                       context.ItemForInputSelection.PosOnCanvas.Y + MagGraphItem.GridSize.Y * (inputLineIndex - 0.5f),
                                                       MagGraphItem.GridSize.Y);
        
        if (context.ShouldAttemptToSnapToInput)
        {
            // Snap items to input location (we assume that all dragged items are snapped...)
            var targetPos = context.ItemForInputSelection.PosOnCanvas
                            + new Vector2(-context.ActiveSourceItem.Size.X,
                                          (inputLineIndex) * MagGraphItem.GridSize.Y);
            
            var moveDelta = targetPos - context.ActiveSourceItem.PosOnCanvas;
            
            var affectedItemsAsNodes = context.ItemMovement.DraggedItems.Select(i => i as ISelectableCanvasObject).ToList();
            var newMoveComment = new ModifyCanvasElementsCommand(composition.Symbol.Id, affectedItemsAsNodes, context.Selector);
            context.MacroCommand.AddExecutedCommandForUndo(newMoveComment);
            
            foreach (var item in affectedItemsAsNodes)
            {
                item.PosOnCanvas += moveDelta;
            }
            
            newMoveComment.StoreCurrentValues();
        }
        
        // Complete drag interaction
        context.ItemMovement.Reset();
        context.Layout.FlagAsChanged();
    }
    
    internal static void DrawHiddenInputSelector(GraphUiContext context)
    {
        if (context.ItemForInputSelection == null)
            return;
        
        if (context.StateMachine.CurrentState != GraphStates.PickInput)
            return;
        
        var screenPos = context.Canvas.TransformPosition(context.PeekAnchorInCanvas);
        
        ImGui.SetNextWindowPos(screenPos);
        
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                       | ImGuiWindowFlags.NoMove
                                       | ImGuiWindowFlags.Tooltip // ugly as f**k. Sadly .PopUp will lead to random crashes.
                                       | ImGuiWindowFlags.NoFocusOnAppearing
                                       | ImGuiWindowFlags.NoScrollbar
                                       | ImGuiWindowFlags.AlwaysUseWindowPadding;
        
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 4);
        
        ImGui.PushStyleColor(ImGuiCol.PopupBg, UiColors.BackgroundFull.Fade(0.6f).Rgba);
        if (ImGui.BeginChild("Popup",
                             new Vector2(100, 120),
                             true,
                             flags))
        {
            var childUi = context.ItemForInputSelection.SymbolUi;
            if (childUi != null)
            {
                var inputIndex = 0;
                foreach (var inputUi in childUi.InputUis.Values)
                {
                    var input = context.ItemForInputSelection.Instance!.Inputs[inputIndex];
                    if (inputUi.Type == context.DraggedPrimaryOutputType)
                    {
                        var isConnected = input.HasInputConnections;
                        var prefix = isConnected ? "× " : "   ";
                        if (ImGui.Selectable(prefix + inputUi.InputDefinition.Name))
                        {
                            TryConnectHiddenInput(context, inputUi);
                            context.CompleteMacroCommand();
                            
                            Reset(context);
                            context.StateMachine.SetState(GraphStates.Default, context);
                        }
                    }
                    
                    inputIndex++;
                }
            }
            
            // Cancel by clicking outside
            var isPopupHovered = ImRect.RectWithSize(ImGui.GetWindowPos(), ImGui.GetWindowSize())
                                       .Contains(ImGui.GetMousePos());
            
            if (!isPopupHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                context.StateMachine.SetState(GraphStates.Default, context);
            }
            
            ImGui.PopStyleVar(1);
        }
        
        ImGui.EndChild();
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
    }
    
    public static void Reset(GraphUiContext context)
    {
        context.TempConnections.Clear();
        context.ActiveSourceItem = null;
        context.DraggedPrimaryOutputType = null;
    }
}