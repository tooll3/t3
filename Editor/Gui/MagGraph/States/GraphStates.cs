using System.Diagnostics;
using ImGuiNET;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using MagItemMovement = T3.Editor.Gui.MagGraph.Interaction.MagItemMovement;

namespace T3.Editor.Gui.MagGraph.States;

internal static class GraphStates
{
    internal static State Default
        = new(
              Enter: context =>
                     {
                         // Todo: this should be a reset method in context
                         context.TempConnections.Clear();
                         context.PrimaryOutputItem = null;
                         context.DraggedPrimaryOutputType = null;
                         context.Placeholder?.Reset(context);
                     },
              Update: context =>
                      {
                          // Check keyboard commands if focused...
                          if (context.Canvas.IsFocused && context.Canvas.IsHovered && !ImGui.IsAnyItemActive())
                          {
                              // Tab create placeholder
                              if (ImGui.IsKeyReleased(ImGuiKey.Tab))
                              {
                                  var focusedItem =
                                      context.Selector.Selection.Count == 1 &&
                                      context.Canvas.IsItemVisible(context.Selector.Selection[0])
                                          ? context.Selector.Selection[0]
                                          : null;

                                  if (focusedItem != null)
                                  {
                                      context.Placeholder.OpenForItem(context, focusedItem);
                                  }
                                  else
                                  {
                                      var posOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
                                      context.Placeholder.OpenOnCanvas(context, posOnCanvas);
                                  }

                                  context.StateMachine.SetState(Placeholder, context);
                              }

                              else if (ImGui.IsKeyReleased(ImGuiKey.Delete) || ImGui.IsKeyReleased(ImGuiKey.Backspace))
                              {
                                  Modifications.DeleteSelectedOps(context);
                              }
                          }

                          // Mouse click
                          var clickedDown = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
                          if (!clickedDown)
                              return;

                          if (context.ActiveItem == null)
                          {
                              context.StateMachine.SetState(HoldBackground, context);
                          }
                          else
                          {
                              if (context.ActiveOutputId == Guid.Empty)
                              {
                                  context.StateMachine.SetState(HoldItem, context);
                              }
                              else
                              {
                                  context.StateMachine.SetState(HoldOutput, context);
                              }
                          }
                      },
              Exit: _ => { }
             );

