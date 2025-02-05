using System.Diagnostics;
using ImGuiNET;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.Commands.Graph;
using T3.Editor.UiModel.InputsAndTypes;
using T3.Editor.UiModel.ProjectHandling;
using T3.Editor.UiModel.Selection;

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
        var composition = context.CompositionInstance;
        
        Debug.Assert(context.ActiveSourceItem != null && context.ItemForInputSelection != null);
        Debug.Assert(context.MacroCommand != null);
        Debug.Assert(context.ItemForInputSelection.Variant == MagGraphItem.Variants.Operator); // This will bite us later...
        
        if (context.ActiveSourceItem.OutputLines.Length == 0)
        {
            Log.Warning("no visible output to connect?");
            return;
        }
        
        // Create connection

        var sourceParentOrChildId = context.ActiveSourceItem.Variant == MagGraphItem.Variants.Input ? Guid.Empty : context.ActiveSourceItem.Id;
        var connectionToAdd = new Symbol.Connection(sourceParentOrChildId,
                                                    context.ActiveSourceOutputId,
                                                    context.ItemForInputSelection.Id,
                                                    targetInputUi.Id);
        
        if (Structure.CheckForCycle(composition.Symbol, connectionToAdd))
        {
            Log.Debug("Sorry, this connection would create a cycle.");
            return;
        }

        var inputConnectionCount = context.CompositionInstance.Symbol.Connections.Count(c => c.TargetParentOrChildId == context.ItemForInputSelection.Id
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
        
        var isInputLineNotConnected = context.ItemForInputSelection.InputLines[inputLineIndex].ConnectionIn == null;
        
        if (!isInputLineNotConnected && inputLineIndex > 0)
            MagItemMovement.MoveSnappedItemsVertically(context,
                                                       MagItemMovement.CollectSnappedItems(context.ItemForInputSelection),
                                                       context.ItemForInputSelection.PosOnCanvas.Y + MagGraphItem.GridSize.Y * (inputLineIndex + 0.5f),
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
        if (context.ItemForInputSelection == null || context.DraggedPrimaryOutputType == null)
        {
            return;
        }

        if (context.StateMachine.CurrentState != GraphStates.PickInput)
        {
            return;
        }
        
        var screenPos = context.Canvas.TransformPosition(context.PeekAnchorInCanvas);
        
        ImGui.SetNextWindowPos(screenPos);

        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 4);
        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0,0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
        //ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Color.Transparent.Rgba);
        var lastSize = WindowContentExtend.GetLastAndReset(); 
                   // + ImGui.GetStyle().WindowPadding * 2
                   // + new Vector2(10,3);
        
        //lastSize.Y = lastSize.Y.Clamp(0f,300f);

        
        ImGui.PushStyleColor(ImGuiCol.ChildBg, UiColors.BackgroundFull.Fade(0.7f).Rgba);
        ImGui.PushStyleColor(ImGuiCol.Button, UiColors.BackgroundFull.Fade(0.0f).Rgba);
        
        //ImGui.PushStyleColor(ImGuiCol.PopupBg, UiColors.BackgroundFull.Fade(0.6f).Rgba);
        if (ImGui.BeginChild("Popup",
                             lastSize,
                             true,
                             ImGuiWindowFlags.NoResize
                             | ImGuiWindowFlags.NoScrollbar
                             | ImGuiWindowFlags.AlwaysUseWindowPadding
                             ))
        {
            var childUi = context.ItemForInputSelection.SymbolUi;
            if (childUi != null)
            {
                if (context.DraggedPrimaryOutputType == null || !TypeNameRegistry.Entries.TryGetValue(context.DraggedPrimaryOutputType, out var typeName))
                {
                    typeName = context.DraggedPrimaryOutputType.Name;
                }

                ImGui.PushFont(Fonts.FontSmall);
                ImGui.TextColored(UiColors.TextMuted, typeName + " inputs");
                ImGui.PopFont();                
                
                var inputIndex = 0;
                foreach (var inputUi in childUi.InputUis.Values)
                {

                    
                    var input = context.ItemForInputSelection.Instance!.Inputs[inputIndex];
                    if (inputUi.Type == context.DraggedPrimaryOutputType)
                    {
                        //var parameterHelp = "";
                        
                        var isConnected = input.HasInputConnections;
                        var prefix = isConnected ? "× " : "   ";
                        var inputDefinitionName = prefix + inputUi.InputDefinition.Name;
                        var labelSize = ImGui.CalcTextSize(inputDefinitionName);
                        var width = MathF.Max(lastSize.X  -4, labelSize.X +30);
                        var buttonSize = new Vector2(width, ImGui.GetFrameHeight());
                        
                        if (ImGui.Button(inputDefinitionName, buttonSize))
                        {
                            TryConnectHiddenInput(context, inputUi);
                            context.CompleteMacroCommand();
                            
                            Reset(context);
                            context.StateMachine.SetState(GraphStates.Default, context);
                        }

                        if (!string.IsNullOrEmpty(inputUi.Description))
                        {
                            CustomComponents.TooltipForLastItem(inputUi.Description);
                        }
                        
                        WindowContentExtend.ExtendToLastItem();
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
        ImGui.PopStyleVar(5);
        ImGui.PopStyleColor(2);
    }
    
    public static void Reset(GraphUiContext context)
    {
        context.TempConnections.Clear();
        context.ActiveSourceItem = null;
        context.DraggedPrimaryOutputType = null;
    }
    
    public static void Init(GraphUiContext context)
    {
        WindowContentExtend.GetLastAndReset();
    }
}