    /// <summary>
    /// Active while long tapping on background for insertion
    /// </summary>
    internal static State HoldBackground
        = new(
              Enter: _ => { },
              Update: context =>
                      {
                          if (!ImGui.IsMouseDown(ImGuiMouseButton.Left)
                              || !context.Canvas.IsFocused
                              || ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                          {
                              context.StateMachine.SetState(Default, context);
                              return;
                          }

                          const float longTapDuration = 0.3f;
                          var longTapProgress = context.StateMachine.StateTime / longTapDuration;
                          MagItemMovement.UpdateLongPressIndicator(longTapProgress);

                          if (!(longTapProgress > 1))
                              return;

                          // TODO: setting both, state and placeholder, feels awkward.
                          context.StateMachine.SetState(Placeholder, context);
                          var posOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
                          context.Placeholder.OpenOnCanvas(context, posOnCanvas);
                      },
              Exit: _ => { }
             );

    internal static State Placeholder
        = new(
              Enter: _ => { },
              Update: context =>
                      {
                          if (context.Placeholder.PlaceholderItem != null)
                              return;

                          context.Placeholder.Cancel(context);
                          context.StateMachine.SetState(Default, context);
                      },
              Exit: _ => { }
             );

    internal static State HoldItem
        = new(
              Enter: context =>
                     {
                         var item = context.ActiveItem;
                         Debug.Assert(item != null);

                         var selector = context.Selector;

                         var isPartOfSelection = selector.IsSelected(item);
                         if (isPartOfSelection)
                         {
                             context.ItemMovement.SetDraggedItems(selector.Selection);
                         }
                         else
                         {
                             context.ItemMovement.SetDraggedItemIdsToSnappedForItem(item);
                         }

                         //context.ItemMovement.StartDragOperation(composition);
                     },
              Update: context =>
                      {
                          Debug.Assert(context.ActiveItem != null);

                          if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                          {
                              MagItemMovement.SelectActiveItem(context);
                              context.StateMachine.SetState(Default, context);
                              return;
                          }

                          if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                          {
                              context.StateMachine.SetState(DragItems, context);
                              return;
                          }

                          const float longTapDuration = 0.3f;
                          var longTapProgress = context.StateMachine.StateTime / longTapDuration;
                          MagItemMovement.UpdateLongPressIndicator(longTapProgress);

                          if (!(longTapProgress > 1))
                              return;

                          MagItemMovement.SelectActiveItem(context);
                          context.ItemMovement.SetDraggedItemIds([context.ActiveItem.Id]);
                          context.StateMachine.SetState(HoldItemAfterLongTap, context);
                      },
              Exit: _ => { }
             );

    internal static State HoldItemAfterLongTap
        = new(
              Enter: _ => { },
              Update: context =>
                      {
                          Debug.Assert(context.ActiveItem != null);

                          if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                          {
                              MagItemMovement.SelectActiveItem(context);
                              context.StateMachine.SetState(Default, context);
                              return;
                          }

                          if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                          {
                              context.StateMachine.SetState(DragItems, context);
                          }
                      },
              Exit: _ => { }
             );

    internal static State DragItems
        = new(
              Enter: context =>
                     {
                         context.ItemMovement.PrepareDragInteraction();
                         context.ItemMovement.StartDragOperation(context);
                     },
              Update: context =>
                      {
                          if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                          {
                              context.ItemMovement.CompleteDragOperation(context);

                              context.StateMachine.SetState(Default, context);
                              return;
                          }

                          context.ItemMovement.UpdateDragging(context);
                      },
              Exit: context => { context.ItemMovement.StopDragOperation(); }
             );

    /// <summary>
    /// Active while long tapping on background for insertion
    /// </summary>
    internal static State HoldOutput
        = new(
              Enter: _ => { },
              Update: context =>
                      {
                          Debug.Assert(context.ActiveItem != null);
                          Debug.Assert(context.ActiveItem.OutputLines.Length > 0);
                          Debug.Assert(context.ActiveOutputId != Guid.Empty);

                          // Click
                          if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                          {
                              context.Placeholder.OpenForItem(context, context.ActiveItem, context.ActiveOutputDirection);
                              context.StateMachine.SetState(Placeholder, context);
                              return;
                          }

                          if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                          {
                              var outputLine = context.ActiveItem.OutputLines[0];
                              var output = outputLine.Output;

                              var tempConnection = new MagGraphConnection
                                                       {
                                                           Style = MagGraphConnection.ConnectionStyles.Unknown,
                                                           SourcePos = context.ActiveItem.PosOnCanvas,
                                                           TargetPos = default,
                                                           SourceItem = context.ActiveItem,
                                                           TargetItem = null,
                                                           SourceOutput = output,
                                                           OutputLineIndex = 0,
                                                           VisibleOutputIndex = 0,
                                                           ConnectionHash = 0,
                                                           IsTemporary = true,
                                                       };
                              context.TempConnections.Add(tempConnection);
                              context.PrimaryOutputItem = context.ActiveItem;
                              context.DraggedPrimaryOutputType = output.ValueType;
                              context.StateMachine.SetState(DragOutput, context);
                          }
                      },
              Exit: _ => { }
             );

    internal static State DragOutput
        = new(
              Enter: _ =>
                     {
                         // TODO: Should be a reset method
                         //context.TempConnections.Clear();
                         //context.PrimaryOutputItem = null;
                         //context.DraggedPrimaryOutputType = null;
                     },
              Update: context =>
                      {
                          //Debug.Assert(context.ActiveItem != null);

                          if (ImGui.IsKeyDown(ImGuiKey.Escape))
                          {
                              context.StateMachine.SetState(Default, context);
                              return;
                          }

                          var mouseReleased = !ImGui.IsMouseDown(ImGuiMouseButton.Left);
                          if (!mouseReleased)
                              return;

                          var hasDisconnections = context.TempConnections.Any(c => c.WasDisconnected);
                          
                          // Was dropped on operator or background...
                          var posOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
                          if (InputPicking.TryInitializeAtPosition(context, posOnCanvas))
                          {
                              context.StateMachine.SetState(PickInput, context);
                          }
                          else if (hasDisconnections)
                          {
                              UndoRedoStack.Add(context.MacroCommand);
                              context.StateMachine.SetState(Default, context);
                              return;
                          }
                          else
                          {
                              context.Placeholder.OpenOnCanvas(context, posOnCanvas, context.DraggedPrimaryOutputType);
                              context.StateMachine.SetState(Placeholder, context);
                          }
                      },
              Exit: _ => { }
             );

    internal static State PickInput
        = new(
              Enter: _ => { },
              Update: context =>
                      {
                          InputPicking.DrawHiddenInputSelector(context);
                      },
              Exit: _ => { }
             );
    
    #region connection end dragging
    internal static State HoldingConnectionEnd
        = new(
              Enter: _ => { },
              Update: context =>
                      {
                          
                          
                          if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                          {
                              context.StateMachine.SetState(Default, context);
                              return;
                          }

                          // Start dragging...
                          // TODO: Clarify how to deal with undo/redo for disrupted connections... 
                          if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                          {
                              var connection = context.ConnectionHovering.HoveredInputConnection;
                              //Debug.Assert(connection != null);
                              if (connection == null)
                                  return;
                              
                              context.MacroCommand = new MacroCommand("Reconnect from input");
                              
                              
                              // Remove existing connection
                              context.MacroCommand.AddAndExecCommand(new DeleteConnectionCommand(context.CompositionOp.Symbol,
                                                                                                 connection.AsSymbolConnection(), 0));
                              
                              var tempConnection = new MagGraphConnection
                                                       {
                                                           Style = MagGraphConnection.ConnectionStyles.Unknown,
                                                           SourcePos = connection.SourcePos,
                                                           TargetPos = default,
                                                           SourceItem = connection.SourceItem,
                                                           TargetItem = null,
                                                           SourceOutput = connection.SourceOutput,
                                                           OutputLineIndex = 0,
                                                           VisibleOutputIndex = 0,
                                                           ConnectionHash = 0,
                                                           IsTemporary = true,
                                                           WasDisconnected = true,
                                                       };
                              context.TempConnections.Add(tempConnection);
                              context.PrimaryOutputItem = connection.SourceItem;
                              context.DraggedPrimaryOutputType = connection.Type;
                              context.ActiveItem = connection.SourceItem;
                              context.StateMachine.SetState(DragOutput, context);
                              return;
                          }
                      },
              Exit: _ => { }
             );

    internal static State DraggingConnectionEnd
        = new(
              Enter: _ => { },
              Update: context =>
                      {
                          if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                          {
                              context.StateMachine.SetState(Default, context);
                              return;
                          }
                          
                      },
              Exit: _ => { }
             );

    
    #endregion
    
}